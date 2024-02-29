using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using PLUME.Sample.Common;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace PLUME
{
    public class TrajectorySegmentPoint
    {
        public ulong Time;
        public Vector3 Position;
        [CanBeNull] public Quaternion? Rotation;
        [CanBeNull] public Marker Marker;

        public TrajectorySegmentPoint(ulong time, Vector3 position, Quaternion? rotation, [CanBeNull] Marker marker)
        {
            Time = time;
            Position = position;
            Rotation = rotation;
            Marker = marker;
        }

        public class TimestampComparer : IComparer<TrajectorySegmentPoint>
        {
            public static readonly TimestampComparer Instance = new();

            public int Compare(TrajectorySegmentPoint x, TrajectorySegmentPoint y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                return x.Time.CompareTo(y.Time);
            }
        }
    }

    public class TrajectoryAnalysisModuleResult : AnalysisModuleResult
    {
        public TrajectoryAnalysisModuleParameters GenerationParameters { get; }

        public List<TrajectorySegmentPoint>[] Segments { get; }

        public TrajectoryAnalysisModuleResult()
        {
        }

        public TrajectoryAnalysisModuleResult(TrajectoryAnalysisModuleParameters generationParameters,
            List<TrajectorySegmentPoint>[] segments)
        {
            GenerationParameters = generationParameters;
            Segments = segments;
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