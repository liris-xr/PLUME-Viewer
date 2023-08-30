namespace PLUME
{
    public struct TrajectoryAnalysisModuleParameters
    {
        public string ObjectIdentifier;
        public ulong StartTime;
        public ulong EndTime;
        public bool IncludeRotations;
        public float TeleportationTolerance;
        public float DecimationTolerance;
        public string[] VisibleMarkers;
    }
}