using UnityEngine;
using UnityEngine.InputSystem;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Click card A, then click card B, to swap them. Purely translates
    /// clicks into PathGrid.SwapCards calls; no gameplay logic lives here.
    /// </summary>
    [RequireComponent(typeof(PathGrid))]
    public class GridInputHandler : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        private PathGrid grid;
        private PathCard selectedCard;

        private void Awake()
        {
            grid = GetComponent<PathGrid>();
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

            PathCard clicked = RaycastCard();
            if (clicked == null) return;

            if (selectedCard == null)
            {
                selectedCard = clicked;
                selectedCard.SetSelected(true);
                return;
            }

            if (selectedCard == clicked)
            {
                selectedCard.SetSelected(false);
                selectedCard = null;
                return;
            }

            grid.SwapCards(selectedCard.Coord, clicked.Coord);
            selectedCard.SetSelected(false);
            selectedCard = null;
        }

        private PathCard RaycastCard()
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector2 worldPos = targetCamera.ScreenToWorldPoint(screenPos);

            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            return hit.collider != null ? hit.collider.GetComponent<PathCard>() : null;
        }
    }
}
