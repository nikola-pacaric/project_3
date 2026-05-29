using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    public class PooledVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem[] _particleSystems;
        [SerializeField, Min(0.01f)] private float _fallbackLifetime = 1f;

        private IObjectPool<PooledVfx> _pool;
        private Coroutine _releaseRoutine;

        public void SetPool(IObjectPool<PooledVfx> pool)
        {
            _pool = pool;
        }

        public void Play(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            ResolveParticleSystems();

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Clear(true);
                particleSystem.Play(true);
            }

            if (_releaseRoutine != null)
            {
                StopCoroutine(_releaseRoutine);
            }

            _releaseRoutine = StartCoroutine(ReleaseAfterLifetime());
        }

        private void OnDisable()
        {
            if (_releaseRoutine != null)
            {
                StopCoroutine(_releaseRoutine);
                _releaseRoutine = null;
            }
        }

        private void OnValidate()
        {
            _fallbackLifetime = Mathf.Max(0.01f, _fallbackLifetime);
            ResolveParticleSystems();
        }

        private IEnumerator ReleaseAfterLifetime()
        {
            yield return new WaitForSeconds(CalculateLifetime());
            _releaseRoutine = null;
            ReturnToPool();
        }

        private float CalculateLifetime()
        {
            ResolveParticleSystems();

            float lifetime = _fallbackLifetime;
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = particleSystem.main;
                lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
            }

            return lifetime;
        }

        private void ReturnToPool()
        {
            if (_pool != null)
            {
                _pool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void ResolveParticleSystems()
        {
            if (_particleSystems == null || _particleSystems.Length == 0)
            {
                _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            }
        }
    }
}
