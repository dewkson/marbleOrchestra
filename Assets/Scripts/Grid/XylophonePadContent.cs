using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Marks a cell as a "xylophone pad" TrackBlock variant: a
    /// TriggerCellContent (see 0039) that's purely visual - no sound of its
    /// own, but otherwise identical to SoundTriggerContent in how
    /// TrackBlockSpawner treats it (BlockType.Trigger, ClosedEndGrooveBlockProfile
    /// plus XylophoneBlockDecoration's simple raised block instead of the
    /// normal full-length groove, marble falls onto it from the previous
    /// block by FallHeight).
    /// </summary>
    [CreateAssetMenu(fileName = "XylophonePad_", menuName = "MarbleOrchestra/Cell Content/Xylophone Pad")]
    public class XylophonePadContent : TriggerCellContent
    {
    }
}
