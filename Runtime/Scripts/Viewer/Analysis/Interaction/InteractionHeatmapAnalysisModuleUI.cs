using System;
using System.Linq;
using PLUME.UI.Element;
using UnityEngine.UIElements;

namespace PLUME.Viewer.Analysis.Interaction
{
    public class InteractionHeatmapAnalysisModuleUI : AnalysisModuleWithResultsUI<InteractionHeatmapAnalysisModule,
        InteractionHeatmapAnalysisResult>
    {
        public Player.Player player;

        public VisualTreeAsset resultEntryTemplate;

        public Action<InteractionHeatmapAnalysisResult> clickedDeleteResult;
        public Action<InteractionHeatmapAnalysisResult, bool> toggledResultVisibility;

        public Button GenerateButton { get; private set; }
        public TextField InteractorsIdsTextField { get; private set; }
        public TextField InteractablesIdsTextField { get; private set; }
        public DropdownField InteractionTypeDropdownField { get; private set; }
        public TimeRangeElement TimeRange { get; private set; }

        protected new void Awake()
        {
            base.Awake();

            GenerateButton = Options.Q<Button>("generate-btn");
            InteractorsIdsTextField = Options.Q<TextField>("interactors");
            InteractablesIdsTextField = Options.Q<TextField>("interactables");
            InteractionTypeDropdownField = Options.Q<DropdownField>("interaction-type");
            TimeRange = Options.Q<TimeRangeElement>("time-range");
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
                var startTimeStr = TimeSpan.FromMilliseconds(result.GenerationParameters.StartTime / 1_000_000.0)
                    .ToString(@"hh\:mm\:ss\.fff");
                var endTimeStr = TimeSpan.FromMilliseconds(result.GenerationParameters.EndTime / 1_000_000.0)
                    .ToString(@"hh\:mm\:ss\.fff");
                var interactionType = result.GenerationParameters.InteractionType.ToString();
                var interactors = string.Join(",", result.GenerationParameters.InteractorsIds);
                var interactables = result.GenerationParameters.InteractablesIds.Length == 0
                    ? "All"
                    : string.Join(",", result.GenerationParameters.InteractablesIds);

                resultEntry.Q("interactors").Q<Label>("value").text = interactors;
                resultEntry.Q("interactables").Q<Label>("value").text = interactables;
                resultEntry.Q("interaction-type").Q<Label>("value").text = interactionType;
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


        public override string GetTitle()
        {
            return "Interaction Heatmap";
        }
    }
}