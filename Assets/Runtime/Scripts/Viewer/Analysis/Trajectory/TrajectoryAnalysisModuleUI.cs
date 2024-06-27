using System;
using System.Globalization;
using System.Linq;
using PLUME.UI.Element;
using UnityEngine;
using UnityEngine.UIElements;

namespace PLUME.Viewer.Analysis.Trajectory
{
    public class
        TrajectoryAnalysisModuleUI : AnalysisModuleWithResultsUI<TrajectoryAnalysisModule,
        TrajectoryAnalysisModuleResult>
    {
        public Action<TrajectoryAnalysisModuleResult> clickedDeleteResult;
        public Player.Player player;

        public VisualTreeAsset resultEntryTemplate;
        public Action<TrajectoryAnalysisModuleResult, bool> toggledResultVisibility;

        public Button GenerateButton { get; private set; }
        public VisualElement GeneratingPanel { get; private set; }
        public ProgressBar GenerationProgressBar { get; private set; }
        public Button CancelButton { get; private set; }
        public TextField ObjectIdTextField { get; private set; }
        public TextField MarkersTextField { get; private set; }
        public Toggle TeleportationSegments { get; private set; }
        public TextField TeleportationToleranceTextField { get; private set; }
        public TextField DecimationToleranceTextField { get; private set; }
        public Toggle IncludeRotations { get; private set; }
        public TimeRangeElement TimeRange { get; private set; }

        protected new void Awake()
        {
            base.Awake();

            GenerateButton = Options.Q<Button>("generate-btn");
            GeneratingPanel = Options.Q("generating");
            GenerationProgressBar = GeneratingPanel.Q<ProgressBar>("progress-bar");
            CancelButton = GeneratingPanel.Q<Button>("cancel-btn");
            ObjectIdTextField = Options.Q<TextField>("object-id");
            MarkersTextField = Options.Q<TextField>("markers");
            TeleportationSegments = Options.Q<Toggle>("teleportation-segments");
            TeleportationToleranceTextField = Options.Q<TextField>("teleportation-tolerance");
            DecimationToleranceTextField = Options.Q<TextField>("decimation-tolerance");
            IncludeRotations = Options.Q<Toggle>("include-rotations");
            TimeRange = Options.Q<TimeRangeElement>("time-range");
        }

        public void RefreshTimeRangeLimits()
        {
            TimeRange.LowLimit = 0u;
            TimeRange.HighLimit = player.Record.Duration;
        }

        public override void RefreshResults()
        {
            if (module.GetResultsCount() > 0)
            {
                ResultsEmptyLabel.style.display = DisplayStyle.None;
                ResultsFoldout.style.display = DisplayStyle.Flex;
            }
            else
            {
                ResultsEmptyLabel.style.display = DisplayStyle.Flex;
                ResultsFoldout.style.display = DisplayStyle.None;
            }

            Results.Clear();

            for (var resultIdx = 0; resultIdx < module.GetResultsCount(); ++resultIdx)
            {
                var result = module.GetResults().ElementAt(resultIdx);

                var resultEntry = resultEntryTemplate.Instantiate();
                var objectId = string.Join(",", result.GenerationParameters.ObjectIdentifier);

                var markersStr = string.Join(",", result.GenerationParameters.VisibleMarkers);
                var teleportationToleranceStr =
                    result.GenerationParameters.TeleportationTolerance.ToString(CultureInfo.InvariantCulture);
                var decimationTolerance =
                    result.GenerationParameters.DecimationTolerance.ToString(CultureInfo.InvariantCulture);
                var includeRotations = result.GenerationParameters.IncludeRotations;
                var startTimeStr = TimeSpan.FromMilliseconds(result.GenerationParameters.StartTime / 1_000_000.0)
                    .ToString(@"hh\:mm\:ss\.fff");
                var endTimeStr = TimeSpan.FromMilliseconds(result.GenerationParameters.EndTime / 1_000_000.0)
                    .ToString(@"hh\:mm\:ss\.fff");
                var segmentsCountStr = result.Segments.Length.ToString();

                resultEntry.Q("object-id").Q<Label>("value").text = objectId;
                resultEntry.Q("markers").Q<Label>("value").text = markersStr.Length > 0 ? markersStr : "None";
                resultEntry.Q("markers").Q<Label>("value").style.unityFontStyleAndWeight =
                    markersStr.Length > 0 ? FontStyle.Normal : FontStyle.Italic;
                resultEntry.Q("teleportation-tolerance").Q<Label>("value").text = teleportationToleranceStr;
                resultEntry.Q("decimation-tolerance").Q<Label>("value").text = decimationTolerance;
                resultEntry.Q("include-rotations").Q<Toggle>("value").value = includeRotations;
                resultEntry.Q("include-rotations").Q<Toggle>("value").SetEnabled(false);
                resultEntry.Q("start-time").Q<Label>("value").text = startTimeStr;
                resultEntry.Q("end-time").Q<Label>("value").text = endTimeStr;
                resultEntry.Q("segments-count").Q<Label>("value").text = segmentsCountStr;

                resultEntry.Q<Button>("delete-btn").clicked += () => clickedDeleteResult?.Invoke(result);
                resultEntry.Q<Label>("result-index").text = $"#{resultIdx + 1}";
                resultEntry.Q<ToggleButton>("show-btn")
                    .SetStateWithoutNotify(module.GetVisibleResults().Contains(result));
                resultEntry.Q<ToggleButton>("show-btn").toggled +=
                    state => toggledResultVisibility?.Invoke(result, state);

                Results.Add(resultEntry);
            }
        }

        public override string GetTitle()
        {
            return "Trajectory";
        }
    }
}