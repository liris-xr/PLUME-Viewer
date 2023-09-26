using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PLUME.Sample.Common;
using PLUME.Sample.Unity;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace PLUME
{
    public class TrajectoryAnalysisModule : AnalysisModuleWithResults<TrajectoryAnalysisModuleResult>
    {
        public Player player;

        public Material fullLineMaterial;
        public Material dottedLineMaterial;
        public GameObject rotationAxesPrefab;
        public GameObject markerLabelPrefab;

        private readonly List<TrajectoryAnalysisModuleResult> _visibleTrajectories = new();
        private readonly List<Trajectory> _trajectories = new();

        private Camera _lastPlayerCamera;

        public IEnumerator GenerateTrajectory(BufferedAsyncRecordLoader loader,
            TrajectoryAnalysisModuleParameters parameters, Action<TrajectoryAnalysisModuleResult> finishCallback)
        {
            if (parameters.EndTime < parameters.StartTime)
            {
                throw new Exception(
                    $"{nameof(parameters.StartTime)} should be less or equal to {nameof(parameters.EndTime)}.");
            }

            var samplesLoadingTask = loader.SamplesInTimeRangeAsync(parameters.StartTime, parameters.EndTime);
            yield return new WaitUntil(() => samplesLoadingTask.IsCompleted);

            var points = new List<TrajectorySegmentPoint>();
            var teleportationIndices = new List<int>();
            var markersIndices = new List<int>();

            var index = 0;

            foreach (var sample in samplesLoadingTask.Result)
            {
                var lastPosition = points.LastOrDefault()?.Position;
                var lastRotation = points.LastOrDefault()?.Rotation;
                
                if (sample.Payload is TransformUpdateRotation transformUpdateRotation)
                {
                    if (!parameters.IncludeRotations) continue;
                    if (transformUpdateRotation.Id.GameObjectId != parameters.ObjectIdentifier) continue;
                    if (!lastPosition.HasValue) continue;
                    var rotation = transformUpdateRotation.WorldRotation.ToEngineType();
                    points.Add(new TrajectorySegmentPoint(sample.Header.Time, lastPosition.Value, rotation, null));
                    ++index;
                }
                if (sample.Payload is TransformUpdatePosition transformUpdatePosition)
                {
                    if (transformUpdatePosition.Id.GameObjectId != parameters.ObjectIdentifier) continue;
                    var position = transformUpdatePosition.WorldPosition.ToEngineType();

                    Quaternion? rotation = parameters.IncludeRotations ? lastRotation.GetValueOrDefault() : null;
                    points.Add(new TrajectorySegmentPoint(sample.Header.Time, position, rotation, null));

                    if (lastPosition.HasValue && Vector3.Distance(position, lastPosition.Value) >=
                        parameters.TeleportationTolerance)
                    {
                        teleportationIndices.Add(index);
                    }
                    ++index;
                }
                else if (sample.Payload is Marker marker)
                {
                    if (parameters.VisibleMarkers != null && parameters.VisibleMarkers.Contains(marker.Label))
                    {
                        markersIndices.Add(index);
                        points.Add(new TrajectorySegmentPoint(sample.Header.Time, lastPosition ?? Vector3.zero,
                            lastRotation, marker));
                        ++index;
                    }
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

            var result = new TrajectoryAnalysisModuleResult(parameters, segments);
            finishCallback(result);
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

        public List<TrajectoryAnalysisModuleResult> GetVisibleResults()
        {
            return _visibleTrajectories;
        }

        public void FixedUpdate()
        {
            foreach (var trajectory in _trajectories)
            {
                trajectory.UpdateMarkersCamera(player.currentCamera);
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