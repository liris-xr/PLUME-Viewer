using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME.Viewer.Analysis.EyeGaze
{
    // cf. https://developer.tobii.com/xr/learn/technical-information/coordinate-systems/#eye-tracking-coordinate-system
    public enum EyeGazeCoordinateSystem
    {
        TrackingSpace,
        World,
        Camera
    }

    public struct EyeGazeAnalysisModuleParameters
    {
        public Guid XrCameraIdentifier;
        public Guid[] ReceiversIdentifiers;
        public bool IncludeReceiversChildren;
        public ulong StartTime;
        public ulong EndTime;
        public EyeGazeCoordinateSystem CoordinateSystem;
        public float FovealVisionOpticalAxisAngle;
        public float NSigmas;
        public float SamplesPerSquareMeter;

        /// <summary>
        /// Input action binding path of the gaze position sample (e.g. "&lt;EyeGaze&gt;/pose/position" or
        /// "&lt;VarjoHMD&gt;/eyeGaze/centerEyePosition").
        /// </summary>
        public string GazePositionBindingPath;

        /// <summary>
        /// Input action binding path of the gaze rotation sample (e.g. "&lt;EyeGaze&gt;/pose/rotation").
        /// </summary>
        public string GazeRotationBindingPath;
    }

    public static class EyeGazeDiagnostics
    {
        /// <summary>
        /// Explains why a gaze binding path yielded no usable sample: either no recorded action carries that path, or
        /// the actions that do recorded a value of another type than the one the projection needs.
        /// </summary>
        public static string DescribeBindingMismatch(
            IEnumerable<RawSample<InputAction>> inputActionSamples,
            string bindingPath,
            InputAction.ValueOneofCase expectedValueCase)
        {
            var samples = inputActionSamples.ToList();

            if (string.IsNullOrWhiteSpace(bindingPath))
                return "The binding path is empty; set it to the recorded gaze binding path.";

            var matchingPath = samples
                .Where(s => s.Payload.BindingPaths.Contains(bindingPath))
                .ToList();

            if (matchingPath.Count == 0)
            {
                var recordedPaths = samples
                    .SelectMany(s => s.Payload.BindingPaths)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                if (recordedPaths.Count == 0)
                    return $"No input action was recorded in this time range, so '{bindingPath}' cannot be found.";

                return $"No recorded input action uses the binding path '{bindingPath}'. " +
                       $"Recorded binding paths: {string.Join(", ", recordedPaths)}";
            }

            var recordedValueCases = matchingPath
                .Select(s => s.Payload.ValueCase)
                .Distinct()
                .OrderBy(c => c.ToString())
                .ToList();

            return $"{matchingPath.Count} recorded action(s) use the binding path '{bindingPath}', but their value is " +
                   $"{string.Join("/", recordedValueCases)} where {expectedValueCase} is required. " +
                   "The position and rotation binding paths are likely swapped.";
        }
    }

    public class EyeGazeAnalysisResult : AnalysisModuleResult
    {
        public EyeGazeAnalysisModuleParameters Parameters;

        public ComputeBuffer MinValueBuffer { get; }
        public ComputeBuffer MaxValueBuffer { get; }

        /// <summary>
        /// List of generated samples for the projection receivers. The key is the hash between the GameObject
        /// identifier and mesh identifier in the record.
        /// </summary>
        public readonly Dictionary<int, MeshSamplerResult> SamplerResults = new();

        public EyeGazeAnalysisResult()
        {
        }

        public EyeGazeAnalysisResult(EyeGazeAnalysisModuleParameters parameters,
            ComputeBuffer minValueBuffer,
            ComputeBuffer maxValueBuffer,
            Dictionary<int, MeshSamplerResult> samplerResults)
        {
            Parameters = parameters;
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