using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Tunable knobs for TerrainDecoration's moss-clump scatter: how many
    /// clumps spawn per block, which colors they're allowed to take, and
    /// how likely they are to bunch up into clusters instead of scattering
    /// independently. A plain data asset (see LevelData/PipeDefinition's
    /// own ScriptableObject convention) rather than a MonoBehaviour, since
    /// TerrainDecoration itself is a static utility with no GameObject of
    /// its own - one asset can be shared by every TrackBlockSpawner that
    /// wants the same look.
    /// Optional: TrackBlockSpawner falls back to <see cref="Default"/> when
    /// no asset is assigned, so leaving the field empty keeps the original
    /// fixed behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainDecorationSettings", menuName = "MarbleOrchestra/Terrain Decoration Settings")]
    public class TerrainDecorationSettings : ScriptableObject
    {
        [Header("Count")]
        [SerializeField, Min(0)] private int minClumps = 4;
        [SerializeField, Min(0)] private int maxClumps = 9;

        [Header("Size")]
        [SerializeField, Min(0.001f)] private float minRadius = 0.05f;
        [SerializeField, Min(0.001f)] private float maxRadius = 0.09f;

        [Header("Color")]
        [Tooltip("Colors a clump may take. Each clump lerps between two random picks from this list for some variation, so a single entry gives every clump that exact color.")]
        [SerializeField] private Color[] palette = { new Color(0.55f, 0.68f, 0.28f), new Color(0.26f, 0.40f, 0.15f) };

        [Header("Clustering")]
        [Tooltip("Chance [0,1] that a clump spawns near a previously placed one on the same block instead of scattering independently across the whole shoulder.")]
        [SerializeField, Range(0f, 1f)] private float clusterChance = 0.5f;
        [Tooltip("Max distance from the anchor clump a clustered clump can land.")]
        [SerializeField, Min(0f)] private float clusterRadius = 0.15f;

        public int MinClumps => Mathf.Min(minClumps, maxClumps);
        public int MaxClumps => Mathf.Max(minClumps, maxClumps);
        public float MinRadius => Mathf.Min(minRadius, maxRadius);
        public float MaxRadius => Mathf.Max(minRadius, maxRadius);
        public float ClusterChance => clusterChance;
        public float ClusterRadius => clusterRadius;

        /// Picks a random color from the palette, blended with a second
        /// random pick for a bit of continuous variation instead of flat
        /// discrete colors - matches TerrainDecoration's original
        /// light/dark-moss lerp, but over an arbitrary user-chosen set.
        public Color RandomColor()
        {
            if (palette == null || palette.Length == 0) return Color.white;
            Color a = palette[Random.Range(0, palette.Length)];
            Color b = palette[Random.Range(0, palette.Length)];
            return Color.Lerp(a, b, Random.value);
        }

        private static TerrainDecorationSettings defaultInstance;

        /// Lazily-created fallback matching the scatter's original fixed
        /// behavior (4-9 clumps, the old light/dark moss palette, no
        /// clustering bias), used whenever no asset is assigned.
        public static TerrainDecorationSettings Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = CreateInstance<TerrainDecorationSettings>();
                    defaultInstance.hideFlags = HideFlags.DontSave;
                    defaultInstance.clusterChance = 0f; // no clustering unless a designer opts in via a real asset
                }

                return defaultInstance;
            }
        }
    }
}
