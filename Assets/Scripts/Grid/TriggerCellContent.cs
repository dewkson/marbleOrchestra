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
        [SerializeField] private float fallHeight = 0.3f; // default for newly created content (existing assets keep their saved value)

        [SerializeField] private InstrumentType instrument = InstrumentType.Xylophone; // which 3D element the block shows (see 0055) - Xylophone keeps assets saved before this field looking unchanged

        [SerializeField] private InstrumentVisual visual; // optional external 3D model (0055 follow-up); when set it replaces the built-in geometry of `instrument`

        public float FallHeight => fallHeight;
        public InstrumentVisual Visual => visual;
        public InstrumentType Instrument => instrument;
    }
}
