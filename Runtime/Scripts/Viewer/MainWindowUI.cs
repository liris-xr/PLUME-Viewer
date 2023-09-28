﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using PLUME.Sample.Common;
using PLUME.Sample.LSL;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;

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
        public AspectRatioContainerElement PreviewRenderAspectRatio { get; private set; }
        public VisualElement PreviewRender { get; private set; }

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
            PreviewRenderAspectRatio = root.Q("preview").Q<AspectRatioContainerElement>("aspect-ratio");
            PreviewRender = PreviewRenderAspectRatio.Q<VisualElement>("render");

            HierarchyTree = root.Q<TreeView>("hierarchy-tree");
            HierarchyTree.SetRootItems(new List<TreeViewItemData<Transform>>());
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
            };

            HierarchyTree.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.ctrlKey && evt.keyCode == KeyCode.C)
                {
                    var selectedItems = HierarchyTree.GetSelectedItems<Transform>();
                    
                    Debug.Log(selectedItems.ElementAt(0).id);
                    
                    GUIUtility.systemCopyBuffer = string.Join(",",
                        selectedItems.Select(t =>
                            player.GetPlayerContext().GetRecordIdentifier(t.data.gameObject.GetInstanceID())));
                }
            });

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

        public void CreateMarkers()
        {
            var markersLoader = player.GetMarkersLoader();
            var markerColors = new Dictionary<string, Color>();

            foreach (var s in markersLoader.All())
            {
                if (s.Payload is not Marker marker)
                    continue;

                if (!markerColors.TryGetValue(marker.Label, out var markerColor))
                {
                    markerColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                    markerColors.Add(marker.Label, markerColor);
                }

                var markerElement = new TimelineMarkerElement();
                markerElement.SetColor(markerColor);
                markerElement.SetTime(s.Header.Time);
                Timeline.AddMarker(markerElement);
            }
        }

        public void CreatePhysiologicalTracks()
        {
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

                var streamColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                var channelsTrack = new TimelinePhysiologicalSignalTrackElement[channelCount];

                for (var channelIdx = 0; channelIdx < channelCount; channelIdx++)
                {
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
                .OrderBy(s => s.Header!.Time)
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
                                    new Vector2(unpackedSample.Header!.Time, sample.FloatValue.Value[channelIdx]));
                                break;
                            case StreamSample.ValuesOneofCase.DoubleValue:
                                min = Math.Min(min, (float)sample.DoubleValue.Value[channelIdx]);
                                max = Math.Max(max, (float)sample.DoubleValue.Value[channelIdx]);
                                points.Add(new Vector2(unpackedSample.Header!.Time,
                                    (float)sample.DoubleValue.Value[channelIdx]));
                                break;
                            case StreamSample.ValuesOneofCase.Int8Value:
                                min = Math.Min(min, sample.Int8Value.Value[channelIdx]);
                                max = Math.Max(max, sample.Int8Value.Value[channelIdx]);
                                points.Add(new Vector2(unpackedSample.Header!.Time,
                                    sample.Int8Value.Value[channelIdx]));
                                break;
                            case StreamSample.ValuesOneofCase.Int16Value:
                                min = Math.Min(min, sample.Int16Value.Value[channelIdx]);
                                max = Math.Max(max, sample.Int16Value.Value[channelIdx]);
                                points.Add(
                                    new Vector2(unpackedSample.Header!.Time, sample.Int16Value.Value[channelIdx]));
                                break;
                            case StreamSample.ValuesOneofCase.Int32Value:
                                min = Math.Min(min, sample.Int32Value.Value[channelIdx]);
                                max = Math.Max(max, sample.Int32Value.Value[channelIdx]);
                                points.Add(
                                    new Vector2(unpackedSample.Header!.Time, sample.Int32Value.Value[channelIdx]));
                                break;
                            case StreamSample.ValuesOneofCase.Int64Value:
                                min = Math.Min(min, sample.Int64Value.Value[channelIdx]);
                                max = Math.Max(max, sample.Int64Value.Value[channelIdx]);
                                points.Add(
                                    new Vector2(unpackedSample.Header!.Time, sample.Int64Value.Value[channelIdx]));
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