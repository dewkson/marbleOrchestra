using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Universal building block for the marble track: a solid, closed box
    /// whose size, height, material and orientation are configurable, and
    /// whose rollable top-surface shape comes from a pluggable Profile
    /// (flat by default, see IBlockProfile). Terrain, gameplay and
    /// instrument blocks are meant to reuse this same prefab/component and
    /// only swap the Profile or add sibling components (e.g. a future
    /// instrument trigger reading EntryPointLocal/ExitPointLocal) - never
    /// subclass it, since a Unity prefab can't swap a component's type per
    /// instance.
    /// Orientation is split in two: Yaw rotates the whole transform around
    /// the vertical axis (the block's footprint stays a normal, level box -
    /// flat bottom, vertical sides - at any yaw), while Tilt is baked
    /// directly into the mesh (the entry/exit rings of the top surface are
    /// shifted up/down by half the tilt-implied drop each), so only the
    /// rollable top surface itself slopes - never the block's bounding box.
    /// Rebuild() regenerates the mesh from the current Size/Height/Tilt/
    /// Profile; call it after changing any of those at runtime. Yaw alone
    /// is a cheap transform op and doesn't require a rebuild.
    /// A block normally sweeps its Profile's cross-section linearly from
    /// entry (local -Z) to exit (local +Z) - see BuildStraightMesh. SetCurve
    /// (see 0040) switches a block to a curved 90-degree sweep instead, for
    /// a Normal block whose path actually turns - see BuildCurvedMesh.
    /// How the MARBLE moves across the block is a separate, equally
    /// pluggable concern (Profile is the shape, Trace is the movement):
    /// see Trace/IMarbleTrace/0038.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class TrackBlock : MonoBehaviour
    {
        [SerializeField] private Vector2 size = Vector2.one; // x = width (lateral), y = length (along travel direction)
        [SerializeField] private float height = 0.2f; // solid body thickness below the rollable top surface
        [SerializeField] private Material material;
        [SerializeField] private Material grooveMaterial; // rollable-groove submesh; falls back to `material` when unset (e.g. FlatBoxProfile blocks, which have no groove segments at all)
        [SerializeField] private float yawDegrees; // rotation around world/local Y - facing direction
        [SerializeField] private float tiltDegrees; // downhill slope of the top surface, entry (higher) to exit (lower)
        [SerializeField] private BlockDefinition definition; // WHAT this block is - see the Definition property below

        [SerializeField] private bool isCurved; // true: BuildCurvedMesh instead of BuildStraightMesh - see SetCurve/0040
        [SerializeField] private Vector3 curveInVec = Vector3.forward; // horizontal unit vector, travel direction entering - see SetCurve
        [SerializeField] private Vector3 curveOutVec = Vector3.forward; // horizontal unit vector, travel direction exiting - see SetCurve

        public Vector2 Size { get => size; set { size = value; Rebuild(); } }
        public float Height { get => height; set { height = value; Rebuild(); } }

        public Material Material
        {
            get => material;
            set { material = value; ApplyMaterial(); }
        }

        /// See GrooveMaterial's field comment - the groove submesh's own
        /// material (e.g. an earthy brown, distinct from the shoulders'
        /// grass color - see 0032).
        public Material GrooveMaterial
        {
            get => grooveMaterial;
            set { grooveMaterial = value; ApplyMaterial(); }
        }

        public float YawDegrees { get => yawDegrees; set { yawDegrees = value; ApplyOrientation(); } }

        /// Changing Tilt reshapes the mesh (see class remarks), so it
        /// triggers a Rebuild rather than a cheap transform update.
        public float TiltDegrees { get => tiltDegrees; set { tiltDegrees = value; Rebuild(); } }

        private IBlockProfile profile = FlatBoxProfile.Instance;
        public IBlockProfile Profile
        {
            get => profile;
            set { profile = value ?? FlatBoxProfile.Instance; Rebuild(); }
        }

        /// True once SetCurve has switched this block to a curved 90-degree
        /// sweep (see 0040) instead of the default linear one.
        public bool IsCurved => isCurved;

        /// Switches this block to a curved sweep between two perpendicular
        /// travel directions instead of the default straight -Z-to-+Z one -
        /// see BuildCurvedMesh/0040 for the geometry. inputDirectionLocal/
        /// outputDirectionLocal are horizontal unit vectors in this block's
        /// OWN local space (e.g. Direction.ToLocalVector3()) - a curved
        /// block is built grid-axis-aligned, so callers should also leave
        /// YawDegrees at 0 rather than deriving it from a travel direction.
        /// Never meaningful together with an IClosedEndBlockProfile (see
        /// Rebuild's guard) - only a straight-through Normal-block groove
        /// can turn; a Trigger's closed entry half never rolls through.
        public void SetCurve(Vector3 inputDirectionLocal, Vector3 outputDirectionLocal)
        {
            isCurved = true;
            curveInVec = inputDirectionLocal.normalized;
            curveOutVec = outputDirectionLocal.normalized;
            Rebuild();
        }

        /// Reverts to the default straight sweep - a no-op (no wasted
        /// Rebuild) if this block was never curved in the first place.
        public void ClearCurve()
        {
            if (!isCurved) return;
            isCurved = false;
            Rebuild();
        }

        /// WHAT this block is (grid/content-derived facts), as opposed to
        /// this component's own concern of HOW it looks - see 0027. Set
        /// once by whoever spawns the block (TrackBlockSpawner); TrackBlock
        /// itself never reads or depends on it. Backed by a [SerializeField]
        /// (rather than an auto-property) specifically so it shows up in
        /// the Inspector for debugging - external code still can't mutate
        /// it through a live block reference, since the getter returns a
        /// copy of the struct.
        public BlockDefinition Definition => definition;
        public void SetDefinition(BlockDefinition value) => definition = value;

        private IMarbleTrace trace;

        /// The path the marble takes THROUGH this block in Kinematic3D
        /// (see IMarbleTrace/0038) - the movement counterpart to Profile:
        /// swapped per block variant by whoever spawns the block
        /// (TrackBlockSpawner), never subclassed. Defaults to a straight
        /// roll along this block's own groove floor, from its entry edge
        /// to its exit edge, so a block always has a usable trace even if
        /// nobody set one.
        public IMarbleTrace Trace => trace ?? StraightMarbleTrace.BetweenZ(this, -HalfLength, HalfLength);

        public void SetTrace(IMarbleTrace value) => trace = value;

        /// Total vertical descent of the top surface from entry to exit,
        /// implied by TiltDegrees over the block's own length.
        private float Drop => size.y * Mathf.Tan(tiltDegrees * Mathf.Deg2Rad);

        /// Local-space point on the rollable surface at the entry (higher)
        /// edge, used to chain this block to the previous one's exit point.
        /// Only its Y is meaningful for a curved block (see BuildCurvedMesh) -
        /// its X/Z are always the straight-sweep entry point, never the
        /// curve's own (see SampleGroovePointLocal instead for that).
        public Vector3 EntryPointLocal => profile.EntryPoint(size) + Vector3.up * (Drop * 0.5f);

        /// Local-space point on the rollable surface at the exit (lower)
        /// edge, used to chain this block to the next one's entry point.
        public Vector3 ExitPointLocal => profile.ExitPoint(size) - Vector3.up * (Drop * 0.5f);

        /// Local-space point on this block's own rollable surface at
        /// fractional position t (0 = entry, 1 = exit) - the TRUE geometric
        /// point, unmodified (matching EntryPointLocal/ExitPointLocal
        /// exactly at t=0/1). Sampled by CurvedMarbleTrace (see 0038), i.e.
        /// the marble follows this arc as it really is: since every block's
        /// trace covers exactly its own extent, a straight neighbor's own
        /// trace simply ends where this one begins, and neither has to be
        /// distorted to meet the other (earlier attempts blended this
        /// method's endpoints toward the grid cell centers instead, which
        /// stretched the whole curve out of shape - its true chord is
        /// considerably shorter than a cell-center-to-cell-center span for
        /// a 90-degree turn; see 0039/0041 follow-ups).
        public Vector3 SampleGroovePointLocal(float t)
        {
            t = Mathf.Clamp01(t);
            if (!isCurved) return Vector3.Lerp(EntryPointLocal, ExitPointLocal, t);

            // Same ring-center formula as BuildCurvedMesh, at this single
            // fractional position - see that method's own remarks for the
            // full derivation. The centerline is exactly the ring center
            // itself (cross-section x = 0), so no `right` axis is needed.
            float radius = HalfLength;
            Vector3 center = radius * (curveOutVec - curveInVec);
            float angle = t * Mathf.PI * 0.5f;
            Vector3 radial = Mathf.Cos(angle) * -curveOutVec + Mathf.Sin(angle) * curveInVec;
            Vector3 ringCenter = center + radius * radial;
            float dy = Drop * (0.5f - t);
            return ringCenter + Vector3.up * (profile.EntryPoint(size).y + dy);
        }

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        private void OnEnable()
        {
            Rebuild();
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            CacheComponents();

            List<Vector3> vertices = new List<Vector3>();
            List<int> mainTriangles = new List<int>();
            List<int> grooveTriangles = new List<int>();

            // A curved sweep only ever makes sense for a plain through-
            // rolling groove - an IClosedEndBlockProfile (Start/Goal/
            // Trigger) never turns (see SetCurve's remarks and
            // TrackBlockSpawner, which never calls SetCurve for those
            // types), but this guard keeps Rebuild itself safe regardless.
            if (isCurved && !(profile is IClosedEndBlockProfile))
                BuildCurvedMesh(vertices, mainTriangles, grooveTriangles);
            else
                BuildStraightMesh(vertices, mainTriangles, grooveTriangles);

            Mesh mesh = new Mesh { name = "TrackBlock" };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = 2; // 0 = shoulders/walls/skirts/bottom (`material`), 1 = the groove itself (`grooveMaterial`) - see IBlockProfile.IsGrooveSegment
            mesh.SetTriangles(mainTriangles, 0);
            mesh.SetTriangles(grooveTriangles, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
            meshCollider.sharedMaterial = LowFrictionMaterial();

            ApplyMaterial();
            ApplyOrientation();
        }

        /// Default sweep: entry (local Z = -HalfLength) straight to exit
        /// (local Z = +HalfLength) - unchanged since before 0040, used for
        /// every block except an actual 90-degree turn (see isCurved).
        private void BuildStraightMesh(List<Vector3> vertices, List<int> mainTriangles, List<int> grooveTriangles)
        {
            float halfDrop = Drop * 0.5f;

            Vector2[] entryCrossSection;
            Vector2[] exitCrossSection;

            if (profile is IClosedEndBlockProfile closedEnd)
            {
                // Groove-shaped on the open half, a flattened (same points,
                // Y=0) version of it on the closed half, so both halves
                // share the same point layout and the internal wall between
                // them (see AppendInternalWall) can connect them point for
                // point - only the arc points actually differ in height,
                // since the shoulder points already sit at Y=0 either way.
                Vector2[] grooveShape = profile.BuildCrossSection(size);
                Vector2[] flatShape = FlattenToRim(grooveShape);
                float wallZ = Mathf.Clamp(closedEnd.WallZ, -HalfLength * 0.9f, HalfLength * 0.9f);

                if (closedEnd.ClosedAtEntry)
                {
                    entryCrossSection = OffsetY(flatShape, halfDrop);
                    exitCrossSection = OffsetY(grooveShape, -halfDrop);

                    AppendTopSurface(entryCrossSection, -HalfLength, flatShape, wallZ, vertices, mainTriangles, grooveTriangles, forceShoulderMaterial: true);
                    AppendTopSurface(grooveShape, wallZ, exitCrossSection, HalfLength, vertices, mainTriangles, grooveTriangles);
                    AppendInternalWall(flatShape, grooveShape, wallZ, vertices, mainTriangles, flip: true);
                }
                else
                {
                    entryCrossSection = OffsetY(grooveShape, halfDrop);
                    exitCrossSection = OffsetY(flatShape, -halfDrop);

                    AppendTopSurface(entryCrossSection, -HalfLength, grooveShape, wallZ, vertices, mainTriangles, grooveTriangles);
                    AppendTopSurface(flatShape, wallZ, exitCrossSection, HalfLength, vertices, mainTriangles, grooveTriangles, forceShoulderMaterial: true);
                    AppendInternalWall(flatShape, grooveShape, wallZ, vertices, mainTriangles, flip: false);
                }
            }
            else
            {
                Vector2[] baseCrossSection = profile.BuildCrossSection(size);
                entryCrossSection = OffsetY(baseCrossSection, halfDrop);
                exitCrossSection = OffsetY(baseCrossSection, -halfDrop);
                AppendTopSurface(entryCrossSection, -HalfLength, exitCrossSection, HalfLength, vertices, mainTriangles, grooveTriangles);
            }

            AppendSideWalls(entryCrossSection, exitCrossSection, vertices, mainTriangles);
            AppendEndCaps(entryCrossSection, exitCrossSection, vertices, mainTriangles);
            AppendBottom(entryCrossSection, vertices, mainTriangles);
        }

        private const int CurveRingSegments = 8; // resolution of the 90-degree arc sweep - see 0040

        /// Curved 90-degree sweep for a turning Normal block (see 0040):
        /// builds CurveRingSegments+1 rings along a quarter-circle arc from
        /// the entry edge (curveInVec side of the square footprint) to the
        /// exit edge (curveOutVec side), instead of BuildStraightMesh's
        /// single linear sweep.
        /// Geometry (see the 0040 plan for the full derivation): with
        /// R = HalfLength (== half the square footprint's width too),
        /// entry point A = -curveInVec*R, exit point B = curveOutVec*R, the
        /// arc center is C = R*(curveOutVec - curveInVec) - this places
        /// both A and B at distance R from C for ANY curveInVec/curveOutVec,
        /// and since they're perpendicular here, exactly a 90-degree arc.
        /// At parameter t in [0,1] (angle = t*90 degrees):
        ///   position(t)  = C + R * (cos(angle)*(-curveOutVec) + sin(angle)*curveInVec)
        ///   forward(t)   ~ sin(angle)*curveOutVec + cos(angle)*curveInVec
        ///   right(t)     = Cross(up, forward(t)) - same convention as the
        ///                  implicit right=+X when forward=+Z in the
        ///                  straight sweep, just rotated per ring.
        /// Each ring's own Y offset ramps Tilt's Drop linearly across the
        /// sweep, same as the straight sweep's halfDrop but generalized
        /// from 2 rings to N.
        private void BuildCurvedMesh(List<Vector3> vertices, List<int> mainTriangles, List<int> grooveTriangles)
        {
            float radius = HalfLength;
            Vector3 center = radius * (curveOutVec - curveInVec);

            int ringCount = CurveRingSegments + 1;
            Vector2[] crossSection = profile.BuildCrossSection(size);

            // Which cross-section index (0 or the last one) is the
            // channel's OUTER rail - the one that actually needs the
            // corner filler - versus the INNER one that collapses exactly
            // onto the pivot corner (see AppendCurvedFillerSegment) depends
            // on the turn's rotational sense: Cross(up, curveInVec) equals
            // +curveOutVec for a "clockwise" turn (outer = index 0) and
            // -curveOutVec for "counter-clockwise" (outer = the last
            // index) - verified by hand for both senses. Getting this
            // backwards for one of the two senses is exactly what made the
            // filler attach to the wrong (already-at-the-corner) side,
            // stretching it across the groove instead of into the empty
            // far corner.
            bool isClockwise = Vector3.Dot(Vector3.Cross(Vector3.up, curveInVec), curveOutVec) > 0f;
            int railIndex = isClockwise ? 0 : crossSection.Length - 1;
            bool railFirst = !isClockwise; // see AppendCurvedFillerSegment's natural-order requirement

            Vector3[] ringCenters = new Vector3[ringCount];
            Vector3[] ringRights = new Vector3[ringCount];
            Vector2[][] ringCrossSections = new Vector2[ringCount][];
            Vector3[] ringFillPoints = new Vector3[ringCount]; // see AppendCurvedFillerSegment - fills the square's far corner

            for (int r = 0; r < ringCount; r++)
            {
                float t = (float)r / CurveRingSegments;
                float angle = t * Mathf.PI * 0.5f;
                Vector3 radial = Mathf.Cos(angle) * -curveOutVec + Mathf.Sin(angle) * curveInVec;
                Vector3 forward = Mathf.Sin(angle) * curveOutVec + Mathf.Cos(angle) * curveInVec;

                ringCenters[r] = center + radius * radial;
                ringRights[r] = Vector3.Cross(Vector3.up, forward);
                ringCrossSections[r] = OffsetY(crossSection, Drop * (0.5f - t));

                // Distance from center, along the same radial direction, to
                // where it hits the block's TRUE square boundary (the two
                // far edges, meeting at the corner diagonally opposite the
                // curve's pivot corner) - see the 0040 follow-up fix's
                // derivation. sin/cos of angle are exactly radial's own
                // dot products with curveInVec/-curveOutVec here, so this
                // needs no separate trig call; Max avoids a divide-by-zero
                // at the two ends, where it correctly matches the
                // cross-section's own half-width (radius) exactly.
                float fillDistance = 2f * radius / Mathf.Max(Mathf.Sin(angle), Mathf.Cos(angle));
                ringFillPoints[r] = center + fillDistance * radial + Vector3.up * (Drop * (0.5f - t));
            }

            for (int r = 0; r < ringCount - 1; r++)
            {
                AppendCurvedTopSurfaceSegment(ringCenters[r], ringRights[r], ringCrossSections[r],
                    ringCenters[r + 1], ringRights[r + 1], ringCrossSections[r + 1], vertices, mainTriangles, grooveTriangles);
                AppendCurvedSideWallSegment(ringCenters[r], ringRights[r], ringCrossSections[r],
                    ringCenters[r + 1], ringRights[r + 1], ringCrossSections[r + 1], vertices, mainTriangles);
                AppendCurvedBottomSegment(ringCenters[r], ringRights[r], ringCrossSections[r],
                    ringCenters[r + 1], ringRights[r + 1], ringCrossSections[r + 1], vertices, mainTriangles);

                Vector3 nearRail = RingPoint(ringCenters[r], ringRights[r], ringCrossSections[r][railIndex]);
                Vector3 farRail = RingPoint(ringCenters[r + 1], ringRights[r + 1], ringCrossSections[r + 1][railIndex]);
                AppendCurvedFillerSegment(nearRail, ringFillPoints[r], farRail, ringFillPoints[r + 1], railFirst, vertices, mainTriangles);
            }

            AppendCurvedSkirt(ringCenters[0], ringRights[0], ringCrossSections[0], vertices, mainTriangles, flip: true);
            AppendCurvedSkirt(ringCenters[ringCount - 1], ringRights[ringCount - 1], ringCrossSections[ringCount - 1], vertices, mainTriangles, flip: false);
        }

        /// Maps a profile's 2D cross-section point (x = lateral offset,
        /// y = vertical offset) into 3D through a ring's own center/right
        /// transform - the curved-sweep equivalent of a straight sweep's
        /// implicit (x, y, z) construction.
        private static Vector3 RingPoint(Vector3 center, Vector3 right, Vector2 point) => center + right * point.x + Vector3.up * point.y;

        private static Vector3 RingFloorPoint(Vector3 center, Vector3 right, float x, float bottomY) => center + right * x + Vector3.up * bottomY;

        /// Curved analogue of AppendTopSurface: identical winding/groove-
        /// split logic, just mapping each cross-section point through its
        /// own ring's center/right transform instead of a shared (x, y, z).
        private void AppendCurvedTopSurfaceSegment(Vector3 nearCenter, Vector3 nearRight, Vector2[] nearCrossSection,
            Vector3 farCenter, Vector3 farRight, Vector2[] farCrossSection,
            List<Vector3> vertices, List<int> mainTriangles, List<int> grooveTriangles)
        {
            int nearRing = vertices.Count;
            for (int j = 0; j < nearCrossSection.Length; j++)
                vertices.Add(RingPoint(nearCenter, nearRight, nearCrossSection[j]));

            int farRing = vertices.Count;
            for (int j = 0; j < farCrossSection.Length; j++)
                vertices.Add(RingPoint(farCenter, farRight, farCrossSection[j]));

            for (int j = 0; j < nearCrossSection.Length - 1; j++)
            {
                int a = nearRing + j;
                int b = nearRing + j + 1;
                int c = farRing + j;
                int d = farRing + j + 1;

                List<int> triangles = profile.IsGrooveSegment(j, size) ? grooveTriangles : mainTriangles;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        /// Curved analogue of AppendSideWalls, one ring-segment at a time -
        /// a lofted band instead of a single full-length quad - reusing the
        /// existing AppendQuad since both walls are already fully resolved
        /// 3D points here. Same flip convention as AppendSideWalls (false
        /// for the left rail, true for the right rail).
        private void AppendCurvedSideWallSegment(Vector3 nearCenter, Vector3 nearRight, Vector2[] nearCrossSection,
            Vector3 farCenter, Vector3 farRight, Vector2[] farCrossSection, List<Vector3> vertices, List<int> triangles)
        {
            Vector2 nearLeft = nearCrossSection[0];
            Vector2 farLeft = farCrossSection[0];
            Vector2 nearRightPoint = nearCrossSection[nearCrossSection.Length - 1];
            Vector2 farRightPoint = farCrossSection[farCrossSection.Length - 1];

            AppendQuad(
                RingPoint(nearCenter, nearRight, nearLeft), RingPoint(farCenter, farRight, farLeft),
                RingFloorPoint(nearCenter, nearRight, nearLeft.x, BottomY), RingFloorPoint(farCenter, farRight, farLeft.x, BottomY),
                vertices, triangles, flip: false);

            AppendQuad(
                RingPoint(nearCenter, nearRight, nearRightPoint), RingPoint(farCenter, farRight, farRightPoint),
                RingFloorPoint(nearCenter, nearRight, nearRightPoint.x, BottomY), RingFloorPoint(farCenter, farRight, farRightPoint.x, BottomY),
                vertices, triangles, flip: true);
        }

        /// Curved analogue of AppendBottom, one ring-segment at a time -
        /// both rings' left/right footprint points at BottomY, still a flat
        /// quad since BottomY is constant across every ring.
        private void AppendCurvedBottomSegment(Vector3 nearCenter, Vector3 nearRight, Vector2[] nearCrossSection,
            Vector3 farCenter, Vector3 farRight, Vector2[] farCrossSection, List<Vector3> vertices, List<int> triangles)
        {
            float nearLeftX = nearCrossSection[0].x;
            float nearRightX = nearCrossSection[nearCrossSection.Length - 1].x;
            float farLeftX = farCrossSection[0].x;
            float farRightX = farCrossSection[farCrossSection.Length - 1].x;

            AppendQuad(
                RingFloorPoint(nearCenter, nearRight, nearLeftX, BottomY), RingFloorPoint(nearCenter, nearRight, nearRightX, BottomY),
                RingFloorPoint(farCenter, farRight, farLeftX, BottomY), RingFloorPoint(farCenter, farRight, farRightX, BottomY),
                vertices, triangles, flip: true);
        }

        /// Fills the block's TRUE square footprint out to its actual edges:
        /// the channel tube built by AppendCurved*Segment above is
        /// deliberately narrower than the full square (its own outer rail -
        /// nearRail/farRail, at whichever cross-section index BuildCurvedMesh
        /// determined is the outer one for this turn's rotational sense -
        /// traces an arc that cuts across the corner diagonally opposite
        /// the curve's pivot corner instead of reaching it), so without
        /// this the block's silhouette would be rounded there instead of
        /// square. This adds the missing flat shoulder strip from that
        /// same outer rail out to nearFill/farFill (the true square
        /// boundary, see BuildCurvedMesh's fillDistance) - top, bottom, and
        /// the true exterior wall at the outer edge. Self-contained/
        /// watertight on its own; harmlessly coincides with the tube's own
        /// outer wall (see AppendCurvedSideWallSegment) at their shared
        /// seam.
        /// railFirst says whether the rail point sits at the LOWER lateral
        /// index than the (virtual, one-step-further-out) fill point, or
        /// the other way around - it flips between the two rotational
        /// senses because the outer rail itself is at index 0 for one
        /// sense and at the last index for the other (see BuildCurvedMesh),
        /// and getting this order backwards inverts the resulting normal -
        /// see AppendCurvedTopSurfaceSegment's own (a, c, b)(b, c, d)
        /// pattern, which this mirrors for a "natural low-to-high index"
        /// pair plus AppendQuad's flip.
        private void AppendCurvedFillerSegment(Vector3 nearRail, Vector3 nearFill, Vector3 farRail, Vector3 farFill,
            bool railFirst, List<Vector3> vertices, List<int> triangles)
        {
            Vector3 nearRailBottom = new Vector3(nearRail.x, BottomY, nearRail.z);
            Vector3 nearFillBottom = new Vector3(nearFill.x, BottomY, nearFill.z);
            Vector3 farRailBottom = new Vector3(farRail.x, BottomY, farRail.z);
            Vector3 farFillBottom = new Vector3(farFill.x, BottomY, farFill.z);

            Vector3 nearLow = railFirst ? nearRail : nearFill;
            Vector3 nearHigh = railFirst ? nearFill : nearRail;
            Vector3 farLow = railFirst ? farRail : farFill;
            Vector3 farHigh = railFirst ? farFill : farRail;
            Vector3 nearLowBottom = railFirst ? nearRailBottom : nearFillBottom;
            Vector3 nearHighBottom = railFirst ? nearFillBottom : nearRailBottom;
            Vector3 farLowBottom = railFirst ? farRailBottom : farFillBottom;
            Vector3 farHighBottom = railFirst ? farFillBottom : farRailBottom;

            // Top/bottom: same natural-low-to-high-index pattern as
            // AppendCurvedTopSurfaceSegment/AppendCurvedBottomSegment
            // (flip:false for the up-facing top, flip:true for the
            // down-facing bottom), just with the rail/fill order swapped
            // per railFirst.
            AppendQuad(nearLow, nearHigh, farLow, farHigh, vertices, triangles, flip: false);
            AppendQuad(nearLowBottom, nearHighBottom, farLowBottom, farHighBottom, vertices, triangles, flip: true);
            // True exterior wall, at the fill boundary itself. Its outward
            // direction is Cross(sweepDirection, up) for flip:false (see
            // AppendSideWalls' own hand-verified left/right convention,
            // e.g. flip:false => -X when sweeping toward +Z) - and the fill
            // boundary's own sweep direction (which of the block's two far
            // edges it's tracing, and which way along it) reverses between
            // the two rotational senses, so this needs the SAME railFirst
            // flip as the top/bottom pair above (verified by hand for both
            // senses: flip:false is correct for clockwise, flip:true for
            // counter-clockwise - getting this wrong made the true
            // exterior wall face inward instead of outward for one sense).
            AppendQuad(nearFill, farFill, nearFillBottom, farFillBottom, vertices, triangles, flip: railFirst);
        }

        /// Curved analogue of AppendSkirt, for a single ring's own cross-
        /// section - used only at the two true ends of the curve (ring 0
        /// and the last ring), same as AppendEndCaps for a straight sweep.
        private void AppendCurvedSkirt(Vector3 center, Vector3 right, Vector2[] crossSection, List<Vector3> vertices, List<int> triangles, bool flip)
        {
            for (int j = 0; j < crossSection.Length - 1; j++)
            {
                Vector2 a = crossSection[j];
                Vector2 b = crossSection[j + 1];
                AppendQuad(
                    RingPoint(center, right, a), RingPoint(center, right, b),
                    RingFloorPoint(center, right, a.x, BottomY), RingFloorPoint(center, right, b.x, BottomY),
                    vertices, triangles, flip);
            }
        }

        private static PhysicsMaterial lowFrictionMaterial;

        /// Low friction/no bounce so a marble rolls smoothly across block
        /// boundaries instead of catching or bouncing at an edge.
        private static PhysicsMaterial LowFrictionMaterial()
        {
            if (lowFrictionMaterial == null)
            {
                lowFrictionMaterial = new PhysicsMaterial("TrackBlockSurface")
                {
                    dynamicFriction = 0.05f,
                    staticFriction = 0.05f,
                    bounciness = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
            }
            return lowFrictionMaterial;
        }

        private static Vector2[] OffsetY(Vector2[] source, float dy)
        {
            Vector2[] result = new Vector2[source.Length];
            for (int i = 0; i < source.Length; i++) result[i] = new Vector2(source[i].x, source[i].y + dy);
            return result;
        }

        private void CacheComponents()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
        }

        private void ApplyMaterial()
        {
            CacheComponents();
            if (material == null) return;
            meshRenderer.sharedMaterials = new[] { material, grooveMaterial != null ? grooveMaterial : material };
        }

        /// Yaw only - rotates the whole block around the vertical axis so
        /// it faces the travel direction. Tilt lives in the mesh itself
        /// (see Rebuild), not in the transform, so the block's footprint
        /// stays a normal, level box (flat bottom, vertical sides) at any
        /// yaw, and yaw/tilt never need to be composed into one rotation.
        private void ApplyOrientation()
        {
            transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
        }

        private float HalfLength => size.y * 0.5f;
        private float BottomY => -height;

        /// Splits the top surface's quads across the two submeshes (see
        /// Rebuild) by asking the profile which cross-section segments are
        /// groove vs. shoulder, so e.g. GrooveBlockProfile's rollable U can
        /// render in its own material distinct from the flat shoulders.
        /// nearZ/farZ let a closed-end block (see IClosedEndBlockProfile)
        /// call this twice - once per half - instead of always spanning the
        /// full -HalfLength..HalfLength range. forceShoulderMaterial skips
        /// the groove/shoulder split entirely (always shoulder material) -
        /// used for a closed-end block's flat half, which has no groove to
        /// speak of even though it shares the groove profile's own point
        /// layout (see FlattenToRim).
        private void AppendTopSurface(Vector2[] nearCrossSection, float nearZ, Vector2[] farCrossSection, float farZ, List<Vector3> vertices, List<int> mainTriangles, List<int> grooveTriangles, bool forceShoulderMaterial = false)
        {
            int nearRing = vertices.Count;
            for (int j = 0; j < nearCrossSection.Length; j++)
                vertices.Add(new Vector3(nearCrossSection[j].x, nearCrossSection[j].y, nearZ));

            int farRing = vertices.Count;
            for (int j = 0; j < farCrossSection.Length; j++)
                vertices.Add(new Vector3(farCrossSection[j].x, farCrossSection[j].y, farZ));

            for (int j = 0; j < nearCrossSection.Length - 1; j++)
            {
                int a = nearRing + j;
                int b = nearRing + j + 1;
                int c = farRing + j;
                int d = farRing + j + 1;

                List<int> triangles = !forceShoulderMaterial && profile.IsGrooveSegment(j, size) ? grooveTriangles : mainTriangles;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        /// Same X positions as the given cross-section, but flattened to
        /// Y=0 (the shoulder/rim height) - the "closed" counterpart to a
        /// groove cross-section, used by a closed-end block's flat half
        /// (see IClosedEndBlockProfile) so it shares that cross-section's
        /// exact point layout and can be joined to it point-for-point by
        /// AppendInternalWall.
        private static Vector2[] FlattenToRim(Vector2[] source)
        {
            Vector2[] result = new Vector2[source.Length];
            for (int i = 0; i < source.Length; i++) result[i] = new Vector2(source[i].x, 0f);
            return result;
        }

        /// Seals the step between a closed-end block's flat half and its
        /// groove half at their shared Z (see Rebuild) - like AppendSkirt,
        /// but dropping to another cross-section's own per-point height
        /// instead of a constant BottomY, so it exactly fills the gap where
        /// the flat top (Y=0 throughout) sits above the groove's arc
        /// (dipping below Y=0). Degenerates to a zero-height, invisible
        /// quad on the shoulder segments, where both cross-sections already
        /// agree (Y=0), which is harmless.
        private static void AppendInternalWall(Vector2[] topCrossSection, Vector2[] bottomCrossSection, float z, List<Vector3> vertices, List<int> triangles, bool flip)
        {
            for (int j = 0; j < topCrossSection.Length - 1; j++)
            {
                Vector2 topA = topCrossSection[j];
                Vector2 topB = topCrossSection[j + 1];
                Vector2 bottomA = bottomCrossSection[j];
                Vector2 bottomB = bottomCrossSection[j + 1];
                AppendQuad(
                    new Vector3(topA.x, topA.y, z), new Vector3(topB.x, topB.y, z),
                    new Vector3(bottomA.x, bottomA.y, z), new Vector3(bottomB.x, bottomB.y, z),
                    vertices, triangles, flip);
            }
        }

        /// Flat side walls at the leftmost/rightmost cross-section point,
        /// dropping from the (possibly tilted) top surface down to the
        /// flat bottom, spanning the full entry-to-exit length. Each wall
        /// stays exactly in its X = const plane (tilt only ever changes Y),
        /// so it remains a vertical wall even when the top surface slopes.
        private void AppendSideWalls(Vector2[] entryCrossSection, Vector2[] exitCrossSection, List<Vector3> vertices, List<int> triangles)
        {
            Vector2 entryLeft = entryCrossSection[0];
            Vector2 exitLeft = exitCrossSection[0];
            Vector2 entryRight = entryCrossSection[entryCrossSection.Length - 1];
            Vector2 exitRight = exitCrossSection[exitCrossSection.Length - 1];

            // Note: flip is inverted relative to the Z-constant skirts below -
            // for an X-constant plane, the same topLeft/topRight/bottomLeft/
            // bottomRight winding produces the opposite outward direction.
            AppendQuad(
                new Vector3(entryLeft.x, entryLeft.y, -HalfLength), new Vector3(exitLeft.x, exitLeft.y, HalfLength),
                new Vector3(entryLeft.x, BottomY, -HalfLength), new Vector3(exitLeft.x, BottomY, HalfLength),
                vertices, triangles, flip: false);

            AppendQuad(
                new Vector3(entryRight.x, entryRight.y, -HalfLength), new Vector3(exitRight.x, exitRight.y, HalfLength),
                new Vector3(entryRight.x, BottomY, -HalfLength), new Vector3(exitRight.x, BottomY, HalfLength),
                vertices, triangles, flip: true);
        }

        /// Entry/exit skirts: trace each ring's own silhouette down to the
        /// flat bottom, closing the block off there (so a standalone block
        /// is watertight even where a neighbor isn't chained on yet).
        private void AppendEndCaps(Vector2[] entryCrossSection, Vector2[] exitCrossSection, List<Vector3> vertices, List<int> triangles)
        {
            AppendSkirt(entryCrossSection, -HalfLength, vertices, triangles, flip: true);
            AppendSkirt(exitCrossSection, HalfLength, vertices, triangles, flip: false);
        }

        private void AppendSkirt(Vector2[] crossSection, float z, List<Vector3> vertices, List<int> triangles, bool flip)
        {
            for (int j = 0; j < crossSection.Length - 1; j++)
            {
                Vector2 a = crossSection[j];
                Vector2 b = crossSection[j + 1];
                AppendQuad(
                    new Vector3(a.x, a.y, z), new Vector3(b.x, b.y, z),
                    new Vector3(a.x, BottomY, z), new Vector3(b.x, BottomY, z),
                    vertices, triangles, flip);
            }
        }

        /// Flat bottom rectangle. X range is identical for the entry and
        /// exit rings (tilt only ever changes Y), so either can be used.
        private void AppendBottom(Vector2[] crossSection, List<Vector3> vertices, List<int> triangles)
        {
            float left = crossSection[0].x;
            float right = crossSection[crossSection.Length - 1].x;

            AppendQuad(
                new Vector3(left, BottomY, -HalfLength), new Vector3(right, BottomY, -HalfLength),
                new Vector3(left, BottomY, HalfLength), new Vector3(right, BottomY, HalfLength),
                vertices, triangles, flip: true);
        }

        private static void AppendQuad(Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight,
            List<Vector3> vertices, List<int> triangles, bool flip)
        {
            int baseIndex = vertices.Count;
            vertices.Add(topLeft);
            vertices.Add(topRight);
            vertices.Add(bottomLeft);
            vertices.Add(bottomRight);

            if (!flip)
            {
                triangles.Add(baseIndex); triangles.Add(baseIndex + 2); triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 1); triangles.Add(baseIndex + 2); triangles.Add(baseIndex + 3);
            }
            else
            {
                triangles.Add(baseIndex); triangles.Add(baseIndex + 1); triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1); triangles.Add(baseIndex + 3); triangles.Add(baseIndex + 2);
            }
        }
    }
}
