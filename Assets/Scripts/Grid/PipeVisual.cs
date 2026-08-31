using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Draws a pipe purely from its Direction flags (a "+" of arms towards
    /// each connected side). No hand-authored art needed per pipe type.
    /// "Highlighted" (click-click selection, and drag-and-drop's dragged/
    /// hovered pipes - see 0004) is shown as a white border sprite that
    /// peeks out from behind the card background, rather than tinting the
    /// small center hub - the hub always just shows the pipe's own color.
    /// </summary>
    public class PipeVisual : MonoBehaviour
    {
        [SerializeField] private float armThickness = 0.15f;
        [SerializeField] private float hubSize = 0.2f;
        [SerializeField] private Color borderColor = Color.white;
        [SerializeField] private float borderScale = 1.1f; // relative to the 1x1 background - how far the border peeks out on each side
        [SerializeField] private Color connectedTint = new Color(0.35f, 0.65f, 1f);
        [SerializeField] private Color pathCompleteTint = new Color(0.35f, 0.9f, 0.45f);
        [SerializeField] private Color roleLabelColor = Color.white;
        [SerializeField] private float roleLabelCharacterSize = 0.05f;
        [SerializeField] private int roleLabelFontSize = 32;
        [SerializeField] private float roleLabelTopOffset = 0.38f; // distance from card center to the label, so it sits above the pipe arms instead of on top of them
        [SerializeField] private int dragSortingBoost = 100; // added to every renderer's sortingOrder while dragged, so the card draws over every other pipe regardless of layout

        private static Sprite pixelSprite;

        private SpriteRenderer borderRenderer;
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer hubRenderer;
        private readonly SpriteRenderer[] armRenderers = new SpriteRenderer[4];
        private TextMesh roleLabel;
        private MeshRenderer roleLabelRenderer;
        private Color baseColor = Color.white;
        private Color baseBackgroundColor = Color.gray;
        private bool dragElevated;

        public void Refresh(PipeDefinition definition)
        {
            EnsureBuilt();

            Direction connections = definition != null ? definition.Connections : Direction.None;
            baseColor = definition != null ? definition.Color : Color.white;
            baseBackgroundColor = definition != null ? definition.BackgroundColor : Color.gray;
            backgroundRenderer.color = baseBackgroundColor;

            hubRenderer.color = baseColor;
            hubRenderer.enabled = connections != Direction.None;

            for (int i = 0; i < DirectionExtensions.All.Length; i++)
            {
                bool connected = (connections & DirectionExtensions.All[i]) != 0;
                armRenderers[i].enabled = connected;
                armRenderers[i].color = baseColor;
            }

            PipeRole role = definition != null ? definition.Role : PipeRole.Normal;
            roleLabel.text = role switch
            {
                PipeRole.Start => "Start",
                PipeRole.Goal => "Ziel",
                _ => string.Empty
            };
        }

        public void SetHighlighted(bool highlighted)
        {
            if (borderRenderer == null) return;
            borderRenderer.enabled = highlighted;
        }

        /// While dragged (see 0004's GridInputHandler), the card should
        /// visually cover every other pipe it passes over, regardless of
        /// draw order - bump every one of this pipe's renderers well above
        /// the sortingOrder range any other pipe uses (max 4, see
        /// EnsureBuilt), then restore exactly on release.
        public void SetDragElevated(bool elevated)
        {
            if (backgroundRenderer == null || dragElevated == elevated) return;
            dragElevated = elevated;

            int delta = elevated ? dragSortingBoost : -dragSortingBoost;
            backgroundRenderer.sortingOrder += delta;
            borderRenderer.sortingOrder += delta;
            hubRenderer.sortingOrder += delta;
            roleLabelRenderer.sortingOrder += delta;
            foreach (SpriteRenderer arm in armRenderers) arm.sortingOrder += delta;
        }

        public void SetConnectivity(CellConnectivity connectivity)
        {
            if (backgroundRenderer == null) return;

            switch (connectivity)
            {
                case CellConnectivity.PathComplete:
                    backgroundRenderer.color = pathCompleteTint;
                    break;
                case CellConnectivity.Connected:
                    backgroundRenderer.color = connectedTint;
                    break;
                default:
                    backgroundRenderer.color = baseBackgroundColor;
                    break;
            }
        }

        private void EnsureBuilt()
        {
            if (hubRenderer != null) return;

            Sprite sprite = GetPixelSprite();

            // Behind the background (lower sortingOrder) and slightly
            // larger, so only a thin rim shows around the card's edges
            // when enabled - a border made of the same 1x1 pixel sprite,
            // no extra art needed.
            GameObject border = new GameObject("SelectionBorder");
            border.transform.SetParent(transform, false);
            borderRenderer = border.AddComponent<SpriteRenderer>();
            borderRenderer.sprite = sprite;
            borderRenderer.sortingOrder = -1;
            borderRenderer.color = borderColor;
            borderRenderer.enabled = false;
            border.transform.localScale = Vector3.one * borderScale;

            GameObject background = new GameObject("Background");
            background.transform.SetParent(transform, false);
            backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = sprite;
            backgroundRenderer.sortingOrder = 0;
            background.transform.localScale = Vector3.one;

            GameObject hub = new GameObject("Hub");
            hub.transform.SetParent(transform, false);
            hubRenderer = hub.AddComponent<SpriteRenderer>();
            hubRenderer.sprite = sprite;
            hubRenderer.sortingOrder = 2;
            hub.transform.localScale = new Vector3(hubSize, hubSize, 1f);

            for (int i = 0; i < DirectionExtensions.All.Length; i++)
            {
                Direction dir = DirectionExtensions.All[i];
                GameObject arm = new GameObject($"Arm_{dir}");
                arm.transform.SetParent(transform, false);

                SpriteRenderer renderer = arm.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 1;

                Vector2 offset = (Vector2)dir.ToGridOffset() * 0.25f;
                arm.transform.localPosition = offset;

                bool horizontal = dir == Direction.Left || dir == Direction.Right;
                arm.transform.localScale = horizontal
                    ? new Vector3(0.5f, armThickness, 1f)
                    : new Vector3(armThickness, 0.5f, 1f);

                armRenderers[i] = renderer;
            }

            GameObject label = new GameObject("RoleLabel");
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0f, roleLabelTopOffset, 0f);

            roleLabel = label.AddComponent<TextMesh>();
            roleLabel.text = string.Empty;
            roleLabel.anchor = TextAnchor.UpperCenter;
            roleLabel.alignment = TextAlignment.Center;
            roleLabel.characterSize = roleLabelCharacterSize;
            roleLabel.fontSize = roleLabelFontSize;
            roleLabel.color = roleLabelColor;

            roleLabelRenderer = label.GetComponent<MeshRenderer>();
            roleLabelRenderer.sortingOrder = 4;
        }

        private static Sprite GetPixelSprite()
        {
            if (pixelSprite != null) return pixelSprite;

            Texture2D texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            pixelSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return pixelSprite;
        }
    }
}
