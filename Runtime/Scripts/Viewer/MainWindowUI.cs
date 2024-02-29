using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using PLUME.Sample;
using PLUME.Sample.Common;
using PLUME.Sample.LSL;
using PLUME.UI.Element;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;

namespace PLUME.Viewer
{
    [RequireComponent(typeof(UIDocument))]
    public class MainWindowUI : MonoBehaviour
    {
        public Player.Player player;

        public UIDocument document;

        public VisualElement LoadingPanel { get; private set; }

        public VisualElement ViewerPanel { get; private set; }

        public TimelineElement Timeline { get; private set; }
        public TimeFieldElement TimeIndicator { get; private set; }
        public TimeScaleElement TimeScale { get; private set; }

        public VisualElement MediaController { get; private set; }
        public ToggleButton PlayPauseButton { get; private set; }
        public Button StopButton { get; private set; }
        public Button ResetViewButton { get; private set; }

        public Button DecreaseSpeedButton { get; private set; }
        public Button IncreaseSpeedButton { get; private set; }
        public TextField SpeedTextField { get; private set; }
        public EnumField CameraEnumField { get; private set; }

        public ToggleButton ToggleMaximizePreviewButton { get; private set; }
        public VisualElement Preview { get; private set; }
        public AspectRatioContainerElement PreviewRenderAspectRatio { get; private set; }
        public VisualElement PreviewRender { get; private set; }

        public TreeView HierarchyTree { get; private set; }

        public CollapseBarElement RecordsCollapseBar { get; private set; }
        public CollapseBarElement TimelineCollapseBar { get; private set; }
        public CollapseBarElement AnalysisCollapseBar { get; private set; }

        public VisualElement AnalysisContainer { get; private set; }
        public VisualElement MarkersContainer { get; private set; }
        public VisualElement MarkersListView { get; private set; }

        public TwoPaneSplitView VerticalSplitView { get; private set; }
        public TwoPaneSplitView HorizontalSplitView1 { get; private set; }
        public TwoPaneSplitView HorizontalSplitView2 { get; private set; }

        private void Awake()
        {
            var root = document.rootVisualElement;

            LoadingPanel = root.Q("loading-panel");
            ViewerPanel = root.Q("viewer");

            Timeline = ViewerPanel.Q<TimelineElement>("timeline");
            TimeIndicator = Timeline.Q<TimeFieldElement>();
            TimeScale = Timeline.Q<TimeScaleElement>();

            MediaController = ViewerPanel.Q<VisualElement>("media-controller");
            PlayPauseButton = MediaController.Q<ToggleButton>("play-pause-btn");
            StopButton = MediaController.Q<Button>("stop-btn");
            ResetViewButton = MediaController.Q<Button>("reset-view");

            DecreaseSpeedButton = MediaController.Q<Button>("decrease-speed-btn");
            IncreaseSpeedButton = MediaController.Q<Button>("increase-speed-btn");
            SpeedTextField = MediaController.Q<TextField>("speed-textfield");
            CameraEnumField = MediaController.Q<EnumField>("camera-selection");

            ToggleMaximizePreviewButton = MediaController.Q<ToggleButton>("toggle-maximize-preview-btn");
            Preview = ViewerPanel.Q("preview");
            PreviewRenderAspectRatio = ViewerPanel.Q("preview").Q<AspectRatioContainerElement>("aspect-ratio");
            PreviewRender = PreviewRenderAspectRatio.Q<VisualElement>("render");

            HierarchyTree = ViewerPanel.Q<TreeView>("hierarchy-tree");
            HierarchyTree.makeItem = () =>
            {
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;
                container.Add(new Label { name = "name" });
                return container;
            };
            HierarchyTree.bindItem = (element, i) =>
            {
                var t = HierarchyTree.GetItemDataForIndex<Transform>(i);
                if (t == null)
                    return;
                element.Q<Label>("name").text = t.gameObject.name;
                element.Q<Label>("name").style.color = t.gameObject.activeInHierarchy
                    ? new StyleColor(Color.white)
                    : new StyleColor(Color.gray);

                // Dirty fix for selection not working in tree view
                element.RegisterCallback<MouseDownEvent>(_ => { HierarchyTree.SetSelection(i); });
            };
            HierarchyTree.SetRootItems(new List<TreeViewItemData<Transform>>());

            HierarchyTree.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.ctrlKey && evt.keyCode == KeyCode.C)
                {
                    var selectedItems = HierarchyTree.GetSelectedItems<Transform>();

                    GUIUtility.systemCopyBuffer = string.Join(",",
                        selectedItems.Select(t =>
                            player.GetPlayerContext().GetRecordIdentifier(t.data.gameObject.GetInstanceID())));
                }
            });

            RecordsCollapseBar = ViewerPanel.Q<CollapseBarElement>("records-collapse-bar");
            TimelineCollapseBar = ViewerPanel.Q<CollapseBarElement>("timeline-collapse-bar");
            AnalysisCollapseBar = ViewerPanel.Q<CollapseBarElement>("analysis-collapse-bar");

            AnalysisContainer = ViewerPanel.Q("analysis-container");

            MarkersContainer = ViewerPanel.Q("markers");
            MarkersListView = MarkersContainer.Q<ListView>("markers-list");

            VerticalSplitView = ViewerPanel.Q<TwoPaneSplitView>("vertical-pane-split-view");
            HorizontalSplitView1 = ViewerPanel.Q<TwoPaneSplitView>("horizontal-pane-split-view-1");
            HorizontalSplitView2 = ViewerPanel.Q<TwoPaneSplitView>("horizontal-pane-split-view-2");

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

        public void RefreshMarkers()
        {
            var markerEntryUxml = Resources.Load<VisualTreeAsset>("UI/Uxml/markers_list_entry");

            Timeline.ClearMarkers();
            MarkersListView.Clear();

            var markersLoader = player.GetMarkersLoader();
            var markerColors = new Dictionary<string, Color>();

            var groupedMarkerTimelineElements = new Dictionary<string, List<TimelineMarkerElement>>();
            var groupedMarkerSamples = new Dictionary<string, List<UnpackedSample>>();

            foreach (var s in markersLoader.All())
            {
                if (s.Payload is not Marker marker)
                    continue;

                if (!markerColors.TryGetValue(marker.Label, out var markerColor))
                {
                    Random.InitState(marker.Label.GetHashCode());
                    markerColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                    markerColors.Add(marker.Label, markerColor);
                }

                var markerElement = new TimelineMarkerElement();
                markerElement.SetColor(markerColor);
                markerElement.SetTime(s.Timestamp!.Value);
                Timeline.AddMarker(markerElement);

                if (groupedMarkerSamples.ContainsKey(marker.Label))
                {
                    groupedMarkerTimelineElements[marker.Label].Add(markerElement);
                    groupedMarkerSamples[marker.Label].Add(s);
                }
                else
                {
                    groupedMarkerTimelineElements.Add(marker.Label, new List<TimelineMarkerElement> { markerElement });
                    groupedMarkerSamples.Add(marker.Label, new List<UnpackedSample> { s });
                }
            }

            foreach (var (markerLabel, markerSamples) in groupedMarkerSamples.OrderBy(pair => pair.Key))
            {
                var entry = markerEntryUxml.Instantiate().Q("markers-list-entry");

                var showAllBtn = MarkersContainer.Q<Button>("show-all");
                var hideAllBtn = MarkersContainer.Q<Button>("hide-all");

                var prevBtn = entry.Q("marker-snap").Q<Button>("prev");
                var nextBtn = entry.Q("marker-snap").Q<Button>("next");

                entry.Q("marker-color").style.backgroundColor = markerColors[markerLabel];
                entry.Q<Label>("marker-label").text = markerLabel;

                showAllBtn.clicked += () => { entry.Q<Toggle>("marker-toggle").value = true; };

                hideAllBtn.clicked += () => { entry.Q<Toggle>("marker-toggle").value = false; };

                entry.Q<Toggle>("marker-toggle").value = true;
                entry.Q<Toggle>("marker-toggle").RegisterValueChangedCallback(evt =>
                {
                    foreach (var timelineMarkerElement in groupedMarkerTimelineElements[markerLabel])
                    {
                        timelineMarkerElement.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                    }

                    prevBtn.SetEnabled(evt.newValue);
                    nextBtn.SetEnabled(evt.newValue);
                });
                prevBtn.clicked += () =>
                {
                    // TODO: move this into the MainWindowPresenter
                    var t = player.GetCurrentPlayTimeInNanoseconds();

                    var snapMarker = markerSamples.OrderBy(s => s.Timestamp).LastOrDefault(s => s.Timestamp < t);
                    if (snapMarker != null)
                    {
                        player.JumpToTime(snapMarker.Timestamp!.Value);
                    }
                };
                nextBtn.clicked += () =>
                {
                    var t = player.GetCurrentPlayTimeInNanoseconds();
                    var snapMarker = markerSamples.OrderBy(s => s.Timestamp).FirstOrDefault(s => s.Timestamp > t);
                    if (snapMarker != null)
                    {
                        player.JumpToTime(snapMarker.Timestamp!.Value);
                    }
                };
                MarkersListView.Q<ScrollView>().Add(entry);
            }
        }

        public void RefreshPhysiologicalTracks()
        {
            Timeline.ClearTracks();

            var physioSignalsLoader = player.GetPhysiologicalSignalsLoader();

            var tracks = new Dictionary<string, TimelinePhysiologicalSignalTrackElement[]>();

            foreach (var s in physioSignalsLoader.AllOfType<StreamOpen>())
            {
                if (s.Payload is not StreamOpen streamOpen)
                    continue;

                var xmlInfo = XElement.Parse(streamOpen.XmlHeader);
                var streamName = xmlInfo.Element("name")!.Value;
                var channelFormat = xmlInfo.Element("channel_format")!.Value;
                var channelCount = int.Parse(xmlInfo.Element("channel_count")!.Value);
                var nominalSamplingRate = float.Parse(xmlInfo.Element("nominal_srate")!.Value);

                if (channelFormat == "string")
                    continue;

                Random.InitState(streamName.GetHashCode());
                var streamColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                var channelsTrack = new TimelinePhysiologicalSignalTrackElement[channelCount];

                for (var channelIdx = 0; channelIdx < channelCount; channelIdx++)
                {
                    Random.InitState((streamName + channelIdx).GetHashCode());
                    var channelColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                    channelsTrack[channelIdx] = new TimelinePhysiologicalSignalTrackElement();
                    channelsTrack[channelIdx].SetName(streamName);
                    channelsTrack[channelIdx].SetFrequency(nominalSamplingRate);
                    channelsTrack[channelIdx].SetChannel(channelIdx);
                    channelsTrack[channelIdx].SetStreamColor(streamColor);
                    channelsTrack[channelIdx].SetChannelColor(channelColor);
                    Timeline.AddTrack(channelsTrack[channelIdx]);
                }

                tracks.Add(streamOpen.StreamInfo.LslStreamId, channelsTrack);
            }

            var streamSamples = physioSignalsLoader.AllOfType<StreamSample>()
                .OrderBy(s => s.Timestamp)
                .GroupBy(s => ((StreamSample)s.Payload).StreamInfo.LslStreamId);

            foreach (var streamSamplesGroup in streamSamples)
            {
                var streamId = streamSamplesGroup.Key;

                if (!tracks.TryGetValue(streamId, out var physioTracks))
                    continue;

                for (var channelIdx = 0; channelIdx < physioTracks.Length; ++channelIdx)
                {
                    var min = float.MaxValue;
                    var max = float.MinValue;

                    var points = new List<Vector2>();

                    foreach (var unpackedSample in streamSamplesGroup)
                    {
                        if (unpackedSample.Payload is not StreamSample sample)
                            continue;

                        switch (sample.ValuesCase)
                        {
                            case StreamSample.ValuesOneofCase.FloatValue:
                                min = Math.Min(min, sample.FloatValue.Value[channelIdx]);
                                max = Math.Max(max, sample.FloatValue.Value[channelIdx]);
                                points.Add(
                                    new Vector2(unpackedSample.Timestamp!.Value, sample.FloatValue.Value[channelIdx]));
                                break;
                            case StreamSample.ValuesOneofCase.DoubleValue:
                                min = Math.Min(min, (float)sample.DoubleValue.Value[channelIdx]);
                                max = Math.Max(max, (float)sample.DoubleValue.Value[channelIdx]);
                                points.Add(new Vector2(unpackedSample.Timestamp!.Value,
                                    (float)sample.DoubleValue.Value[channelIdx]));
                                break;
                            case StreamSample.ValuesOneofCase.Int8Value:
                                min = Math.Min(min, sample.Int8Value.Value[channelIdx]);
                                max = Math.Max(max, sample.Int8Value.Value[channelIdx]);
                                points.Add(new Vector2(unpackedSample.Timestamp!.Value,
                                    sample.Int8Value.Value[channelIdx]));
                                break;
                            case StreamSample.ValuesOneofCase.Int16Value:
                                min = Math.Min(min, sample.Int16Value.Value[channelIdx]);
                                max = Math.Max(max, sample.Int16Value.Value[channelIdx]);
                                points.Add(
                                    new Vector2(unpackedSample.Timestamp!.Value, sample.Int16Value.Value[channelIdx]));
                                break;
                            case StreamSample.ValuesOneofCase.Int32Value:
                                min = Math.Min(min, sample.Int32Value.Value[channelIdx]);
                                max = Math.Max(max, sample.Int32Value.Value[channelIdx]);
                                points.Add(
                                    new Vector2(unpackedSample.Timestamp!.Value, sample.Int32Value.Value[channelIdx]));
                                break;
                            case StreamSample.ValuesOneofCase.Int64Value:
                                min = Math.Min(min, sample.Int64Value.Value[channelIdx]);
                                max = Math.Max(max, sample.Int64Value.Value[channelIdx]);
                                points.Add(
                                    new Vector2(unpackedSample.Timestamp!.Value, sample.Int64Value.Value[channelIdx]));
                                break;
                            case StreamSample.ValuesOneofCase.None:
                            case StreamSample.ValuesOneofCase.StringValue:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    physioTracks[channelIdx].SetPoints(points);
                    physioTracks[channelIdx].SetMinValue(min);
                    physioTracks[channelIdx].SetMaxValue(max);
                }
            }
        }

        public void RefreshTimelineScale()
        {
            Timeline.Duration = player.GetRecordDurationInNanoseconds();
            Timeline.TicksPerDivision = 10;
            Timeline.TimeDivisionDuration = 100000000;
            Timeline.TimeDivisionWidth = 100;
        }

        public void RefreshTimelineTimeIndicator()
        {
            TimeIndicator.SetTimeWithoutNotify(player.GetCurrentPlayTimeInNanoseconds());
        }

        public void RefreshTimelineCursor()
        {
            Timeline.SetCurrentTime(player.GetCurrentPlayTimeInNanoseconds());

            if (player.IsPlaying())
            {
                Timeline.KeepTimeCursorInView();
            }
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
    }
}