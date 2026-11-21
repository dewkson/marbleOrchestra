using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Which animation a cell's visual effect plays when its block is
    /// triggered (see 0068). None is the first value on purpose: Unity's
    /// serializer never stores null for a [Serializable] class in a list,
    /// it stores a default-constructed one instead - so "no effect" has to
    /// be a value of its own, not a null entry. New effects: add a value
    /// here, its fields to CellVisualEffect and a case in
    /// BlockVisualEffectSystem.
    /// </summary>
    public enum VisualEffectType
    {
        None,
        SubLevelColorFlash, // every TrackBlock of the triggering cell's SubLevel jumps to FlashColor, then lerps back to its own base color over Duration
    }

    /// <summary>
    /// A visual effect bound to a grid cell (see 0068) - authored in the
    /// Level Grid Editor's Visuals tab, independent of the cell's content
    /// (sound/instrument). Fires whenever the marble triggers that cell's
    /// block, so instruments still drive visuals, but what is shown is no
    /// longer part of the instrument's own data.
    /// </summary>
    [System.Serializable]
    public class CellVisualEffect
    {
        [SerializeField] private VisualEffectType type = VisualEffectType.None;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField, Min(0.01f)] private float duration = 0.4f; // seconds from the flash peak back to the base color
        [SerializeField] private bool excludeBlockedCells = true; // true: blocked cells' filler blocks keep their color, only cells the player can put cards on flash

        public VisualEffectType Type => type;
        public Color FlashColor => flashColor;
        public float Duration => Mathf.Max(0.01f, duration);
        public bool ExcludeBlockedCells => excludeBlockedCells;
        public bool IsSet => type != VisualEffectType.None;

        public CellVisualEffect(VisualEffectType type, Color flashColor, float duration, bool excludeBlockedCells)
        {
            this.type = type;
            this.flashColor = flashColor;
            this.duration = Mathf.Max(0.01f, duration);
            this.excludeBlockedCells = excludeBlockedCells;
        }

        public bool SameAs(CellVisualEffect other) =>
            other != null && other.type == type && other.flashColor == flashColor && Mathf.Approximately(other.duration, duration)
            && other.excludeBlockedCells == excludeBlockedCells;
    }

    /// <summary>
    /// Optional base color for every TrackBlock in an area (see 0068) - a
    /// SubLevel's own, or the level-wide one for cells outside any
    /// SubLevel. Off by default, so levels authored before this keep
    /// their card-/override-derived block colors unchanged.
    /// </summary>
    [System.Serializable]
    public class BlockColorSettings
    {
        [SerializeField] private bool useBaseColor;
        [SerializeField] private Color baseColor = new Color(0.30f, 0.45f, 0.20f); // TrackBlockSpawner's own default terrain color (see 0032)

        public bool UseBaseColor => useBaseColor;
        public Color BaseColor => baseColor;

        public void Set(bool use, Color color)
        {
            useBaseColor = use;
            baseColor = color;
        }
    }
}
