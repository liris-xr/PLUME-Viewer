using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace PLUME.Viewer.Analysis.EyeGaze
{
    public class EyeGazeVertexHeatmapMeshResult : IDisposable
    {
        public string Name;
        public readonly uint NVertices;
        public readonly uint NTriangles;

        /// <summary>
        /// Per-vertex accumulated weights, stored as IEEE 754 float bits inside a uint buffer
        /// so that atomic CAS operations can be used by the projection compute shader.
        /// </summary>
        public readonly ComputeBuffer VertexValueBuffer;

        public readonly GraphicsBuffer IndexBuffer;
        public readonly int IndexBufferStride;
        public readonly GraphicsBuffer VertexBuffer;
        public readonly int VertexBufferStride;
        public readonly int VertexBufferPositionOffset;

        public EyeGazeVertexHeatmapMeshResult(
            uint nVertices,
            uint nTriangles,
            ComputeBuffer vertexValueBuffer,
            GraphicsBuffer indexBuffer,
            int indexBufferStride,
            GraphicsBuffer vertexBuffer,
            int vertexBufferStride,
            int vertexBufferPositionOffset)
        {
            NVertices = nVertices;
            NTriangles = nTriangles;
            VertexValueBuffer = vertexValueBuffer;
            IndexBuffer = indexBuffer;
            IndexBufferStride = indexBufferStride;
            VertexBuffer = vertexBuffer;
            VertexBufferStride = vertexBufferStride;
            VertexBufferPositionOffset = vertexBufferPositionOffset;
        }

        public void Dispose()
        {
            VertexValueBuffer?.Dispose();
            IndexBuffer?.Dispose();
            VertexBuffer?.Dispose();
        }
    }

    public class EyeGazeVertexHeatmapResult : AnalysisModuleResult
    {
        public EyeGazeAnalysisModuleParameters Parameters;

        /// <summary>
        /// Per-mesh vertex projection results. Key is HashCode.Combine(gameObjectIdentifier, meshIdentifier).
        /// </summary>
        public readonly Dictionary<int, EyeGazeVertexHeatmapMeshResult> MeshResults = new();

        /// <summary>
        /// Global maximum accumulated vertex weight across all meshes (uint bits of a float).
        /// Initialized to 0 so the shader reads 0.0f until data arrives.
        /// </summary>
        public readonly ComputeBuffer MaxValueBuffer;

        public EyeGazeVertexHeatmapResult() { }

        public EyeGazeVertexHeatmapResult(
            EyeGazeAnalysisModuleParameters parameters,
            ComputeBuffer maxValueBuffer,
            Dictionary<int, EyeGazeVertexHeatmapMeshResult> meshResults)
        {
            Parameters = parameters;
            MaxValueBuffer = maxValueBuffer;
            MeshResults = meshResults;
        }

        public float MaxValue
        {
            get
            {
                var data = new uint[1];
                MaxValueBuffer.GetData(data);
                return BitConverter.ToSingle(BitConverter.GetBytes(data[0]));
            }
        }

        public void Dispose()
        {
            foreach (var meshResult in MeshResults.Values)
                meshResult.Dispose();

            MaxValueBuffer?.Release();
        }

        public override void Save(Stream outputStream) => throw new NotImplementedException();
        public override void Load(Stream inputStream) => throw new NotImplementedException();
    }
}
