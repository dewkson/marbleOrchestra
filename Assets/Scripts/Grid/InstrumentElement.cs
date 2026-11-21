using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// What InstrumentBlockDecoration hands to InstrumentPadFeedback: the
    /// built-in element's root (scaled for the pulse - pivot at its base)
    /// and its renderer, which the feedback paints in its own pad color.
    /// External 3D models (InstrumentVisual) were dropped with 0068 -
    /// visuals beyond the pad itself now live in the Level Grid Editor's
    /// Visuals tab.
    /// </summary>
    public class InstrumentElement
    {
        public Transform Root { get; private set; }
        public Renderer[] Renderers { get; private set; }

        public static InstrumentElement Tinted(Renderer renderer) =>
            new InstrumentElement { Root = renderer.transform, Renderers = new[] { renderer } };
    }
}
