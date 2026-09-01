using UnityEngine.UIElements;

namespace PLUME.Viewer.Analysis.EyeGaze
{
    public class EyeGazePreviewAnalysisModuleUI : AnalysisModuleUI<EyeGazePreviewAnalysisModule>
    {
        public Toggle EnabledToggle { get; private set; }
        public TextField XrCameraIdTextField { get; private set; }
        public TextField GazePositionBindingTextField { get; private set; }
        public TextField GazeRotationBindingTextField { get; private set; }
        public EnumField CoordinateSystemEnumField { get; private set; }
        public FloatField FovealAngleField { get; private set; }
        public FloatField NearOffsetField { get; private set; }
        public FloatField ConeLengthField { get; private set; }

        protected new void Awake()
        {
            base.Awake();

            EnabledToggle = Options.Q<Toggle>("enabled");
            XrCameraIdTextField = Options.Q<TextField>("xr-camera");
            GazePositionBindingTextField = Options.Q<TextField>("gaze-position-binding");
            GazeRotationBindingTextField = Options.Q<TextField>("gaze-rotation-binding");
            CoordinateSystemEnumField = Options.Q<EnumField>("coordinate-system");
            FovealAngleField = Options.Q<FloatField>("foveal-angle");
            NearOffsetField = Options.Q<FloatField>("near-offset");
            ConeLengthField = Options.Q<FloatField>("cone-length");
        }

        public override string GetTitle() => "Eye Gaze Preview";
    }
}
