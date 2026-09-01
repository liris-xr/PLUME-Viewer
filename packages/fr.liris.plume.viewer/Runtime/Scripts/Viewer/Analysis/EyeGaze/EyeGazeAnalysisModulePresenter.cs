using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PLUME.Viewer.Analysis.EyeGaze
{
    public class EyeGazeAnalysisModulePresenter : MonoBehaviour
    {
        public Player.Player player;

        public string defaultXrCameraId = "";
        public string defaultProjectionReceiversIds = "";
        public string defaultGazePositionBindingPath = "<EyeGaze>/pose/position";
        public string defaultGazeRotationBindingPath = "<EyeGaze>/pose/rotation";
        public EyeGazeCoordinateSystem defaultCoordinateSystem = EyeGazeCoordinateSystem.Camera;
        public float defaultFovealVisionOpticalAxisAngle = 2.5f;
        public float defaultNSigmas = 4f;
        public float defaultSamplesPerSquareMeter = 1000f;
        
        public EyeGazeAnalysisModule module;
        public EyeGazeAnalysisModuleUI ui;

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
            ui.SamplesPerSquareMeterField.value = defaultSamplesPerSquareMeter;
            
            ui.clickedDeleteResult += OnClickDeleteResult;
            ui.clickedExportResult += OnClickExportResult;
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

        // TODO: remove, quick and dirty fix to prevent heatmap results to be visible while another type of heatmap is being shown
        private void OnVisibleHeatmapModuleChanged(AnalysisModule visibleModule)
        {
            if (visibleModule != null && visibleModule != module)
            {
                module.SetVisibleResult(null);
                ui.RefreshResults();
            }
        }

        // TODO: remove, quick and dirty fix to prevent heatmap results to be visible while another type of heatmap is being shown
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

            var generationParameters = new EyeGazeAnalysisModuleParameters
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
                SamplesPerSquareMeter = ui.SamplesPerSquareMeterField.value,
                GazePositionBindingPath = ui.GazePositionBindingTextField.value.Trim(),
                GazeRotationBindingPath = ui.GazeRotationBindingTextField.value.Trim()
            };

            var onFinishCallback = new Action<EyeGazeAnalysisResult>(result =>
            {
                module.AddResult(result);
                module.SetVisibleResult(result);

                if (player.GetVisibleHeatmapModule() != module)
                {
                    player.SetVisibleHeatmapModule(module);
                }

                ui.RefreshResults();
            });

            _generationCoroutine = StartCoroutine(module.GenerateHeatmap(player.Record,
                player.RecordAssetBundle, generationParameters, onFinishCallback));
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
            // TODO: remove, quick and dirty fix to prevent heatmap results to be visible while another type of heatmap is being generated
            var otherModuleGenerating = player.GetModuleGenerating() != null && player.GetModuleGenerating() != module;

            ui.GenerateButton.style.display = module.IsGenerating ? DisplayStyle.None : DisplayStyle.Flex;
            ui.GenerateButton.SetEnabled(!module.IsGenerating && !otherModuleGenerating);

            ui.GeneratingPanel.style.display = module.IsGenerating ? DisplayStyle.Flex : DisplayStyle.None;
            ui.CancelButton.SetEnabled(module.IsGenerating);

            if (module.IsGenerating)
            {
                ui.GenerationProgressBar.value = module.GenerationProgress;
            }
        }

        private void OnClickDeleteResult(EyeGazeAnalysisResult result)
        {
            if (module.GetVisibleResult() == result && player.GetVisibleHeatmapModule() == module)
            {
                player.SetVisibleHeatmapModule(null);
            }

            module.RemoveResult(result);
            ui.RefreshResults();
        }

        private void OnClickExportResult(EyeGazeAnalysisResult result)
        {
            module.ExportResult(result);
        }

        private void OnToggleResultVisibility(EyeGazeAnalysisResult result, bool visible)
        {
            // Disable show/hide when generating
            if (player.GetModuleGenerating() == null)
            {
                if (visible)
                {
                    module.SetVisibleResult(result);

                    if (player.GetVisibleHeatmapModule() != module)
                    {
                        player.SetVisibleHeatmapModule(module);
                    }
                }
                else if (module.GetVisibleResult() == result)
                {
                    module.SetVisibleResult(null);

                    if (player.GetVisibleHeatmapModule() == module)
                    {
                        player.SetVisibleHeatmapModule(null);
                    }
                }
            }

            ui.RefreshResults();
        }
    }
}