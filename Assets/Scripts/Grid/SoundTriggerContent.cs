using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Data for a cell that plays an AudioClip (and flashes its block in a
    /// given color) when the marble arrives - read by TrackBlockSpawner
    /// into that block's BlockDefinition (see 0027); the actual reaction
    /// happens block-side (InstrumentReaction/BlockFlashFeedback, see
    /// 0023/0024), not here.
    /// </summary>
    [CreateAssetMenu(fileName = "Sound_", menuName = "MarbleOrchestra/Cell Content/Sound Trigger")]
    public class SoundTriggerContent : CellContentDefinition
    {
        [SerializeField] private AudioClip clip;
        [SerializeField] private Color flashColor = Color.white;

        public AudioClip Clip => clip;
        public Color FlashColor => flashColor;
    }
}
