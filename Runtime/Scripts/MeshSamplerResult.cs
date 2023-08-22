using System;
using UnityEngine;

namespace PLUME
{
    public class MeshSamplerResult : IDisposable
    {
        public readonly uint NTriangles;
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

        public MeshSamplerResult(uint nTriangles, uint nSamples, uint nSamplesMaxPerTriangle,
            GraphicsBuffer indexBuffer, int indexBufferStride, GraphicsBuffer vertexBuffer, int vertexBufferStride,
            int vertexBufferPositionOffset, ComputeBuffer sampleValuesBuffer, ComputeBuffer trianglesResolutionBuffer,
            ComputeBuffer trianglesSamplesIndexOffsetBuffer)
        {
            NTriangles = nTriangles;
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