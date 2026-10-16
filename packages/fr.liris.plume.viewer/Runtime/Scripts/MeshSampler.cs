using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace PLUME
{
    public class MeshSampler : MonoBehaviour
    {
        public ComputeShader meshSamplingShader;

        public static Mesh MakeReadableMeshCopy(Mesh nonReadableMesh)
        {
            // We were forced to do this because the Stanford Bunny model would disappear when changing the graphics buffer targets.
            // This is probably due to the fact that they are not readable when loading from the asset bundle.
            // TODO: find a way to make all mesh readable in the asset bundle
            var meshCopy = new Mesh();
            meshCopy.indexFormat = nonReadableMesh.indexFormat;

            // Handle vertices
            var verticesBuffer = nonReadableMesh.GetVertexBuffer(0);
            var totalSize = verticesBuffer.stride * verticesBuffer.count;
            var data = new byte[totalSize];
            verticesBuffer.GetData(data);
            meshCopy.SetVertexBufferParams(nonReadableMesh.vertexCount, nonReadableMesh.GetVertexAttributes());
            meshCopy.SetVertexBufferData(data, 0, 0, totalSize);
            verticesBuffer.Release();

            // Handle triangles
            meshCopy.subMeshCount = nonReadableMesh.subMeshCount;
            var indexesBuffer = nonReadableMesh.GetIndexBuffer();
            var tot = indexesBuffer.stride * indexesBuffer.count;
            var indexesData = new byte[tot];
            indexesBuffer.GetData(indexesData);
            meshCopy.SetIndexBufferParams(indexesBuffer.count, nonReadableMesh.indexFormat);
            meshCopy.SetIndexBufferData(indexesData, 0, 0, tot);
            indexesBuffer.Release();

            // Restore submesh structure
            uint currentIndexOffset = 0;
            for (var i = 0; i < meshCopy.subMeshCount; i++)
            {
                var subMeshIndexCount = nonReadableMesh.GetIndexCount(i);
                meshCopy.SetSubMesh(i, new SubMeshDescriptor((int)currentIndexOffset, (int)subMeshIndexCount));
                currentIndexOffset += subMeshIndexCount;
            }

            // Recalculate normals and bounds
            meshCopy.RecalculateNormals();
            meshCopy.RecalculateBounds();

            return meshCopy;
        }

        /// <summary>
        /// Generate quasi-uniform sampling for the given mesh <paramref name="mesh"/>.
        /// </summary>
        /// 
        /// <param name="mesh">The mesh to sample</param>
        /// 
        /// <param name="samplesPerSquareMeter">
        /// Number of samples per square meter the mesh sampler will try to generate. The reference is defined in object
        /// local space (based on vertices positions and calculated triangles' area).
        /// </param>
        /// 
        /// <param name="worldScale"> Lossy world scale of the object. This is used to take into account any stretching
        /// of meshes that would result in more samples to be generated.
        /// </param>
        /// 
        /// <returns>
        /// Return the generated <see cref="MeshSamplerResult"/>. The responsability for releasing the generating buffer
        /// is left to the calling method.
        /// </returns>
        public MeshSamplerResult Sample(Mesh mesh, float samplesPerSquareMeter, Vector3 worldScale)
        {
            using var totalSamplesCountBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)));
            using var trianglesMaxResolutionBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)));

            var readableMeshCopy = MakeReadableMeshCopy(mesh);
            readableMeshCopy.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            readableMeshCopy.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

            var vertexBuffer = readableMeshCopy.GetVertexBuffer(0);
            var vertexBufferStride = vertexBuffer.stride;
            var vertexBufferPositionOffset = readableMeshCopy.GetVertexAttributeOffset(VertexAttribute.Position);
            var indexBuffer = readableMeshCopy.GetIndexBuffer();
            var indexBufferStride = indexBuffer.stride;

            var nTriangles = (uint)(indexBuffer.count / 3);
            var nVertices = (uint)vertexBuffer.count;

            if (nTriangles == 0)
            {
                indexBuffer.Release();
                vertexBuffer.Release();
                Destroy(readableMeshCopy);
                throw new MeshSamplingException($"Mesh '{mesh.name}' has no triangles to sample.");
            }

            // Specifically set the initial values to 0 in the buffer to prevent undefined behaviour of InterlockedMax and InterlockedAdd
            var totalSamplesCountArr = new uint[] { 0 };
            var trianglesMaxResolutionArr = new uint[] { 0 };
            totalSamplesCountBuffer.SetData(totalSamplesCountArr);
            trianglesMaxResolutionBuffer.SetData(trianglesMaxResolutionArr);

            var trianglesResolutionBuffer = new ComputeBuffer((int)nTriangles, Marshal.SizeOf(typeof(uint)));
            var trianglesSamplesIndexOffsetBuffer =
                new ComputeBuffer((int)nTriangles, Marshal.SizeOf(typeof(uint)));

            ComputeTrianglesResolution(meshSamplingShader,
                worldScale,
                samplesPerSquareMeter,
                nTriangles,
                totalSamplesCountBuffer,
                trianglesResolutionBuffer,
                trianglesMaxResolutionBuffer,
                indexBuffer,
                indexBufferStride,
                vertexBuffer,
                vertexBufferStride,
                vertexBufferPositionOffset);

            // Counted on the CPU in 64 bits: the GPU total is a 32-bit counter, which silently wraps on large meshes.
            var totalSamples = ComputeTrianglesSamplesIndexOffsets(
                trianglesResolutionBuffer,
                trianglesSamplesIndexOffsetBuffer);

            // Sample indices are 32-bit and the values buffer has a maximum size in bytes.
            var maxSamples = Math.Min(SystemInfo.maxGraphicsBufferSize / Marshal.SizeOf(typeof(float)), int.MaxValue);

            if (totalSamples > (ulong)maxSamples)
            {
                trianglesResolutionBuffer.Release();
                trianglesSamplesIndexOffsetBuffer.Release();
                indexBuffer.Release();
                vertexBuffer.Release();
                Destroy(readableMeshCopy);
                throw new MeshSamplingException(
                    $"Mesh '{mesh.name}' covers about {totalSamples / samplesPerSquareMeter:N0} m² in the world " +
                    $"(scale {worldScale}), which needs {totalSamples:N0} samples at {samplesPerSquareMeter} " +
                    $"samples/m². A GPU buffer holds at most {maxSamples:N0} samples here, about " +
                    $"{maxSamples / samplesPerSquareMeter:N0} m² at this density.");
            }

            var nSamples = (uint)totalSamples;
            var sampleValuesBuffer = new ComputeBuffer((int)nSamples, Marshal.SizeOf(typeof(float)));

            var triangleResolutionMaxArr = new uint[1];
            trianglesMaxResolutionBuffer.GetData(triangleResolutionMaxArr);
            var nSamplesMaxPerTriangle = (uint)NthTriangleFormula(triangleResolutionMaxArr[0]);

            var sampledMesh = new MeshSamplerResult(nTriangles, nVertices, nSamples, nSamplesMaxPerTriangle,
                indexBuffer,
                indexBufferStride, vertexBuffer, vertexBufferStride,
                vertexBufferPositionOffset, sampleValuesBuffer,
                trianglesResolutionBuffer,
                trianglesSamplesIndexOffsetBuffer);

            ClearMeshSamplerResult(sampledMesh, meshSamplingShader);
            return sampledMesh;
        }

        private static ulong NthTriangleFormula(uint r)
        {
            return ((ulong)r + 1) * ((ulong)r + 2) / 2u;
        }

        private static void ClearMeshSamplerResult(MeshSamplerResult result, ComputeShader shader)
        {
            var kernel = shader.FindKernel("clear_samples_value");
            shader.SetInt("n_samples", (int)result.NSamples);
            shader.SetBuffer(kernel, "samples_value_buffer", result.SampleValuesBuffer);
            shader.GetKernelThreadGroupSizes(kernel, out var threadGroupSize, out _, out _);
            var totalNumberOfGroupsX = Mathf.CeilToInt(result.NSamples / (float)threadGroupSize);
            shader.SplitDispatch(kernel, totalNumberOfGroupsX, 1);
        }

        private static void ComputeTrianglesResolution(
            ComputeShader shader,
            Vector3 worldScale,
            float samplesPerSqMeter,
            uint nTriangles,
            ComputeBuffer totalSamplesCountBuffer,
            ComputeBuffer trianglesResolutionBuffer,
            ComputeBuffer trianglesMaxResolutionBuffer,
            GraphicsBuffer indexBuffer,
            int indexBufferStride,
            GraphicsBuffer vertexBuffer,
            int vertexBufferStride,
            int vertexBufferPositionOffset)
        {
            var computeTrianglesResolutionsKernel = shader.FindKernel("compute_triangles_resolution");
            shader.SetVector("scale", worldScale);
            shader.SetInt("n_triangles", (int)nTriangles);
            shader.SetFloat("samples_density", samplesPerSqMeter);
            shader.SetBuffer(computeTrianglesResolutionsKernel, "index_buffer", indexBuffer);
            shader.SetInt("index_buffer_stride", indexBufferStride);
            shader.SetBuffer(computeTrianglesResolutionsKernel, "vertex_buffer", vertexBuffer);
            shader.SetInt("vertex_buffer_stride", vertexBufferStride);
            shader.SetInt("vertex_buffer_position_offset", vertexBufferPositionOffset);
            shader.SetBuffer(computeTrianglesResolutionsKernel, "total_samples_count_buffer", totalSamplesCountBuffer);
            shader.SetBuffer(computeTrianglesResolutionsKernel, "triangles_resolution_buffer",
                trianglesResolutionBuffer);
            shader.SetBuffer(computeTrianglesResolutionsKernel, "triangles_max_resolution_buffer",
                trianglesMaxResolutionBuffer);
            shader.GetKernelThreadGroupSizes(computeTrianglesResolutionsKernel, out var threadGroupSize, out _, out _);
            var totalNumberOfGroupsX = Mathf.CeilToInt(nTriangles / (float)threadGroupSize);
            shader.SplitDispatch(computeTrianglesResolutionsKernel, totalNumberOfGroupsX, 1);
        }

        /// <returns>The total number of samples. The offsets are only valid when it fits in 32 bits.</returns>
        private static ulong ComputeTrianglesSamplesIndexOffsets(
            ComputeBuffer trianglesResolutionBuffer,
            ComputeBuffer trianglesSamplesIndexOffsetBuffer)
        {
            // CPU implementation of prefix sum
            var nTriangles = trianglesResolutionBuffer.count;

            var trianglesResolutionArr = new uint[nTriangles];
            trianglesResolutionBuffer.GetData(trianglesResolutionArr);

            var exclusivePrefixSum = new uint[nTriangles];
            var total = 0ul;

            for (var i = 0; i < nTriangles; ++i)
            {
                exclusivePrefixSum[i] = (uint)total;
                total += NthTriangleFormula(trianglesResolutionArr[i]);
            }

            trianglesSamplesIndexOffsetBuffer.SetData(exclusivePrefixSum);

            // GPU Implementation
            // TODO: implement parallel prefix sum
            // var kernel = shader.FindKernel("compute_triangles_samples_index_offset");
            //
            // shader.SetBuffer(kernel, "triangles_resolution_buffer", trianglesResolutionBuffer);
            // shader.SetBuffer(kernel, "triangles_samples_index_offset_buffer", trianglesSamplesIndexOffsetBuffer);
            // shader.Dispatch(kernel, 1, 1, 1);

            return total;
        }
    }

    /// <summary>A mesh that cannot be sampled (no triangles, or more samples than one buffer holds).</summary>
    public class MeshSamplingException : Exception
    {
        public MeshSamplingException(string message) : base(message)
        {
        }
    }
}