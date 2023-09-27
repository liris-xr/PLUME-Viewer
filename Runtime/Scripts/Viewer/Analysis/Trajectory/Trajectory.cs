using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PLUME
{
    public class Trajectory : MonoBehaviour
    {
        public TrajectoryAnalysisModuleResult result;
        public Material fullLineMaterial;
        public Material dottedLineMaterial;
        public GameObject rotationAxesPrefab;
        public GameObject markerLabelPrefab;

        public float rotationAxesSize = 0.1f;
        public float lineWidth = 0.02f;

        private readonly List<MarkerBillboardFX> _billboards = new();

        private void Start()
        {
            for (var segmentIdx = 0; segmentIdx < result.Segments.Length; ++segmentIdx)
            {
                var segmentPoints = result.Segments[segmentIdx];

                AddContinuousSegment(segmentPoints);

                if (result.GenerationParameters.TeleportationSegments)
                {
                    if (segmentIdx < result.Segments.Length - 1)
                    {
                        var nextSegment = result.Segments[segmentIdx + 1];
                        AddTeleportationSegment(segmentPoints.Last(), nextSegment.First());
                    }
                }

                if (result.GenerationParameters.IncludeRotations)
                {
                    AddRotationAxes(segmentPoints);
                }

                AddMarkersLabel(segmentPoints);
            }
        }

        private void AddMarkersLabel(List<TrajectorySegmentPoint> segmentPoints)
        {
            foreach (var point in segmentPoints)
            {
                if (point.Marker != null)
                {
                    var marker = Instantiate(markerLabelPrefab, transform);
                    var label = marker.GetComponentInChildren<TextMeshProUGUI>();
                    label.text = point.Marker.Label;
                    marker.transform.position = point.Position;
                    _billboards.Add(marker.GetComponent<MarkerBillboardFX>());
                }
            }
        }

        private void AddRotationAxes(List<TrajectorySegmentPoint> segmentPoints)
        {
            foreach (var point in segmentPoints)
            {
                var rotationAxes = Instantiate(rotationAxesPrefab, transform);

                rotationAxes.transform.localScale =
                    new Vector3(rotationAxesSize, rotationAxesSize, rotationAxesSize);
                rotationAxes.transform.position = point.Position;

                if (point.Rotation.HasValue)
                {
                    rotationAxes.transform.rotation = point.Rotation.Value;
                }
            }
        }

        private void AddTeleportationSegment(TrajectorySegmentPoint pointA, TrajectorySegmentPoint pointB)
        {
            var t0 = pointA.Time;
            var t1 = pointB.Time;
            var gradient = new Gradient();
            var key0 = new GradientColorKey(GetColorAtTime(t0), 0);
            var key1 = new GradientColorKey(GetColorAtTime(t1), 1);
            gradient.SetKeys(new[] {key0, key1}, new GradientAlphaKey[] { });

            var segmentGameObject = new GameObject("Teleportation Segment");
            var lineRenderer = segmentGameObject.AddComponent<LineRenderer>();
            lineRenderer.numCapVertices = 4;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPositions(new[] {pointA.Position, pointB.Position});
            lineRenderer.useWorldSpace = true;
            lineRenderer.colorGradient = gradient;
            lineRenderer.startWidth = lineWidth * 1.4f;
            lineRenderer.textureMode = LineTextureMode.RepeatPerSegment;
            lineRenderer.material = dottedLineMaterial;

            // Keep dots aspect ratio correct
            var instanceMaterial = lineRenderer.material;
            var lineLength = Vector3.Distance(pointA.Position, pointB.Position);
            var ratio = lineLength / lineRenderer.startWidth;

            instanceMaterial.mainTextureScale = new Vector2(ratio, 1);

            segmentGameObject.transform.parent = gameObject.transform;
        }

        private void AddContinuousSegment(List<TrajectorySegmentPoint> segmentPoints)
        {
            if (segmentPoints.Count == 1)
            {
                var time = segmentPoints[0].Time;
                var position = segmentPoints[0].Position;
                var color = GetColorAtTime(time);
                
                var segmentGameObject = new GameObject("Continuous Segment");
                var lineRenderer = segmentGameObject.AddComponent<LineRenderer>();
                lineRenderer.numCapVertices = 4;
                lineRenderer.numCornerVertices = 4;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPositions(new[] { position, position });
                lineRenderer.useWorldSpace = true;
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
                lineRenderer.sharedMaterial = fullLineMaterial;
                lineRenderer.startWidth = lineWidth;
                segmentGameObject.transform.parent = gameObject.transform;
            }
            
            var times = segmentPoints.Select(point => point.Time).ToArray();
            var positions = segmentPoints.Select(point => point.Position).ToArray();

            if (positions.Length > 0)
            {
                var t0 = times.First();
                var t1 = times.Last();
                var gradient = new Gradient();
                var key0 = new GradientColorKey(GetColorAtTime(t0), 0);
                var key1 = new GradientColorKey(GetColorAtTime(t1), 1);
                gradient.SetKeys(new[] {key0, key1}, new GradientAlphaKey[] { });

                var segmentGameObject = new GameObject("Continuous Segment");
                var lineRenderer = segmentGameObject.AddComponent<LineRenderer>();
                lineRenderer.numCapVertices = 4;
                lineRenderer.numCornerVertices = 4;
                lineRenderer.positionCount = positions.Length;
                lineRenderer.SetPositions(positions);
                lineRenderer.useWorldSpace = true;
                lineRenderer.colorGradient = gradient;
                lineRenderer.sharedMaterial = fullLineMaterial;
                lineRenderer.startWidth = lineWidth;
                segmentGameObject.transform.parent = gameObject.transform;
            }
        }
        
        public void UpdateMarkersCamera(Camera cam)
        {
            foreach(var billboard in _billboards)
            {
                billboard.camera = cam;
            }
        }

        private Color GetColorAtTime(ulong time)
        {
            var startColor = Color.blue;
            var endColor = Color.red;
            var duration = result.GenerationParameters.EndTime - result.GenerationParameters.StartTime;
            var t = (time - result.GenerationParameters.StartTime) / (float) duration;
            return Color.Lerp(startColor, endColor, t);
        }
    }
}