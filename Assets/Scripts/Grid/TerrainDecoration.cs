using System.Collections.Generic;
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
    /// Count/size/color/clustering are all driven by a
    /// TerrainDecorationSettings asset (falls back to
    /// TerrainDecorationSettings.Default when none is given).
    /// Uses the engine's built-in sphere mesh (Resources.GetBuiltinResource)
    /// rather than an imported asset, matching this codebase's otherwise
    /// fully procedural art (see Marble.cs, PipeVisual.cs).
    /// </summary>
    public static class TerrainDecoration
    {
        private const int MaxPlacementAttempts = 20; // per clump - see SpawnClump

        private static Mesh clumpMesh;
        private static Material clumpMaterial;

        /// Only meaningful for DefaultBiome for now - no-ops for anything
        /// else, ready for a future biome to plug in its own decoration
        /// instead (see BlockDefinition.Biome's placeholder comment).
        public static void Scatter(TrackBlock block, Vector2Int cell, string biome, float grooveRadius, float sideWidth, Vector2 size, TerrainDecorationSettings settings = null)
        {
            if (biome != BlockDefinition.DefaultBiome) return;
            if (sideWidth <= 0f) return; // no flat shoulder to place anything on

            if (settings == null) settings = TerrainDecorationSettings.Default;

            Random.State previousState = Random.state;
            Random.InitState(unchecked(cell.x * 73856093 ^ cell.y * 19349663));

            int count = Random.Range(settings.MinClumps, settings.MaxClumps + 1);
            List<Vector3> placed = new List<Vector3>(count); // anchors for ClusterChance - see SpawnClump
            for (int i = 0; i < count; i++)
            {
                Vector3? position = SpawnClump(block, grooveRadius, size, settings, placed);
                if (position.HasValue) placed.Add(position.Value);
            }

            Random.state = previousState;
        }

        /// Rejection-samples the block's square footprint instead of
        /// placing along straight x/z shoulder strips, so the same rule
        /// works for a curved block too (see TrackBlock.ShoulderSurfaceY),
        /// whose groove sweeps diagonally across exactly those strips.
        /// With settings.ClusterChance, samples around a previously placed
        /// clump on this same block instead of across the whole footprint,
        /// so clumps tend to bunch into little groups rather than spread
        /// evenly. Gives up (one clump fewer) rather than looping forever
        /// when no valid spot turns up in range - too narrow a shoulder, or
        /// a cluster anchor pinned right against the groove/edge.
        /// Returns the local position actually used (for the next clump's
        /// own clustering), or null if this one was skipped.
        private static Vector3? SpawnClump(TrackBlock block, float grooveRadius, Vector2 size, TerrainDecorationSettings settings, List<Vector3> placed)
        {
            float radius = Random.Range(settings.MinRadius, settings.MaxRadius);
            float squash = Random.Range(0.4f, 0.7f); // flattened sphere reads as a low clump rather than a ball
            // Measured (not assumed) from the mesh's own bounds, so this
            // stays correct even if GetClumpMesh ever changes - plus a
            // small safety margin, since a mesh's bounds are its exact
            // vertex extent with zero slack for float rounding.
            float footprint = radius * ClumpMeshHalfExtent * 1.05f;

            // Inset by the clump's own footprint so it never pokes past the
            // block's outer edges or over the groove's lip.
            float halfWidth = size.x * 0.5f - footprint;
            float halfLength = size.y * 0.5f - footprint;
            float minDistanceFromCenterline = grooveRadius + footprint;
            if (halfWidth <= 0f || halfLength <= 0f) return null;

            bool cluster = placed.Count > 0 && Random.value < settings.ClusterChance;
            Vector3 anchor = cluster ? placed[Random.Range(0, placed.Count)] : Vector3.zero;

            for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                Vector3 position;
                if (cluster)
                {
                    Vector2 offset = Random.insideUnitCircle * settings.ClusterRadius;
                    position = new Vector3(
                        Mathf.Clamp(anchor.x + offset.x, -halfWidth, halfWidth), 0f,
                        Mathf.Clamp(anchor.z + offset.y, -halfLength, halfLength));
                }
                else
                {
                    position = new Vector3(Random.Range(-halfWidth, halfWidth), 0f, Random.Range(-halfLength, halfLength));
                }

                position.y = block.ShoulderSurfaceY(position, out float distanceFromCenterline);
                if (distanceFromCenterline < minDistanceFromCenterline) continue;

                CreateClump(block, position, radius, squash, settings);
                return position;
            }

            return null;
        }

        private static void CreateClump(TrackBlock block, Vector3 position, float radius, float squash, TerrainDecorationSettings settings)
        {
            GameObject clump = new GameObject("MossClump");
            clump.transform.SetParent(block.transform, false);
            clump.transform.localPosition = position;
            clump.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            clump.transform.localScale = new Vector3(radius, radius * squash, radius);

            MeshFilter filter = clump.AddComponent<MeshFilter>();
            filter.sharedMesh = GetClumpMesh();

            MeshRenderer renderer = clump.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetClumpMaterial();

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            Color tint = settings.RandomColor();
            properties.SetColor("_BaseColor", tint);
            properties.SetColor("_Color", tint);
            renderer.SetPropertyBlock(properties);
        }

        private static Mesh GetClumpMesh()
        {
            if (clumpMesh == null) clumpMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            return clumpMesh;
        }

        /// The clump mesh's own horizontal half-extent at scale 1 (x and z
        /// are identical for a sphere) - read from its actual bounds
        /// instead of assumed, so SpawnClump's edge/groove inset stays
        /// correct regardless of what GetClumpMesh returns.
        private static float ClumpMeshHalfExtent => GetClumpMesh().bounds.extents.x;

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
