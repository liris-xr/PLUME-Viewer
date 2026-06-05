using System;
using PLUME.Sample.Unity;
using PLUME.Viewer.Player;
using UnityEngine;

namespace PLUME.Viewer.Analysis.EyeGaze
{
    public class EyeGazePreviewAnalysisModule : AnalysisModule
    {
        public Player.Player player;

        public Material coneMaterial;

        [NonSerialized] public bool isEnabled;
        [NonSerialized] public string xrCameraId = "";
        [NonSerialized] public EyeGazeCoordinateSystem coordinateSystem = EyeGazeCoordinateSystem.Camera;
        [NonSerialized] public float halfAngleDeg = 2.5f;
        [NonSerialized] public float nearOffset = 0.15f;
        [NonSerialized] public float coneLength = 2f;

        private Transform _coneOffsetTransform;
        private Transform _coneTransform;
        private MeshFilter _coneMeshFilter;

        private float _builtHalfAngleDeg;
        private float _builtNearOffset;
        private float _builtConeLength;

        private IReadOnlySamplesSortedList<RawSample<InputAction>> _positionSamples;
        private IReadOnlySamplesSortedList<RawSample<InputAction>> _rotationSamples;

        private void Awake()
        {
            var offsetGo = new GameObject("EyeGazePreview_Offset");
            offsetGo.transform.SetParent(transform, false);
            _coneOffsetTransform = offsetGo.transform;

            var coneGo = new GameObject("EyeGazePreview_Cone");
            coneGo.transform.SetParent(_coneOffsetTransform, false);
            _coneTransform = coneGo.transform;

            _coneMeshFilter = coneGo.AddComponent<MeshFilter>();
            _coneMeshFilter.mesh = CreateConeMesh(halfAngleDeg, coneLength, nearOffset);
            _builtHalfAngleDeg = halfAngleDeg;
            _builtNearOffset = nearOffset;
            _builtConeLength = coneLength;

            var meshRenderer = coneGo.AddComponent<MeshRenderer>();
            meshRenderer.material = coneMaterial != null ? coneMaterial : CreateDefaultMaterial();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            coneGo.SetActive(false);
        }

        private void CacheGazeSamples()
        {
            var inputActions = player.Record.InputActions;
            _positionSamples = inputActions.Where(
                s => s.Payload.BindingPaths.Contains("<EyeGaze>/pose/position"));
            _rotationSamples = inputActions.Where(
                s => s.Payload.BindingPaths.Contains("<EyeGaze>/pose/rotation"));
        }

        private void LateUpdate()
        {
            if (!Mathf.Approximately(halfAngleDeg, _builtHalfAngleDeg) ||
                !Mathf.Approximately(nearOffset, _builtNearOffset) ||
                !Mathf.Approximately(coneLength, _builtConeLength))
            {
                _coneMeshFilter.mesh = CreateConeMesh(halfAngleDeg, coneLength, nearOffset);
                _builtHalfAngleDeg = halfAngleDeg;
                _builtNearOffset = nearOffset;
                _builtConeLength = coneLength;
            }

            var coneGo = _coneTransform.gameObject;

            if (!isEnabled)
            {
                coneGo.SetActive(false);
                return;
            }

            if (!player.IsRecordLoaded)
            {
                coneGo.SetActive(false);
                return;
            }

            if (_positionSamples == null)
                CacheGazeSamples();

            var ctx = player.GetMainPlayerContext();
            if (ctx == null)
            {
                coneGo.SetActive(false);
                return;
            }

            if (!Guid.TryParse(xrCameraId, out var cameraGuid))
            {
                coneGo.SetActive(false);
                return;
            }

            var replayCameraId = ctx.GetReplayInstanceId(cameraGuid);
            if (!replayCameraId.HasValue)
            {
                coneGo.SetActive(false);
                return;
            }

            var xrCamera = ctx.FindGameObjectByInstanceId(replayCameraId.Value);
            if (xrCamera == null)
            {
                coneGo.SetActive(false);
                return;
            }

            var currentTime = player.GetCurrentPlayTimeInNanoseconds();
            var posIdx = _positionSamples.FirstIndexBeforeTimestamp(currentTime);
            var rotIdx = _rotationSamples.FirstIndexBeforeTimestamp(currentTime);

            if (posIdx < 0 || rotIdx < 0)
            {
                coneGo.SetActive(false);
                return;
            }

            var gazePosition = _positionSamples[posIdx].Payload.Vector3.ToEngineType();
            var gazeRotation = _rotationSamples[rotIdx].Payload.Quaternion.ToEngineType();

            coneGo.SetActive(true);
            ApplyGazePose(xrCamera, gazePosition, gazeRotation);
        }

        private void ApplyGazePose(GameObject xrCamera, Vector3 gazePosition, Quaternion gazeRotation)
        {
            switch (coordinateSystem)
            {
                case EyeGazeCoordinateSystem.TrackingSpace:
                case EyeGazeCoordinateSystem.World:
                {
                    var xrCameraOffset = xrCamera.transform.parent;
                    if (xrCameraOffset == null) return;

                    _coneOffsetTransform.position = xrCameraOffset.position;
                    _coneOffsetTransform.rotation = xrCameraOffset.rotation;
                    _coneTransform.localPosition = gazePosition;
                    _coneTransform.localRotation = gazeRotation;
                    break;
                }
                case EyeGazeCoordinateSystem.Camera:
                {
                    _coneOffsetTransform.position = xrCamera.transform.position;
                    _coneOffsetTransform.rotation = xrCamera.transform.rotation;

                    var q = gazeRotation;
                    var rotation = new Quaternion(q.x, -q.y, -q.z, -q.w);
                    var dir = Quaternion.LookRotation(rotation * -Vector3.forward);
                    _coneTransform.localPosition = gazePosition;
                    _coneTransform.localRotation = dir;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(coordinateSystem));
            }
        }

        private static Mesh CreateConeMesh(float halfAngleDeg, float length, float nearOffset = 0.15f, int segments = 24)
        {
            var mesh = new Mesh { name = "EyeGazeCone" };

            var tanAngle = Mathf.Tan(halfAngleDeg * Mathf.Deg2Rad);
            var cosAngle = Mathf.Cos(halfAngleDeg * Mathf.Deg2Rad);
            var sinAngle = Mathf.Sin(halfAngleDeg * Mathf.Deg2Rad);
            var nearRadius = nearOffset * tanAngle;
            var farRadius  = length * tanAngle;

            // Vertices are not shared between side and caps so each region gets its own normals.
            // Layout:
            //   Side near ring : [0 .. seg-1]
            //   Side far ring  : [seg .. 2seg-1]
            //   Near cap center: [2seg]
            //   Near cap ring  : [2seg+1 .. 3seg]
            //   Far cap center : [3seg+1]
            //   Far cap ring   : [3seg+2 .. 4seg+1]
            var vertices  = new Vector3[4 * segments + 2];
            var normals   = new Vector3[4 * segments + 2];
            var triangles = new int[segments * 12];

            for (var i = 0; i < segments; i++)
            {
                var angle = 2f * Mathf.PI * i / segments;
                var c = Mathf.Cos(angle);
                var s = Mathf.Sin(angle);

                // Side — analytic outward normal: (c·cosα, s·cosα, −sinα)
                var sideN = new Vector3(c * cosAngle, s * cosAngle, -sinAngle);
                vertices[i]            = new Vector3(c * nearRadius, s * nearRadius, nearOffset);
                normals[i]             = sideN;
                vertices[segments + i] = new Vector3(c * farRadius, s * farRadius, length);
                normals[segments + i]  = sideN;

                // Near cap ring — normal (0, 0, −1)
                vertices[2 * segments + 1 + i] = new Vector3(c * nearRadius, s * nearRadius, nearOffset);
                normals[2 * segments + 1 + i]  = new Vector3(0f, 0f, -1f);

                // Far cap ring — normal (0, 0, +1)
                vertices[3 * segments + 2 + i] = new Vector3(c * farRadius, s * farRadius, length);
                normals[3 * segments + 2 + i]  = new Vector3(0f, 0f, 1f);
            }

            vertices[2 * segments]     = new Vector3(0f, 0f, nearOffset);
            normals[2 * segments]      = new Vector3(0f, 0f, -1f);
            vertices[3 * segments + 1] = new Vector3(0f, 0f, length);
            normals[3 * segments + 1]  = new Vector3(0f, 0f, 1f);

            var triIdx = 0;
            for (var i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;

                // Side — winding produces outward (negative Z-component) normal
                triangles[triIdx++] = i;
                triangles[triIdx++] = segments + next;
                triangles[triIdx++] = segments + i;

                triangles[triIdx++] = i;
                triangles[triIdx++] = next;
                triangles[triIdx++] = segments + next;

                // Near cap — (center, next, i) produces −Z normal
                triangles[triIdx++] = 2 * segments;
                triangles[triIdx++] = 2 * segments + 1 + next;
                triangles[triIdx++] = 2 * segments + 1 + i;

                // Far cap — (center, i, next) produces +Z normal
                triangles[triIdx++] = 3 * segments + 1;
                triangles[triIdx++] = 3 * segments + 2 + i;
                triangles[triIdx++] = 3 * segments + 2 + next;
            }

            mesh.vertices  = vertices;
            mesh.normals   = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateDefaultMaterial()
        {
            var shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Standard");

            var mat = new Material(shader) { name = "EyeGazePreviewMaterial" };

            if (mat.HasProperty("_Color"))
                mat.color = new Color(1f, 0.4f, 0f, 0.4f);

            return mat;
        }
    }
}
