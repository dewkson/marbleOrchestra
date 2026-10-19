using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Data-driven look of the 3D marble (see 0061): one asset per design
    /// (Create > MarbleOrchestra > Marble Design), swapped by assigning a
    /// different one to MarbleController. The surface is either an
    /// authored Material (full freedom) or a procedural texture generated
    /// from this asset's own colors/pattern settings - so a new design
    /// needs no code. With no asset assigned, MarbleController falls back
    /// to CreateWalnut() (the default look).
    /// </summary>
    [CreateAssetMenu(fileName = "MarbleDesign", menuName = "MarbleOrchestra/Marble Design")]
    public class MarbleDesign : ScriptableObject
    {
        public enum PatternType { Wood, Veined }

        [Tooltip("If set, used as-is and all procedural settings below are ignored.")]
        [SerializeField] private Material materialOverride;
        [SerializeField] private PatternType pattern = PatternType.Wood;
        [SerializeField] private Color baseColor = new Color(0.30f, 0.19f, 0.11f);
        [Tooltip("Wood: color of the grain lines. Veined: color of the veins.")]
        [SerializeField] private Color accentColor = new Color(0.25f, 0.15f, 0.08f);
        [Tooltip("Lines / veins across the texture - higher = finer.")]
        [SerializeField, Min(1f)] private float patternFrequency = 18f;
        [Tooltip("How strongly noise bends the pattern.")]
        [SerializeField, Min(0f)] private float turbulence = 1.5f;
        [Tooltip("0 = only the base color, 1 = full accent color at the pattern's peaks.")]
        [SerializeField, Range(0f, 1f)] private float patternStrength = 0.35f;
        [SerializeField, Range(0f, 1f)] private float smoothness = 0.45f;
        [SerializeField, Range(0f, 1f)] private float metallic = 0f;
        [SerializeField, Min(16)] private int textureSize = 256;

        private Material cachedMaterial;

        /// The shared material for this design - built once, then reused by
        /// every marble (they'd otherwise each bake an identical texture).
        public Material GetMaterial()
        {
            if (cachedMaterial != null) return cachedMaterial;
            cachedMaterial = materialOverride != null ? materialOverride : BuildMaterial();
            return cachedMaterial;
        }

        private void OnValidate()
        {
            if (cachedMaterial != null && cachedMaterial != materialOverride) Destroy(cachedMaterial);
            cachedMaterial = null; // rebuilt on next use after an Inspector edit
        }

        /// Default design: dark walnut with a very light grain.
        public static MarbleDesign CreateWalnut()
        {
            MarbleDesign design = CreateInstance<MarbleDesign>();
            design.name = "Walnut (default)";
            return design; // field initializers above are the walnut look
        }

        /// The former pink marble look, kept as a ready alternative.
        public static MarbleDesign CreatePinkMarble()
        {
            MarbleDesign design = CreateInstance<MarbleDesign>();
            design.name = "Pink Marble";
            design.pattern = PatternType.Veined;
            design.baseColor = new Color(0.95f, 0.75f, 0.85f);
            design.accentColor = new Color(0.70f, 0.15f, 0.42f);
            design.patternFrequency = 14f;
            design.turbulence = 4f;
            design.patternStrength = 1f;
            design.smoothness = 0.55f;
            design.textureSize = 128;
            return design;
        }

        private Material BuildMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material material = new Material(shader) { name = name };

            Texture2D texture = BuildTexture();
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            else material.mainTexture = texture;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);

            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);

            return material;
        }

        private Texture2D BuildTexture()
        {
            int size = Mathf.Max(16, textureSize);
            Texture2D texture = new Texture2D(size, size) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float t = pattern == PatternType.Wood ? WoodValue(u, v) : VeinValue(u, v);
                    pixels[y * size + x] = Color.Lerp(baseColor, accentColor, t * patternStrength);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// Stretched, wavy growth lines: the grain runs along u (around the
        /// sphere's equator) and is bent by low-frequency noise, with a
        /// little fine fibre noise on top so it doesn't look printed.
        private float WoodValue(float u, float v)
        {
            float warp = Turbulence(u * 2f, v * 2f, 3) * turbulence;
            float line = Mathf.Sin((v * patternFrequency + warp) * Mathf.PI * 2f) * 0.5f + 0.5f;
            line = line * line; // thin lines instead of even bands
            float fibre = Mathf.PerlinNoise(u * 6f, v * 80f);
            return Mathf.Clamp01(line * 0.7f + fibre * 0.3f);
        }

        /// Classic Perlin-marble recipe: turbulence-distorted sine bands.
        private float VeinValue(float u, float v)
        {
            float wave = Mathf.Sin((u + v) * patternFrequency * Mathf.PI + turbulence * Turbulence(u, v, 4));
            return 1f - (wave * 0.5f + 0.5f); // 1 = vein color
        }

        private static float Turbulence(float u, float v, int octaves)
        {
            float value = 0f;
            float freq = 1f;
            float weight = 1f;
            for (int i = 0; i < octaves; i++)
            {
                value += Mathf.PerlinNoise(u * freq, v * freq) * weight;
                freq *= 2f;
                weight *= 0.5f;
            }
            return value;
        }
    }
}
