using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Drives the marbles of every completed Start-to-Goal track of
    /// PathGrid's last validation, all running concurrently. Each track
    /// loops on one shared BeatClock (see 0043): only the blocks between
    /// Start and Goal count as the loop's steps, Start and Goal overlap
    /// the neighbouring laps - so a 16-step loop is an 18-block track -
    /// and every lap starts on a downbeat of the level's loop
    /// (LevelData.LoopLengthSteps). Play is refused whenever no track is
    /// currently complete.
    /// Keyboard-driven for now: SPACE toggles between planning (stopped,
    /// pipes editable) and simulation (playing) - S stops without clearing,
    /// R resets - so it is testable without any UI; the public methods are
    /// ready for UI buttons later.
    /// Lives on its own GameObject; grid is wired in the Inspector or
    /// auto-found at Awake. Cell-content reactions (sound, visual
    /// feedback) live entirely on the triggered TrackBlock's sibling
    /// components (see 0023) - this class only decides WHETHER a block
    /// triggers, never HOW it reacts, so it has no AudioSource of its own.
    /// </summary>
    public class MarbleController : MonoBehaviour
    {
        /// See 0014: three ways to try the marble's movement.
        /// Kinematic2D is the original, flat 2D grid movement (unchanged).
        /// Kinematic3D samples TrackBlockSpawner's groove directly, no
        /// physics engine involved. Physics3D drops a Rigidbody marble onto
        /// the spawned blocks' MeshColliders and lets gravity/collision roll it.
        public enum MovementMode { Kinematic2D, Kinematic3D, Physics3D }

        [SerializeField] private PathGrid grid;
        [SerializeField] private TrackBlockSpawner terrain;
        [SerializeField] private MovementMode movementMode = MovementMode.Kinematic2D;
        [SerializeField] private float cellsPerSecond = 3f;
        [SerializeField] private float marbleRadius = 0.15f;
        [SerializeField] private float marbleRadius3D = 0.1f; // separate, smaller by default so it doesn't stick in the groove
        [SerializeField] private Color marbleColor = new Color(0.1f, 0.1f, 0.1f);
        [SerializeField] private float physicsDropHeight = 0.05f; // extra height above the Start the physics marble drops from
        [SerializeField] private float physicsSpawnOffset = 0.3f; // fraction of the first cell the physics marble spawns past Start, so it lands where the slope is already there
        [SerializeField] private float physicsGoalRadius = 0.25f; // horizontal distance to Goal at which a physics marble counts as arrived
        [SerializeField] private float physicsTimeoutMultiplier = 4f; // safety margin over the kinematic duration before a stuck physics marble is force-ended

        private readonly List<Marble> marbles = new List<Marble>();
        private int activeRunCount;
        private BeatClock clock;
        private int stepsPerLoop;

        /// How long the marble may wait for TrackBlockSpawner to actually
        /// spawn a just-completed track's blocks before this lap is given
        /// up on - the two components' Update order is undefined, so the
        /// very frame Play() was pressed the blocks may not exist yet.
        private const float TrackSpawnTimeoutSeconds = 1f;

        public bool IsPlaying => activeRunCount > 0;
        public bool CanPlay => HasCompletedTrack();
        public float MarbleRadius => marbleRadius;
        public float MarbleRadius3D => marbleRadius3D;

        /// The marble a follow-camera (see CameraModeTransition) should
        /// track: simply the first currently active one. Good enough for
        /// the common single-track case; with multiple concurrent tracks
        /// it's an arbitrary pick that can shift to a different track when
        /// this one loops (RunTrack re-adds its fresh marble at the end of
        /// the list each lap), rather than something worth tracking more
        /// precisely for a first version.
        public Transform PrimaryMarbleTransform => marbles.Count > 0 ? marbles[0].transform : null;

        private void Awake()
        {
            if (grid == null) grid = FindAnyObjectByType<PathGrid>();
            if (terrain == null) terrain = FindAnyObjectByType<TrackBlockSpawner>();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.spaceKey.wasPressedThisFrame) TogglePlay();
            if (Keyboard.current.sKey.wasPressedThisFrame) Stop();
            if (Keyboard.current.rKey.wasPressedThisFrame) ResetMarble();
        }

        /// Switches between planning (stopped, pipes editable) and
        /// simulation: stops and clears a running simulation, or starts one
        /// if a completed track exists. No-op if no track is valid yet.
        public bool TogglePlay()
        {
            if (IsPlaying)
            {
                ResetMarble();
                return true;
            }

            return Play();
        }

        public bool Play()
        {
            if (IsPlaying) return true;

            if (!CanPlay)
            {
                Debug.LogWarning("MarbleController: Play ignoriert, aktuell existiert keine gueltige Bahn.");
                return false;
            }

            ClearMarbles();
            activeRunCount = 0;

            // Beat 0 is now - every track's first lap starts on it.
            clock = new BeatClock(cellsPerSecond);
            stepsPerLoop = grid.Level != null ? grid.Level.LoopLengthSteps : 0;

            foreach (PathValidationResult result in grid.LastValidations)
            {
                if (!result.GoalReached) continue;

                activeRunCount++;
                StartCoroutine(RunTrack(result.OrderedPath[0]));
            }

            return true;
        }

        /// Stops every track loop and every lap still in flight (see
        /// RunLap) - they're the only coroutines this component runs.
        public void Stop()
        {
            StopAllCoroutines();
            activeRunCount = 0;
        }

        public void ResetMarble()
        {
            Stop();
            ClearMarbles();
        }

        private bool HasCompletedTrack()
        {
            IReadOnlyList<PathValidationResult> results = grid.LastValidations;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].GoalReached) return true;
            }
            return false;
        }

        private void ClearMarbles()
        {
            foreach (Marble marble in marbles)
            {
                if (marble != null) Destroy(marble.gameObject);
            }
            marbles.Clear();
        }

        /// Keeps one track looping for as long as it stays completely
        /// validated (see 0043). Start and Goal are outside the loop's bar
        /// (always silent, see TrackBlockSpawner), so a lap's steps are
        /// only path[1..Count-2] and Start/Goal overlap the neighbouring
        /// laps: the next marble appears on Start the moment the current
        /// one reaches the last block before Goal. On a track exactly one
        /// loop long it then reaches path[1] on the very beat the current
        /// one reaches Goal - 16 steps = 18 blocks, looping every 16
        /// beats. On a shorter track it waits parked on Start until its
        /// lap is due.
        /// Each lap runs as its own coroutine (RunLap), since two marbles
        /// share the track during that overlap. Laps are scheduled on the
        /// shared clock's beat grid - lap k rolls off Start at exactly
        /// k * LapLengthInSteps beats, so nothing drifts. The path is
        /// re-resolved before every lap, so a pipe swap that breaks the
        /// track stops the loop at the next lap.
        private IEnumerator RunTrack(Vector2Int startCoord)
        {
            IReadOnlyList<Vector2Int> path = FindCurrentPath(startCoord);
            double startBeat = 0d; // beat this lap's marble rolls off Start - Play() just started the clock

            while (path != null)
            {
                Marble marble = CreateMarbleForMode();
                marbles.Add(marble);
                StartCoroutine(RunLap(marble, path, startBeat));

                double nextSpawnBeat = startBeat + path.Count - 2; // this marble reaches the last block before Goal
                startBeat += LapLengthInSteps(path.Count);

                while (clock.CurrentBeat < nextSpawnBeat) yield return null;
                path = FindCurrentPath(startCoord);
            }

            activeRunCount--;
        }

        /// One marble's single run from Start to Goal (waiting parked on
        /// Start until startBeat first), after which it vanishes.
        private IEnumerator RunLap(Marble marble, IReadOnlyList<Vector2Int> path, double startBeat)
        {
            switch (movementMode)
            {
                case MovementMode.Kinematic3D:
                    yield return RunAlongPath3D(marble, path, startBeat);
                    break;
                case MovementMode.Physics3D:
                    yield return RunAlongPathPhysics(marble, path, startBeat);
                    break;
                default:
                    yield return RunAlongPath(marble, path, startBeat);
                    break;
            }

            marble.gameObject.SetActive(false);
            Destroy(marble.gameObject);
            marbles.Remove(marble);
        }

        /// A track's lap in beats: its steps (every block except Start and
        /// Goal) rounded up to whole loops of stepsPerLoop, so every track
        /// restarts on a shared downbeat - a shorter track rests until the
        /// next one, a longer one spans several loops. stepsPerLoop 0 falls
        /// back to the step count itself. Never below one beat, so a bare
        /// Start-Goal track can't spawn marbles endlessly.
        private int LapLengthInSteps(int pathLength)
        {
            int steps = Mathf.Max(pathLength - 2, 1);
            if (stepsPerLoop <= 0) return steps;
            return (steps + stepsPerLoop - 1) / stepsPerLoop * stepsPerLoop;
        }

        private Marble CreateMarbleForMode()
        {
            switch (movementMode)
            {
                case MovementMode.Kinematic3D:
                    return Marble.CreateSphere3D(transform, marbleRadius3D, withPhysics: false);
                case MovementMode.Physics3D:
                    return Marble.CreateSphere3D(transform, marbleRadius3D, withPhysics: true);
                default:
                    return Marble.Create(transform, marbleRadius, marbleColor);
            }
        }

        private IReadOnlyList<Vector2Int> FindCurrentPath(Vector2Int startCoord)
        {
            foreach (PathValidationResult result in grid.LastValidations)
            {
                if (!result.GoalReached) continue;
                if (result.OrderedPath.Count == 0 || result.OrderedPath[0] != startCoord) continue;
                return result.OrderedPath;
            }
            return null;
        }

        /// Flat 2D movement: cell i is reached at beat i of the lap, and the
        /// Goal holds its beat like every other cell, so a lap is
        /// path.Count beats in this mode too. Waits parked on Start until
        /// the lap's downbeat.
        private IEnumerator RunAlongPath(Marble marble, IReadOnlyList<Vector2Int> path, double lapStartBeat)
        {
            int lastIndex = path.Count - 1;
            int nextTrigger = 0;

            while (true)
            {
                double lapBeat = clock.CurrentBeat - lapStartBeat;

                float position = (float)System.Math.Max(0d, System.Math.Min(lapBeat, lastIndex));
                int from = Mathf.FloorToInt(position);
                int to = Mathf.Min(from + 1, lastIndex);
                marble.transform.localPosition = Vector3.Lerp(grid.CellToLocalPosition(path[from]), grid.CellToLocalPosition(path[to]), position - from);

                // Every cell reached by now - several at once only after a
                // frame hitch, so no note is ever skipped.
                while (nextTrigger <= lastIndex && nextTrigger <= lapBeat) TriggerCellContent(marble, path[nextTrigger++]);

                if (lapBeat >= path.Count) yield break;
                yield return null;
            }
        }

        /// Kinematic 3D movement: no physics engine involved. Every block
        /// defines the path the marble takes across IT - a straight roll,
        /// a curve's real arc, or a Trigger block's fall-bounce-roll onto
        /// its pad (see IMarbleTrace/0038) - and this simply plays those
        /// traces back end to end, each in its own real time, so the
        /// marble rolls away from Start, actually falls where the track
        /// drops, and vanishes into the Goal hole exactly where the
        /// terrain's hole is (see 0013/0014). Its rotation is faked to
        /// match (see RollMarble) from each frame's position delta, since
        /// sampling alone never turns it.
        /// Every block takes exactly one beat (1 / cellsPerSecond, see
        /// TrackTraceSegment.Duration), whatever its geometry - a cell is
        /// a beat, which is what keeps the sounds the marble sets off in
        /// time. Only the marble's pace WITHIN a block varies.
        /// A block's own trigger fires at its trace's ImpactTime - i.e.
        /// when the marble is SEEN arriving (hitting the pad), not when it
        /// crosses the cell boundary. That phase is identical on every
        /// Trigger block (see TriggerFallMarbleTrace), so the notes stay a
        /// whole number of beats apart.
        /// Position is read off the shared clock every frame (block =
        /// whole beats into the lap, progress = the fraction), never
        /// accumulated from Time.deltaTime - so it can't drift (see 0043).
        /// Before the lap's downbeat the marble waits parked at the very
        /// beginning of Start's trace.
        private IEnumerator RunAlongPath3D(Marble marble, IReadOnlyList<Vector2Int> path, double lapStartBeat)
        {
            // Park the marble at Start right away (a rough guess is fine -
            // it's replaced by the first real trace sample below), so it
            // never shows up at the origin for the frames the track's
            // blocks may still need to appear.
            marble.transform.position = terrain.GetShoulderWorldPosition(path, 0f) + Vector3.up * marbleRadius3D;

            float waited = 0f;
            while (!terrain.HasTrackFor(path) && waited < TrackSpawnTimeoutSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            bool hasPreviousPosition = false;
            Vector3 previousPosition = Vector3.zero;
            int nextTrigger = 0;

            while (true)
            {
                double lapBeat = clock.CurrentBeat - lapStartBeat;

                // Segments are re-resolved every frame rather than cached
                // for the whole lap, so a pipe swap that rebuilds this
                // track underneath a running marble ends its lap cleanly
                // (RunTrack then re-checks the path) instead of sampling
                // dead blocks.
                double position = System.Math.Max(0d, System.Math.Min(lapBeat, path.Count));
                int index = System.Math.Min((int)position, path.Count - 1);
                if (!terrain.TryGetTraceSegment(path, index, cellsPerSecond, out TrackTraceSegment segment)) yield break;

                Vector3 worldPosition = segment.SampleWorld((float)(position - index) * segment.Duration, marbleRadius3D);
                if (hasPreviousPosition) RollMarble(marble, previousPosition, worldPosition);
                marble.transform.position = worldPosition;
                previousPosition = worldPosition;
                hasPreviousPosition = true;

                // Every block whose impact moment has passed - several at
                // once only after a frame hitch, so no note is ever skipped.
                while (nextTrigger < path.Count)
                {
                    if (!terrain.TryGetTraceSegment(path, nextTrigger, cellsPerSecond, out TrackTraceSegment triggerSegment)) yield break;
                    if (lapBeat < nextTrigger + triggerSegment.ImpactTime / triggerSegment.Duration) break;
                    TriggerCellContent(marble, path[nextTrigger++]);
                }

                if (lapBeat >= path.Count) yield break;
                yield return null;
            }
        }

        /// Kinematic3D has no physics engine to derive rolling from (unlike
        /// Physics3D, where real rolling contact against the MeshCollider
        /// already produces it) - a trace only ever moves the marble's
        /// position, never its rotation. This fakes the same
        /// visual: a sphere of marbleRadius3D rolling without slipping from
        /// `from` to `to` turns by angle = distance/radius, around the axis
        /// Vector3.Cross(Vector3.up, direction) - which is exactly the
        /// world-space angular velocity a real rolling ball would have here
        /// (from v = radius * (omega x up), solved for omega), so the top
        /// of the marble always turns toward where it's actually heading
        /// instead of spinning on an arbitrary or backwards axis.
        /// Only the HORIZONTAL part of the step drives it: rolling contact
        /// is what spins a marble, and a fall (see TriggerFallMarbleTrace)
        /// has none - counting its drop as rolling distance would make the
        /// marble spin up wildly in mid-air. On the track's own gentle
        /// slopes the two are the same to within a fraction of a percent.
        private void RollMarble(Marble marble, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance < 1e-6f) return;

            Vector3 axis = Vector3.Cross(Vector3.up, delta / distance);
            if (axis.sqrMagnitude < 1e-6f) return; // travel direction parallel to world-up - no well-defined roll axis (shouldn't occur on this track)

            float angleDegrees = (distance / marbleRadius3D) * Mathf.Rad2Deg;
            marble.transform.rotation = Quaternion.AngleAxis(angleDegrees, axis.normalized) * marble.transform.rotation;
        }

        /// Physics-based movement: drops a gravity-driven Rigidbody marble
        /// just above the track - slightly past Start (physicsSpawnOffset)
        /// so it lands where the downhill slope is already present and
        /// immediately starts rolling, rather than on the flat solid cap
        /// right at Start itself - and lets Unity physics roll it along the
        /// terrain's MeshCollider groove until it gets close to the Goal
        /// hole (or a generous timeout elapses, in case it derails).
        /// Held kinematic above Start until the lap's downbeat, so it's
        /// only ever dropped ON the beat grid - the roll itself isn't.
        private IEnumerator RunAlongPathPhysics(Marble marble, IReadOnlyList<Vector2Int> path, double lapStartBeat)
        {
            Vector3 startPos = terrain.GetShoulderWorldPosition(path, physicsSpawnOffset) + Vector3.up * (marbleRadius3D + physicsDropHeight);
            marble.transform.position = startPos;

            Rigidbody rb = marble.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            while (clock.CurrentBeat < lapStartBeat) yield return null;

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
            }

            TriggerCellContent(marble, path[0]);

            // path.Count is the very END of the last block's own trace (see
            // TrackBlockSpawner.GetShoulderWorldPosition) - i.e. the Goal's
            // sealed groove end inside the tunnel mouth, which is where a
            // marble that has really arrived ends up.
            Vector3 goalPos = terrain.GetShoulderWorldPosition(path, path.Count);
            float timeout = (path.Count - 1) / Mathf.Max(cellsPerSecond, 0.01f) * physicsTimeoutMultiplier + 2f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                elapsed += Time.deltaTime;

                Vector3 marblePos = marble.transform.position;
                float horizontalDistSqr = new Vector2(marblePos.x - goalPos.x, marblePos.z - goalPos.z).sqrMagnitude;
                if (horizontalDistSqr <= physicsGoalRadius * physicsGoalRadius && marblePos.y <= goalPos.y + marbleRadius3D)
                {
                    break;
                }

                yield return null;
            }

            TriggerCellContent(marble, path[path.Count - 1]);
        }

        /// Fires the BlockTrigger on whichever TrackBlock sits at this
        /// cell, if it's configured to trigger at all (see 0027's
        /// BlockDefinition.Trigger, resolved once at spawn time from the
        /// cell's CellContentDefinition). What happens next - sound, visual
        /// feedback - is entirely up to that block's own sibling
        /// components (see 0023); this method deliberately knows nothing
        /// about any of that.
        private void TriggerCellContent(Marble marble, Vector2Int coord)
        {
            TrackBlock block = terrain != null ? terrain.GetBlockAt(coord) : null;
            if (block != null && block.Definition.Trigger != TriggerBehavior.None)
                block.GetComponent<BlockTrigger>()?.Fire();
        }
    }
}
