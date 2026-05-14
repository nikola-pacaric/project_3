using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Warblade.Data;
using Warblade.Data.Events;
using Warblade.Entities;
using Warblade.Systems;

namespace Warblade.Managers
{
    /// <summary>
    /// Drives level-by-level progression by sequencing LevelData through WaveRunner.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [System.Serializable]
        public class LevelChangedEvent : UnityEvent<int> { }

        public static LevelManager Instance { get; private set; }

        [SerializeField] private WaveRunner _waveRunner;
        [SerializeField] private EnemySpawner _enemySpawner;
        [SerializeField] private PlayerShooting _playerShooting;
        [SerializeField] private LevelData[] _levels;
        [SerializeField, Min(1)] private int _startingLevel = 1;
        [SerializeField] private bool _playOnStart = true;
        [SerializeField, Min(0f)] private float _levelTransitionDelay = 2f;
        [SerializeField, Min(1)] private int _shopInterval = 4;
        [Header("M5 Boss Test")]
        [SerializeField] private bool _enableTestBossAfterWaves;
        [SerializeField, Min(1)] private int _testBossLevel = 5;
        [SerializeField] private Boss _testBoss;
        [SerializeField] private LevelChangedEvent _onLevelStarted = new LevelChangedEvent();
        [SerializeField] private LevelChangedEvent _onLevelCompleted = new LevelChangedEvent();
        [Header("Event Channels")]
        [SerializeField] private IntEventChannel _levelStarted;
        [SerializeField] private IntEventChannel _levelCompleted;

        private Coroutine _levelRoutine;
        private bool _isGameOver;

        /// <summary>
        /// Current active level number.
        /// </summary>
        public int CurrentLevel { get; private set; }
        public LevelChangedEvent OnLevelStarted => _onLevelStarted;
        public LevelChangedEvent OnLevelCompleted => _onLevelCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CurrentLevel = Mathf.Max(1, _startingLevel);

            if (_waveRunner != null)
            {
                _waveRunner.SuppressAutoplay();
            }
        }

        private void OnEnable()
        {
            PlayerHealth.GameOverRaised += HandleGameOver;
        }

        private void OnDisable()
        {
            PlayerHealth.GameOverRaised -= HandleGameOver;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (_playOnStart)
            {
                PlayCurrentLevel();
            }
        }

        private void OnValidate()
        {
            if (_waveRunner == null)
            {
                Debug.LogWarning($"[{nameof(LevelManager)}] Assign {nameof(WaveRunner)} on '{name}'.");
            }

            if (_enemySpawner == null)
            {
                Debug.LogWarning($"[{nameof(LevelManager)}] Assign {nameof(EnemySpawner)} on '{name}'.");
            }

            if (_playerShooting == null)
            {
                Debug.LogWarning($"[{nameof(LevelManager)}] Assign {nameof(PlayerShooting)} on '{name}' so level transitions can disable shooting while bullets clear.");
            }
        }

        /// <summary>
        /// Starts progression from the current level.
        /// </summary>
        public void PlayCurrentLevel()
        {
            if (_waveRunner == null || _enemySpawner == null)
            {
                Debug.LogError($"[{nameof(LevelManager)}] Missing required references.");
                return;
            }

            if (_levels == null || _levels.Length == 0)
            {
                Debug.LogError($"[{nameof(LevelManager)}] No LevelData assets configured.");
                return;
            }

            _isGameOver = false;

            if (_levelRoutine != null)
            {
                StopCoroutine(_levelRoutine);
            }

            _levelRoutine = StartCoroutine(LevelLoopRoutine());
        }

        private IEnumerator LevelLoopRoutine()
        {
            while (!_isGameOver)
            {
                LevelData levelData = ResolveCurrentLevelData();
                if (levelData == null)
                {
                    Debug.LogError($"[{nameof(LevelManager)}] Failed to resolve data for level {CurrentLevel}.");
                    _levelRoutine = null;
                    yield break;
                }

                _onLevelStarted?.Invoke(CurrentLevel);
                _levelStarted?.Raise(CurrentLevel);
                RunStatsManager.Instance?.ClearCurrentLevelDebuffs();
                _playerShooting?.SetShootingEnabled(true);
                _waveRunner.PlayWaves(levelData.Waves);
                yield return WaitForLevelClearRoutine();

                if (_isGameOver) break;
                if (ShouldRunTestBossAfterWaves(CurrentLevel))
                {
                    yield return RunTestBossEncounterRoutine();
                }

                if (_isGameOver) break;
                _onLevelCompleted?.Invoke(CurrentLevel);
                _levelCompleted?.Raise(CurrentLevel);
                if (_levelTransitionDelay > 0f)
                {
                    yield return new WaitForSeconds(_levelTransitionDelay);
                }

                int completedLevel = CurrentLevel;
                int nextLevel = CurrentLevel + 1;
                if (!HasLevelData(nextLevel))
                {
                    Debug.Log($"[{nameof(LevelManager)}] Completed authored levels at Level {CurrentLevel}. Waiting for restart.");
                    _levelRoutine = null;
                    yield break;
                }

                if (ShouldEnterShopAfterLevel(completedLevel))
                {
                    yield return EnterShopRoutine();
                    if (_isGameOver) break;
                }

                CurrentLevel = nextLevel;
            }

            _levelRoutine = null;
        }

        /// <summary>
        /// Opens the shop immediately for development testing.
        /// </summary>
        [ContextMenu("Force Shop Entry")]
        public void ForceShopEntry()
        {
            GameManager.Instance?.EnterShop();
        }

        [ContextMenu("Force Test Boss Encounter")]
        public void ForceTestBossEncounter()
        {
            if (_levelRoutine != null)
            {
                StopCoroutine(_levelRoutine);
            }

            _isGameOver = false;
            _levelRoutine = StartCoroutine(RunForcedTestBossRoutine());
        }

        private IEnumerator WaitForLevelClearRoutine()
        {
            while (!_isGameOver)
            {
                bool enemiesCleared =
                    _waveRunner.HasCompletedSequence &&
                    _enemySpawner.ActiveEnemyCount == 0;

                if (enemiesCleared)
                {
                    break;
                }

                yield return null;
            }

            _playerShooting?.SetShootingEnabled(false);

            while (!_isGameOver && (Bullet.ActiveBulletCount > 0 || Pickup.ActivePickupCount > 0))
            {
                yield return null;
            }
        }

        private LevelData ResolveCurrentLevelData()
        {
            if (_levels == null || _levels.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < _levels.Length; i++)
            {
                LevelData candidate = _levels[i];
                if (candidate != null && candidate.LevelNumber == CurrentLevel)
                {
                    return candidate;
                }
            }

            return null;
        }

        private bool HasLevelData(int levelNumber)
        {
            if (_levels == null) return false;

            for (int i = 0; i < _levels.Length; i++)
            {
                LevelData candidate = _levels[i];
                if (candidate != null && candidate.LevelNumber == levelNumber)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldEnterShopAfterLevel(int completedLevel)
        {
            return _shopInterval > 0 && completedLevel % _shopInterval == 0;
        }

        private bool ShouldRunTestBossAfterWaves(int levelNumber)
        {
            return _enableTestBossAfterWaves && levelNumber == _testBossLevel;
        }

        private IEnumerator RunTestBossEncounterRoutine()
        {
            if (_testBoss == null)
            {
                Debug.LogWarning($"[{nameof(LevelManager)}] Test boss is enabled for level {_testBossLevel}, but no boss reference is assigned.");
                yield break;
            }

            _playerShooting?.SetShootingEnabled(true);
            _testBoss.Spawn();

            while (!_isGameOver && !_testBoss.IsDefeated)
            {
                yield return null;
            }

            _playerShooting?.SetShootingEnabled(false);

            while (!_isGameOver && (Bullet.ActiveBulletCount > 0 || Pickup.ActivePickupCount > 0))
            {
                yield return null;
            }
        }

        private IEnumerator RunForcedTestBossRoutine()
        {
            yield return RunTestBossEncounterRoutine();
            _levelRoutine = null;
        }

        private IEnumerator EnterShopRoutine()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogWarning($"[{nameof(LevelManager)}] Cannot enter shop because no {nameof(GameManager)} is active.");
                yield break;
            }

            gameManager.EnterShop();
            while (!_isGameOver && GameManager.Instance != null && GameManager.Instance.IsShopOpen)
            {
                yield return null;
            }
        }

        private void HandleGameOver()
        {
            _isGameOver = true;
            CurrentLevel = Mathf.Max(1, _startingLevel);

            if (_levelRoutine != null)
            {
                StopCoroutine(_levelRoutine);
                _levelRoutine = null;
            }

            if (_waveRunner != null)
            {
                _waveRunner.StopWaves();
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }
        }
    }
}
