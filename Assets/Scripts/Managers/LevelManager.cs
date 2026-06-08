using System;
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

        [Serializable]
        private class CampaignBossRoute
        {
            [SerializeField, Min(1)] private int _levelNumber = 25;
            [SerializeField] private Boss _boss;

            public int LevelNumber => _levelNumber;
            public Boss Boss => _boss;
        }

        public static LevelManager Instance { get; private set; }

        [SerializeField] private WaveRunner _waveRunner;
        [SerializeField] private EnemySpawner _enemySpawner;
        [SerializeField] private PlayerShooting _playerShooting;
        [SerializeField] private SectorTransitionController _sectorTransitionController;
        [SerializeField] private CycleScalingData _cycleScalingData;
        [SerializeField] private LevelData[] _levels;
        [SerializeField] private CampaignBossRoute[] _campaignBosses = Array.Empty<CampaignBossRoute>();
        [SerializeField, Min(1)] private int _startingLevel = 1;
        [SerializeField] private bool _playOnStart;
        [SerializeField, Min(0f)] private float _levelStartDelay = 2f;
        [SerializeField, Min(0f)] private float _levelTransitionDelay = 2f;
        [SerializeField, Min(1)] private int _shopInterval = 4;
        [SerializeField, Min(1)] private int _sectorTransitionInterval = 4;
        [SerializeField, Range(0f, 1f)] private float _finalDiveRemainingRatio = 0.1f;
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

        /// <summary>
        /// Player-facing cycle number. Levels 1-100 are cycle 1, 101-200 are cycle 2.
        /// </summary>
        public int CurrentCycleNumber => CycleScalingData.GetCycleNumberForLevel(CurrentLevel);

        /// <summary>
        /// Authored campaign level reused for the current absolute level inside a 100-level cycle.
        /// </summary>
        public int CurrentCampaignLevel => CycleScalingData.GetCampaignLevelForCycle(CurrentLevel);

        public LevelChangedEvent OnLevelStarted => _onLevelStarted;
        public LevelChangedEvent OnLevelCompleted => _onLevelCompleted;
        public Transform PlayerTransform => _playerShooting == null ? null : _playerShooting.transform;

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
            if (_playOnStart && (GameManager.Instance == null || GameManager.Instance.IsPlaying))
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

            ValidateCampaignBossRoutes();
        }

        /// <summary>
        /// Starts progression from the current level.
        /// </summary>
        public void PlayCurrentLevel()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"[{nameof(LevelManager)}] Play Mode is required to start level flow.");
                return;
            }

            if (_waveRunner == null || _enemySpawner == null)
            {
                Debug.LogError($"[{nameof(LevelManager)}] Missing required references.");
                return;
            }

            if (!HasAnyPlayableLevel())
            {
                Debug.LogError($"[{nameof(LevelManager)}] No LevelData or campaign boss routes configured.");
                return;
            }

            _isGameOver = false;

            if (_levelRoutine != null)
            {
                StopCoroutine(_levelRoutine);
            }

            _levelRoutine = StartCoroutine(LevelLoopRoutine());
        }

        /// <summary>
        /// Resets progression to the configured starting level and clears any active level runtime objects.
        /// </summary>
        public void ResetToStartingLevel()
        {
            CurrentLevel = Mathf.Max(1, _startingLevel);
            _isGameOver = false;

            if (_levelRoutine != null)
            {
                StopCoroutine(_levelRoutine);
                _levelRoutine = null;
            }

            _waveRunner?.StopWaves();
            _enemySpawner?.ClearActiveEnemies();
        }

        private IEnumerator LevelLoopRoutine()
        {
            while (!_isGameOver)
            {
                CycleScalingState cycleScaling = ResolveCurrentCycleScaling();
                _enemySpawner.SetCycleScaling(cycleScaling);
                _onLevelStarted?.Invoke(CurrentLevel);
                _levelStarted?.Raise(CurrentLevel);
                if (_levelStartDelay > 0f)
                {
                    yield return new WaitForSeconds(_levelStartDelay);
                    if (_isGameOver) break;
                }

                if (IsCampaignBossLevel(CurrentLevel))
                {
                    Boss boss = ResolveCampaignBoss(CurrentLevel);
                    if (boss == null)
                    {
                        Debug.LogError($"[{nameof(LevelManager)}] Level {CurrentLevel} is a boss level, but no campaign boss is assigned for campaign level {CurrentCampaignLevel}.");
                        _levelRoutine = null;
                        yield break;
                    }

                    yield return RunCampaignBossEncounterRoutine(boss, cycleScaling);
                }
                else
                {
                    AudioManager.Instance?.PlayMusicIfConfigured(AudioCue.MusicGameplay);

                    LevelData levelData = ResolveCurrentLevelData();
                    if (levelData == null)
                    {
                        Debug.LogError($"[{nameof(LevelManager)}] Failed to resolve data for level {CurrentLevel} using campaign level {CurrentCampaignLevel}.");
                        _levelRoutine = null;
                        yield break;
                    }

                    _playerShooting?.SetShootingEnabled(true);
                    _enemySpawner.BeginLevelEnemyTracking();
                    _waveRunner.PlayWaves(levelData.Waves);
                    yield return WaitForLevelClearRoutine();
                }

                if (_isGameOver) break;
                AwardSpecialPerfectClearBonus();
                _onLevelCompleted?.Invoke(CurrentLevel);
                _levelCompleted?.Raise(CurrentLevel);
                if (_levelTransitionDelay > 0f)
                {
                    yield return new WaitForSeconds(_levelTransitionDelay);
                }

                int completedLevel = CurrentLevel;
                int nextLevel = CurrentLevel + 1;
                if (!HasPlayableLevel(nextLevel))
                {
                    Debug.Log($"[{nameof(LevelManager)}] Completed authored levels at Level {CurrentLevel}. Waiting for restart.");
                    _levelRoutine = null;
                    yield break;
                }

                if (ShouldPlaySectorTransitionAfterLevel(completedLevel))
                {
                    yield return _sectorTransitionController.PlayTransitionRoutine();
                    if (_isGameOver) break;
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
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"[{nameof(LevelManager)}] Play Mode is required to force shop entry.");
                return;
            }

            GameManager.Instance?.EnterShop();
        }

        [ContextMenu("Dev/Play Current Or First Campaign Boss")]
        public void ForceCurrentCampaignBossEncounter()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"[{nameof(LevelManager)}] Play Mode is required to force a boss encounter.");
                return;
            }

            int bossLevel = CurrentLevel;
            Boss boss = ResolveCampaignBoss(CurrentLevel);
            if (boss == null)
            {
                boss = ResolveFirstCampaignBoss(out bossLevel);
                if (boss == null)
                {
                    Debug.LogWarning($"[{nameof(LevelManager)}] Cannot force a campaign boss because no campaign boss routes are assigned.");
                    return;
                }
            }

            CurrentLevel = bossLevel;

            _waveRunner?.StopWaves();
            _enemySpawner?.ClearActiveEnemies();

            if (_levelRoutine != null)
            {
                StopCoroutine(_levelRoutine);
            }

            _isGameOver = false;
            _levelRoutine = StartCoroutine(RunForcedCampaignBossRoutine(boss));
        }

        [ContextMenu("Dev/Play Boss Level 25")]
        private void PlayBossLevel25()
        {
            PlayLevelForTesting(25);
        }

        [ContextMenu("Dev/Play Boss Level 50")]
        private void PlayBossLevel50()
        {
            PlayLevelForTesting(50);
        }

        [ContextMenu("Dev/Play Boss Level 75")]
        private void PlayBossLevel75()
        {
            PlayLevelForTesting(75);
        }

        [ContextMenu("Dev/Play Boss Level 100")]
        private void PlayBossLevel100()
        {
            PlayLevelForTesting(100);
        }

        [ContextMenu("Dev/Play Cycle 2 Level 101")]
        private void PlayCycle2Level101()
        {
            PlayLevelForTesting(101);
        }

        /// <summary>
        /// Restarts level flow at a specific level for development testing.
        /// </summary>
        public void PlayLevelForTesting(int levelNumber)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"[{nameof(LevelManager)}] Play Mode is required to jump to level {levelNumber}.");
                return;
            }

            CurrentLevel = Mathf.Max(1, levelNumber);

            if (_levelRoutine != null)
            {
                StopCoroutine(_levelRoutine);
                _levelRoutine = null;
            }

            _waveRunner?.StopWaves();
            _enemySpawner?.ClearActiveEnemies();
            _enemySpawner?.SetCycleScaling(ResolveCurrentCycleScaling());
            PlayCurrentLevel();
        }

        private IEnumerator WaitForLevelClearRoutine()
        {
            while (!_isGameOver)
            {
                if (_waveRunner.HasCompletedSequence)
                {
                    _enemySpawner.ForceFinalEnemiesToDive(_finalDiveRemainingRatio);
                }

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
                if (candidate != null && candidate.LevelNumber == CurrentCampaignLevel)
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
                if (candidate != null && candidate.LevelNumber == CycleScalingData.GetCampaignLevelForCycle(levelNumber))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAnyPlayableLevel()
        {
            return (_levels != null && _levels.Length > 0) ||
                (_campaignBosses != null && _campaignBosses.Length > 0);
        }

        private bool HasPlayableLevel(int levelNumber)
        {
            if (IsCampaignBossLevel(levelNumber))
            {
                return ResolveCampaignBoss(levelNumber) != null;
            }

            return HasLevelData(levelNumber);
        }

        private bool ShouldEnterShopAfterLevel(int completedLevel)
        {
            return _shopInterval > 0 && completedLevel % _shopInterval == 0;
        }

        private bool ShouldPlaySectorTransitionAfterLevel(int completedLevel)
        {
            if (_sectorTransitionController == null)
            {
                return false;
            }

            int campaignLevel = CycleScalingData.GetCampaignLevelForCycle(completedLevel);
            bool completedEnemySet = _sectorTransitionInterval > 0 && campaignLevel % _sectorTransitionInterval == 0;
            return completedEnemySet || IsCampaignBossLevel(completedLevel);
        }

        private static bool IsCampaignBossLevel(int levelNumber)
        {
            return levelNumber > 0 && CycleScalingData.GetCampaignLevelForCycle(levelNumber) % 25 == 0;
        }

        private Boss ResolveCampaignBoss(int levelNumber)
        {
            if (_campaignBosses == null)
            {
                return null;
            }

            int campaignLevel = CycleScalingData.GetCampaignLevelForCycle(levelNumber);
            for (int i = 0; i < _campaignBosses.Length; i++)
            {
                CampaignBossRoute route = _campaignBosses[i];
                if (route != null && route.LevelNumber == campaignLevel)
                {
                    return route.Boss;
                }
            }

            return null;
        }

        private Boss ResolveFirstCampaignBoss(out int levelNumber)
        {
            levelNumber = 0;
            if (_campaignBosses == null)
            {
                return null;
            }

            for (int i = 0; i < _campaignBosses.Length; i++)
            {
                CampaignBossRoute route = _campaignBosses[i];
                if (route != null && route.Boss != null)
                {
                    levelNumber = route.LevelNumber;
                    return route.Boss;
                }
            }

            return null;
        }

        private IEnumerator RunCampaignBossEncounterRoutine(Boss bossReference, CycleScalingState cycleScaling)
        {
            AudioManager.Instance?.PlayMusicIfConfigured(AudioCue.MusicBoss);

            bool createdInstance = false;
            Boss boss = PrepareBossInstance(bossReference, out createdInstance);
            if (boss == null)
            {
                yield break;
            }

            boss.SetCycleScaling(cycleScaling);
            _playerShooting?.SetShootingEnabled(true);
            if (!boss.IsEncounterRunning)
            {
                boss.Spawn();
            }

            while (!_isGameOver && !boss.IsDefeated)
            {
                yield return null;
            }

            _playerShooting?.SetShootingEnabled(false);

            while (!_isGameOver && (Bullet.ActiveBulletCount > 0 || Pickup.ActivePickupCount > 0))
            {
                yield return null;
            }

            if (createdInstance && boss != null)
            {
                Destroy(boss.gameObject);
            }

            if (!_isGameOver)
            {
                AudioManager.Instance?.PlayMusicIfConfigured(AudioCue.MusicGameplay);
            }
        }

        private Boss PrepareBossInstance(Boss bossReference, out bool createdInstance)
        {
            createdInstance = false;
            if (bossReference == null)
            {
                return null;
            }

            Boss boss = bossReference;
            if (!bossReference.gameObject.scene.IsValid())
            {
                boss = Instantiate(bossReference);
                createdInstance = true;
            }

            boss.SetPlayerTarget(_playerShooting == null ? null : _playerShooting.transform);
            return boss;
        }

        private IEnumerator RunForcedCampaignBossRoutine(Boss boss)
        {
            yield return RunCampaignBossEncounterRoutine(boss, ResolveCurrentCycleScaling());
            _levelRoutine = null;
        }

        private CycleScalingState ResolveCurrentCycleScaling()
        {
            return _cycleScalingData == null
                ? CycleScalingData.ResolveDefault(CurrentLevel)
                : _cycleScalingData.Resolve(CurrentLevel);
        }

        private void ValidateCampaignBossRoutes()
        {
            if (_campaignBosses == null)
            {
                return;
            }

            for (int i = 0; i < _campaignBosses.Length; i++)
            {
                CampaignBossRoute route = _campaignBosses[i];
                if (route == null)
                {
                    Debug.LogWarning($"[{nameof(LevelManager)}] Campaign boss route {i} is empty on '{name}'.", this);
                    continue;
                }

                if (!IsCampaignBossLevel(route.LevelNumber))
                {
                    Debug.LogWarning(
                        $"[{nameof(LevelManager)}] Campaign boss route {i} uses level {route.LevelNumber}. Boss campaign levels should be 25, 50, 75, 100, etc.",
                        this);
                }

                if (route.Boss == null)
                {
                    Debug.LogWarning($"[{nameof(LevelManager)}] Campaign boss route {i} has no boss assigned on '{name}'.", this);
                }
            }
        }

        private void AwardSpecialPerfectClearBonus()
        {
            if (_enemySpawner == null || ScoreManager.Instance == null)
            {
                return;
            }

            int bonusScore = _enemySpawner.ConsumeSpecialPerfectClearBonusScore();
            if (bonusScore > 0)
            {
                ScoreManager.Instance.AddScore(bonusScore);
            }
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

            _enemySpawner?.ClearActiveEnemies();

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }
        }
    }
}
