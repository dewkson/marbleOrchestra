using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Marks a cell as a "xylophone pad" TrackBlock variant (see 0022):
    /// purely a marker for now, no fields - TrackBlockSpawner checks for
    /// this type to swap in ClosedEndGrooveBlockProfile (same
    /// groove-starts-near-center mechanism as Start/Goal) plus
    /// XylophonePillowDecoration's horseshoe-shaped pillow instead of the
    /// normal full-length groove. No sound/trigger behavior yet - purely
    /// visual, unlike SoundTriggerContent.
    /// </summary>
    [CreateAssetMenu(fileName = "XylophonePad_", menuName = "MarbleOrchestra/Cell Content/Xylophone Pad")]
    public class XylophonePadContent : CellContentDefinition
    {
    }
}
