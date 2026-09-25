using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Draws a pipe purely from its Direction flags (a "+" of arms towards
    /// each connected side). No hand-authored art needed per pipe type.
    /// Every card sits in a frame (see 0030) whose color carries all the
    /// state feedback - level default, locked, correctly connected, path
    /// complete (start reaches goal), and the click/drag selection
    /// highlight (see 0004) - using the colors and width configured once
    /// on the LevelData, instead of tinting the card face.
    /// </summary>
    public class PipeVisual : MonoBehaviour
    {
        [SerializeField] private float armThickness = 0.15f;
        [SerializeField] private float hubSize = 0.2f;
        [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.1f); // frame color while selected/dragged/hovered
        [SerializeField] private Color roleLabelColor = Color.white;
        [SerializeField] private float roleLabelCharacterSize = 0.05f;
        [SerializeField] private int roleLabelFontSize = 32;
        [SerializeField] private float roleLabelTopOffset = 0.38f; // distance from card center to the label, so it sits above the pipe arms instead of on top of them
        [SerializeField] private int dragSortingBoost = 100; // added to every renderer's sortingOrder while dragged, so the card draws over every other pipe regardless of layout

        private static Sprite pixelSprite;

        private SpriteRenderer frameRenderer;
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer imageRenderer;
        private SpriteRenderer hubRenderer;
        private readonly SpriteRenderer[] armRenderers = new SpriteRenderer[4];
        private TextMesh roleLabel;
        private MeshRenderer roleLabelRenderer;
        private Color baseColor = Color.white;
        private LevelData style;
        private bool locked;
        private bool highlighted;
        private CellConnectivity connectivity = CellConnectivity.Disconnected;
        private bool dragElevated;

        public void Refresh(PipeDefinition definition, LevelData levelStyle)
        {
            EnsureBuilt();
            style = levelStyle;

            Direction connections = definition != null ? definition.Connections : Direction.None;
            baseColor = definition != null ? definition.Color : Color.white;
            locked = definition != null && definition.Locked;
            backgroundRenderer.color = definition != null ? definition.BackgroundColor : Color.gray;

            Sprite image = definition != null ? definition.CardImage : null;
            ApplyFrameAndImage(image);

            bool showPipes = image == null || style.ShowPipesOnImageCards;

            hubRenderer.color = baseColor;
            hubRenderer.enabled = showPipes && connections != Direction.None;

            for (int i = 0; i < DirectionExtensions.All.Length; i++)
            {
                bool connected = (connections & DirectionExtensions.All[i]) != 0;
                armRenderers[i].enabled = showPipes && connected;
                armRenderers[i].color = baseColor;
            }

            PipeRole role = definition != null ? definition.Role : PipeRole.Normal;
            roleLabel.text = role switch
            {
                PipeRole.Start => "Start",
                PipeRole.Goal => "Ziel",
                _ => string.Empty
            };

            UpdateFrameColor();
        }

        /// Static, non-interactive card for a blocked cell that has a
        /// picture assigned (see 0030): no hub/arms, just the framed image.
        public void RefreshBlocked(CardLook look, LevelData levelStyle)
        {
            EnsureBuilt();
            style = levelStyle;
            locked = false;

            backgroundRenderer.color = new Color(0.18f, 0.18f, 0.18f);
            hubRenderer.enabled = false;
            foreach (SpriteRenderer arm in armRenderers) arm.enabled = false;
            roleLabel.text = string.Empty;

            ApplyFrameAndImage(look.Image);
            UpdateFrameColor();
        }

        /// The frame always exists (its width is the level's card border
        /// width); the background - and the picture on top of it, if any -
        /// is inset by that width.
        private void ApplyFrameAndImage(Sprite image)
        {
            float inset = 1f - 2f * style.CardBorderThickness;
            backgroundRenderer.transform.localScale = Vector3.one * inset;

            imageRenderer.enabled = image != null;
            if (image == null) return;

            imageRenderer.sprite = image;
            Vector2 size = image.bounds.size;
            imageRenderer.transform.localScale = new Vector3(
                size.x > 0f ? inset / size.x : 1f,
                size.y > 0f ? inset / size.y : 1f,
                1f);
        }

        public void SetHighlighted(bool isHighlighted)
        {
            highlighted = isHighlighted;
            UpdateFrameColor();
        }

        public void SetConnectivity(CellConnectivity newConnectivity)
        {
            connectivity = newConnectivity;
            UpdateFrameColor();
        }

        /// Highlight beats connectivity beats locked beats the default
        /// frame color - a locked Start/Goal is usually connected too, and
        /// that live state is the more useful thing to show.
        private void UpdateFrameColor()
        {
            if (frameRenderer == null || style == null) return;

            if (highlighted) frameRenderer.color = highlightColor;
            else if (connectivity == CellConnectivity.PathComplete) frameRenderer.color = style.PathCompleteBorderColor;
            else if (connectivity == CellConnectivity.Connected) frameRenderer.color = style.ConnectedBorderColor;
            else if (locked) frameRenderer.color = style.LockedBorderColor;
            else frameRenderer.color = style.CardBorderColor;
        }

        /// While dragged (see 0004's GridInputHandler), the card should
        /// visually cover every other pipe it passes over, regardless of
        /// draw order - bump every one of this pipe's renderers well above
        /// the sortingOrder range any other pipe uses (max 5, see
        /// EnsureBuilt), then restore exactly on release.
        public void SetDragElevated(bool elevated)
        {
            if (backgroundRenderer == null || dragElevated == elevated) return;
            dragElevated = elevated;

            int delta = elevated ? dragSortingBoost : -dragSortingBoost;
            frameRenderer.sortingOrder += delta;
            backgroundRenderer.sortingOrder += delta;
            imageRenderer.sortingOrder += delta;
            hubRenderer.sortingOrder += delta;
            roleLabelRenderer.sortingOrder += delta;
            foreach (SpriteRenderer arm in armRenderers) arm.sortingOrder += delta;
        }

        private void EnsureBuilt()
        {
            if (hubRenderer != null) return;

            Sprite sprite = GetPixelSprite();

            // Full-size frame behind the (inset) background: only the rim
            // shows, made of the same 1x1 pixel sprite - no extra art.
            GameObject frame = new GameObject("CardFrame");
            frame.transform.SetParent(transform, false);
            frameRenderer = frame.AddComponent<SpriteRenderer>();
            frameRenderer.sprite = sprite;
            frameRenderer.sortingOrder = -1;

            GameObject background = new GameObject("Background");
            background.transform.SetParent(transform, false);
            backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = sprite;
            backgroundRenderer.sortingOrder = 0;

            GameObject image = new GameObject("CardImage");
            image.transform.SetParent(transform, false);
            imageRenderer = image.AddComponent<SpriteRenderer>();
            imageRenderer.sortingOrder = 1;
            imageRenderer.enabled = false;

            GameObject hub = new GameObject("Hub");
            hub.transform.SetParent(transform, false);
            hubRenderer = hub.AddComponent<SpriteRenderer>();
            hubRenderer.sprite = sprite;
            hubRenderer.sortingOrder = 3;
            hub.transform.localScale = new Vector3(hubSize, hubSize, 1f);

            for (int i = 0; i < DirectionExtensions.All.Length; i++)
            {
                Direction dir = DirectionExtensions.All[i];
                GameObject arm = new GameObject($"Arm_{dir}");
                arm.transform.SetParent(transform, false);

                SpriteRenderer renderer = arm.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 2;

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
            roleLabelRenderer.sortingOrder = 5;
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
