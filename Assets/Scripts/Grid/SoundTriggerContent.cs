using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Data for a cell that plays an AudioClip when the marble arrives -
    /// read by TrackBlockSpawner into that block's BlockDefinition (see
    /// 0027); the actual reaction happens block-side (InstrumentReaction,
    /// see 0023/0024), not here. A TriggerCellContent (see 0039), so it
    /// makes its block BlockType.Trigger - the marble falls onto it from
    /// the previous block by FallHeight. Sound only since 0068: how the
    /// blocks react visually is authored per cell in the Level Grid
    /// Editor's Visuals tab.
    /// </summary>
    [CreateAssetMenu(fileName = "Sound_", menuName = "MarbleOrchestra/Cell Content/Sound Trigger")]
    public class SoundTriggerContent : TriggerCellContent
    {
        [SerializeField] private AudioClip clip;

        public AudioClip Clip => clip;
    }
}
