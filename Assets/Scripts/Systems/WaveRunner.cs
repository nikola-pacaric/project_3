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
                if (wave == null) continue;

                int slotCount = wave.SlotCount;
                if (slotCount <= 0) continue;

                Vector2 anchor = wave.FormationAnchorPosition;
                Vector2 entryStartCenter = wave.EntryStartCenter;
                Vector2 entrySpacingStep = wave.EntrySpacingStep;
                float halfSpan = (slotCount - 1) * 0.5f;

                Color waveColor = Color.HSVToRGB((waveIndex * 0.173f) % 1f, 0.65f, 1f);
                Gizmos.color = waveColor;
                DrawCross(anchor, 0.22f);
                Gizmos.DrawWireSphere(entryStartCenter, 0.12f);

                if (wave.UsesWaypointEntryPath)
                {
                    Vector2[] sharedPathPoints = wave.BuildSharedEntryPathWorldPoints(entryStartCenter);
                    Vector2[] sharedControlPoints = wave.BuildEntryPathWorldControlPoints(sharedPathPoints);
                    DrawSegmentedQuadraticPath(sharedPathPoints, sharedControlPoints, _gizmoCurveSamples);
                    DrawSegmentControlPoints(sharedPathPoints, sharedControlPoints);

                    for (int waypointIndex = 0; waypointIndex < wave.EntryPathWaypointCount; waypointIndex++)
                    {
                        Vector2 waypoint = wave.GetEntryPathWaypointWorldPosition(waypointIndex);
                        Gizmos.DrawWireSphere(waypoint, 0.14f);
                        DrawCross(waypoint, 0.08f);
                    }
                }

                for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
                {
                    WaveData.WaveSlot slot = wave.GetSlot(slotIndex);
                    Vector2 start = entryStartCenter + entrySpacingStep * (slotIndex - halfSpan);
                    Vector2 end = anchor + slot.LocalPosition;

                    if (wave.UsesWaypointEntryPath)
                    {
                        Vector2 branchStart = wave.GetEntryPathEndWorldPosition(entryStartCenter);
                        Vector2 branchControl = wave.GetEntryPathBranchControlPoint(branchStart, end);
                        DrawQuadraticPath(branchStart, branchControl, end, _gizmoCurveSamples);
                        DrawBranchControlPoint(branchStart, branchControl, end);
                    }
                    else
                    {
                        Vector2 control = ((start + end) * 0.5f) + slot.EntryControlOffset;
                        DrawQuadraticPath(start, control, end, _gizmoCurveSamples);
                        Gizmos.DrawSphere(start, 0.05f);
                    }

                    Gizmos.DrawWireSphere(end, 0.10f);
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

                if (wave.SlotCount <= 0)
                {
                    Debug.LogError($"[{nameof(WaveRunner)}] Wave '{wave.name}' has no slots.");
                    continue;
                }

                Formation runtimeFormation = CreateRuntimeFormation(wave, waveIndex);
                Vector2 entryStartCenter = wave.EntryStartCenter;
                Vector2 entrySpacingStep = wave.EntrySpacingStep;

                Coroutine spawnRoutine = _enemySpawner.SpawnFormation(
                    wave,
                    entryStartCenter,
                    entrySpacingStep,
                    wave.PerSlotDelay,
                    runtimeFormation: runtimeFormation);

                bool hasNextWave = waveIndex < waves.Count - 1;
                bool isLastWave = !hasNextWave;
                if (isLastWave || wave.NextWaveStartTrigger == WaveData.NextWaveTrigger.WaitUntilThisWaveFinishedSpawning)
                {
                    yield return spawnRoutine;
                }

                if (hasNextWave && wave.NextWaveStartTrigger == WaveData.NextWaveTrigger.WaitUntilEnemiesCleared)
                {
                    yield return spawnRoutine;
                    while (_enemySpawner.ActiveEnemyCount > 0)
                    {
                        yield return null;
                    }
                }
            }

            _runRoutine = null;
            _hasCompletedSequence = true;
        }

        private Formation CreateRuntimeFormation(WaveData wave, int waveIndex)
        {
            GameObject formationObject = new GameObject($"WaveFormation_{waveIndex + 1}_{wave.name}");
            formationObject.transform.SetParent(transform, true);

            Formation formation = formationObject.AddComponent<Formation>();
            formation.Configure(wave);
            _runtimeFormations.Add(formation);

            return formation;
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

        private static void DrawSegmentedQuadraticPath(Vector2[] points, Vector2[] controlPoints, int samples)
        {
            if (points == null || points.Length < 2 || controlPoints == null || controlPoints.Length == 0) return;

            int clampedSamples = Mathf.Max(samples, 4);
            Vector2 previous = points[0];
            for (int i = 1; i <= clampedSamples; i++)
            {
                float t = i / (float)clampedSamples;
                Vector2 point = BezierPath.EvaluateSegmentedQuadraticPath(points, controlPoints, t);
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
        }

        private static void DrawSegmentControlPoints(Vector2[] points, Vector2[] controlPoints)
        {
            if (points == null || controlPoints == null) return;

            Color previousColor = Gizmos.color;
            Gizmos.color = Color.yellow;
            int segmentCount = Mathf.Min(points.Length - 1, controlPoints.Length);
            for (int i = 0; i < segmentCount; i++)
            {
                Gizmos.DrawWireSphere(controlPoints[i], 0.07f);
                Gizmos.DrawLine(points[i], controlPoints[i]);
                Gizmos.DrawLine(controlPoints[i], points[i + 1]);
            }

            Gizmos.color = previousColor;
        }

        private static void DrawBranchControlPoint(Vector2 start, Vector2 control, Vector2 end)
        {
            Color previousColor = Gizmos.color;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(control, 0.07f);
            Gizmos.DrawLine(start, control);
            Gizmos.DrawLine(control, end);
            Gizmos.color = previousColor;
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
