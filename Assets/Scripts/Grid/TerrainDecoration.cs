using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Scatters a handful of small moss-clump decorations on a TrackBlock's
    /// flat shoulders (the strips beside the rollable groove - see
    /// GrooveProfileUtility) for the "Default" biome (see 0032). Purely
    /// cosmetic: no collider, parented to the block so it's destroyed along
    /// with it (TrackBlockSpawner's SyncTracks already tears down/rebuilds
    /// whole tracks). Deterministic per cell (seeded from its grid
    /// coordinate) so a track keeps the same scatter pattern across
    /// SyncTracks rebuilds instead of visibly re-rolling.
    /// Uses the engine's built-in sphere mesh (Resources.GetBuiltinResource)
    /// rather than an imported asset, matching this codebase's otherwise
    /// fully procedural art (see Marble.cs, PipeVisual.cs).
    /// </summary>
    public static class TerrainDecoration
    {
        private const int MinClumps = 4;
        private const int MaxClumps = 9;
        private static readonly Color LightMoss = new Color(0.55f, 0.68f, 0.28f);
        private static readonly Color DarkMoss = new Color(0.26f, 0.40f, 0.15f);

        private static Mesh clumpMesh;
        private static Material clumpMaterial;

        /// Only meaningful for DefaultBiome for now - no-ops for anything
        /// else, ready for a future biome to plug in its own decoration
        /// instead (see BlockDefinition.Biome's placeholder comment).
        public static void Scatter(TrackBlock block, Vector2Int cell, string biome, float grooveRadius, float sideWidth, Vector2 size)
        {
            if (biome != BlockDefinition.DefaultBiome) return;
            if (sideWidth <= 0f) return; // no flat shoulder to place anything on

            Random.State previousState = Random.state;
            Random.InitState(unchecked(cell.x * 73856093 ^ cell.y * 19349663));

            int count = Random.Range(MinClumps, MaxClumps + 1);
            for (int i = 0; i < count; i++) SpawnClump(block, grooveRadius, sideWidth, size);

            Random.state = previousState;
        }

        private static void SpawnClump(TrackBlock block, float grooveRadius, float sideWidth, Vector2 size)
        {
            float x = Random.Range(grooveRadius, grooveRadius + sideWidth);
            if (Random.value < 0.5f) x = -x;
            float z = Random.Range(-size.y * 0.5f, size.y * 0.5f);

            GameObject clump = new GameObject("MossClump");
            clump.transform.SetParent(block.transform, false);
            clump.transform.localPosition = new Vector3(x, 0f, z);
            clump.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float radius = Random.Range(0.05f, 0.09f);
            float squash = Random.Range(0.4f, 0.7f); // flattened sphere reads as a low clump rather than a ball
            clump.transform.localScale = new Vector3(radius, radius * squash, radius);

            MeshFilter filter = clump.AddComponent<MeshFilter>();
            filter.sharedMesh = GetClumpMesh();

            MeshRenderer renderer = clump.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetClumpMaterial();

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            Color tint = Color.Lerp(DarkMoss, LightMoss, Random.value);
            properties.SetColor("_BaseColor", tint);
            properties.SetColor("_Color", tint);
            renderer.SetPropertyBlock(properties);
        }

        private static Mesh GetClumpMesh()
        {
            if (clumpMesh == null) clumpMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            return clumpMesh;
        }

        private static Material GetClumpMaterial()
        {
            if (clumpMaterial != null) return clumpMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            clumpMaterial = new Material(shader) { enableInstancing = true }; // required for per-renderer MaterialPropertyBlock colors to work with the SRP batcher
            if (clumpMaterial.HasProperty("_Smoothness")) clumpMaterial.SetFloat("_Smoothness", 0.1f);
            if (clumpMaterial.HasProperty("_Glossiness")) clumpMaterial.SetFloat("_Glossiness", 0.1f);
            if (clumpMaterial.HasProperty("_Metallic")) clumpMaterial.SetFloat("_Metallic", 0f);

            return clumpMaterial;
        }
    }
}
