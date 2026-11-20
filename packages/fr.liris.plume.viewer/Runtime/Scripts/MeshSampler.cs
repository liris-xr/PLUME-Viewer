﻿using System;
using System.Collections.Generic;
using System.Linq;
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
            return MakeReadableMeshCopy(nonReadableMesh, out _);
        }

        /// <summary>
        /// Copies <paramref name="nonReadableMesh"/> through its GPU buffers, so that meshes loaded from asset bundles
        /// (not readable) can be sampled.
        /// <para>
        /// The copy has a single submesh holding the triangles of every triangle submesh of the source, in order, with
        /// each submesh's baseVertex folded into its indices: the sampler and the projection read the index buffer as
        /// one triangle list. Submeshes of another topology (lines, points, quads) are dropped.
        /// </para>
        /// </summary>
        /// <param name="subMeshFirstTriangle">For each submesh of the source, the index of its first triangle in the
        /// copy, or -1 when the submesh was dropped.</param>
        public static Mesh MakeReadableMeshCopy(Mesh nonReadableMesh, out int[] subMeshFirstTriangle)
        {
            // We were forced to do this because the Stanford Bunny model would disappear when changing the graphics buffer targets.
            // This is probably due to the fact that they are not readable when loading from the asset bundle.
            // TODO: find a way to make all mesh readable in the asset bundle
            var meshCopy = new Mesh();

            // Handle vertices
            var verticesBuffer = nonReadableMesh.GetVertexBuffer(0);
            var totalSize = verticesBuffer.stride * verticesBuffer.count;
            var data = new byte[totalSize];
            verticesBuffer.GetData(data);
            meshCopy.SetVertexBufferParams(nonReadableMesh.vertexCount, nonReadableMesh.GetVertexAttributes());
            meshCopy.SetVertexBufferData(data, 0, 0, totalSize);
            verticesBuffer.Release();

            // Handle triangles
            var sourceIndices = ReadIndexBuffer(nonReadableMesh);
            var subMeshCount = nonReadableMesh.subMeshCount;
            var indices = new List<uint>(sourceIndices.Length);
            subMeshFirstTriangle = new int[subMeshCount];

            for (var i = 0; i < subMeshCount; i++)
            {
                var subMesh = nonReadableMesh.GetSubMesh(i);

                if (subMesh.topology != MeshTopology.Triangles)
                {
                    subMeshFirstTriangle[i] = -1;
                    continue;
                }

                subMeshFirstTriangle[i] = indices.Count / 3;
                var indexCount = subMesh.indexCount - subMesh.indexCount % 3;

                for (var j = 0; j < indexCount; j++)
                    indices.Add((uint)(sourceIndices[subMesh.indexStart + j] + subMesh.baseVertex));
            }

            if (nonReadableMesh.vertexCount > ushort.MaxValue)
            {
                meshCopy.SetIndexBufferParams(indices.Count, IndexFormat.UInt32);
                meshCopy.SetIndexBufferData(indices, 0, 0, indices.Count);
            }
            else
            {
                meshCopy.SetIndexBufferParams(indices.Count, IndexFormat.UInt16);
                meshCopy.SetIndexBufferData(indices.Select(index => (ushort)index).ToArray(), 0, 0, indices.Count);
            }

            meshCopy.subMeshCount = 1;
            meshCopy.SetSubMesh(0, new SubMeshDescriptor(0, indices.Count), MeshUpdateFlags.DontRecalculateBounds);

            // Recalculate normals and bounds
            meshCopy.RecalculateNormals();
            meshCopy.RecalculateBounds();

            return meshCopy;
        }

        private static uint[] ReadIndexBuffer(Mesh mesh)
        {
            var buffer = mesh.GetIndexBuffer();

            if (buffer == null)
                return Array.Empty<uint>();

            try
            {
                var indices = new uint[buffer.count];

                if (buffer.stride == 2)
                {
                    var shortIndices = new ushort[buffer.count];
                    buffer.GetData(shortIndices);
                    for (var i = 0; i < shortIndices.Length; ++i)
                        indices[i] = shortIndices[i];
                }
                else
                {
                    buffer.GetData(indices);
                }

                return indices;
            }
            finally
            {
                buffer.Release();
            }
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
        ///
        /// <exception cref="MeshSamplingException">The mesh cannot be sampled. Nothing is left allocated.</exception>
        public MeshSamplerResult Sample(Mesh mesh, float samplesPerSquareMeter, Vector3 worldScale)
        {
            Mesh readableMeshCopy = null;
            GraphicsBuffer vertexBuffer = null;
            GraphicsBuffer indexBuffer = null;
            ComputeBuffer trianglesResolutionBuffer = null;
            ComputeBuffer trianglesSamplesIndexOffsetBuffer = null;
            ComputeBuffer sampleValuesBuffer = null;

            try
            {
                using var trianglesMaxResolutionBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)));

                readableMeshCopy = MakeReadableMeshCopy(mesh, out var subMeshFirstTriangle);
                readableMeshCopy.indexBufferTarget |= GraphicsBuffer.Target.Raw;
                readableMeshCopy.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

                vertexBuffer = readableMeshCopy.GetVertexBuffer(0);
                var vertexBufferStride = vertexBuffer.stride;
                var vertexBufferPositionOffset = readableMeshCopy.GetVertexAttributeOffset(VertexAttribute.Position);
                indexBuffer = readableMeshCopy.GetIndexBuffer();
                var indexBufferStride = indexBuffer.stride;

                var nTriangles = (uint)(indexBuffer.count / 3);
                var nVertices = (uint)vertexBuffer.count;

                if (nTriangles == 0)
                    throw new MeshSamplingException($"Mesh '{mesh.name}' has no triangles to sample.");

                // Specifically set the initial value to 0 in the buffer to prevent undefined behaviour of InterlockedMax
                trianglesMaxResolutionBuffer.SetData(new uint[] { 0 });

                trianglesResolutionBuffer = new ComputeBuffer((int)nTriangles, Marshal.SizeOf(typeof(uint)));
                trianglesSamplesIndexOffsetBuffer = new ComputeBuffer((int)nTriangles, Marshal.SizeOf(typeof(uint)));

                ComputeTrianglesResolution(meshSamplingShader,
                    worldScale,
                    samplesPerSquareMeter,
                    nTriangles,
                    trianglesResolutionBuffer,
                    trianglesMaxResolutionBuffer,
                    indexBuffer,
                    indexBufferStride,
                    vertexBuffer,
                    vertexBufferStride,
                    vertexBufferPositionOffset);

                // Counted on the CPU in 64 bits: a 32-bit total silently wraps on large meshes.
                var totalSamples = ComputeTrianglesSamplesIndexOffsets(
                    trianglesResolutionBuffer,
                    trianglesSamplesIndexOffsetBuffer);

                // Sample indices are 32-bit and the values buffer has a maximum size in bytes.
                var maxSamples = Math.Min(SystemInfo.maxGraphicsBufferSize / Marshal.SizeOf(typeof(float)), int.MaxValue);

                if (totalSamples > (ulong)maxSamples)
                {
                    throw new MeshSamplingException(
                        $"Mesh '{mesh.name}' covers about {totalSamples / samplesPerSquareMeter:N0} m² in the world " +
                        $"(scale {worldScale}), which needs {totalSamples:N0} samples at {samplesPerSquareMeter} " +
                        $"samples/m². A GPU buffer holds at most {maxSamples:N0} samples here, about " +
                        $"{maxSamples / samplesPerSquareMeter:N0} m² at this density.");
                }

                var nSamples = (uint)totalSamples;
                sampleValuesBuffer = new ComputeBuffer((int)nSamples, Marshal.SizeOf(typeof(float)));

                var triangleResolutionMaxArr = new uint[1];
                trianglesMaxResolutionBuffer.GetData(triangleResolutionMaxArr);
                var nSamplesMaxPerTriangle = (uint)NthTriangleFormula(triangleResolutionMaxArr[0]);

                var sampledMesh = new MeshSamplerResult(nTriangles, nVertices, nSamples, nSamplesMaxPerTriangle,
                    indexBuffer,
                    indexBufferStride, vertexBuffer, vertexBufferStride,
                    vertexBufferPositionOffset, sampleValuesBuffer,
                    trianglesResolutionBuffer,
                    trianglesSamplesIndexOffsetBuffer,
                    subMeshFirstTriangle,
                    readableMeshCopy);

                ClearMeshSamplerResult(sampledMesh, meshSamplingShader);
                return sampledMesh;
            }
            catch (Exception e)
            {
                sampleValuesBuffer?.Release();
                trianglesSamplesIndexOffsetBuffer?.Release();
                trianglesResolutionBuffer?.Release();
                indexBuffer?.Release();
                vertexBuffer?.Release();
                if (readableMeshCopy != null)
                    Destroy(readableMeshCopy);

                if (e is MeshSamplingException)
                    throw;

                // Any other failure (unexpected mesh layout, GPU allocation) only concerns this mesh.
                throw new MeshSamplingException($"Mesh '{mesh.name}' could not be sampled: {e.Message}", e);
            }
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

    /// <summary>A mesh that cannot be sampled (no triangles, more samples than one buffer holds, or another failure).</summary>
    public class MeshSamplingException : Exception
    {
        public MeshSamplingException(string message) : base(message)
        {
        }

        public MeshSamplingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
