using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PLUME.Viewer.Analysis.EyeGaze
{
    public class EyeGazeAnalysisModulePresenter : MonoBehaviour
    {
        public Player.Player player;

        public EyeGazeAnalysisModule module;
        public EyeGazeAnalysisModuleUI ui;

        private Coroutine _generationCoroutine;

        public void Start()
        {
            ui.GenerateButton.clicked += OnClickGenerate;
            ui.CancelButton.clicked += OnClickCancel;
            ui.XrCameraIdTextField.value = "2ba16eb02f5542f698366385cb5c7f31";
            ui.ProjectionReceiversIdsTextField.value =
                "b8b528b4-78f8-4bcb-880e-8f3d228123dd,536bb4b3-3537-43b6-9b3d-87b0f051b0ec,7dac28ee-1491-4bf6-b04f-a5982109b713,dab82065-7dc6-4b87-8b46-b55e69158932,2f1e5832-74e3-48f7-898b-f4fdbf5c5afb,179b53f1-65d7-4921-8faf-5163956741cd,1c455832-9aa9-4d39-908b-2ba83bcd596a,edb0c44b-36e6-4652-b60f-ad35a8ffd9da,01014b44-b07d-4ceb-80ec-76ca7872889e,4fc45f72-79e1-496b-90c4-f50802385e26,d565d02b-2a27-4dbb-8113-b67c4a21c559,04601c9d-02f1-4242-9273-209f513dd0fb,5bed4f47-c6ad-4c5a-bb4b-d6f8f6dac689,37f85214-460e-463e-a4fc-45c00733a057,8f59a5e3-e9e7-4211-bfb3-1c475e55fdba,5508cdc8-cc41-4570-ae28-ba91119889f6,0e40b1db-7188-4843-9efc-96f9f19dc668,7615d436-a11d-4635-9f91-8f5e7019c08c,2ce3a46c-6702-490b-a3d1-c91b49fb5a55,13ef2fec-89cb-45f4-bc15-3cebfba91bf4";
            ui.EyeGazeCoordinateSystemEnumField.value = EyeGazeCoordinateSystem.Camera;
            
            ui.clickedDeleteResult += OnClickDeleteResult;
            ui.clickedExportResult += OnClickExportResult;
            ui.toggledResultVisibility += OnToggleResultVisibility;

            ui.RefreshTimeRangeLimits();
            ui.TimeRange.Reset();

            player.onVisibleHeatmapModuleChanged += visibleModule =>
            {
                // TODO: remove, quick and dirty fix to prevent heatmap results to be visible while another type of heatmap is being shown
                if (visibleModule != null && visibleModule != module)
                {
                    module.SetVisibleResult(null);
                    ui.RefreshResults();
                }
            };

            player.onGeneratingModuleChanged += generatingModule =>
            {
                // TODO: remove, quick and dirty fix to prevent heatmap results to be visible while another type of heatmap is being shown
                if (generatingModule != null && generatingModule != module)
                {
                    module.SetVisibleResult(null);
                    ui.RefreshResults();
                }
            };
        }

        private void OnClickGenerate()
        {
            module.SetVisibleResult(null);
            ui.GenerateButton.SetEnabled(false);

            var generationParameters = new EyeGazeAnalysisModuleParameters
            {
                XrCameraIdentifier = ui.XrCameraIdTextField.value.Trim(),
                ReceiversIdentifiers = ui.ProjectionReceiversIdsTextField.value.Trim().Split(",")
                    .Where(s => s.Length > 0).ToArray(),
                IncludeReceiversChildren = ui.IncludeReceiversChildrenToggle.value,
                StartTime = ui.TimeRange.StartTime,
                EndTime = ui.TimeRange.EndTime,
                CoordinateSystem = (EyeGazeCoordinateSystem)ui.EyeGazeCoordinateSystemEnumField.value
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