using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PLUME.Viewer.Analysis.Trajectory
{
    public class TrajectoryAnalysisModulePresenter : AnalysisModuleWithResultsPresenter<
        TrajectoryAnalysisModule, TrajectoryAnalysisModuleResult, TrajectoryAnalysisModuleUI>
    {
        public Player.Player player;

        public string defaultObjectId = "";
        public string defaultMarkers = "";
        public float defaultTeleportationTolerance = 0.1f;
        public bool defaultTeleportationSegments = true;
        public float defaultDecimationTolerance = 0.01f;
        public bool defaultIncludeRotations;
        
        private Coroutine _generationCoroutine;

        public void Start()
        {
            ui.GenerateButton.clicked += OnClickGenerate;
            ui.CancelButton.clicked += OnClickCancel;
            ui.ObjectIdTextField.value = defaultObjectId;
            ui.MarkersTextField.value = defaultMarkers;
            ui.TeleportationToleranceTextField.value = defaultTeleportationTolerance.ToString(CultureInfo.InvariantCulture);
            ui.TeleportationSegments.value = defaultTeleportationSegments;
            ui.DecimationToleranceTextField.value = defaultDecimationTolerance.ToString(CultureInfo.InvariantCulture);
            ui.IncludeRotations.value = defaultIncludeRotations;

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
            var objectId = Guid.Parse(ui.ObjectIdTextField.value.Trim());
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

            _generationCoroutine = StartCoroutine(module.GenerateTrajectory(player.Record, player.RecordAssetBundle,
                parameters, onFinishCallback));
        }

        public void FixedUpdate()
        {
            // TODO: remove, quick and dirty fix to prevent heatmap results to be visible while another type of heatmap is being generated
            var otherModuleGenerating = player.GetModuleGenerating() != null && player.GetModuleGenerating() != module;
            ui.GenerateButton.SetEnabled(!otherModuleGenerating);

            if (module.IsGenerating)
            {
                ui.GenerationProgressBar.value = module.GenerationProgress;
            }
        }

        private void OnClickCancel()
        {
            if (_generationCoroutine == null) return;
            StopCoroutine(_generationCoroutine);
            module.CancelGenerate();
            module.HideAllResults();
            ui.RefreshResults();
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