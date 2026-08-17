using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Draws a card purely from its Direction flags (a "+" of arms towards
    /// each connected side). No hand-authored art needed per card type.
    /// </summary>
    public class CardVisual : MonoBehaviour
    {
        [SerializeField] private float armThickness = 0.15f;
        [SerializeField] private float hubSize = 0.2f;
        [SerializeField] private Color highlightColor = Color.yellow;

        private static Sprite pixelSprite;

        private SpriteRenderer hubRenderer;
        private readonly SpriteRenderer[] armRenderers = new SpriteRenderer[4];
        private Color baseColor = Color.white;

        public void Refresh(CardDefinition definition)
        {
            EnsureBuilt();

            Direction connections = definition != null ? definition.Connections : Direction.None;
            baseColor = definition != null ? definition.Color : Color.white;

            hubRenderer.color = baseColor;
            hubRenderer.enabled = connections != Direction.None;

            for (int i = 0; i < DirectionExtensions.All.Length; i++)
            {
                bool connected = (connections & DirectionExtensions.All[i]) != 0;
                armRenderers[i].enabled = connected;
                armRenderers[i].color = baseColor;
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            if (hubRenderer == null) return;
            hubRenderer.color = highlighted ? highlightColor : baseColor;
        }

        private void EnsureBuilt()
        {
            if (hubRenderer != null) return;

            Sprite sprite = GetPixelSprite();

            GameObject hub = new GameObject("Hub");
            hub.transform.SetParent(transform, false);
            hubRenderer = hub.AddComponent<SpriteRenderer>();
            hubRenderer.sprite = sprite;
            hub.transform.localScale = new Vector3(hubSize, hubSize, 1f);

            for (int i = 0; i < DirectionExtensions.All.Length; i++)
            {
                Direction dir = DirectionExtensions.All[i];
                GameObject arm = new GameObject($"Arm_{dir}");
                arm.transform.SetParent(transform, false);

                SpriteRenderer renderer = arm.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;

                Vector2 offset = (Vector2)dir.ToGridOffset() * 0.25f;
                arm.transform.localPosition = offset;

                bool horizontal = dir == Direction.Left || dir == Direction.Right;
                arm.transform.localScale = horizontal
                    ? new Vector3(0.5f, armThickness, 1f)
                    : new Vector3(armThickness, 0.5f, 1f);

                armRenderers[i] = renderer;
            }
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
