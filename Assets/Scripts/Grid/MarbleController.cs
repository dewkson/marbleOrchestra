using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Drives one Marble per completed Start-to-Goal track of PathGrid's
    /// last validation, all running concurrently. Each track loops: on
    /// reaching Goal, its marble is replaced by a fresh one starting at
    /// Start again, instantly and with no gap between laps. Play is
    /// refused whenever no track is currently complete.
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
        private readonly List<Coroutine> runRoutines = new List<Coroutine>();
        private int activeRunCount;

        public bool IsPlaying => activeRunCount > 0;
        public bool CanPlay => HasCompletedTrack();
        public float MarbleRadius => marbleRadius;
        public float MarbleRadius3D => marbleRadius3D;

        private void Awake()
        {
            if (grid == null) grid = FindFirstObjectByType<PathGrid>();
            if (terrain == null) terrain = FindFirstObjectByType<TrackBlockSpawner>();
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
            runRoutines.Clear();
            activeRunCount = 0;

            foreach (PathValidationResult result in grid.LastValidations)
            {
                if (!result.GoalReached) continue;

                Marble marble = CreateMarbleForMode();
                marbles.Add(marble);
                activeRunCount++;
                runRoutines.Add(StartCoroutine(RunTrack(marble, result.OrderedPath[0])));
            }

            return true;
        }

        public void Stop()
        {
            if (runRoutines.Count == 0) return;

            foreach (Coroutine routine in runRoutines)
            {
                StopCoroutine(routine);
            }
            runRoutines.Clear();
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

        /// Loops one marble around the track from startCoord for as long as
        /// that track stays completely validated. Re-resolves the current
        /// path from grid.LastValidations before every lap, so a pipe swap
        /// that breaks the track stops the loop at the next lap boundary.
        /// The old marble is destroyed and a fresh one spawned at Start in
        /// the same synchronous step (no yield in between), so the swap at
        /// Goal reads as instant rather than a teleport of one instance.
        private IEnumerator RunTrack(Marble marble, Vector2Int startCoord)
        {
            IReadOnlyList<Vector2Int> path = FindCurrentPath(startCoord);

            while (path != null)
            {
                switch (movementMode)
                {
                    case MovementMode.Kinematic3D:
                        yield return RunAlongPath3D(marble, path);
                        break;
                    case MovementMode.Physics3D:
                        yield return RunAlongPathPhysics(marble, path);
                        break;
                    default:
                        yield return RunAlongPath(marble, path);
                        break;
                }

                marble.gameObject.SetActive(false);
                Destroy(marble.gameObject);
                marbles.Remove(marble);

                path = FindCurrentPath(startCoord);
                if (path == null) break;

                marble = CreateMarbleForMode();
                marbles.Add(marble);
            }

            activeRunCount--;
        }

        private Marble CreateMarbleForMode()
        {
            switch (movementMode)
            {
                case MovementMode.Kinematic3D:
                    return Marble.CreateSphere3D(transform, marbleRadius3D, marbleColor, withPhysics: false);
                case MovementMode.Physics3D:
                    return Marble.CreateSphere3D(transform, marbleRadius3D, marbleColor, withPhysics: true);
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

        private IEnumerator RunAlongPath(Marble marble, IReadOnlyList<Vector2Int> path)
        {
            marble.transform.localPosition = grid.CellToLocalPosition(path[0]);
            TriggerCellContent(marble, path[0]);

            float duration = 1f / Mathf.Max(cellsPerSecond, 0.01f);

            for (int i = 1; i < path.Count; i++)
            {
                Vector3 from = grid.CellToLocalPosition(path[i - 1]);
                Vector3 to = grid.CellToLocalPosition(path[i]);

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    marble.transform.localPosition = Vector3.Lerp(from, to, elapsed / duration);
                    yield return null;
                }

                marble.transform.localPosition = to;
                TriggerCellContent(marble, path[i]);
            }
        }

        /// Kinematic 3D movement: no physics engine involved, the marble's
        /// world position is sampled directly from TrackBlockSpawner's
        /// groove geometry at the same cellsPerSecond tempo as the 2D mode,
        /// so it appears to roll away from Start and vanish into the Goal
        /// hole exactly where the terrain's hole is (see 0013/0014).
        private IEnumerator RunAlongPath3D(Marble marble, IReadOnlyList<Vector2Int> path)
        {
            float speed = Mathf.Max(cellsPerSecond, 0.01f);
            float endPosition = path.Count - 1;

            marble.transform.position = terrain.SampleGroovePosition(path, 0f, marbleRadius3D);
            TriggerCellContent(marble, path[0]);
            int lastTriggeredIndex = 0;

            float pathPosition = 0f;
            while (pathPosition < endPosition)
            {
                pathPosition = Mathf.Min(pathPosition + Time.deltaTime * speed, endPosition);
                marble.transform.position = terrain.SampleGroovePosition(path, pathPosition, marbleRadius3D);

                int currentIndex = Mathf.FloorToInt(pathPosition);
                if (currentIndex > lastTriggeredIndex)
                {
                    lastTriggeredIndex = currentIndex;
                    TriggerCellContent(marble, path[currentIndex]);
                }

                yield return null;
            }

            TriggerCellContent(marble, path[path.Count - 1]);
        }

        /// Physics-based movement: drops a gravity-driven Rigidbody marble
        /// just above the track - slightly past Start (physicsSpawnOffset)
        /// so it lands where the downhill slope is already present and
        /// immediately starts rolling, rather than on the flat solid cap
        /// right at Start itself - and lets Unity physics roll it along the
        /// terrain's MeshCollider groove until it gets close to the Goal
        /// hole (or a generous timeout elapses, in case it derails).
        private IEnumerator RunAlongPathPhysics(Marble marble, IReadOnlyList<Vector2Int> path)
        {
            Vector3 startPos = terrain.GetShoulderWorldPosition(path, physicsSpawnOffset) + Vector3.up * (marbleRadius3D + physicsDropHeight);
            marble.transform.position = startPos;

            Rigidbody rb = marble.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;

            TriggerCellContent(marble, path[0]);

            Vector3 goalPos = terrain.GetShoulderWorldPosition(path, path.Count - 1);
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
