using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PLUME.Sample.Common;
using PLUME.Viewer.Player;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace PLUME.Viewer.Analysis.Trajectory
{
    public class TrajectoryAnalysisModule : AnalysisModuleWithResults<TrajectoryAnalysisModuleResult>
    {
        public Player.Player player;

        public Material fullLineMaterial;
        public Material dottedLineMaterial;
        public GameObject rotationAxesPrefab;
        public GameObject markerLabelPrefab;

        private readonly List<TrajectoryAnalysisModuleResult> _visibleTrajectories = new();
        private readonly List<Trajectory> _trajectories = new();

        private Camera _lastPlayerCamera;

        public PlayerContext _generationContext;
        public bool IsGenerating { get; private set; }
        public float GenerationProgress { get; private set; }

        public IEnumerator GenerateTrajectory(Record record, RecordAssetBundle recordAssetBundle,
            TrajectoryAnalysisModuleParameters parameters, Action<TrajectoryAnalysisModuleResult> finishCallback)
        {
            if (parameters.EndTime < parameters.StartTime)
            {
                throw new Exception(
                    $"{nameof(parameters.StartTime)} should be less or equal to {nameof(parameters.EndTime)}.");
            }

            if (player.GetModuleGenerating() != null)
            {
                Debug.LogWarning("Another module is already generating");
                yield break;
            }

            GenerationProgress = 0;
            IsGenerating = true;
            player.SetModuleGenerating(this);

            _generationContext = PlayerContext.NewContext("GenerateTrajectoryContext_" + Guid.NewGuid(), recordAssetBundle);

            // Skip frames before the start time
            if (parameters.StartTime > 0)
            {
                var skippedFrames = record.Frames.GetInTimeRange(0, parameters.StartTime - 1u);
                _generationContext.PlayFrames(player.PlayerModules, skippedFrames);
            }

            // Frames in the time range
            var frames = record.Frames.GetInTimeRange(parameters.StartTime, parameters.EndTime);

            var teleportationIndices = new List<int>();
            var markersIndices = new List<int>();
            var points = new List<TrajectorySegmentPoint>();
            var teleportationToleranceSq = parameters.TeleportationTolerance * parameters.TeleportationTolerance;

            var lastYieldTime = Time.unscaledTimeAsDouble;
            
            foreach (var frame in frames)
            {
                _generationContext.PlayFrame(player.PlayerModules, frame);

                var replayId = _generationContext.GetReplayInstanceId(parameters.ObjectIdentifier);

                // Object to generate the trajectory for is not present in the frame
                if (!replayId.HasValue)
                    continue;

                var go = _generationContext.FindGameObjectByInstanceId(replayId.Value);

                // TODO: if frame contains a teleportation sample, add a new segment, instead of using a threshold

                var t = go.transform;
                Quaternion? rotation = parameters.IncludeRotations ? t.rotation : null;
                var point = new TrajectorySegmentPoint(frame.Timestamp, t.position, rotation, null);

                if (points.Count > 0 && (points[^1].Position - point.Position).sqrMagnitude >= teleportationToleranceSq)
                    teleportationIndices.Add(points.Count);

                points.Add(point);

                GenerationProgress = (frame.Timestamp - parameters.StartTime) /
                                     (float)(parameters.EndTime - parameters.StartTime);
                
                var time = Time.unscaledTimeAsDouble;
                if (time - lastYieldTime > 1.0f / Application.targetFrameRate)
                {
                    lastYieldTime = time;
                    // Only used to not freeze the game while generating
                    yield return new WaitForEndOfFrame();
                }
            }

            GenerationProgress = 1;

            if (parameters.VisibleMarkers != null)
            {
                var markers = record.Markers.GetInTimeRange(parameters.StartTime, parameters.EndTime);

                // Used to skip the points that we know are before the marker timestamp
                var startSearchIdx = 0;

                foreach (var sample in markers)
                {
                    var marker = sample.Payload;

                    if (!parameters.VisibleMarkers.Contains(marker.Label))
                        continue;

                    var lookupTrajectoryPoint =
                        new TrajectorySegmentPoint(sample.Timestamp, Vector3.zero, null, null);

                    var idx = points.BinarySearch(startSearchIdx, points.Count - startSearchIdx, lookupTrajectoryPoint,
                        TrajectorySegmentPoint.TimestampComparer.Instance);

                    if (idx >= 0)
                    {
                        points[idx].Marker = marker;
                        markersIndices.Add(idx);
                    }
                    else
                    {
                        idx = ~idx;

                        if (idx < points.Count)
                        {
                            points[idx].Marker = marker;
                            markersIndices.Add(idx);
                        }
                    }

                    startSearchIdx = idx;
                }
            }

            // Simplify trajectory but make sure that we keep the points with markers and teleportation intact
            var pointsToKeep = new List<int>();
            LineUtility.Simplify(points.Select(point => point.Position).ToList(), parameters.DecimationTolerance,
                pointsToKeep);

            foreach (var teleportationIndex in teleportationIndices)
            {
                pointsToKeep.Add(teleportationIndex);

                if (teleportationIndex > 0)
                {
                    pointsToKeep.Add(teleportationIndex - 1);
                }
            }

            pointsToKeep.AddRange(markersIndices);
            pointsToKeep.Sort();

            var nSegments = teleportationIndices.Count + 1;
            var segments = new List<TrajectorySegmentPoint>[nSegments];

            for (var segmentIdx = 0; segmentIdx < nSegments; ++segmentIdx)
            {
                int startIdx;
                int endIdx;

                if (nSegments == 1)
                {
                    startIdx = 0;
                    endIdx = points.Count - 1;
                }
                else if (segmentIdx == 0)
                {
                    startIdx = 0;
                    endIdx = teleportationIndices[segmentIdx] - 1;
                }
                else if (segmentIdx == nSegments - 1)
                {
                    startIdx = teleportationIndices[segmentIdx - 1];
                    endIdx = points.Count - 1;
                }
                else
                {
                    startIdx = teleportationIndices[segmentIdx - 1];
                    endIdx = teleportationIndices[segmentIdx] - 1;
                }

                segments[segmentIdx] = pointsToKeep.Where(i => i >= startIdx && i <= endIdx).Select(idx => points[idx])
                    .ToList();
            }

            PlayerContext.Destroy(_generationContext);
            _generationContext = null;

            PlayerContext.Activate(player.GetPlayerContext());
            IsGenerating = false;

            if (player.GetModuleGenerating() == this)
                player.SetModuleGenerating(null);

            var result = new TrajectoryAnalysisModuleResult(parameters, segments);
            finishCallback(result);
        }

        public void CancelGenerate()
        {
            if (_generationContext != null)
            {
                PlayerContext.Destroy(_generationContext);
                _generationContext = null;
            }

            PlayerContext.Activate(player.GetPlayerContext());
            IsGenerating = false;

            if (player.GetModuleGenerating() == this)
                player.SetModuleGenerating(null);
        }

        public void SetResultVisibility(TrajectoryAnalysisModuleResult result, bool visible)
        {
            foreach (var trajectory in _trajectories)
            {
                if (trajectory.result == result)
                {
                    trajectory.gameObject.SetActive(visible);
                }
            }

            if (_visibleTrajectories.Contains(result) && !visible)
            {
                _visibleTrajectories.Remove(result);
            }
            else if (!_visibleTrajectories.Contains(result) && visible)
            {
                _visibleTrajectories.Add(result);
            }
        }

        public void HideAllResults()
        {
            foreach (var result in GetResults())
            {
                SetResultVisibility(result, false);
            }
        }

        public List<TrajectoryAnalysisModuleResult> GetVisibleResults()
        {
            return _visibleTrajectories;
        }

        public void FixedUpdate()
        {
            foreach (var trajectory in _trajectories)
            {
                trajectory.UpdateMarkersCamera(player.GetCurrentPreviewCamera().GetCamera());
            }
        }

        public override void AddResult(TrajectoryAnalysisModuleResult result)
        {
            base.AddResult(result);

            var trajectoryGameObject = new GameObject("Trajectory Result");
            var trajectory = trajectoryGameObject.AddComponent<Trajectory>();
            trajectory.fullLineMaterial = fullLineMaterial;
            trajectory.dottedLineMaterial = dottedLineMaterial;
            trajectory.rotationAxesPrefab = rotationAxesPrefab;
            trajectory.markerLabelPrefab = markerLabelPrefab;
            trajectory.result = result;
            trajectoryGameObject.transform.parent = gameObject.transform;
            _trajectories.Add(trajectory);
        }

        public override void RemoveResult(TrajectoryAnalysisModuleResult result)
        {
            base.RemoveResult(result);

            foreach (var trajectory in _trajectories)
            {
                if (trajectory.result == result)
                {
                    Destroy(trajectory.gameObject);
                }
            }

            if (_visibleTrajectories.Contains(result))
            {
                _visibleTrajectories.Remove(result);
            }
        }
    }
}