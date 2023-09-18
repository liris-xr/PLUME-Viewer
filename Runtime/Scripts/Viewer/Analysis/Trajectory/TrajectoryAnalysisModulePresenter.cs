using System;

namespace PLUME.UI.Analysis
{
    public class TrajectoryAnalysisModulePresenter : AnalysisModuleWithResultsPresenter<
        TrajectoryAnalysisModule, TrajectoryAnalysisModuleResult, TrajectoryAnalysisModuleUI>
    {
        public Player player;

        public void Start()
        {
            ui.GenerateButton.clicked += OnClickGenerate;
            ui.ObjectIdTextField.value = "30558";
            ui.MarkersTextField.value = "";
            ui.TeleportationToleranceTextField.value = "0.1";
            ui.DecimationToleranceTextField.value = "0.01";
            ui.IncludeRotations.value = false;
            ui.MarkersTextField.value = "";

            ui.clickedDeleteResult += OnClickDeleteResult;
            ui.toggledResultVisibility += OnToggleResultVisibility;

            ui.RefreshTimeRangeLimits();
            ui.TimeRange.Reset();
        }

        private void OnClickGenerate()
        {
            var objectId = ui.ObjectIdTextField.value;
            var markers = ui.MarkersTextField.value.Split(",");
            var teleportationTolerance = float.Parse(ui.TeleportationToleranceTextField.value);
            var decimationTolerance = float.Parse(ui.DecimationToleranceTextField.value);
            var includeRotations = ui.IncludeRotations.value;
            var startTime = ui.TimeRange.StartTime;
            var endTime = ui.TimeRange.EndTime;
            
            ui.GenerateButton.SetEnabled(false);

            var onFinishCallback = new Action<TrajectoryAnalysisModuleResult>(result =>
            {
                ui.GenerateButton.SetEnabled(true);
                module.AddResult(result);
                module.SetResultVisibility(result, true);
                ui.RefreshResults();
            });

            var parameters = new TrajectoryAnalysisModuleParameters();
            parameters.ObjectIdentifier = objectId;
            parameters.VisibleMarkers = markers;
            parameters.IncludeRotations = includeRotations;
            parameters.TeleportationTolerance = teleportationTolerance;
            parameters.DecimationTolerance = decimationTolerance;
            parameters.StartTime = startTime;
            parameters.EndTime = endTime;

            // StartCoroutine(module.GenerateTrajectory(player.GetRecordLoader(), parameters, onFinishCallback));
            module.GenerateTrajectory(player.GetRecordLoader(), parameters, onFinishCallback);
        }

        private void OnClickDeleteResult(TrajectoryAnalysisModuleResult result)
        {
            module.RemoveResult(result);
            ui.RefreshResults();
        }

        private void OnToggleResultVisibility(TrajectoryAnalysisModuleResult result, bool visible)
        {
            module.SetResultVisibility(result, visible);
            ui.RefreshResults();
        }
    }
}