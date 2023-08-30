using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace PLUME.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainWindowUI : MonoBehaviour
    {
        public Player player;

        public UIDocument document;
        
        public TimelineElement Timeline { get; private set; }
        public TimeFieldElement TimeIndicator { get; private set; }
        public TimeScaleElement TimeScale { get; private set; }

        public VisualElement MediaController { get; private set; }
        public ToggleButton PlayPauseButton { get; private set; }
        public Button StopButton { get; private set; }

        public Button DecreaseSpeedButton { get; private set; }
        public Button IncreaseSpeedButton { get; private set; }
        public TextField SpeedTextField { get; private set; }

        public ToggleButton ToggleMaximizePreviewButton { get; private set; }
        public VisualElement Preview { get; private set; }
        public AspectRatioContainerElement PreviewRender { get; private set; }
        
        public TreeView HierarchyTree { get; private set; }

        public CollapseBarElement RecordsCollapseBar { get; private set; }
        public CollapseBarElement TimelineCollapseBar { get; private set; }
        public CollapseBarElement AnalysisCollapseBar { get; private set; }

        public TwoPaneSplitView VerticalSplitView { get; private set; }
        public TwoPaneSplitView HorizontalSplitView1 { get; private set; }
        public TwoPaneSplitView HorizontalSplitView2 { get; private set; }

        private void Awake()
        {
            var root = document.rootVisualElement;
            Timeline = root.Q<TimelineElement>("timeline");
            TimeIndicator = Timeline.Q<TimeFieldElement>();
            TimeScale = Timeline.Q<TimeScaleElement>();

            MediaController = root.Q<VisualElement>("media-controller");
            PlayPauseButton = MediaController.Q<ToggleButton>("play-pause-btn");
            StopButton = MediaController.Q<Button>("stop-btn");

            DecreaseSpeedButton = MediaController.Q<Button>("decrease-speed-btn");
            IncreaseSpeedButton = MediaController.Q<Button>("increase-speed-btn");
            SpeedTextField = MediaController.Q<TextField>("speed-textfield");

            ToggleMaximizePreviewButton = MediaController.Q<ToggleButton>("toggle-maximize-preview-btn");
            Preview = root.Q("preview");
            PreviewRender = root.Q("preview").Q<AspectRatioContainerElement>("render");
            
            HierarchyTree = root.Q<TreeView>("hierarchy-tree");
            HierarchyTree.SetRootItems(new List<TreeViewItemData<Transform>>());
            HierarchyTree.makeItem = () =>
            {
                Profiler.BeginSample("Make item");
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;
                container.Add(new Label {name = "name"});

                var recordIdLabel = new Label {name = "record-id"};
                recordIdLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                recordIdLabel.style.color = new StyleColor(Color.gray);
                container.Add(recordIdLabel);
                Profiler.EndSample();
                return container;
            };
            HierarchyTree.bindItem = (element, i) =>
            {
                var t = HierarchyTree.GetItemDataForIndex<Transform>(i);
                if (t == null)
                    return;
                var recordIdentifier = player.GetPlayerContext().GetRecordIdentifier(t.gameObject.GetInstanceID());
                element.Q<Label>("name").text = t.gameObject.name;
                element.Q<Label>("name").style.color = t.gameObject.activeInHierarchy ? new StyleColor(Color.white) : new StyleColor(Color.gray);
                element.Q<Label>("record-id").text = recordIdentifier ?? "N/A";
            };

            RecordsCollapseBar = root.Q<CollapseBarElement>("records-collapse-bar");
            TimelineCollapseBar = root.Q<CollapseBarElement>("timeline-collapse-bar");
            AnalysisCollapseBar = root.Q<CollapseBarElement>("analysis-collapse-bar");

            VerticalSplitView = root.Q<TwoPaneSplitView>("vertical-pane-split-view");
            HorizontalSplitView1 = root.Q<TwoPaneSplitView>("horizontal-pane-split-view-1");
            HorizontalSplitView2 = root.Q<TwoPaneSplitView>("horizontal-pane-split-view-2");

            RecordsCollapseBar.toggledCollapse += collapsed =>
            {
                if (collapsed)
                    HorizontalSplitView1.CollapseChild(0);
                else
                    HorizontalSplitView1.UnCollapse();
            };
            
            AnalysisCollapseBar.toggledCollapse += collapsed =>
            {
                if (collapsed)
                    HorizontalSplitView2.CollapseChild(1);
                else
                    HorizontalSplitView2.UnCollapse();
            };
            
            TimelineCollapseBar.toggledCollapse += collapsed =>
            {
                if (collapsed)
                    VerticalSplitView.CollapseChild(1);
                else
                    VerticalSplitView.UnCollapse();
            };
        }

        public bool IsTimeIndicatorFocused()
        {
            return TimeIndicator != null && TimeIndicator.IsFocused();
        }

        public void RefreshTimelineScale()
        {
            Timeline.Duration = player.GetRecordDurationInNanoseconds();
            Timeline.TimeDivisionDuration = 1_000_000_000u;
            Timeline.TimeDivisionWidth = 100;
            Timeline.Repaint();
        }

        public void RefreshTimelineTimeIndicator()
        {
            TimeIndicator.SetTimeWithoutNotify(player.GetCurrentPlayTimeInNanoseconds());
        }

        public void RefreshTimelineCursor()
        {
            Timeline.SetCursorTime(player.GetCurrentPlayTimeInNanoseconds());
        }

        public void RefreshPlayPauseButton()
        {
        
            PlayPauseButton.SetStateWithoutNotify(player.IsPlaying());
        }

        public void RefreshSpeed()
        {
            SpeedTextField.value = player.GetPlaySpeed().ToString("x#.###", CultureInfo.InvariantCulture);
        }

        public void CollapseSidePanels()
        {
            RecordsCollapseBar.Collapse();
            TimelineCollapseBar.Collapse();
            AnalysisCollapseBar.Collapse();
        }

        public void InflateSidePanels()
        {
            RecordsCollapseBar.Inflate();
            TimelineCollapseBar.Inflate();
            AnalysisCollapseBar.Inflate();
        }

        public T Q<T>(string name = null, string className = null) where T : VisualElement
        {
            return document.rootVisualElement.Q<T>(name, className);
        }

        public VisualElement Q(string name = null, string className = null)
        {
            return document.rootVisualElement.Q(name, className);
        }

    }
}