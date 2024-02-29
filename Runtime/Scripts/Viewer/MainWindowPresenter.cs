using System;
using System.Collections.Generic;
using PLUME.Viewer;
using UnityEngine;
using UnityEngine.UIElements;

namespace PLUME.UI
{
    // TODO: refactor by splitting into multiple classes
    [RequireComponent(typeof(MainWindowUI))]
    public class MainWindowPresenter : MonoBehaviour
    {
        public Player player;

        private MainWindowUI _mainWindowUI;

        private bool _loading = true;

        private void Awake()
        {
            _mainWindowUI = GetComponent<MainWindowUI>();
        }

        private void Start()
        {
            _mainWindowUI.PreviewRenderAspectRatio.RegisterCallback<FocusInEvent>(OnPreviewRenderFocused);
            _mainWindowUI.PreviewRenderAspectRatio.RegisterCallback<FocusOutEvent>(OnPreviewRenderUnfocused);

            _mainWindowUI.PreviewRender.RegisterCallback<MouseEnterEvent>(OnPreviewRenderMouseEnter);
            _mainWindowUI.PreviewRender.RegisterCallback<MouseLeaveEvent>(OnPreviewRenderMouseLeave);

            _mainWindowUI.PreviewRenderAspectRatio.RegisterCallback<NavigationMoveEvent>(OnPreviewRenderNavigationMove);
            _mainWindowUI.PreviewRenderAspectRatio.RegisterCallback<KeyDownEvent>(OnPreviewRenderKeyDown);
            _mainWindowUI.PreviewRender.style.backgroundImage =
                Background.FromRenderTexture(player.PreviewRenderTexture);

            _mainWindowUI.Timeline.RegisterCallback<KeyDownEvent>(OnPlayPauseKeyDown);
            _mainWindowUI.PlayPauseButton.RegisterCallback<KeyDownEvent>(OnPlayPauseKeyDown);

            _mainWindowUI.PlayPauseButton.toggled += OnTogglePlayPause;
            _mainWindowUI.TimeIndicator.timeChanged += OnTimeIndicatorChanged;
            _mainWindowUI.TimeScale.dragged += OnTimeScaleDragged;
            _mainWindowUI.TimeScale.clicked += OnTimeScaleClicked;
            _mainWindowUI.StopButton.clicked += OnClickStop;
            _mainWindowUI.DecreaseSpeedButton.clicked += OnClickDecreaseSpeed;
            _mainWindowUI.IncreaseSpeedButton.clicked += OnClickIncreaseSpeed;
            _mainWindowUI.ToggleMaximizePreviewButton.toggled += OnClickToggleMaximizePreview;

            _mainWindowUI.CameraEnumField.SetValueWithoutNotify(player.GetCurrentPreviewCamera().GetCameraType());
            _mainWindowUI.CameraEnumField.RegisterValueChangedCallback(OnCameraSelectionChanged);

            RefreshResetViewButton();
            _mainWindowUI.ResetViewButton.clicked += OnClickResetView;

            _mainWindowUI.Timeline.focusable = true;
            player.GetPlayerContext().updatedHierarchy += OnHierarchyUpdateEvent;
        }

        private void OnClickResetView()
        {
            var cam = player.GetCurrentPreviewCamera();

            if (cam != null)
            {
                cam.ResetView();
            }
        }

        private void RefreshResetViewButton()
        {
            _mainWindowUI.ResetViewButton.SetEnabled(player.GetCurrentPreviewCamera().GetCameraType() !=
                                                     PreviewCameraType.Main);
        }

        private void OnCameraSelectionChanged(ChangeEvent<Enum> evt)
        {
            var cameraType = (PreviewCameraType)evt.newValue;

            switch (cameraType)
            {
                case PreviewCameraType.Free:
                    player.SetCurrentPreviewCamera(player.GetFreeCamera());
                    break;
                case PreviewCameraType.TopView:
                    player.SetCurrentPreviewCamera(player.GetTopViewCamera());
                    break;
                case PreviewCameraType.Main:
                    player.SetCurrentPreviewCamera(player.GetMainCamera());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            RefreshResetViewButton();
        }

        private void OnHierarchyUpdateEvent(IHierarchyUpdateEvent evt)
        {
            var controller = _mainWindowUI.HierarchyTree.viewController;
            var ctx = player.GetPlayerContext();

            switch (evt)
            {
                case HierarchyUpdateCreateTransformEvent createEvt:
                {
                    var id = createEvt.transformIdentifier.GetHashCode();
                    var instanceId = player.GetPlayerContext().GetReplayInstanceId(createEvt.transformIdentifier);

                    if (instanceId.HasValue)
                    {
                        var t = ObjectExtensions.FindObjectFromInstanceID(instanceId.Value) as Transform;

                        if (t != null)
                        {
                            var itemData = new TreeViewItemData<Transform>(id, t);
                            _mainWindowUI.HierarchyTree.AddItem(itemData);
                        }
                    }

                    break;
                }
                case HierarchyUpdateDestroyTransformEvent destroyEvt:
                {
                    var id = destroyEvt.transformIdentifier.GetHashCode();
                    _mainWindowUI.HierarchyTree.TryRemoveItem(id);
                    break;
                }
                case HierarchyUpdateSiblingIndexEvent siblingUpdateEvt:
                {
                    var id = siblingUpdateEvt.transformIdentifier.GetHashCode();

                    var t = _mainWindowUI.HierarchyTree.GetItemDataForId<Transform>(id);

                    if (t != null)
                    {
                        if (t.parent == null)
                        {
                            controller.Move(id, -1, siblingUpdateEvt.siblingIndex);
                        }
                        else
                        {
                            var parentId = ctx.GetRecordIdentifier(t.parent.GetInstanceID()).GetHashCode();
                            controller.Move(id, parentId, siblingUpdateEvt.siblingIndex);
                        }
                    }

                    break;
                }
                case HierarchyUpdateEnabledEvent enabledUpdateEvt:
                {
                    var id = enabledUpdateEvt.transformIdentifier.GetHashCode();
                    var index = controller.GetIndexForId(id);
                    if (index != -1)
                    {
                        _mainWindowUI.HierarchyTree.RefreshItem(index);
                    }

                    break;
                }
                case HierarchyUpdateParentEvent updateParentEvt:
                {
                    var id = updateParentEvt.transformIdentifier.GetHashCode();

                    // Null Guid
                    if (updateParentEvt.parentTransformIdentifier == "00000000-0000-0000-0000-000000000000")
                    {
                        controller.Move(id, -1, updateParentEvt.siblingIdx);
                    }
                    else
                    {
                        var parentId = updateParentEvt.parentTransformIdentifier.GetHashCode();
                        controller.Move(id, parentId, updateParentEvt.siblingIdx);
                    }

                    break;
                }
                case HierarchyUpdateResetEvent:
                {
                    _mainWindowUI.HierarchyTree.ClearSelection();
                    _mainWindowUI.HierarchyTree.SetRootItems(new List<TreeViewItemData<Transform>>());
                    _mainWindowUI.HierarchyTree.Rebuild();
                    break;
                }
            }
        }

        private void OnTimeIndicatorChanged(ChangeEvent<ulong> evt)
        {
            player.JumpToTime(Math.Clamp(evt.newValue, 0, player.GetRecordDurationInNanoseconds()));
        }

        private void OnTimeScaleDragged(ulong time)
        {
            player.JumpToTime(Math.Clamp(time, 0, player.GetRecordDurationInNanoseconds()));
        }

        private void OnTimeScaleClicked(ulong time)
        {
            player.JumpToTime(Math.Clamp(time, 0, player.GetRecordDurationInNanoseconds()));
        }

        private void OnPlayPauseKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Space)
            {
                player.TogglePlaying();
                _mainWindowUI.RefreshPlayPauseButton();
            }
        }

        private void OnPreviewRenderKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                _mainWindowUI.PreviewRenderAspectRatio.Blur();
                _mainWindowUI.Timeline.Focus();
            }
        }

        private void OnPreviewRenderNavigationMove(NavigationMoveEvent evt)
        {
            // Prevent navigation keys to change focus to another pane (WASD, arrows, joystick, ...)
            evt.PreventDefault();
        }

        private void OnPreviewRenderMouseEnter(MouseEnterEvent evt)
        {
            if (player.GetTopViewCamera() != null)
            {
                player.GetTopViewCamera().ZoomDisabled = false;
            }
        }

        private void OnPreviewRenderMouseLeave(MouseLeaveEvent evt)
        {
            if (player.GetTopViewCamera() != null)
            {
                player.GetTopViewCamera().ZoomDisabled = true;
            }
        }

        private void OnPreviewRenderFocused(FocusInEvent evt)
        {
            if (player.GetFreeCamera() != null)
            {
                player.GetFreeCamera().InputDisabled = false;
            }

            if (player.GetTopViewCamera() != null)
            {
                player.GetTopViewCamera().InputDisabled = false;
            }
        }

        private void OnPreviewRenderUnfocused(FocusOutEvent evt)
        {
            if (player.GetFreeCamera() != null)
            {
                player.GetFreeCamera().InputDisabled = true;
            }

            if (player.GetTopViewCamera() != null)
            {
                player.GetTopViewCamera().InputDisabled = true;
                player.GetTopViewCamera().ZoomDisabled = true;
            }
        }

        private void OnClickToggleMaximizePreview(bool state)
        {
            if (state)
                _mainWindowUI.CollapseSidePanels();
            else
                _mainWindowUI.InflateSidePanels();
        }

        private void OnClickDecreaseSpeed()
        {
            player.SetPlaySpeed(Mathf.Max(0.25f, player.GetPlaySpeed() - 0.25f));
            _mainWindowUI.RefreshSpeed();
        }

        private void OnClickIncreaseSpeed()
        {
            player.SetPlaySpeed(Mathf.Min(5, player.GetPlaySpeed() + 0.25f));
            _mainWindowUI.RefreshSpeed();
        }

        private void OnTogglePlayPause(bool state)
        {
            if (state && !player.IsPlaying())
                player.StartPlaying();
            else if (!state && player.IsPlaying())
                player.PausePlaying();
        }

        private void OnClickStop()
        {
            player.StopPlaying();
            _mainWindowUI.RefreshPlayPauseButton();
        }

        private void ShowViewerPanel()
        {
            _mainWindowUI.ViewerPanel.style.display = DisplayStyle.Flex;
            _mainWindowUI.LoadingPanel.style.display = DisplayStyle.None;

            _mainWindowUI.RefreshTimelineScale();
            _mainWindowUI.RefreshTimelineTimeIndicator();
            _mainWindowUI.RefreshTimelineCursor();
            _mainWindowUI.RefreshPlayPauseButton();
            _mainWindowUI.RefreshSpeed();

            _mainWindowUI.RefreshMarkers();
            _mainWindowUI.RefreshPhysiologicalTracks();

            // By default, show 30s of the record in the timeline
            _mainWindowUI.Timeline.ShowTimePeriod(0, 60_000_000_000);
            _mainWindowUI.Timeline.Focus();

            _mainWindowUI.HierarchyTree.ClearSelection();
        }

        public void Update()
        {
            if (_loading)
            {
                var isLoading = !player.GetPlayerAssets().IsLoaded() || !player.GetMarkersLoader().FinishedLoading ||
                                !player.GetPhysiologicalSignalsLoader().FinishedLoading;

                if (isLoading)
                {
                    _mainWindowUI.ViewerPanel.style.display = DisplayStyle.None;
                    _mainWindowUI.LoadingPanel.style.display = DisplayStyle.Flex;

                    if (!player.GetPlayerAssets().IsLoaded())
                    {
                        _mainWindowUI.LoadingPanel.Q<ProgressBar>("progress-bar").value =
                            player.GetPlayerAssets().GetLoadingProgress();
                        _mainWindowUI.LoadingPanel.Q<ProgressBar>("progress-bar").title = "Loading asset bundle...";
                    }
                    else if (!player.GetMarkersLoader().FinishedLoading)
                    {
                        _mainWindowUI.LoadingPanel.Q<ProgressBar>("progress-bar").value = 0;
                        _mainWindowUI.LoadingPanel.Q<ProgressBar>("progress-bar").title = "Loading markers...";
                    }
                    else if (!player.GetPhysiologicalSignalsLoader().FinishedLoading)
                    {
                        _mainWindowUI.LoadingPanel.Q<ProgressBar>("progress-bar").value = 0;
                        _mainWindowUI.LoadingPanel.Q<ProgressBar>("progress-bar").title =
                            "Loading physiological signals...";
                    }
                }
                else
                {
                    ShowViewerPanel();
                    _loading = false;
                }
            }

            var isGenerating = player.GetModuleGenerating() != null;
            _mainWindowUI.PlayPauseButton.SetEnabled(!isGenerating);
            _mainWindowUI.StopButton.SetEnabled(!isGenerating);
            _mainWindowUI.DecreaseSpeedButton.SetEnabled(!isGenerating);
            _mainWindowUI.IncreaseSpeedButton.SetEnabled(!isGenerating);

            _mainWindowUI.PreviewRender.Q<Label>("generating-label").style.display =
                isGenerating ? DisplayStyle.Flex : DisplayStyle.None;

            _mainWindowUI.PreviewRender.Q<Label>("free-camera-instructions").style.display =
                player.GetCurrentPreviewCamera() is FreeCamera ? DisplayStyle.Flex : DisplayStyle.None;
            _mainWindowUI.PreviewRender.Q<Label>("top-view-camera-instructions").style.display =
                player.GetCurrentPreviewCamera() is TopViewCamera ? DisplayStyle.Flex : DisplayStyle.None;

            if (!_mainWindowUI.IsTimeIndicatorFocused())
            {
                _mainWindowUI.RefreshTimelineTimeIndicator();
            }

            if (player.IsPlaying() != _mainWindowUI.PlayPauseButton.GetState())
            {
                _mainWindowUI.RefreshPlayPauseButton();
            }

            _mainWindowUI.RefreshTimelineCursor();
        }
    }
}