using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PLUME.UI
{
    [RequireComponent(typeof(MainWindowUI))]
    public class MainWindowPresenter : MonoBehaviour
    {
        public Player player;
        public FreeCamera freeCamera;

        private MainWindowUI _mainWindowUI;

        private void Awake()
        {
            _mainWindowUI = GetComponent<MainWindowUI>();
        }

        private void Start()
        {
            _mainWindowUI.PreviewRenderAspectRatio.RegisterCallback<FocusInEvent>(OnPreviewRenderFocused);
            _mainWindowUI.PreviewRenderAspectRatio.RegisterCallback<FocusOutEvent>(OnPreviewRenderUnfocused);
            _mainWindowUI.PreviewRenderAspectRatio.RegisterCallback<NavigationMoveEvent>(OnPreviewRenderNavigationMove);
            _mainWindowUI.PreviewRenderAspectRatio.RegisterCallback<KeyDownEvent>(OnPreviewRenderKeyDown);
            _mainWindowUI.PreviewRender.style.backgroundImage =
                Background.FromRenderTexture(freeCamera.GetRenderTexture());

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

            _mainWindowUI.CreateMarkers();
            _mainWindowUI.CreatePhysiologicalTracks();

            _mainWindowUI.RefreshTimelineScale();
            _mainWindowUI.RefreshTimelineTimeIndicator();
            _mainWindowUI.RefreshTimelineCursor();
            _mainWindowUI.RefreshPlayPauseButton();
            _mainWindowUI.RefreshSpeed();

            _mainWindowUI.Timeline.focusable = true;
            _mainWindowUI.Timeline.Focus();
            
            // By default, show 30s of the record in the timeline
            _mainWindowUI.Timeline.ShowTimePeriod(0, 30_000_000_000);

            player.GetPlayerContext().updatedHierarchy += OnHierarchyUpdateEvent;
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

                    if (updateParentEvt.parentTransformIdentifier == null)
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

        private void OnPreviewRenderFocused(FocusInEvent evt)
        {
            if (freeCamera != null)
            {
                freeCamera.Disabled = false;
            }
        }

        private void OnPreviewRenderUnfocused(FocusOutEvent evt)
        {
            if (freeCamera != null)
            {
                freeCamera.Disabled = true;
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

        public void Update()
        {
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