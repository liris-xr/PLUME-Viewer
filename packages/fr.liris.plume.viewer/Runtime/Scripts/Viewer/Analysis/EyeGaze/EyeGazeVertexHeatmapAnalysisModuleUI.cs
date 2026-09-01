using System;
using System.Linq;
using PLUME.UI.Element;
using UnityEngine.UIElements;

namespace PLUME.Viewer.Analysis.EyeGaze
{
    public class EyeGazeVertexHeatmapAnalysisModuleUI
        : AnalysisModuleWithResultsUI<EyeGazeVertexHeatmapAnalysisModule, EyeGazeVertexHeatmapResult>
    {
        public Player.Player player;

        public VisualTreeAsset resultEntryTemplate;

        public Action<EyeGazeVertexHeatmapResult> clickedDeleteResult;
        public Action<EyeGazeVertexHeatmapResult, bool> toggledResultVisibility;

        public Button GenerateButton { get; private set; }
        public ProgressBar GenerationProgressBar { get; private set; }
        public VisualElement GeneratingPanel { get; private set; }
        public Button CancelButton { get; private set; }
        public TextField XrCameraIdTextField { get; private set; }
        public TextField ProjectionReceiversIdsTextField { get; private set; }
        public TextField GazePositionBindingTextField { get; private set; }
        public TextField GazeRotationBindingTextField { get; private set; }
        public Toggle IncludeReceiversChildrenToggle { get; private set; }
        public TimeRangeElement TimeRange { get; private set; }
        public EnumField EyeGazeCoordinateSystemEnumField { get; private set; }
        public FloatField FovealAngleField { get; private set; }
        public FloatField NSigmasField { get; private set; }

        protected new void Awake()
        {
            base.Awake();

            GenerateButton = Options.Q<Button>("generate-btn");
            GeneratingPanel = Options.Q("generating");
            CancelButton = GeneratingPanel.Q<Button>("cancel-btn");
            GenerationProgressBar = GeneratingPanel.Q<ProgressBar>("progress-bar");
            XrCameraIdTextField = Options.Q<TextField>("xr-camera");
            ProjectionReceiversIdsTextField = Options.Q<TextField>("projection-receivers");
            GazePositionBindingTextField = Options.Q<TextField>("gaze-position-binding");
            GazeRotationBindingTextField = Options.Q<TextField>("gaze-rotation-binding");
            IncludeReceiversChildrenToggle = Options.Q<Toggle>("include-receivers-children");
            TimeRange = Options.Q<TimeRangeElement>("time-range");
            EyeGazeCoordinateSystemEnumField = Options.Q<EnumField>("coordinate-system");
            FovealAngleField = Options.Q<FloatField>("foveal-angle");
            NSigmasField = Options.Q<FloatField>("n-sigmas");
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
                var receiversStr = string.Join(",", result.Parameters.ReceiversIdentifiers);

                resultEntry.Q("xr-camera").Q<Label>("value").text = result.Parameters.XrCameraIdentifier.ToString();
                resultEntry.Q("projection-receivers").Q<Label>("value").text = receiversStr;
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
            TimeRange.HighLimit = player.Record.Duration;
        }

        public override string GetTitle() => "Eye Gaze Vertex Heatmap";
    }
}
