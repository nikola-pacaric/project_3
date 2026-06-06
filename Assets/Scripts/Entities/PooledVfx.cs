using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering.Universal;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    public class PooledVfx : MonoBehaviour
    {
        private const string DeathCoreName = "DeathCore";
        private const string EnergyRingName = "EnergyRing";
        private const string ImpactLightName = "ImpactLight";

        private struct ParticleColorBinding
        {
            public ParticleColorBinding(ParticleSystem particleSystem, ParticleSystem.MinMaxGradient baseStartColor)
            {
                ParticleSystem = particleSystem;
                BaseStartColor = baseStartColor;
            }

            public ParticleSystem ParticleSystem { get; }
            public ParticleSystem.MinMaxGradient BaseStartColor { get; }
        }

        private struct LightColorBinding
        {
            public LightColorBinding(Light2D light, Color baseColor)
            {
                Light = light;
                BaseColor = baseColor;
            }

            public Light2D Light { get; }
            public Color BaseColor { get; }
        }

        [SerializeField] private ParticleSystem[] _particleSystems;
        [SerializeField, Min(0.01f)] private float _fallbackLifetime = 1f;

        private IObjectPool<PooledVfx> _pool;
        private ParticleColorBinding[] _tintTargets;
        private LightColorBinding[] _lightTintTargets;
        private Coroutine _releaseRoutine;

        public void SetPool(IObjectPool<PooledVfx> pool)
        {
            _pool = pool;
        }

        public void Play(Vector3 position, Quaternion rotation)
        {
            Play(position, rotation, null);
        }

        public void Play(Vector3 position, Quaternion rotation, Color? tintColor)
        {
            transform.SetPositionAndRotation(position, rotation);
            ResolveParticleSystems();
            ApplyTint(tintColor);

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

        private void ApplyTint(Color? tintColor)
        {
            ResolveTintTargets();
            ResolveLightTintTargets();

            for (int i = 0; i < _tintTargets.Length; i++)
            {
                ParticleColorBinding target = _tintTargets[i];
                if (target.ParticleSystem == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = target.ParticleSystem.main;
                main.startColor = tintColor.HasValue
                    ? CreateTintedStartColor(target.BaseStartColor, tintColor.Value)
                    : target.BaseStartColor;
            }

            for (int i = 0; i < _lightTintTargets.Length; i++)
            {
                LightColorBinding target = _lightTintTargets[i];
                if (target.Light == null)
                {
                    continue;
                }

                target.Light.color = tintColor.HasValue
                    ? ApplyTintColor(target.BaseColor, tintColor.Value)
                    : target.BaseColor;
            }
        }

        private void ResolveTintTargets()
        {
            if (_tintTargets != null)
            {
                return;
            }

            ResolveParticleSystems();

            int targetCount = 0;
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (ShouldTintParticleSystem(particleSystem))
                {
                    targetCount++;
                }
            }

            _tintTargets = new ParticleColorBinding[targetCount];
            int targetIndex = 0;
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (!ShouldTintParticleSystem(particleSystem))
                {
                    continue;
                }

                _tintTargets[targetIndex] = new ParticleColorBinding(
                    particleSystem,
                    particleSystem.main.startColor);
                targetIndex++;
            }
        }

        private void ResolveLightTintTargets()
        {
            if (_lightTintTargets != null)
            {
                return;
            }

            Light2D[] lights = GetComponentsInChildren<Light2D>(true);
            int targetCount = 0;
            for (int i = 0; i < lights.Length; i++)
            {
                if (ShouldTintLight(lights[i]))
                {
                    targetCount++;
                }
            }

            _lightTintTargets = new LightColorBinding[targetCount];
            int targetIndex = 0;
            for (int i = 0; i < lights.Length; i++)
            {
                Light2D light = lights[i];
                if (!ShouldTintLight(light))
                {
                    continue;
                }

                _lightTintTargets[targetIndex] = new LightColorBinding(light, light.color);
                targetIndex++;
            }
        }

        private static bool ShouldTintParticleSystem(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                return false;
            }

            string objectName = particleSystem.gameObject.name;
            return objectName == DeathCoreName || objectName == EnergyRingName;
        }

        private static bool ShouldTintLight(Light2D light)
        {
            return light != null && light.gameObject.name == ImpactLightName;
        }

        private static ParticleSystem.MinMaxGradient CreateTintedStartColor(
            ParticleSystem.MinMaxGradient baseStartColor,
            Color tintColor)
        {
            switch (baseStartColor.mode)
            {
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(
                        ApplyTintColor(baseStartColor.colorMin, tintColor),
                        ApplyTintColor(baseStartColor.colorMax, tintColor));

                case ParticleSystemGradientMode.Color:
                default:
                    return new ParticleSystem.MinMaxGradient(ApplyTintColor(baseStartColor.color, tintColor));
            }
        }

        private static Color ApplyTintColor(Color baseColor, Color tintColor)
        {
            tintColor.a = baseColor.a;
            return tintColor;
        }
    }
}
