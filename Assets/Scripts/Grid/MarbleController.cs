using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Drives a Marble along PathGrid's last validated Start-to-Goal path.
    /// Play is refused whenever no complete path currently exists.
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
        private Marble marble;
        private Coroutine runRoutine;

        public bool IsPlaying => runRoutine != null;
        public bool CanPlay => grid.LastValidation.GoalReached;

        private void Awake()
        {
            grid = GetComponent<PathGrid>();

            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;

            marble = Marble.Create(transform, marbleRadius, marbleColor);
            marble.gameObject.SetActive(false);
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
                Debug.LogWarning("MarbleController: Play ignoriert, aktuell existiert kein gueltiger Pfad.");
                return false;
            }

            runRoutine = StartCoroutine(RunAlongPath(grid.LastValidation.OrderedPath));
            return true;
        }

        public void Stop()
        {
            if (runRoutine == null) return;
            StopCoroutine(runRoutine);
            runRoutine = null;
        }

        public void ResetMarble()
        {
            Stop();
            marble.gameObject.SetActive(false);
        }

        private IEnumerator RunAlongPath(IReadOnlyList<Vector2Int> path)
        {
            marble.gameObject.SetActive(true);
            marble.transform.localPosition = grid.CellToLocalPosition(path[0]);
            PlayCellSound(path[0]);

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
                PlayCellSound(path[i]);
            }

            runRoutine = null;
        }

        private void PlayCellSound(Vector2Int coord)
        {
            AudioClip clip = grid.GetCard(coord)?.Definition?.Sound;
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
