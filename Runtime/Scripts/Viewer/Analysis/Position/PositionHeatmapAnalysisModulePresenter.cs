using System;
using System.Linq;

namespace PLUME.UI.Analysis
{
    public class PositionHeatmapAnalysisModulePresenter : AnalysisModuleWithResultsPresenter<
        PositionHeatmapAnalysisModule, PositionHeatmapAnalysisResult, PositionHeatmapAnalysisModuleUI>
    {
        public Player player;

        public void Start()
        {
            ui.GenerateButton.clicked += OnClickGenerate;
            ui.ProjectionCasterIdTextField.value = "34868";
            ui.ProjectionReceiversIdsTextField.value = "34830";

            ui.clickedDeleteResult += OnClickDeleteResult;
            ui.toggledResultVisibility += OnToggleResultVisibility;

            ui.RefreshTimeRangeLimits();
            ui.TimeRange.Reset();
        }

        private void OnClickGenerate()
        {
            module.SetVisibleResult(null);

            var projectionCasterId = ui.ProjectionCasterIdTextField.value;
            var projectionReceiversIds = ui.ProjectionReceiversIdsTextField.value.Split(",").ToArray();
            var startTime = ui.TimeRange.StartTime;
            var endTime = ui.TimeRange.EndTime;

            ui.GenerateButton.SetEnabled(false);

            var onFinishCallback = new Action<PositionHeatmapAnalysisResult>(result =>
            {
                ui.GenerateButton.SetEnabled(true);
                module.AddResult(result);
                module.SetVisibleResult(result);
                ui.RefreshResults();
            });

            // StartCoroutine(module.GenerateHeatmap(player.GetRecordLoader(), player.GetPlayerAssets(),
            //     projectionCasterId, projectionReceiversIds, startTime, endTime, onFinishCallback));
            module.GenerateHeatmap(player.GetRecordLoader(), player.GetPlayerAssets(),
                projectionCasterId, projectionReceiversIds, startTime, endTime, onFinishCallback);
        }

        private void OnClickDeleteResult(PositionHeatmapAnalysisResult result)
        {
            module.RemoveResult(result);
            ui.RefreshResults();
        }

        private void OnToggleResultVisibility(PositionHeatmapAnalysisResult result, bool visible)
        {
            if (visible)
                module.SetVisibleResult(result);
            else if (module.GetVisibleResult() == result)
                module.SetVisibleResult(null);

            ui.RefreshResults();
        }
    }
}