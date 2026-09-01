using UnityEngine;

namespace PLUME.Viewer.Analysis.EyeGaze
{
    public class EyeGazePreviewAnalysisModulePresenter : MonoBehaviour
    {
        public string defaultXrCameraId = "";
        public string defaultGazePositionBindingPath = "<EyeGaze>/pose/position";
        public string defaultGazeRotationBindingPath = "<EyeGaze>/pose/rotation";
        public EyeGazeCoordinateSystem defaultCoordinateSystem = EyeGazeCoordinateSystem.Camera;
        public float defaultFovealAngleDeg = 2.5f;
        public float defaultNearOffset = 0.15f;
        public float defaultConeLength = 2f;

        public EyeGazePreviewAnalysisModule module;
        public EyeGazePreviewAnalysisModuleUI ui;

        private void Start()
        {
            ui.EnabledToggle.value = false;
            ui.XrCameraIdTextField.value = defaultXrCameraId;
            ui.GazePositionBindingTextField.value = defaultGazePositionBindingPath;
            ui.GazeRotationBindingTextField.value = defaultGazeRotationBindingPath;
            ui.CoordinateSystemEnumField.value = defaultCoordinateSystem;
            ui.FovealAngleField.value = defaultFovealAngleDeg;
            ui.NearOffsetField.value = defaultNearOffset;
            ui.ConeLengthField.value = defaultConeLength;
        }

        private void FixedUpdate()
        {
            module.isEnabled = ui.EnabledToggle.value;
            module.xrCameraId = ui.XrCameraIdTextField.value.Trim();
            module.gazePositionBindingPath = ui.GazePositionBindingTextField.value.Trim();
            module.gazeRotationBindingPath = ui.GazeRotationBindingTextField.value.Trim();
            module.coordinateSystem = (EyeGazeCoordinateSystem)ui.CoordinateSystemEnumField.value;
            module.halfAngleDeg = ui.FovealAngleField.value;
            module.nearOffset = ui.NearOffsetField.value;
            module.coneLength = ui.ConeLengthField.value;
        }
    }
}
