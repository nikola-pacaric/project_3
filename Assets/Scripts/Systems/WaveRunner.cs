using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warblade.Data;

namespace Warblade.Systems
{
    /// <summary>
    /// Runs a timed sequence of WaveData assets and spawns each wave via EnemySpawner.
    /// </summary>
    public class WaveRunner : MonoBehaviour
    {
        [SerializeField] private EnemySpawner _enemySpawner;
        [SerializeField] private List<WaveData> _waves = new List<WaveData>();
        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField, Min(4)] private int _gizmoCurveSamples = 24;

        private readonly List<Formation> _runtimeFormations = new List<Formation>();
        private Coroutine _runRoutine;
        private bool _autoplaySuppressed;
        private bool _hasCompletedSequence;

        public bool IsRunning => _runRoutine != null;
        public bool HasCompletedSequence => _hasCompletedSequence;

        private void Start()
        {
            if (_playOnStart && !_autoplaySuppressed)
            {
                RunWaves();
            }
        }

        private void OnDisable()
        {
            StopWaves();
        }

        private void OnValidate()
        {
            if (_enemySpawner == null)
            {
                Debug.LogWarning($"[{nameof(WaveRunner)}] Assign {nameof(EnemySpawner)} on '{name}'.");
            }
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmos || _waves == null) return;

            for (int waveIndex = 0; waveIndex < _waves.Count; waveIndex++)
            {
                WaveData wave = _waves[waveIndex];
                if (wave == null || wave.FormationData == null) continue;

                int slotCount = wave.FormationData.SlotCount;
                if (slotCount <= 0) continue;

                Vector2 anchor = wave.FormationAnchorPosition;
                Vector2 entryStartCenter = anchor + GetEntryDirection(wave.Side) * wave.EntryDistance;
                Vector2 entrySpacingStep = GetEntrySpacingDirection(wave.Side) * wave.EntrySpacing;
                float halfSpan = (slotCount - 1) * 0.5f;

                Color waveColor = Color.HSVToRGB((waveIndex * 0.173f) % 1f, 0.65f, 1f);
                Gizmos.color = waveColor;
                DrawCross(anchor, 0.22f);
                Gizmos.DrawWireSphere(entryStartCenter, 0.12f);

                for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
                {
                    FormationData.SlotDefinition slot = wave.FormationData.GetSlot(slotIndex);
                    Vector2 start = entryStartCenter + entrySpacingStep * (slotIndex - halfSpan);
                    Vector2 end = anchor + slot.LocalPosition;
                    Vector2 control = ((start + end) * 0.5f) + slot.EntryControlOffset;

                    DrawQuadraticPath(start, control, end, _gizmoCurveSamples);
                    Gizmos.DrawWireSphere(end, 0.10f);
                    Gizmos.DrawSphere(start, 0.05f);
                }
            }
        }

        /// <summary>
        /// Starts the configured wave sequence.
        /// </summary>
        [ContextMenu("Run Waves")]
        public void RunWaves()
        {
            PlayWaves(_waves);
        }

        /// <summary>
        /// Starts a wave sequence from the provided list.
        /// </summary>
        public void PlayWaves(IReadOnlyList<WaveData> waves)
        {
            StopWaves();

            if (_enemySpawner == null)
            {
                Debug.LogError($"[{nameof(WaveRunner)}] Cannot run waves without {nameof(EnemySpawner)}.");
                return;
            }

            _hasCompletedSequence = false;
            _runRoutine = StartCoroutine(RunWavesRoutine(waves));
        }

        /// <summary>
        /// Stops the current wave sequence and destroys runtime formation anchors.
        /// </summary>
        [ContextMenu("Stop Waves")]
        public void StopWaves()
        {
            if (_runRoutine != null)
            {
                StopCoroutine(_runRoutine);
                _runRoutine = null;
            }

            _hasCompletedSequence = false;
            CleanupRuntimeFormations();
        }

        /// <summary>
        /// Disables Play On Start so an external system can control wave execution.
        /// </summary>
        public void SuppressAutoplay()
        {
            _autoplaySuppressed = true;
        }

        private IEnumerator RunWavesRoutine(IReadOnlyList<WaveData> waves)
        {
            if (waves == null || waves.Count == 0)
            {
                _runRoutine = null;
                _hasCompletedSequence = true;
                yield break;
            }

            for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
            {
                WaveData wave = waves[waveIndex];
                if (wave == null)
                {
                    Debug.LogWarning($"[{nameof(WaveRunner)}] Wave at index {waveIndex} is null.");
                    continue;
                }

                if (wave.SpawnDelay > 0f)
                {
                    yield return new WaitForSeconds(wave.SpawnDelay);
                }

                if (wave.FormationData == null)
                {
                    Debug.LogError($"[{nameof(WaveRunner)}] Wave '{wave.name}' has no FormationData.");
                    continue;
                }

                Formation formation = CreateRuntimeFormation(wave, waveIndex);
                Vector2 entryStartCenter = wave.FormationAnchorPosition + GetEntryDirection(wave.Side) * wave.EntryDistance;
                Vector2 entrySpacingStep = GetEntrySpacingDirection(wave.Side) * wave.EntrySpacing;

                _enemySpawner.SpawnFormation(
                    formation,
                    wave,
                    entryStartCenter,
                    entrySpacingStep,
                    wave.PerSlotDelay);
            }

            _runRoutine = null;
            _hasCompletedSequence = true;
        }

        private Formation CreateRuntimeFormation(WaveData wave, int waveIndex)
        {
            GameObject formationObject = new GameObject($"WaveFormation_{waveIndex + 1}_{wave.name}");
            formationObject.transform.SetParent(transform, true);

            Formation formation = formationObject.AddComponent<Formation>();
            formation.Configure(wave.FormationData, wave.FormationAnchorPosition);
            _runtimeFormations.Add(formation);

            return formation;
        }

        private static Vector2 GetEntryDirection(WaveData.EntrySide side)
        {
            switch (side)
            {
                case WaveData.EntrySide.Left:
                    return Vector2.left;
                case WaveData.EntrySide.Right:
                    return Vector2.right;
                default:
                    return Vector2.up;
            }
        }

        private static Vector2 GetEntrySpacingDirection(WaveData.EntrySide side)
        {
            switch (side)
            {
                case WaveData.EntrySide.Left:
                case WaveData.EntrySide.Right:
                    return Vector2.up;
                default:
                    return Vector2.right;
            }
        }

        private static void DrawCross(Vector2 center, float size)
        {
            Gizmos.DrawLine(center + Vector2.left * size, center + Vector2.right * size);
            Gizmos.DrawLine(center + Vector2.down * size, center + Vector2.up * size);
        }

        private static void DrawQuadraticPath(Vector2 start, Vector2 control, Vector2 end, int samples)
        {
            int clampedSamples = Mathf.Max(samples, 4);
            Vector2 previous = start;
            for (int i = 1; i <= clampedSamples; i++)
            {
                float t = i / (float)clampedSamples;
                Vector2 point = BezierPath.EvaluateQuadratic(start, control, end, t);
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
        }

        private void CleanupRuntimeFormations()
        {
            for (int i = 0; i < _runtimeFormations.Count; i++)
            {
                Formation formation = _runtimeFormations[i];
                if (formation != null)
                {
                    Destroy(formation.gameObject);
                }
            }

            _runtimeFormations.Clear();
        }
    }
}
