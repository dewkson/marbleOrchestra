using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Common base for CellContentDefinition subclasses that make a block
    /// BlockType.Trigger (see ITriggerCellContent/0039) - carries the data
    /// every trigger content needs: how far the marble falls onto it from
    /// the previous block, and which instrument element (pad shape) it
    /// lands on. Since 0068 a content holds no other visuals (no external
    /// 3D model, no flash color) - those live in the Level Grid Editor's
    /// Visuals tab, independent of the content.
    /// </summary>
    public abstract class TriggerCellContent : CellContentDefinition, ITriggerCellContent
    {
        [SerializeField] private float fallHeight = 0.3f; // default for newly created content (existing assets keep their saved value)

        [SerializeField] private InstrumentType instrument = InstrumentType.Xylophone; // which pad the marble lands on (see 0055) - Xylophone keeps assets saved before this field looking unchanged

        public float FallHeight => fallHeight;
        public InstrumentType Instrument => instrument;
    }
}
