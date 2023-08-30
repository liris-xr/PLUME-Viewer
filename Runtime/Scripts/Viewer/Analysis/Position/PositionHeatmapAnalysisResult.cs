using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PLUME
{
    public class PositionHeatmapAnalysisResult : AnalysisModuleResult, IDisposable
    {
        public string ProjectionCasterIdentifier { get; }
        public string[] ProjectionReceiversIdentifiers  { get; }
        public ulong ProjectionStartTime { get; }
        public ulong ProjectionEndTime { get; }
        public ComputeBuffer MinValueBuffer { get; }
        public ComputeBuffer MaxValueBuffer { get; }
        
        /// <summary>
        /// List of generated samples for the projection receivers. The key is the mesh ID in the record.
        /// </summary>
        public readonly Dictionary<int, MeshSamplerResult> SamplerResults = new();

        public PositionHeatmapAnalysisResult() {}
        
        public PositionHeatmapAnalysisResult(string projectionCasterIdentifier, string[] projectionReceiversIdentifiers,
            ulong projectionStartTime, ulong projectionEndTime,
            ComputeBuffer minValueBuffer,
            ComputeBuffer maxValueBuffer,
            Dictionary<int, MeshSamplerResult> samplerResults)
        {
            ProjectionCasterIdentifier = projectionCasterIdentifier;
            ProjectionReceiversIdentifiers = projectionReceiversIdentifiers;
            ProjectionStartTime = projectionStartTime;
            ProjectionEndTime = projectionEndTime;
            MinValueBuffer = minValueBuffer;
            MaxValueBuffer = maxValueBuffer;
            SamplerResults = samplerResults;
        }

        public float MinValue
        {
            get
            {
                var samplesMinValueArr = new uint[1];
                MinValueBuffer.GetData(samplesMinValueArr);
                return BitConverter.ToSingle(BitConverter.GetBytes(samplesMinValueArr[0]));
            }
        }
        
        public float MaxValue
        {
            get
            {
                var samplesMaxValueArr = new uint[1];
                MaxValueBuffer.GetData(samplesMaxValueArr);
                return BitConverter.ToSingle(BitConverter.GetBytes(samplesMaxValueArr[0]));
            }
        }

        public void Dispose()
        {
            foreach (var samplerResult in SamplerResults.Values)
            {
                samplerResult.Dispose();
            }
            
            MinValueBuffer.Release();
            MaxValueBuffer.Release();
        }

        public override void Save(Stream outputStream)
        {
            throw new NotImplementedException();
        }

        public override void Load(Stream inputStream)
        {
            throw new NotImplementedException();
        }
    }
}