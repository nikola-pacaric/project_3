using UnityEngine;
using Warblade.Data;
using Warblade.Systems;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    public class EnemyMovement : MonoBehaviour
    {
        private readonly EnemyEntryPath _entryPath = new EnemyEntryPath();
        private readonly EnemyDivePath _divePath = new EnemyDivePath();
        private readonly EnemyMotherRoam _motherRoam = new EnemyMotherRoam();

        private Enemy _enemy;
        private EnemyData _data;
        private CycleScalingState _cycleScaling = CycleScalingState.Default;
        private Transform _playerTransform;
        private Formation _formation;
        private int _formationSlotIndex = -1;
        private Vector2 _formationPosition;
        private Vector2 _entryControlOffset;
        private EnemyState _state = EnemyState.Entering;

        private float _nextDiveTime;
        private float _lingerEndTime;
        private bool _isPassThroughDive;
        private bool _isReturningToSpawnForDespawn;

        private float _entryElapsed;
        private float _entryDuration;

        internal EnemyState State => _state;

        internal bool CanForceDive =>
            _enemy != null &&
            !_enemy.HasDespawned &&
            _data != null &&
            _data.BehaviorMode == EnemyBehaviorMode.Formation &&
            (_state == EnemyState.InFormation || _state == EnemyState.Returning);

        internal void Initialize(Enemy enemy)
        {
            _enemy = enemy;
        }

        internal void Spawn(
            EnemyData data,
            CycleScalingState cycleScaling,
            Vector2 startPosition,
            Vector2 formationPosition,
            Transform playerTransform,
            Formation formation,
            int formationSlotIndex,
            Vector2 entryControlOffset,
            Vector2[] entryPathPoints,
            Vector2[] entryPathControlPoints)
        {
            _data = data;
            _cycleScaling = cycleScaling;
            _playerTransform = playerTransform;
            _formation = formation;
            _formationSlotIndex = formationSlotIndex;
            _formationPosition = formationPosition;
            _entryControlOffset = entryControlOffset;
            _isReturningToSpawnForDespawn = false;
            transform.position = startPosition;

            BeginEntry(entryControlOffset, entryPathPoints, entryPathControlPoints);
        }

        internal void Tick()
        {
            if (_enemy == null || _enemy.HasDespawned || _data == null)
            {
                return;
            }

            if (_state == EnemyState.InFormation || _state == EnemyState.Returning)
            {
                _formationPosition = ResolveFormationPosition();
            }

            switch (_state)
            {
                case EnemyState.Entering:
                    UpdateEntry();
                    break;

                case EnemyState.InFormation:
                    transform.position = _formationPosition;
                    if (Time.time >= _nextDiveTime)
                    {
                        TryStartDive();
                    }
                    break;

                case EnemyState.MotherRoaming:
                    UpdateMotherRoam();
                    break;

                case EnemyState.Diving:
                    UpdateDive();
                    break;

                case EnemyState.Lingering:
                    if (Time.time >= _lingerEndTime)
                    {
                        StartReturn();
                    }
                    break;

                case EnemyState.Returning:
                    if (_isReturningToSpawnForDespawn)
                    {
                        UpdateReturnToSpawnForDespawn();
                        break;
                    }

                    MoveToward(_formationPosition, ResolveDiveSpeed());
                    if (Reached(_formationPosition))
                    {
                        EnterFormation();
                    }
                    break;
            }
        }

        internal void ForceDive()
        {
            if (!CanForceDive) return;

            StartDive();
        }

        internal void DrawGizmosSelected(EnemyData previewData, Vector2 previewFormationPosition, Vector2 previewEntryControlOffset)
        {
            DrawMotherMovementGizmos(previewData);

            if (Application.isPlaying && _entryDuration > 0f && _entryPath.HasSegmentedPath)
            {
                Gizmos.color = Color.cyan;
                DrawSegmentedQuadraticGizmo(_entryPath.Points, _entryPath.ControlPoints, 32);

                Vector2[] points = _entryPath.Points;
                for (int i = 0; i < points.Length; i++)
                {
                    Gizmos.DrawWireSphere(points[i], 0.10f);
                }

                Vector2[] controlPoints = _entryPath.ControlPoints;
                if (controlPoints != null)
                {
                    Gizmos.color = Color.yellow;
                    for (int i = 0; i < controlPoints.Length; i++)
                    {
                        Gizmos.DrawWireSphere(controlPoints[i], 0.08f);
                        Gizmos.DrawLine(points[i], controlPoints[i]);
                        Gizmos.DrawLine(controlPoints[i], points[i + 1]);
                    }
                }

                return;
            }

            Vector2 start;
            Vector2 control;
            Vector2 end;

            if (Application.isPlaying && _entryDuration > 0f)
            {
                start = _entryPath.Start;
                control = _entryPath.ControlPoint;
                end = _entryPath.End;
            }
            else
            {
                start = transform.position;
                end = previewFormationPosition;
                Vector2 midpoint = (start + end) * 0.5f;
                control = midpoint + previewEntryControlOffset;
            }

            Gizmos.color = Color.cyan;
            Vector2 previous = start;
            const int samples = 24;
            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 point = BezierPath.EvaluateQuadratic(start, control, end, t);
                Gizmos.DrawLine(previous, point);
                previous = point;
            }

            Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
            Gizmos.DrawLine(start, control);
            Gizmos.DrawLine(control, end);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(start, 0.15f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(control, 0.12f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(end, 0.15f);
        }

        private void BeginEntry(Vector2 entryControlOffset, Vector2[] entryPathPoints, Vector2[] entryPathControlPoints)
        {
            Vector2 start = transform.position;
            Vector2 end = ResolveFormationPosition();

            _entryDuration = _entryPath.Begin(
                start,
                end,
                entryControlOffset,
                entryPathPoints,
                entryPathControlPoints,
                ResolveEntrySpeed());
            _entryElapsed = 0f;
            _state = EnemyState.Entering;
        }

        private void UpdateEntry()
        {
            if (_entryDuration <= Mathf.Epsilon)
            {
                EnterFormation();
                return;
            }

            _entryElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_entryElapsed / _entryDuration);

            _entryPath.RetargetEnd(ResolveFormationPosition());
            transform.position = _entryPath.Evaluate(t);

            if (t >= 1f)
            {
                EnterFormation();
            }
        }

        private Vector2 ResolveFormationPosition()
        {
            if (_formation != null && _formation.HasSlot(_formationSlotIndex))
            {
                return _formation.GetSlotWorldPosition(_formationSlotIndex);
            }

            return _formationPosition;
        }

        private void EnterFormation()
        {
            _formationPosition = ResolveFormationPosition();

            switch (_data.BehaviorMode)
            {
                case EnemyBehaviorMode.Formation:
                    StartFormationIdle();
                    break;

                case EnemyBehaviorMode.Mother:
                    StartMotherRoam();
                    break;

                case EnemyBehaviorMode.KamikazeReturn:
                    StartReturnToSpawnForDespawn();
                    break;

                case EnemyBehaviorMode.BonusSnake:
                    _enemy.Release(killed: false);
                    break;

                default:
                    StartFormationIdle();
                    break;
            }
        }

        private void StartFormationIdle()
        {
            _enemy.EndLimitedDive();
            _state = EnemyState.InFormation;
            _nextDiveTime = Time.time + Random.Range(_data.DiveCooldownMin, _data.DiveCooldownMax);

            if (_enemy.IsFinalDivePressureActive)
            {
                TryStartDive();
            }
        }

        private void StartMotherRoam()
        {
            _enemy.EndLimitedDive();
            _state = EnemyState.MotherRoaming;
            _motherRoam.Start(transform, _data);
        }

        private void UpdateMotherRoam()
        {
            _motherRoam.Tick(transform, _data, ResolveMotherRoamSpeed());
        }

        private void TryStartDive()
        {
            if (!_enemy.TryBeginLimitedDive())
            {
                _nextDiveTime = Time.time + Random.Range(0.5f, 1.25f);
                return;
            }

            StartDive();
        }

        private void StartDive()
        {
            _isPassThroughDive = Random.value < _data.PassThroughChance;
            _divePath.Begin(transform.position, _data, _playerTransform, ResolveDiveSpeed());
            _state = EnemyState.Diving;
        }

        private void UpdateDive()
        {
            if (_divePath.Tick(transform, _data, _playerTransform, Time.deltaTime))
            {
                StartLinger();
            }
        }

        private void StartLinger()
        {
            if (_data.DiesAtDiveBottom)
            {
                _enemy.Release(killed: false);
                return;
            }

            _lingerEndTime = Time.time + Random.Range(_data.LingerDurationMin, _data.LingerDurationMax);
            _state = EnemyState.Lingering;
        }

        private void StartReturn()
        {
            if (_isReturningToSpawnForDespawn)
            {
                _state = EnemyState.Returning;
                return;
            }

            if (_isPassThroughDive)
            {
                Vector2 liveFormationPosition = ResolveFormationPosition();
                transform.position = new Vector2(liveFormationPosition.x, _data.RespawnTopY);
                _entryPath.ClearSegmentedPath();
                BeginEntry(_entryControlOffset, null, null);
            }
            else
            {
                _state = EnemyState.Returning;
            }
        }

        private void StartReturnToSpawnForDespawn()
        {
            _formationPosition = _entryPath.Start;
            _isReturningToSpawnForDespawn = true;
            _entryElapsed = 0f;
            _entryDuration = _entryPath.CalculateDuration(ResolveEntrySpeed());
            _state = EnemyState.Returning;
        }

        private void UpdateReturnToSpawnForDespawn()
        {
            if (_entryDuration <= Mathf.Epsilon)
            {
                transform.position = _entryPath.Start;
                _enemy.Release(killed: false);
                return;
            }

            _entryElapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(_entryElapsed / _entryDuration);
            transform.position = _entryPath.Evaluate(t);

            if (t <= 0f)
            {
                transform.position = _entryPath.Start;
                _enemy.Release(killed: false);
            }
        }

        private void MoveToward(Vector2 target, float speed)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime);
        }

        private float ResolveEntrySpeed()
        {
            return _data.EntrySpeed * _cycleScaling.EnemySpeedMultiplier;
        }

        private float ResolveDiveSpeed()
        {
            return _data.DiveSpeed * _cycleScaling.EnemySpeedMultiplier;
        }

        private float ResolveMotherRoamSpeed()
        {
            return Mathf.Max(0.01f, _data.MotherRoamSpeed * _cycleScaling.EnemySpeedMultiplier);
        }

        private bool Reached(Vector2 target) => (Vector2)transform.position == target;

        private void DrawMotherMovementGizmos(EnemyData previewData)
        {
            EnemyData gizmoData = Application.isPlaying ? _data : previewData;
            if (gizmoData == null || gizmoData.BehaviorMode != EnemyBehaviorMode.Mother)
            {
                return;
            }

            Vector2 boundsMin = gizmoData.MotherRoamBoundsMin;
            Vector2 boundsMax = gizmoData.MotherRoamBoundsMax;
            Vector2 boundsCenter = (boundsMin + boundsMax) * 0.5f;
            Vector2 boundsSize = boundsMax - boundsMin;

            Gizmos.color = new Color(1f, 0.35f, 1f, 0.9f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);

            Gizmos.color = new Color(1f, 0.35f, 1f, 0.35f);
            Gizmos.DrawLine(new Vector2(boundsMin.x, boundsCenter.y), new Vector2(boundsMax.x, boundsCenter.y));
            Gizmos.DrawLine(new Vector2(boundsCenter.x, boundsMin.y), new Vector2(boundsCenter.x, boundsMax.y));

            if (!Application.isPlaying)
            {
                return;
            }

            Gizmos.color = Color.magenta;
            _motherRoam.DrawRuntimeGizmos(transform);
        }

        private static void DrawSegmentedQuadraticGizmo(Vector2[] points, Vector2[] controlPoints, int samples)
        {
            if (points == null || points.Length < 2 || controlPoints == null || controlPoints.Length == 0) return;

            Vector2 previous = points[0];
            int clampedSamples = Mathf.Max(samples, 4);
            for (int i = 1; i <= clampedSamples; i++)
            {
                float t = i / (float)clampedSamples;
                Vector2 point = BezierPath.EvaluateSegmentedQuadraticPath(points, controlPoints, t);
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
        }
    }
}
