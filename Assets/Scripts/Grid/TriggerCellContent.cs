using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Common base for CellContentDefinition subclasses that make a block
    /// BlockType.Trigger (see ITriggerCellContent/0039) - carries the one
    /// piece of data every trigger content needs regardless of whether it's
    /// sound-only, visual-only, or both: how far the marble falls onto it
    /// from the previous block.
    /// </summary>
    public abstract class TriggerCellContent : CellContentDefinition, ITriggerCellContent
    {
        [SerializeField] private float fallHeight = 1.2f; // clearly visible fall from the previous block onto this one's pad, not just a small step

        [SerializeField] private InstrumentType instrument = InstrumentType.Xylophone; // which 3D element the block shows (see 0055) - Xylophone keeps assets saved before this field looking unchanged

        public float FallHeight => fallHeight;
        public InstrumentType Instrument => instrument;
    }
}
