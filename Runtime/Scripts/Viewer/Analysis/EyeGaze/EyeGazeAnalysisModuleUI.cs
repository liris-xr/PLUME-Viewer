using System;
using System.Linq;
using PLUME.UI.Analysis;
using UnityEngine.UIElements;

namespace PLUME.Viewer.Analysis
{
    public class EyeGazeAnalysisModuleUI : AnalysisModuleWithResultsUI<EyeGazeAnalysisModule, EyeGazeAnalysisResult>
    {
        public PLUME.Player player;
        
        public VisualTreeAsset resultEntryTemplate;

        public Action<EyeGazeAnalysisResult> clickedDeleteResult;
        public Action<EyeGazeAnalysisResult, bool> toggledResultVisibility;
        
        public Button GenerateButton { get; private set; }
        public ProgressBar GenerationProgressBar { get; private set; }
        public VisualElement GeneratingPanel { get; private set; }
        public Button CancelButton { get; private set; }
        public TextField XrCameraIdTextField { get; private set; }
        public TextField ProjectionReceiversIdsTextField { get; private set; }
        public TimeRangeElement TimeRange { get; private set; }
        public EnumField EyeGazeCoordinateSystemEnumField { get; private set; }
        public Toggle IncludeReceiversChildrenToggle { get; set; }

        protected new void Awake()
        {
            base.Awake();

            GenerateButton = Options.Q<Button>("generate-btn");
            GeneratingPanel = Options.Q("generating");
            CancelButton = GeneratingPanel.Q<Button>("cancel-btn");
            GenerationProgressBar = GeneratingPanel.Q<ProgressBar>("progress-bar");
            XrCameraIdTextField = Options.Q<TextField>("xr-camera");
            ProjectionReceiversIdsTextField = Options.Q<TextField>("projection-receivers");
            IncludeReceiversChildrenToggle = Options.Q<Toggle>("include-receivers-children");
            TimeRange = Options.Q<TimeRangeElement>("time-range");
            EyeGazeCoordinateSystemEnumField = Options.Q<EnumField>("coordinate-system");
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
                var startTimeStr = TimeSpan.FromMilliseconds(result.Parameters.StartTime / 1_000_000.0)
                    .ToString(@"hh\:mm\:ss\.fff");
                var endTimeStr = TimeSpan.FromMilliseconds(result.Parameters.EndTime / 1_000_000.0)
                    .ToString(@"hh\:mm\:ss\.fff");
                var projectionReceiversIds = string.Join(",", result.Parameters.ReceiversIdentifiers);

                resultEntry.Q("xr-camera").Q<Label>("value").text = result.Parameters.XrCameraIdentifier;
                resultEntry.Q("projection-receivers").Q<Label>("value").text = projectionReceiversIds;
                resultEntry.Q("start-time").Q<Label>("value").text = startTimeStr;
                resultEntry.Q("end-time").Q<Label>("value").text = endTimeStr;

                resultEntry.Q<Label>("result-index").text = $"#{resultIdx + 1}";
                resultEntry.Q<Button>("delete-btn").clicked += () => clickedDeleteResult?.Invoke(result);
                resultEntry.Q<ToggleButton>("show-btn").toggled +=
                    state => toggledResultVisibility?.Invoke(result, state);
                resultEntry.Q<ToggleButton>("show-btn").SetStateWithoutNotify(module.GetVisibleResult() == result);

                Results.Add(resultEntry);
            }
        }
        
        public void RefreshTimeRangeLimits()
        {
            TimeRange.LowLimit = 0u;
            TimeRange.HighLimit = player.GetRecordDurationInNanoseconds();
        }

        public override string GetTitle()
        {
            return "Eye Gaze Heatmap";
        }
    }
}