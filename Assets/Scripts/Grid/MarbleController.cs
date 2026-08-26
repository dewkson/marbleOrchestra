using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Drives one Marble per completed Start-to-Goal track of PathGrid's
    /// last validation, all running concurrently. Play is refused whenever
    /// no track is currently complete.
    /// Keyboard-driven for now (Space = Play, S = Stop, R = Reset) so it is
    /// testable without any UI; the public methods are ready for UI buttons later.
    /// </summary>
    [RequireComponent(typeof(PathGrid))]
    [RequireComponent(typeof(AudioSource))]
    public class MarbleController : MonoBehaviour
    {
        [SerializeField] private float cellsPerSecond = 3f;
        [SerializeField] private float marbleRadius = 0.15f;
        [SerializeField] private Color marbleColor = new Color(0.1f, 0.1f, 0.1f);

        private PathGrid grid;
        private AudioSource audioSource;
        private readonly List<Marble> marbles = new List<Marble>();
        private readonly List<Coroutine> runRoutines = new List<Coroutine>();
        private int activeRunCount;

        public bool IsPlaying => activeRunCount > 0;
        public bool CanPlay => HasCompletedTrack();

        private void Awake()
        {
            grid = GetComponent<PathGrid>();

            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.spaceKey.wasPressedThisFrame) Play();
            if (Keyboard.current.sKey.wasPressedThisFrame) Stop();
            if (Keyboard.current.rKey.wasPressedThisFrame) ResetMarble();
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

                Marble marble = Marble.Create(transform, marbleRadius, marbleColor);
                marbles.Add(marble);
                activeRunCount++;
                runRoutines.Add(StartCoroutine(RunTrack(marble, result.OrderedPath)));
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

        private IEnumerator RunTrack(Marble marble, IReadOnlyList<Vector2Int> path)
        {
            yield return RunAlongPath(marble, path);
            activeRunCount--;
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

        private void TriggerCellContent(Marble marble, Vector2Int coord)
        {
            CellContentDefinition content = grid.GetContent(coord);
            content?.Activate(new CellContentContext(grid, coord, audioSource, marble));
        }
    }
}
