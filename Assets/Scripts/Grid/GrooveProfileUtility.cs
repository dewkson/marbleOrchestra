using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Stateless geometry helper for the U-shaped rail groove cross-section
    /// (flat shoulder - semicircular groove - flat shoulder, side view:
    /// ---u---). Extracted from the old TrackTerrainGenerator so a
    /// per-block groove profile (see IBlockProfile) can build the same
    /// shape once per TrackBlock instead of once per whole ribbon mesh.
    /// </summary>
    public static class GrooveProfileUtility
    {
        /// Cross-section, left to right: flat outer shoulder, a semicircular
        /// U dipping down by grooveRadius and back up (the rail the marble
        /// sits in), flat outer shoulder.
        public static Vector2[] BuildProfile(float grooveRadius, float sideWidth, int grooveArcSegments)
        {
            int arcPoints = grooveArcSegments + 1;
            Vector2[] points = new Vector2[arcPoints + 2];

            points[0] = new Vector2(-(grooveRadius + sideWidth), 0f);

            for (int k = 0; k <= grooveArcSegments; k++)
            {
                float t = (float)k / grooveArcSegments;
                float angle = Mathf.PI + t * Mathf.PI; // sweeps the bottom half-circle, left rim to right rim
                points[1 + k] = new Vector2(Mathf.Cos(angle) * grooveRadius, Mathf.Sin(angle) * grooveRadius);
            }

            points[arcPoints + 1] = new Vector2(grooveRadius + sideWidth, 0f);
            return points;
        }
    }
}
