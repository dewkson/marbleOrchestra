using System.Collections;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Visual feedback for a Trigger block's INSTRUMENT ELEMENT (see
    /// 0042): the raised bar/drum the marble lands on (built by
    /// InstrumentBlockDecoration) briefly flashes and pulses in size when
    /// the block is triggered. Only that element reacts; the TrackBlock's
    /// own body is recolored by the Visuals tab's effects instead (see
    /// 0068/BlockVisualEffectSystem), and blocks without an instrument
    /// element show nothing here - this component simply stays idle until
    /// something hands it an element via Attach.
    /// It also owns the element's NORMAL look: padColor is applied to it
    /// as soon as it's attached, so the bar reads as an instrument rather
    /// than as more terrain. Both the base and the flash color go through
    /// a MaterialPropertyBlock rather than the Material itself, because
    /// TrackBlockSpawner hands the same shared Material to every block.
    /// The flash color is a single setting on this prefab component -
    /// content assets no longer carry one (0068: Content is sound only).
    /// Lives on the TrackBlock prefab next to BlockTrigger, whose event it
    /// subscribes to (see 0023).
    /// </summary>
    [RequireComponent(typeof(TrackBlock))]
    public class InstrumentPadFeedback : MonoBehaviour
    {
        [SerializeField] private Color padColor = new Color(0.85f, 0.72f, 0.35f); // the instrument element's own normal color, applied as soon as it's attached
        [SerializeField] private Color flashColor = Color.white; // color it flashes to when hit
        [SerializeField, Range(0f, 1f)] private float scaleAmount = 0.25f; // how much bigger the element gets at the peak of the pulse (0 = no scaling, 0.25 = a quarter larger)
        [SerializeField] private float pulseDuration = 0.18f; // seconds for the whole up-and-back pulse (color and scale together)

        private BlockTrigger trigger;
        private Renderer[] elements = System.Array.Empty<Renderer>();
        private Transform elementTransform;
        private Vector3 elementBaseScale = Vector3.one;
        private MaterialPropertyBlock propertyBlock;
        private Coroutine pulseRoutine;

        /// Off (LevelData.InstrumentHitAnimation, see 0068): the element
        /// keeps its normal look, but a hit no longer flashes or scales -
        /// visuals then come from the Visuals tab only.
        public bool HitAnimationEnabled { get; set; } = true;

        private void Awake()
        {
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
        /// called by TrackBlockSpawner right after InstrumentBlockDecoration
        /// built it. Until then, and on every block that never gets one,
        /// this component does nothing. The element's pivot must sit at
        /// its own base for the pulse to grow it in place.
        public void Attach(InstrumentElement instrumentElement)
        {
            elements = instrumentElement != null ? instrumentElement.Renderers : System.Array.Empty<Renderer>();
            elementTransform = instrumentElement != null ? instrumentElement.Root : null;
            elementBaseScale = elementTransform != null ? elementTransform.localScale : Vector3.one;
            ApplyColors(0f);
        }

        private void HandleTriggered()
        {
            if (!HitAnimationEnabled || elements.Length == 0) return;

            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            float duration = Mathf.Max(pulseDuration, 0.01f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // One half sine over the whole duration: 0 at the start,
                // 1 in the middle, 0 again at the end - the "up and back"
                // shape for color and scale alike, with no snap at either end.
                float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);

                ApplyColors(pulse);
                if (elementTransform != null) elementTransform.localScale = elementBaseScale * (1f + scaleAmount * pulse);

                yield return null;
            }

            ApplyColors(0f);
            if (elementTransform != null) elementTransform.localScale = elementBaseScale;
            pulseRoutine = null;
        }

        /// Blends every renderer from padColor toward flashColor by `amount`.
        private void ApplyColors(float amount)
        {
            Color color = Color.Lerp(padColor, flashColor, amount);
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] == null) continue;

                propertyBlock.Clear();
                propertyBlock.SetColor("_BaseColor", color); // URP/Lit
                propertyBlock.SetColor("_Color", color);     // Standard fallback - harmless if the shader lacks either property
                elements[i].SetPropertyBlock(propertyBlock);
            }
        }
    }
}
