using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Warblade.Data;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.Entities
{
    public class PlayerHealth : MonoBehaviour, IHitPointDamageable
    {
        public static event Action GameOverRaised;

        [Header("Death and Respawn")]
        [SerializeField, Min(0f)] private float _deathPresentationDuration = 0.75f;
        [SerializeField, Min(0f)] private float _respawnDelay = 1.25f;
        [SerializeField, Min(0f)] private float _respawnInvulnerabilityDuration = 3f;

        [Header("Runtime References")]
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private PlayerShooting _shooting;
        [SerializeField] private Collider2D[] _damageColliders;
        [SerializeField] private SpriteRenderer[] _spriteRenderers;
        [SerializeField] private ParticleSystem[] _thrusterParticles;

        [Header("Events")]
        [SerializeField] private UnityEvent _onDeathStarted;
        [SerializeField] private UnityEvent _onRespawned;
        [SerializeField] private UnityEvent _onRespawnProtectionStarted;
        [SerializeField] private UnityEvent _onRespawnProtectionEnded;
        [SerializeField] private UnityEvent _onGameOver;

        private bool _isDead;
        private bool _isInvulnerable;
        private Coroutine _deathRoutine;
        private Coroutine _respawnProtectionRoutine;

        public bool IsInvulnerable => _isInvulnerable;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            _deathPresentationDuration = Mathf.Max(0f, _deathPresentationDuration);
            _respawnDelay = Mathf.Max(0f, _respawnDelay);
            _respawnInvulnerabilityDuration = Mathf.Max(0f, _respawnInvulnerabilityDuration);
        }

        public void TakeDamage(int amount)
        {
            TakeDamage(amount, transform.position);
        }

        public void TakeDamage(int amount, Vector3 hitPoint)
        {
            if (_isDead) return;
            if (_deathRoutine != null) return;
            if (_isInvulnerable) return;
            if (amount <= 0) return;

            ResolveHit(hitPoint);
        }

        private void ResolveHit(Vector3 hitPoint)
        {
            RunStatsManager runStats = RunStatsManager.Instance;
            if (runStats == null)
            {
                Debug.LogError($"[{nameof(PlayerHealth)}] Cannot resolve player damage without a {nameof(RunStatsManager)}.");
                return;
            }

            if (IsShieldActive(runStats))
            {
                VfxManager.Instance?.Play(VfxCue.ShieldHit, hitPoint);
                return;
            }

            if (runStats.TryConsumeArmour())
            {
                VfxManager.Instance?.Play(VfxCue.PlayerHit, transform.position);
                return;
            }

            runStats.DowngradeWeaponTier();
            bool outOfLives = runStats.LoseLife();
            VfxManager.Instance?.Play(VfxCue.PlayerDeath, transform.position);
            _deathRoutine = StartCoroutine(RunDeathRoutine(outOfLives));
        }

        private IEnumerator RunDeathRoutine(bool isFinalDeath)
        {
            SetPlayerActive(false);
            _onDeathStarted?.Invoke();

            if (_deathPresentationDuration > 0f)
            {
                yield return new WaitForSeconds(_deathPresentationDuration);
            }

            if (isFinalDeath)
            {
                RaiseGameOver();
                _deathRoutine = null;
                yield break;
            }

            if (_respawnDelay > 0f)
            {
                yield return new WaitForSeconds(_respawnDelay);
            }

            Respawn();
            _deathRoutine = null;
        }

        private void RaiseGameOver()
        {
            if (_isDead) return;

            _isDead = true;
            GameManager.Instance?.EnterGameOver();
            GameOverRaised?.Invoke();
            _onGameOver?.Invoke();
        }

        private void Respawn()
        {
            SetPlayerActive(true);
            StartRespawnProtection();
            _onRespawned?.Invoke();
        }

        private void StartRespawnProtection()
        {
            if (_respawnProtectionRoutine != null)
            {
                StopCoroutine(_respawnProtectionRoutine);
            }

            _respawnProtectionRoutine = StartCoroutine(RunRespawnProtection());
        }

        private IEnumerator RunRespawnProtection()
        {
            _isInvulnerable = true;
            _onRespawnProtectionStarted?.Invoke();

            if (_respawnInvulnerabilityDuration > 0f)
            {
                yield return new WaitForSeconds(_respawnInvulnerabilityDuration);
            }

            _isInvulnerable = false;
            _onRespawnProtectionEnded?.Invoke();
            _respawnProtectionRoutine = null;
        }

        private void SetPlayerActive(bool isActive)
        {
            ResolveReferences();

            if (_movement != null)
            {
                _movement.enabled = isActive;
            }

            if (_shooting != null)
            {
                _shooting.SetShootingEnabled(isActive);
            }

            for (int i = 0; i < _damageColliders.Length; i++)
            {
                if (_damageColliders[i] != null)
                {
                    _damageColliders[i].enabled = isActive;
                }
            }

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null)
                {
                    _spriteRenderers[i].enabled = isActive;
                }
            }

            for (int i = 0; i < _thrusterParticles.Length; i++)
            {
                ParticleSystem thruster = _thrusterParticles[i];
                if (thruster == null)
                {
                    continue;
                }

                if (isActive)
                {
                    thruster.Play();
                }
                else
                {
                    thruster.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private bool IsShieldActive(RunStatsManager runStats)
        {
            if (BuffManager.Instance != null)
            {
                return BuffManager.Instance.IsShieldActive;
            }

            return runStats.IsShieldActive;
        }

        private void ResolveReferences()
        {
            if (_movement == null)
            {
                _movement = GetComponent<PlayerMovement>();
            }

            if (_shooting == null)
            {
                _shooting = GetComponent<PlayerShooting>();
            }

            if (_damageColliders == null || _damageColliders.Length == 0)
            {
                _damageColliders = GetComponents<Collider2D>();
            }

            if (_spriteRenderers == null || _spriteRenderers.Length == 0)
            {
                _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            if (_thrusterParticles == null || _thrusterParticles.Length == 0)
            {
                _thrusterParticles = GetComponentsInChildren<ParticleSystem>(true);
            }
        }
    }
}
