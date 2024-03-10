using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PLUME.Viewer.Analysis.Position
{
    public class PositionHeatmapAnalysisModulePresenter : AnalysisModuleWithResultsPresenter<
        PositionHeatmapAnalysisModule, PositionHeatmapAnalysisResult, PositionHeatmapAnalysisModuleUI>
    {
        public Player.Player player;

        public string defaultProjectionCasterId = "";
        public string defaultProjectionReceiversIds = "";
        
        private Coroutine _generationCoroutine;

        public void Start()
        {
            ui.GenerateButton.clicked += OnClickGenerate;
            ui.CancelButton.clicked += OnClickCancel;
            ui.ProjectionCasterIdTextField.value = defaultProjectionCasterId;
            ui.ProjectionReceiversIdsTextField.value = defaultProjectionReceiversIds;

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

            var parameters = new PositionHeatmapAnalysisModuleParameters
            {
                CasterIdentifier = ui.ProjectionCasterIdTextField.value.Trim(),
                ReceiversIdentifiers = ui.ProjectionReceiversIdsTextField.value.Trim().Split(",")
                    .Where(s => s.Length > 0).ToArray(),
                StartTime = ui.TimeRange.StartTime,
                EndTime = ui.TimeRange.EndTime,
                IncludeReceiversChildren = ui.IncludeReceiversChildrenToggle.value
            };

            var onFinishCallback = new Action<PositionHeatmapAnalysisResult>(result =>
            {
                module.AddResult(result);
                module.SetVisibleResult(result);

                if (player.GetVisibleHeatmapModule() != module)
                {
                    player.SetVisibleHeatmapModule(module);
                }

                ui.RefreshResults();
            });

            _generationCoroutine = StartCoroutine(module.GenerateHeatmap(player.Record, player.RecordAssetBundle,
                parameters, onFinishCallback));
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

        private void OnClickDeleteResult(PositionHeatmapAnalysisResult result)
        {
            if (module.GetVisibleResult() == result && player.GetVisibleHeatmapModule() == module)
            {
                player.SetVisibleHeatmapModule(null);
            }

            module.RemoveResult(result);
            ui.RefreshResults();
        }

        private void OnClickExportResult(PositionHeatmapAnalysisResult result)
        {
            module.ExportResult(result);
        }

        private void OnToggleResultVisibility(PositionHeatmapAnalysisResult result, bool visible)
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