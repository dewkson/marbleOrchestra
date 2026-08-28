using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Draws a pipe purely from its Direction flags (a "+" of arms towards
    /// each connected side). No hand-authored art needed per pipe type.
    /// </summary>
    public class PipeVisual : MonoBehaviour
    {
        [SerializeField] private float armThickness = 0.15f;
        [SerializeField] private float hubSize = 0.2f;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private Color connectedTint = new Color(0.35f, 0.65f, 1f);
        [SerializeField] private Color pathCompleteTint = new Color(0.35f, 0.9f, 0.45f);
        [SerializeField] private Color roleLabelColor = Color.white;
        [SerializeField] private float roleLabelCharacterSize = 0.05f;
        [SerializeField] private int roleLabelFontSize = 32;
        [SerializeField] private float roleLabelTopOffset = 0.38f; // distance from card center to the label, so it sits above the pipe arms instead of on top of them

        private static Sprite pixelSprite;

        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer hubRenderer;
        private readonly SpriteRenderer[] armRenderers = new SpriteRenderer[4];
        private TextMesh roleLabel;
        private Color baseColor = Color.white;
        private Color baseBackgroundColor = Color.gray;

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
            if (hubRenderer == null) return;
            hubRenderer.color = highlighted ? highlightColor : baseColor;
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

            MeshRenderer labelRenderer = label.GetComponent<MeshRenderer>();
            labelRenderer.sortingOrder = 4;
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
