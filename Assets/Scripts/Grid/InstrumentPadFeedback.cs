using System.Collections;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Visual feedback for a Trigger block's INSTRUMENT ELEMENT (see
    /// 0042): the raised bar the marble lands on - today built by
    /// XylophoneBlockDecoration, later any other instrument's own shape -
    /// briefly flashes and pulses in size when the block is triggered.
    /// Only that element reacts; the TrackBlock's own body (shoulders,
    /// groove) is deliberately left alone, and blocks without an
    /// instrument element (Normal/Start/Goal) show nothing at all -
    /// this component simply stays idle until something hands it an
    /// element via Attach.
    /// It also owns the element's NORMAL look: padColor is applied to it
    /// as soon as it's attached, so the bar reads as an instrument rather
    /// than as more terrain. Both the base and the flash color go through
    /// a MaterialPropertyBlock rather than the Material itself, because
    /// TrackBlockSpawner hands the same shared Material to every block -
    /// mutating it would recolor the whole track (same reasoning as the
    /// block-wide flash this replaces).
    /// Lives on the TrackBlock prefab next to BlockTrigger, whose event it
    /// subscribes to (see 0023) - so the settings below are edited once,
    /// on that prefab, and apply to every instrument block of every track.
    /// </summary>
    [RequireComponent(typeof(TrackBlock))]
    public class InstrumentPadFeedback : MonoBehaviour
    {
        [SerializeField] private Color padColor = new Color(0.85f, 0.72f, 0.35f); // the instrument element's own normal color, applied as soon as it's attached
        [SerializeField] private Color flashColor = Color.white; // color it flashes to when hit - used unless useContentFlashColor takes over below
        [SerializeField] private bool useContentFlashColor = true; // true: a SoundTriggerContent's own FlashColor wins (per-instrument colors, see 0027); false: always use flashColor
        [SerializeField, Range(0f, 1f)] private float scaleAmount = 0.25f; // how much bigger the element gets at the peak of the pulse (0 = no scaling, 0.25 = a quarter larger)
        [SerializeField] private float pulseDuration = 0.18f; // seconds for the whole up-and-back pulse (color and scale together)

        private TrackBlock block;
        private BlockTrigger trigger;
        private Renderer element;
        private Transform elementTransform;
        private Vector3 elementBaseScale = Vector3.one;
        private MaterialPropertyBlock propertyBlock;
        private Coroutine pulseRoutine;

        private void Awake()
        {
            block = GetComponent<TrackBlock>();
            trigger = GetComponent<BlockTrigger>();
            propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (trigger != null) trigger.Triggered += HandleTriggered;
        }

        private void OnDisable()
        {
            if (trigger != null) trigger.Triggered -= HandleTriggered;
        }

        /// Hands this component the instrument element to react on -
        /// called by whoever builds that element (TrackBlockSpawner, right
        /// after XylophoneBlockDecoration made it). Until then, and on
        /// every block that never gets one, this component does nothing.
        /// The element's pivot must sit at its own base for the pulse to
        /// grow it in place rather than shift it (see
        /// XylophoneBlockDecoration).
        public void Attach(Renderer instrumentElement)
        {
            element = instrumentElement;
            elementTransform = instrumentElement != null ? instrumentElement.transform : null;
            elementBaseScale = elementTransform != null ? elementTransform.localScale : Vector3.one;
            ApplyColor(padColor);
        }

        private void HandleTriggered()
        {
            if (element == null) return;

            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            Color peak = useContentFlashColor && block != null ? block.Definition.FlashColor : flashColor;
            float duration = Mathf.Max(pulseDuration, 0.01f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // One half sine over the whole duration: 0 at the start,
                // 1 in the middle, 0 again at the end - the "up and back"
                // shape for color and scale alike, with no snap at either
                // end (a linear ramp would jump back at the peak).
                float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);

                ApplyColor(Color.Lerp(padColor, peak, pulse));
                if (elementTransform != null) elementTransform.localScale = elementBaseScale * (1f + scaleAmount * pulse);

                yield return null;
            }

            ApplyColor(padColor);
            if (elementTransform != null) elementTransform.localScale = elementBaseScale;
            pulseRoutine = null;
        }

        private void ApplyColor(Color color)
        {
            if (element == null) return;

            propertyBlock.SetColor("_BaseColor", color); // URP/Lit
            propertyBlock.SetColor("_Color", color);     // Standard fallback - harmless if the shader lacks either property
            element.SetPropertyBlock(propertyBlock);
        }
    }
}
