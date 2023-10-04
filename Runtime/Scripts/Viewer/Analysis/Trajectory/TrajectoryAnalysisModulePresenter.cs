using System;
using System.Collections.Generic;

namespace PLUME.UI.Analysis
{
    public class TrajectoryAnalysisModulePresenter : AnalysisModuleWithResultsPresenter<
        TrajectoryAnalysisModule, TrajectoryAnalysisModuleResult, TrajectoryAnalysisModuleUI>
    {
        public Player player;

        public void Start()
        {
            ui.GenerateButton.clicked += OnClickGenerate;
            ui.ObjectIdTextField.value = "882be9d0-c9cc-4b78-b6b3-1d5419f4cd83";
            ui.MarkersTextField.value = "Egg Pick Up";
            ui.TeleportationToleranceTextField.value = "0.1";
            ui.TeleportationSegments.value = true;
            ui.DecimationToleranceTextField.value = "0.01";
            ui.IncludeRotations.value = false;

            ui.clickedDeleteResult += OnClickDeleteResult;
            ui.toggledResultVisibility += OnToggleResultVisibility;

            ui.RefreshTimeRangeLimits();
            ui.TimeRange.Reset();
            
            player.onGeneratingModuleChanged += generatingModule =>
            {
                // TODO: remove, quick and dirty fix to prevent heatmap results to be visible while another type of heatmap is being shown
                if (generatingModule != null)
                {
                    var visibleResults = new List<TrajectoryAnalysisModuleResult>(module.GetVisibleResults());

                    foreach (var result in visibleResults)
                    {
                        module.SetResultVisibility(result, false);
                    }
                    
                    ui.RefreshResults();
                }
            };
        }

        private void OnClickGenerate()
        {
            var objectId = ui.ObjectIdTextField.value;
            var markers = ui.MarkersTextField.value.Split(",");
            var teleportationTolerance = float.Parse(ui.TeleportationToleranceTextField.value);
            var teleportationSegments = ui.TeleportationSegments.value;
            var decimationTolerance = float.Parse(ui.DecimationToleranceTextField.value);
            var includeRotations = ui.IncludeRotations.value;
            var startTime = ui.TimeRange.StartTime;
            var endTime = ui.TimeRange.EndTime;
            
            ui.GenerateButton.text = "Generating...";
            ui.GenerateButton.SetEnabled(false);

            var onFinishCallback = new Action<TrajectoryAnalysisModuleResult>(result =>
            {
                ui.GenerateButton.text = "Generate";
                ui.GenerateButton.SetEnabled(true);
                module.AddResult(result);
                module.SetResultVisibility(result, true);
                ui.RefreshResults();
            });

            var parameters = new TrajectoryAnalysisModuleParameters();
            parameters.ObjectIdentifier = objectId;
            parameters.VisibleMarkers = markers;
            parameters.IncludeRotations = includeRotations;
            parameters.TeleportationSegments = teleportationSegments;
            parameters.TeleportationTolerance = teleportationTolerance;
            parameters.DecimationTolerance = decimationTolerance;
            parameters.StartTime = startTime;
            parameters.EndTime = endTime;

            StartCoroutine(module.GenerateTrajectory(player.GetRecordLoader(), parameters, onFinishCallback));
        }

        public void FixedUpdate()
        {
            // TODO: remove, quick and dirty fix to prevent heatmap results to be visible while another type of heatmap is being generated
            var otherModuleGenerating = player.GetModuleGenerating() != null && player.GetModuleGenerating() != module;
            ui.GenerateButton.SetEnabled(!otherModuleGenerating);
        }

        private void OnClickDeleteResult(TrajectoryAnalysisModuleResult result)
        {
            module.RemoveResult(result);
            ui.RefreshResults();
        }

        private void OnToggleResultVisibility(TrajectoryAnalysisModuleResult result, bool visible)
        {
            // Disable show/hide when generating
            if (player.GetModuleGenerating() == null)
            {
                module.SetResultVisibility(result, visible);
            }

            ui.RefreshResults();
        }
    }
}