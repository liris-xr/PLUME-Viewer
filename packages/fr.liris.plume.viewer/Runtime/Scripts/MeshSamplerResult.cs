using System;
using JetBrains.Annotations;
using UnityEngine;

namespace PLUME
{
    public class MeshSamplerResult : IDisposable
    {
        public readonly uint NTriangles;
        public readonly uint NVertices;
        public readonly uint NSamples;
        public readonly uint NSamplesMaxPerTriangle;

        public readonly ComputeBuffer TrianglesResolutionBuffer;
        public readonly ComputeBuffer TrianglesSamplesIndexOffsetBuffer;

        public readonly GraphicsBuffer IndexBuffer;
        public readonly int IndexBufferStride;

        public readonly GraphicsBuffer VertexBuffer;
        public readonly int VertexBufferStride;
        public readonly int VertexBufferPositionOffset;

        public readonly ComputeBuffer SampleValuesBuffer;

        [CanBeNull] public string Name;

        public MeshSamplerResult(uint nTriangles, uint nVertices, uint nSamples, uint nSamplesMaxPerTriangle,
            GraphicsBuffer indexBuffer, int indexBufferStride, GraphicsBuffer vertexBuffer, int vertexBufferStride,
            int vertexBufferPositionOffset, ComputeBuffer sampleValuesBuffer, ComputeBuffer trianglesResolutionBuffer,
            ComputeBuffer trianglesSamplesIndexOffsetBuffer)
        {
            NTriangles = nTriangles;
            NVertices = nVertices;
            NSamples = nSamples;
            NSamplesMaxPerTriangle = nSamplesMaxPerTriangle;
            IndexBuffer = indexBuffer;
            IndexBufferStride = indexBufferStride;
            VertexBuffer = vertexBuffer;
            VertexBufferStride = vertexBufferStride;
            VertexBufferPositionOffset = vertexBufferPositionOffset;
            SampleValuesBuffer = sampleValuesBuffer;
            TrianglesResolutionBuffer = trianglesResolutionBuffer;
            TrianglesSamplesIndexOffsetBuffer = trianglesSamplesIndexOffsetBuffer;
        }

        /// <summary>
        /// Reads the index buffer back to the CPU. Meshes with more than 65535 vertices use 32-bit indices, so the
        /// buffer must be read at its own index size.
        /// </summary>
        public uint[] ReadIndices()
        {
            var indices = new uint[IndexBuffer.count];

            if (IndexBufferStride == 2)
            {
                var shortIndices = new ushort[IndexBuffer.count];
                IndexBuffer.GetData(shortIndices);
                for (var i = 0; i < shortIndices.Length; ++i)
                    indices[i] = shortIndices[i];
            }
            else
            {
                IndexBuffer.GetData(indices);
            }

            return indices;
        }

        public void Dispose()
        {
            SampleValuesBuffer?.Dispose();
            TrianglesResolutionBuffer?.Dispose();
            TrianglesSamplesIndexOffsetBuffer?.Dispose();
            IndexBuffer?.Dispose();
            VertexBuffer?.Dispose();
        }
    }
}