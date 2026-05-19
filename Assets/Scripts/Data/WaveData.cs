using UnityEngine;
using Warblade.Entities;

namespace Warblade.Data
{
    [CreateAssetMenu(menuName = "Warblade/Data/Wave Data", fileName = "WaveData")]
    public class WaveData : ScriptableObject
    {
        public enum EntrySide
        {
            Top,
            Left,
            Right
        }

        public enum EntryPathMode
        {
            SimpleBezier = 0,
            WaypointPath = 1
        }

        public enum FormationMotionMode
        {
            Static = 0,
            HorizontalSway = 1
        }

        public enum SlotPositionMode
        {
            AnchorRelative = 0,
            World = 1
        }

        public enum NextWaveTrigger
        {
            StartNextImmediately = 0,
            WaitUntilEnemiesCleared = 1,
            WaitUntilThisWaveFinishedSpawning = 2
        }

        [System.Serializable]
        public struct WaveSlot
        {
            [Tooltip("Final formation position. Interpreted by Slot Position Mode.")]
            public Vector2 LocalPosition;
            [Tooltip("Optional per-slot override. Leave empty to use the EnemyData assigned on the wave enemy prefab.")]
            public EnemyData EnemyData;
            [Tooltip("Added to the midpoint of (spawn start -> slot world) for Bezier entry shape.")]
            public Vector2 EntryControlOffset;
        }

        [System.Serializable]
        public struct EntryPathWaypoint
        {
            public Vector2 LocalPosition;
            [Tooltip("Added to the midpoint of the segment from the previous dot to this dot. Zero keeps that segment straight.")]
            public Vector2 ControlOffsetFromPrevious;
        }

        [SerializeField] private WaveSlot[] _slots;
        [Header("Enemy")]
        [Tooltip("Prefab used for every enemy in this wave. Its EnemyData is the default; slot EnemyData can override it.")]
        [SerializeField] private GameObject _enemyPrefab;
        [Tooltip("Anchor Relative keeps slot positions offset from Formation Anchor. World keeps slot positions fixed when Formation Anchor moves.")]
        [SerializeField] private SlotPositionMode _slotPositionMode = SlotPositionMode.AnchorRelative;
        [SerializeField] private Vector2 _formationAnchorPosition = new Vector2(0f, 0f);
        [Header("Formation Motion")]
        [SerializeField] private FormationMotionMode _formationMotionMode = FormationMotionMode.HorizontalSway;
        [SerializeField, Min(0f)] private float _formationSwayAmplitude = 0.5f;
        [SerializeField, Min(0f)] private float _formationSwaySpeed = 1f;
        [SerializeField] private float _formationSwayPhase;
        [Header("Sequencing")]
        [SerializeField, Min(0f)] private float _spawnDelay = 0f;
        [Tooltip("Controls when the next wave in the level may start after this wave begins.")]
        [SerializeField] private NextWaveTrigger _nextWaveTrigger = NextWaveTrigger.StartNextImmediately;
        [SerializeField] private EntrySide _entrySide = EntrySide.Top;
        [SerializeField, Min(0f)] private float _entryDistance = 8f;
        [Tooltip("When disabled, entry start is derived from formation anchor, entry side, and entry distance.")]
        [SerializeField] private bool _useCustomEntryStartCenter;
        [Tooltip("Absolute world position for the center of the entering wave when custom entry start is enabled.")]
        [SerializeField] private Vector2 _entryStartCenter = new Vector2(0f, 8f);
        [SerializeField, Min(0f)] private float _entrySpacing = 1.5f;
        [SerializeField, Min(0f)] private float _perSlotDelay = 0f;
        [Header("Entry Path")]
        [SerializeField] private EntryPathMode _entryPathMode = EntryPathMode.SimpleBezier;
        [Tooltip("Shared path dots in local space relative to the formation anchor. Enemies follow this same route first, then branch to their own slots.")]
        [SerializeField] private EntryPathWaypoint[] _entryPathWaypoints;
        [Tooltip("Added to the midpoint of each enemy's final branch from the shared path end to its slot. Zero keeps the branch straight.")]
        [SerializeField] private Vector2 _entryPathFinalControlOffset;

        public int SlotCount => _slots == null ? 0 : _slots.Length;
        public Enemy EnemyPrefab => _enemyPrefab != null ? _enemyPrefab.GetComponent<Enemy>() : null;
        public EnemyData DefaultEnemyData => EnemyPrefab != null ? EnemyPrefab.Data : null;
        public SlotPositionMode SlotPositions => _slotPositionMode;
        public Vector2 FormationAnchorPosition => _formationAnchorPosition;
        public FormationMotionMode MotionMode => _formationMotionMode;
        public float FormationSwayAmplitude => _formationSwayAmplitude;
        public float FormationSwaySpeed => _formationSwaySpeed;
        public float FormationSwayPhase => _formationSwayPhase;
        public float SpawnDelay => _spawnDelay;
        public NextWaveTrigger NextWaveStartTrigger => _nextWaveTrigger;
        public EntrySide Side => _entrySide;
        public float EntryDistance => _entryDistance;
        public bool UseCustomEntryStartCenter => _useCustomEntryStartCenter;
        public Vector2 EntryStartCenter => _useCustomEntryStartCenter
            ? _entryStartCenter
            : _formationAnchorPosition + GetEntryDirection(_entrySide) * _entryDistance;
        public Vector2 EntrySpacingStep => GetEntrySpacingDirection(_entrySide) * _entrySpacing;
        public float EntrySpacing => _entrySpacing;
        public float PerSlotDelay => _perSlotDelay;
        public EntryPathMode PathMode => _entryPathMode;
        public int EntryPathWaypointCount => _entryPathWaypoints == null ? 0 : _entryPathWaypoints.Length;
        public bool UsesWaypointEntryPath =>
            _entryPathMode == EntryPathMode.WaypointPath &&
            EntryPathWaypointCount > 0;

        public bool HasSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < SlotCount;
        }

        public WaveSlot GetSlot(int slotIndex)
        {
            if (!HasSlot(slotIndex))
            {
                Debug.LogError($"[{nameof(WaveData)}] Slot index {slotIndex} is out of range on '{name}'.");
                return default;
            }

            return _slots[slotIndex];
        }

        public Vector2 GetSlotWorldPosition(int slotIndex)
        {
            if (!HasSlot(slotIndex))
            {
                Debug.LogError($"[{nameof(WaveData)}] Slot index {slotIndex} is out of range on '{name}'.");
                return _formationAnchorPosition;
            }

            return _slotPositionMode == SlotPositionMode.World
                ? _slots[slotIndex].LocalPosition
                : _formationAnchorPosition + _slots[slotIndex].LocalPosition;
        }

        public EnemyData GetEnemyDataForSlot(int slotIndex)
        {
            if (!HasSlot(slotIndex))
            {
                Debug.LogError($"[{nameof(WaveData)}] Slot index {slotIndex} is out of range on '{name}'.");
                return null;
            }

            return _slots[slotIndex].EnemyData != null ? _slots[slotIndex].EnemyData : DefaultEnemyData;
        }

        public Vector2 GetEntryPathWaypointWorldPosition(int waypointIndex)
        {
            if (waypointIndex < 0 || waypointIndex >= EntryPathWaypointCount)
            {
                Debug.LogError($"[{nameof(WaveData)}] Entry path waypoint index {waypointIndex} is out of range on '{name}'.");
                return _formationAnchorPosition;
            }

            return _formationAnchorPosition + _entryPathWaypoints[waypointIndex].LocalPosition;
        }

        public Vector2[] BuildEntryPathWorldPoints(Vector2 startPosition, Vector2 endPosition)
        {
            if (!UsesWaypointEntryPath)
            {
                return null;
            }

            Vector2[] pathPoints = new Vector2[EntryPathWaypointCount + 2];
            pathPoints[0] = startPosition;
            for (int i = 0; i < EntryPathWaypointCount; i++)
            {
                pathPoints[i + 1] = GetEntryPathWaypointWorldPosition(i);
            }

            pathPoints[pathPoints.Length - 1] = endPosition;
            return pathPoints;
        }

        public Vector2[] BuildSharedEntryPathWorldPoints(Vector2 startPosition)
        {
            if (!UsesWaypointEntryPath)
            {
                return null;
            }

            Vector2[] pathPoints = new Vector2[EntryPathWaypointCount + 1];
            pathPoints[0] = startPosition;
            for (int i = 0; i < EntryPathWaypointCount; i++)
            {
                pathPoints[i + 1] = GetEntryPathWaypointWorldPosition(i);
            }

            return pathPoints;
        }

        public Vector2 GetEntryPathEndWorldPosition(Vector2 startPosition)
        {
            return EntryPathWaypointCount > 0
                ? GetEntryPathWaypointWorldPosition(EntryPathWaypointCount - 1)
                : startPosition;
        }

        public Vector2 GetEntryPathBranchControlPoint(Vector2 branchStart, Vector2 endPosition)
        {
            return ((branchStart + endPosition) * 0.5f) + _entryPathFinalControlOffset;
        }

        public Vector2[] BuildEntryPathWorldControlPoints(Vector2[] pathPoints)
        {
            if (!UsesWaypointEntryPath || pathPoints == null || pathPoints.Length < 2)
            {
                return null;
            }

            int segmentCount = pathPoints.Length - 1;
            Vector2[] controlPoints = new Vector2[segmentCount];
            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                Vector2 offset = segmentIndex < EntryPathWaypointCount
                    ? _entryPathWaypoints[segmentIndex].ControlOffsetFromPrevious
                    : _entryPathFinalControlOffset;

                controlPoints[segmentIndex] =
                    ((pathPoints[segmentIndex] + pathPoints[segmentIndex + 1]) * 0.5f) + offset;
            }

            return controlPoints;
        }

        private void OnValidate()
        {
            if (_enemyPrefab == null)
            {
                Debug.LogError($"[{nameof(WaveData)}] Wave '{name}' has no enemy prefab.");
            }
            else if (_enemyPrefab.GetComponent<Enemy>() == null)
            {
                Debug.LogError($"[{nameof(WaveData)}] Wave '{name}' enemy prefab has no {nameof(Enemy)} component.");
            }
            else if (DefaultEnemyData == null)
            {
                Debug.LogError($"[{nameof(WaveData)}] Wave '{name}' enemy prefab has no default {nameof(EnemyData)}.");
            }

            if (_slots == null || _slots.Length == 0)
            {
                Debug.LogError($"[{nameof(WaveData)}] Wave '{name}' has no slots.");
                return;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].EnemyData != null || DefaultEnemyData != null) continue;

                Debug.LogError($"[{nameof(WaveData)}] Wave '{name}' slot {i} has no EnemyData and the prefab has no default.");
            }
        }

        private static Vector2 GetEntryDirection(EntrySide side)
        {
            switch (side)
            {
                case EntrySide.Left:
                    return Vector2.left;
                case EntrySide.Right:
                    return Vector2.right;
                default:
                    return Vector2.up;
            }
        }

        private static Vector2 GetEntrySpacingDirection(EntrySide side)
        {
            switch (side)
            {
                case EntrySide.Left:
                case EntrySide.Right:
                    return Vector2.up;
                default:
                    return Vector2.right;
            }
        }
    }
}
