using System;
using System.Linq;
using UnityEngine;

namespace PLUME.UI.Analysis
{
    public class InteractionHeatmapAnalysisModulePresenter : MonoBehaviour
    {
        public Player player;

        public InteractionHeatmapAnalysisModule module;
        public InteractionHeatmapAnalysisModuleUI ui;

        public void Start()
        {
            ui.GenerateButton.clicked += OnClickGenerate;
            ui.InteractorsIdsTextField.value =
                "f73a0170-aa81-4305-806b-3eadcb864441,32fbd30a-f825-45d9-b929-3f16d339fefa";
            ui.InteractablesIdsTextField.value = "";
            ui.InteractionTypeDropdownField.value = "Hover";

            ui.clickedDeleteResult += OnClickDeleteResult;
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

            var interactorsIds = ui.InteractorsIdsTextField.value.Trim().Split(",").Where(s => s.Length > 0).ToArray();
            var interactablesIds =
                ui.InteractablesIdsTextField.value.Trim().Split(",").Where(s => s.Length > 0).ToArray();

            var interactionType = (InteractionType)ui.InteractionTypeDropdownField.index;
            var startTime = ui.TimeRange.StartTime;
            var endTime = ui.TimeRange.EndTime;

            var generationParameters = new InteractionAnalysisModuleParameters
            {
                InteractorsIds = interactorsIds,
                InteractablesIds = interactablesIds,
                InteractionType = interactionType,
                StartTime = startTime,
                EndTime = endTime
            };

            ui.GenerateButton.text = "Generating...";
            ui.GenerateButton.SetEnabled(false);

            var onFinishCallback = new Action<InteractionHeatmapAnalysisResult>(result =>
            {
                ui.GenerateButton.text = "Generate";
                ui.GenerateButton.SetEnabled(true);
                module.AddResult(result);
                module.SetVisibleResult(result);

                if (player.GetVisibleHeatmapModule() != module)
                {
                    player.SetVisibleHeatmapModule(module);
                }

                ui.RefreshResults();
            });

            StartCoroutine(module.GenerateHeatmap(player.GetFramesLoader(), generationParameters, onFinishCallback));
        }

        public void FixedUpdate()
        {
            // TODO: remove, quick and dirty fix to prevent heatmap results to be visible while another type of heatmap is being generated
            var otherModuleGenerating = player.GetModuleGenerating() != null && player.GetModuleGenerating() != module;
            ui.GenerateButton.SetEnabled(!otherModuleGenerating);
        }

        private void OnClickDeleteResult(InteractionHeatmapAnalysisResult result)
        {
            if (module.GetVisibleResult() == result && player.GetVisibleHeatmapModule() == module)
            {
                player.SetVisibleHeatmapModule(null);
            }

            module.RemoveResult(result);
            ui.RefreshResults();
        }

        private void OnToggleResultVisibility(InteractionHeatmapAnalysisResult result, bool visible)
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