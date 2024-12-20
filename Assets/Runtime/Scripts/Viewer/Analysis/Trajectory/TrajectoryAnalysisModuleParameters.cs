using System;

namespace PLUME.Viewer.Analysis.Trajectory
{
    public struct TrajectoryAnalysisModuleParameters
    {
        public Guid ObjectIdentifier;
        public ulong StartTime;
        public ulong EndTime;
        public bool IncludeRotations;
        public float TeleportationTolerance;
        public bool TeleportationSegments;
        public float DecimationTolerance;
        public string[] VisibleMarkers;
    }
}