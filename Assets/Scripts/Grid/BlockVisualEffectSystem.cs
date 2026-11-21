using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Plays the per-cell visual effects authored in the Level Grid
    /// Editor's Visuals tab (see 0068, CellVisualEffect). Listens to every
    /// block's trigger through the static BlockTrigger.AnyFired, looks up
    /// the triggered cell's own effect in the level and runs it - so an
    /// instrument still drives the visuals, but neither the content asset
    /// nor the block prefab knows anything about them.
    /// SubLevelColorFlash: every TrackBlock of the triggering cell's
    /// SubLevel (track, preview and filler blocks alike) jumps to the
    /// flash color and lerps back to its own base color over the effect's
    /// duration. A new trigger in the same SubLevel restarts the flash.
    /// With ExcludeBlockedCells, blocked cells' filler blocks are left
    /// out, so only cells the player can put cards on flash.
    /// The color goes through a MaterialPropertyBlock on the block's main
    /// material slot only (index 0 - shoulders/walls), since
    /// TrackBlockSpawner shares one Material between many blocks; the
    /// groove keeps its own color.
    /// Added automatically by TrackBlockSpawner, no scene setup needed.
    /// </summary>
    [DisallowMultipleComponent]
    public class BlockVisualEffectSystem : MonoBehaviour
    {
        private const int MainMaterialIndex = 0;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP/Lit
        private static readonly int ColorId = Shader.PropertyToID("_Color"); // Standard fallback

        private class ColorFlash
        {
            public RectInt Area;
            public Color Color;
            public float Duration;
            public float Elapsed;
            public readonly List<TrackBlock> Blocks = new List<TrackBlock>();
        }

        private TrackBlockSpawner spawner;
        private PathGrid grid;
        private readonly List<ColorFlash> flashes = new List<ColorFlash>();
        private MaterialPropertyBlock properties;

        public void Init(TrackBlockSpawner owner, PathGrid pathGrid)
        {
            spawner = owner;
            grid = pathGrid;
        }

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            BlockTrigger.AnyFired += HandleFired;
        }

        private void OnDisable()
        {
            BlockTrigger.AnyFired -= HandleFired;
            foreach (ColorFlash flash in flashes) ResetBlocks(flash);
            flashes.Clear();
        }

        private void HandleFired(BlockTrigger trigger)
        {
            if (grid == null || spawner == null || trigger == null) return;
            if (!trigger.TryGetComponent(out TrackBlock block)) return;

            Vector2Int coord = block.Definition.Coord;
            CellVisualEffect effect = grid.GetVisualEffect(coord);
            if (effect == null) return;

            switch (effect.Type)
            {
                case VisualEffectType.SubLevelColorFlash:
                    StartColorFlash(grid.GetVisualEffectArea(coord), effect);
                    break;
            }
        }

        private void StartColorFlash(RectInt area, CellVisualEffect effect)
        {
            ColorFlash flash = flashes.Find(f => f.Area.Equals(area));
            if (flash == null)
            {
                flash = new ColorFlash { Area = area };
                flashes.Add(flash);
            }

            flash.Color = effect.FlashColor;
            flash.Duration = effect.Duration;
            flash.Elapsed = 0f;
            spawner.CollectBlocksInArea(area, flash.Blocks); // re-collected on every trigger - tracks are rebuilt between plays
            if (effect.ExcludeBlockedCells)
            {
                ResetBlocks(flash, onlyBlocked: true); // a restart with the option just switched on must not leave them tinted
                flash.Blocks.RemoveAll(b => b == null || grid.IsBlocked(b.Definition.Coord));
            }
            Apply(flash, 0f);
        }

        private void LateUpdate()
        {
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                ColorFlash flash = flashes[i];
                flash.Elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(flash.Elapsed / flash.Duration);

                if (t >= 1f)
                {
                    ResetBlocks(flash);
                    flashes.RemoveAt(i);
                    continue;
                }

                Apply(flash, t);
            }
        }

        /// t = 0: full flash color, t = 1: the block's own base color.
        private void Apply(ColorFlash flash, float t)
        {
            foreach (TrackBlock block in flash.Blocks)
            {
                if (block == null || !block.TryGetComponent(out MeshRenderer renderer)) continue;

                Color color = Color.Lerp(flash.Color, BaseColor(block.Material), t);
                properties.Clear();
                properties.SetColor(BaseColorId, color);
                properties.SetColor(ColorId, color);
                renderer.SetPropertyBlock(properties, MainMaterialIndex);
            }
        }

        /// An empty property block removes the override, so the block shows its own material again.
        private void ResetBlocks(ColorFlash flash, bool onlyBlocked = false)
        {
            if (properties == null) return;
            properties.Clear();
            foreach (TrackBlock block in flash.Blocks)
            {
                if (block == null || (onlyBlocked && !grid.IsBlocked(block.Definition.Coord))) continue;
                if (block.TryGetComponent(out MeshRenderer renderer)) renderer.SetPropertyBlock(properties, MainMaterialIndex);
            }
        }

        private static Color BaseColor(Material material)
        {
            if (material == null) return Color.white;
            if (material.HasProperty(BaseColorId)) return material.GetColor(BaseColorId);
            if (material.HasProperty(ColorId)) return material.GetColor(ColorId);
            return Color.white;
        }
    }
}
