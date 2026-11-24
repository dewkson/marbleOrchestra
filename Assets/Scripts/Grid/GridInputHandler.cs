using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Card Swap mode (see 0063) - two ways to swap two pipes: click pipe A, then click pipe B - or
    /// press pipe A, drag onto pipe B, release (see 0004). Purely
    /// translates input into PathGrid.SwapCards calls; no gameplay logic
    /// lives here. Disabled while MarbleController is simulating - the
    /// track can only be edited during planning. Lives on its own
    /// GameObject; grid and marbleController are wired in the Inspector or
    /// auto-found at Awake.
    /// A press only turns into a drag once the pointer has moved past
    /// dragThresholdPixels - short of that, release is treated as a plain
    /// click, so both flows share one press without either stealing the
    /// other's gesture. While dragging, the source pipe visually follows
    /// the cursor (raycast against the grid's own plane, so this works
    /// under any grid/camera orientation - see 0029) and the pipe
    /// currently hovered as a drop target gets the same SetSelected
    /// highlight the click-click flow already uses.
    /// Raycasts in full 3D (Physics.Raycast via ScreenPointToRay) rather
    /// than Physics2D, since the planning camera/grid no longer have to be
    /// front-on (see 0029's top-down planning view) - Physics2D would
    /// silently ignore any rotation beyond the Z axis.
    /// 15-Puzzle mode (see 0066/0067) - the press drives
    /// PathGrid.SlideCards instead, and the card never follows the cursor
    /// freely: a click slides the card (and every card between it and the
    /// free cell) only if exactly ONE direction is open - with several, a
    /// click would have to guess, so it does nothing. A swipe picks the
    /// direction explicitly: as soon as the pointer has moved far enough
    /// along one grid axis, that slide runs as the same controlled
    /// animation a click plays. Hovering a card that a click would move
    /// highlights every card that slide would push along.
    /// </summary>
    public class GridInputHandler : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private PathGrid grid;
        [SerializeField] private MarbleController marbleController;
        [SerializeField] private float dragThresholdPixels = 8f;
        [SerializeField] private AudioClip swapSound;
        [SerializeField, Range(0f, 1f)] private float swapSoundVolume = 1f;

        private PathPipe selectedPipe;

        private PathPipe pressedPipe;
        private Collider pressedCollider;
        private Vector2 pressScreenPos;
        private bool isDragging;
        private Vector3 dragOriginalLocalPosition;
        private PathPipe dragHoverTarget;

        [Tooltip("15-Puzzle: how far (in cells) the pointer has to be swiped along one axis before it counts as a slide in that direction.")]
        [SerializeField, Range(0.05f, 1f)] private float slideDragThresholdCells = 0.25f;

        private readonly List<PathPipe> slidePreview = new List<PathPipe>(); // cards highlighted as "would move" (see 0067)
        private readonly List<Vector2Int> slideLine = new List<Vector2Int>();
        private Vector3 swipeStartGridLocal; // where the 15-Puzzle press started, in the grid's own local space
        private bool swipeMoved; // pointer left dragThresholdPixels - release is no longer a click
        private bool swipeDone; // this press already triggered its slide - ignore the rest of it

        private bool IsFifteenPuzzle => grid != null && grid.IsFifteenPuzzle;

        private void Awake()
        {
            if (grid == null) grid = FindAnyObjectByType<PathGrid>();
            if (marbleController == null) marbleController = FindAnyObjectByType<MarbleController>();
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void Update()
        {
            if (marbleController != null && (marbleController.IsPlaying || marbleController.IsSwitchingSubLevel))
            {
                ClearSlidePreview();
                return;
            }
            if (Pointer.current == null) return;

            if (IsFifteenPuzzle)
            {
                UpdateSlideInput();
                return;
            }

            if (Pointer.current.press.wasPressedThisFrame)
            {
                BeginPress();
            }
            else if (pressedPipe != null && Pointer.current.press.isPressed)
            {
                ContinuePress();
            }
            else if (pressedPipe != null && Pointer.current.press.wasReleasedThisFrame)
            {
                EndPress();
            }
        }

        /// 15-Puzzle counterpart of the Card Swap press/drag handling above
        /// (see the class summary): press, then either release in place
        /// (click) or swipe along one axis - never a free drag.
        private void UpdateSlideInput()
        {
            if (Pointer.current.press.wasPressedThisFrame)
            {
                BeginSlidePress();
            }
            else if (pressedPipe != null && Pointer.current.press.isPressed)
            {
                ContinueSlidePress();
            }
            else if (pressedPipe != null && Pointer.current.press.wasReleasedThisFrame)
            {
                EndSlidePress();
            }
            else if (pressedPipe == null)
            {
                ShowSlidePreview(RaycastPipe(), Direction.None);
            }
        }

        private void BeginSlidePress()
        {
            PathPipe clicked = RaycastPipe();
            if (clicked == null || clicked.IsLocked) return;

            // No free cell anywhere in its row/column (or a locked card in
            // the way) - neither a click nor a swipe could move it.
            if (!grid.CanSlideAny(clicked.Coord)) return;

            Vector2 screenPos = Pointer.current.position.ReadValue();
            if (!RaycastGridPlane(screenPos, out Vector3 worldPos)) return;

            pressedPipe = clicked;
            pressScreenPos = screenPos;
            swipeStartGridLocal = grid.transform.InverseTransformPoint(worldPos);
            swipeMoved = false;
            swipeDone = false;
        }

        /// Fires the slide the moment the swipe has gone far enough along
        /// one axis - the card then glides exactly one cell along that axis
        /// (PathPipe.SlideTo), never further than the free cell allows. A
        /// swipe in a blocked direction does nothing, and the press keeps
        /// watching in case the pointer turns towards an open one.
        private void ContinueSlidePress()
        {
            if (swipeDone) return;

            Vector2 screenPos = Pointer.current.position.ReadValue();
            if (!swipeMoved && (screenPos - pressScreenPos).sqrMagnitude >= dragThresholdPixels * dragThresholdPixels)
                swipeMoved = true;

            if (!RaycastGridPlane(screenPos, out Vector3 worldPos)) return;

            Direction direction = GetSwipeDirection(grid.transform.InverseTransformPoint(worldPos) - swipeStartGridLocal);
            if (direction == Direction.None)
            {
                ShowSlidePreview(pressedPipe, Direction.None);
                return;
            }

            if (SlideAndPlaySound(pressedPipe.Coord, direction))
            {
                swipeDone = true;
                ClearSlidePreview();
            }
            else
            {
                ShowSlidePreview(null, Direction.None);
            }
        }

        /// A release that never became a swipe is a click - it only moves
        /// the card if exactly one direction is open (see
        /// FifteenPuzzleRules.TryGetOnlySlide).
        private void EndSlidePress()
        {
            if (!swipeDone && !swipeMoved) SlideAndPlaySound(pressedPipe.Coord, Direction.None);

            ClearSlidePreview();
            pressedPipe = null;
        }

        /// The grid axis the pointer has moved furthest along since the
        /// press (in the grid's own local space, so it holds for any
        /// grid/camera orientation - see 0029), or None while it's still
        /// within slideDragThresholdCells of where the press started.
        private Direction GetSwipeDirection(Vector3 gridLocalDelta)
        {
            float threshold = grid.CellSize * slideDragThresholdCells;
            if (Mathf.Abs(gridLocalDelta.x) < threshold && Mathf.Abs(gridLocalDelta.y) < threshold) return Direction.None;

            if (Mathf.Abs(gridLocalDelta.x) >= Mathf.Abs(gridLocalDelta.y)) return gridLocalDelta.x > 0f ? Direction.Right : Direction.Left;
            return gridLocalDelta.y > 0f ? Direction.Up : Direction.Down;
        }

        private void BeginPress()
        {
            PathPipe clicked = RaycastPipe();
            if (clicked == null || clicked.IsLocked) return;

            pressedPipe = clicked;
            pressScreenPos = Pointer.current.position.ReadValue();
            isDragging = false;
        }

        private void ContinuePress()
        {
            Vector2 currentScreenPos = Pointer.current.position.ReadValue();

            if (!isDragging)
            {
                if ((currentScreenPos - pressScreenPos).sqrMagnitude < dragThresholdPixels * dragThresholdPixels) return;
                BeginDrag();
            }

            if (RaycastGridPlane(currentScreenPos, out Vector3 worldPos)) pressedPipe.transform.position = worldPos;

            UpdateHoverHighlight(RaycastPipe());
        }

        private void EndPress()
        {
            if (isDragging) EndDrag();
            else HandleClick(pressedPipe);

            pressedPipe = null;
            isDragging = false;
        }

        private void BeginDrag()
        {
            isDragging = true;
            dragOriginalLocalPosition = pressedPipe.transform.localPosition;
            pressedPipe.SetSelected(true);
            pressedPipe.SetDragElevated(true);

            // Disabled for the whole drag so it can't shadow itself once it
            // sits exactly on top of the cursor - RaycastPipe would
            // otherwise hit the dragged pipe's own (now co-located)
            // collider instead of whatever's underneath.
            pressedCollider = pressedPipe.GetComponent<Collider>();
            if (pressedCollider != null) pressedCollider.enabled = false;

            // A drag takes over from any pending click-click selection so
            // the two flows never fight over the same pipe.
            if (selectedPipe != null)
            {
                selectedPipe.SetSelected(false);
                selectedPipe = null;
            }
        }

        private void EndDrag()
        {
            ClearHoverHighlight();
            pressedPipe.SetSelected(false);
            pressedPipe.SetDragElevated(false);
            pressedPipe.transform.localPosition = dragOriginalLocalPosition;

            PathPipe target = RaycastPipe();

            if (pressedCollider != null) pressedCollider.enabled = true;
            pressedCollider = null;

            if (target != null && target != pressedPipe && !target.IsLocked)
                SwapAndPlaySound(pressedPipe.Coord, target.Coord);
        }

        private bool SlideAndPlaySound(Vector2Int from, Direction direction)
        {
            if (!grid.SlideCards(from, direction)) return false;
            if (swapSound != null) AudioSource.PlayClipAtPoint(swapSound, targetCamera.transform.position, swapSoundVolume);
            return true;
        }

        /// Highlights every card a slide from `pipe` would move (see 0067) -
        /// for Direction.None only if a click would actually move it (one
        /// open direction), otherwise only that direction. Nothing is
        /// highlighted if the card can't move that way.
        private void ShowSlidePreview(PathPipe pipe, Direction direction)
        {
            slideLine.Clear();
            if (pipe != null && !pipe.IsLocked) grid.TryGetSlide(pipe.Coord, direction, slideLine, out _);

            bool unchanged = slidePreview.Count == slideLine.Count;
            for (int i = 0; unchanged && i < slideLine.Count; i++)
                unchanged = slidePreview[i] != null && slidePreview[i].Coord == slideLine[i];
            if (unchanged) return;

            ClearSlidePreview();
            foreach (Vector2Int coord in slideLine)
            {
                PathPipe linePipe = grid.GetPipe(coord);
                if (linePipe == null) continue;
                linePipe.SetSelected(true);
                slidePreview.Add(linePipe);
            }
        }

        private void ClearSlidePreview()
        {
            foreach (PathPipe pipe in slidePreview)
                if (pipe != null) pipe.SetSelected(false);
            slidePreview.Clear();
        }

        private void HandleClick(PathPipe clicked)
        {
            if (selectedPipe == null)
            {
                selectedPipe = clicked;
                selectedPipe.SetSelected(true);
                return;
            }

            if (selectedPipe == clicked)
            {
                selectedPipe.SetSelected(false);
                selectedPipe = null;
                return;
            }

            SwapAndPlaySound(selectedPipe.Coord, clicked.Coord);
            selectedPipe.SetSelected(false);
            selectedPipe = null;
        }

        private void SwapAndPlaySound(Vector2Int a, Vector2Int b)
        {
            if (grid.SwapCards(a, b) && swapSound != null)
                AudioSource.PlayClipAtPoint(swapSound, targetCamera.transform.position, swapSoundVolume);
        }

        private void UpdateHoverHighlight(PathPipe hovered)
        {
            PathPipe validHover = hovered != null && hovered != pressedPipe && !hovered.IsLocked ? hovered : null;
            if (validHover == dragHoverTarget) return;

            if (dragHoverTarget != null) dragHoverTarget.SetSelected(false);
            dragHoverTarget = validHover;
            if (dragHoverTarget != null) dragHoverTarget.SetSelected(true);
        }

        private void ClearHoverHighlight()
        {
            if (dragHoverTarget != null) dragHoverTarget.SetSelected(false);
            dragHoverTarget = null;
        }

        private PathPipe RaycastPipe()
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();
            Ray ray = targetCamera.ScreenPointToRay(screenPos);

            return Physics.Raycast(ray, out RaycastHit hit) ? hit.collider.GetComponent<PathPipe>() : null;
        }

        /// Intersects the given screen position's camera ray with the
        /// grid's own plane (normal = grid.transform.forward, since
        /// PathGrid.CellToLocalPosition always returns local Z = 0) - so
        /// the dragged pipe can follow the cursor in world space
        /// regardless of whether the grid is set up front-on or top-down
        /// (see 0029).
        private bool RaycastGridPlane(Vector2 screenPos, out Vector3 worldPos)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPos);
            Plane plane = new Plane(grid.transform.forward, grid.transform.position);

            if (plane.Raycast(ray, out float distance))
            {
                worldPos = ray.GetPoint(distance);
                return true;
            }

            worldPos = default;
            return false;
        }
    }
}
