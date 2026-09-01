using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PLUME.Viewer.Analysis.EyeGaze
{
    public class EyeGazeVertexHeatmapAnalysisModulePresenter : MonoBehaviour
    {
        public Player.Player player;

        public string defaultXrCameraId = "";
        public string defaultProjectionReceiversIds = "";
        public string defaultGazePositionBindingPath = "<EyeGaze>/pose/position";
        public string defaultGazeRotationBindingPath = "<EyeGaze>/pose/rotation";
        public EyeGazeCoordinateSystem defaultCoordinateSystem = EyeGazeCoordinateSystem.Camera;
        public float defaultFovealVisionOpticalAxisAngle = 2.5f;
        public float defaultNSigmas = 4f;

        public EyeGazeVertexHeatmapAnalysisModule module;
        public EyeGazeVertexHeatmapAnalysisModuleUI ui;

        private Coroutine _generationCoroutine;

        public void Start()
        {
            ui.GenerateButton.clicked += OnClickGenerate;
            ui.CancelButton.clicked += OnClickCancel;
            ui.XrCameraIdTextField.value = defaultXrCameraId;
            ui.ProjectionReceiversIdsTextField.value = defaultProjectionReceiversIds;
            ui.GazePositionBindingTextField.value = defaultGazePositionBindingPath;
            ui.GazeRotationBindingTextField.value = defaultGazeRotationBindingPath;
            ui.EyeGazeCoordinateSystemEnumField.value = defaultCoordinateSystem;
            ui.FovealAngleField.value = defaultFovealVisionOpticalAxisAngle;
            ui.NSigmasField.value = defaultNSigmas;

            ui.clickedDeleteResult += OnClickDeleteResult;
            ui.toggledResultVisibility += OnToggleResultVisibility;

            ui.RefreshTimeRangeLimits();
            ui.TimeRange.Reset();

            player.onVisibleHeatmapModuleChanged += OnVisibleHeatmapModuleChanged;
            player.onGeneratingModuleChanged += OnGeneratingModuleChanged;
        }

        private void OnDestroy()
        {
            if (player == null)
                return;

            player.onVisibleHeatmapModuleChanged -= OnVisibleHeatmapModuleChanged;
            player.onGeneratingModuleChanged -= OnGeneratingModuleChanged;
        }

        private void OnVisibleHeatmapModuleChanged(AnalysisModule visibleModule)
        {
            if (visibleModule != null && visibleModule != module)
            {
                module.SetVisibleResult(null);
                ui.RefreshResults();
            }
        }

        private void OnGeneratingModuleChanged(AnalysisModule generatingModule)
        {
            if (generatingModule != null && generatingModule != module)
            {
                module.SetVisibleResult(null);
                ui.RefreshResults();
            }
        }

        private void OnClickGenerate()
        {
            module.SetVisibleResult(null);
            ui.GenerateButton.SetEnabled(false);

            var parameters = new EyeGazeAnalysisModuleParameters
            {
                XrCameraIdentifier = Guid.Parse(ui.XrCameraIdTextField.value.Trim()),
                ReceiversIdentifiers = ui.ProjectionReceiversIdsTextField.value.Trim().Split(",")
                    .Where(s => s.Length > 0).Select(Guid.Parse).ToArray(),
                IncludeReceiversChildren = ui.IncludeReceiversChildrenToggle.value,
                StartTime = ui.TimeRange.StartTime,
                EndTime = ui.TimeRange.EndTime,
                CoordinateSystem = (EyeGazeCoordinateSystem)ui.EyeGazeCoordinateSystemEnumField.value,
                FovealVisionOpticalAxisAngle = ui.FovealAngleField.value,
                NSigmas = ui.NSigmasField.value,
                GazePositionBindingPath = ui.GazePositionBindingTextField.value.Trim(),
                GazeRotationBindingPath = ui.GazeRotationBindingTextField.value.Trim()
            };

            var onFinish = new Action<EyeGazeVertexHeatmapResult>(result =>
            {
                module.AddResult(result);
                module.SetVisibleResult(result);

                if (player.GetVisibleHeatmapModule() != module)
                    player.SetVisibleHeatmapModule(module);

                ui.RefreshResults();
            });

            _generationCoroutine = StartCoroutine(
                module.GenerateHeatmap(player.Record, player.RecordAssetBundle, parameters, onFinish));
        }

        private void OnClickCancel()
        {
            if (_generationCoroutine == null) return;
            StopCoroutine(_generationCoroutine);
            module.CancelGenerate();
            module.SetVisibleResult(null);
            ui.RefreshResults();
        }

        public void FixedUpdate()
        {
            var otherModuleGenerating =
                player.GetModuleGenerating() != null && player.GetModuleGenerating() != module;

            ui.GenerateButton.style.display = module.IsGenerating ? DisplayStyle.None : DisplayStyle.Flex;
            ui.GenerateButton.SetEnabled(!module.IsGenerating && !otherModuleGenerating);

            ui.GeneratingPanel.style.display = module.IsGenerating ? DisplayStyle.Flex : DisplayStyle.None;
            ui.CancelButton.SetEnabled(module.IsGenerating);

            if (module.IsGenerating)
                ui.GenerationProgressBar.value = module.GenerationProgress;
        }

        private void OnClickDeleteResult(EyeGazeVertexHeatmapResult result)
        {
            if (module.GetVisibleResult() == result && player.GetVisibleHeatmapModule() == module)
                player.SetVisibleHeatmapModule(null);

            module.RemoveResult(result);
            ui.RefreshResults();
        }

        private void OnToggleResultVisibility(EyeGazeVertexHeatmapResult result, bool visible)
        {
            if (player.GetModuleGenerating() != null)
                return;

            if (visible)
            {
                module.SetVisibleResult(result);

                if (player.GetVisibleHeatmapModule() != module)
                    player.SetVisibleHeatmapModule(module);
            }
            else if (module.GetVisibleResult() == result)
            {
                module.SetVisibleResult(null);

                if (player.GetVisibleHeatmapModule() == module)
                    player.SetVisibleHeatmapModule(null);
            }

            ui.RefreshResults();
        }
    }
}
