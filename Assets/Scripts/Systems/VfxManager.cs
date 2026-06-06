using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Warblade.Data;
using Warblade.Entities;

namespace Warblade.Systems
{
    /// <summary>
    /// Pools and plays one-shot presentation effects for combat, pickups, and level flow.
    /// </summary>
    public class VfxManager : MonoBehaviour
    {
        [Serializable]
        private class VfxPoolConfig
        {
            [SerializeField] private VfxCue _cue;
            [SerializeField] private PooledVfx _prefab;
            [SerializeField, Min(1)] private int _defaultCapacity = 8;
            [SerializeField, Min(1)] private int _maxSize = 32;

            public VfxCue Cue => _cue;
            public PooledVfx Prefab => _prefab;
            public int DefaultCapacity => _defaultCapacity;
            public int MaxSize => Mathf.Max(_defaultCapacity, _maxSize);
        }

        private sealed class VfxPool
        {
            public VfxPool(PooledVfx prefab, IObjectPool<PooledVfx> pool)
            {
                Prefab = prefab;
                Pool = pool;
            }

            public PooledVfx Prefab { get; }
            public IObjectPool<PooledVfx> Pool { get; }
        }

        public static VfxManager Instance { get; private set; }

        [SerializeField] private Vector3 _defaultWorldPosition = Vector3.zero;
        [SerializeField] private VfxPoolConfig[] _vfxPools = Array.Empty<VfxPoolConfig>();

        private readonly Dictionary<VfxCue, VfxPool> _pools = new Dictionary<VfxCue, VfxPool>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildPools();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Plays a cue at a world position using an identity rotation.
        /// </summary>
        public void Play(VfxCue cue, Vector3 position)
        {
            Play(cue, position, Quaternion.identity);
        }

        /// <summary>
        /// Plays a cue at a world position and applies an optional effect-specific color tint.
        /// </summary>
        public void Play(VfxCue cue, Vector3 position, Color tintColor)
        {
            Play(cue, position, Quaternion.identity, tintColor);
        }

        /// <summary>
        /// Plays a cue at a world position, rotated so directional effects can face the action.
        /// </summary>
        public void Play(VfxCue cue, Vector3 position, Vector2 direction)
        {
            Quaternion rotation = direction.sqrMagnitude > Mathf.Epsilon
                ? Quaternion.LookRotation(Vector3.forward, direction.normalized)
                : Quaternion.identity;

            Play(cue, position, rotation);
        }

        /// <summary>
        /// Plays a cue at the manager's configured default world position.
        /// </summary>
        public void PlayAtDefaultPosition(VfxCue cue)
        {
            Play(cue, _defaultWorldPosition, Quaternion.identity);
        }

        /// <summary>
        /// Plays a cue at a world position with an explicit rotation.
        /// </summary>
        public void Play(VfxCue cue, Vector3 position, Quaternion rotation)
        {
            Play(cue, position, rotation, null);
        }

        private void Play(VfxCue cue, Vector3 position, Quaternion rotation, Color? tintColor)
        {
            if (cue == VfxCue.None)
            {
                return;
            }

            if (!_pools.TryGetValue(cue, out VfxPool vfxPool) || vfxPool.Pool == null)
            {
                return;
            }

            PooledVfx vfx = vfxPool.Pool.Get();
            vfx.Play(position, rotation, tintColor);
        }

        private void BuildPools()
        {
            _pools.Clear();

            if (_vfxPools == null)
            {
                return;
            }

            for (int i = 0; i < _vfxPools.Length; i++)
            {
                VfxPoolConfig config = _vfxPools[i];
                if (config == null || config.Cue == VfxCue.None || config.Prefab == null)
                {
                    continue;
                }

                if (_pools.ContainsKey(config.Cue))
                {
                    Debug.LogWarning($"[{nameof(VfxManager)}] Duplicate VFX cue '{config.Cue}' on '{name}'. The first entry will be used.", this);
                    continue;
                }

                IObjectPool<PooledVfx> pool = null;
                pool = new ObjectPool<PooledVfx>(
                    createFunc: () => CreateVfx(config.Prefab, pool),
                    actionOnGet: vfx => vfx.gameObject.SetActive(true),
                    actionOnRelease: vfx => vfx.gameObject.SetActive(false),
                    actionOnDestroy: vfx => Destroy(vfx.gameObject),
                    collectionCheck: true,
                    defaultCapacity: config.DefaultCapacity,
                    maxSize: config.MaxSize);

                PoolPrewarmer.Prewarm(pool, config.DefaultCapacity);
                _pools.Add(config.Cue, new VfxPool(config.Prefab, pool));
            }
        }

        private PooledVfx CreateVfx(PooledVfx prefab, IObjectPool<PooledVfx> pool)
        {
            PooledVfx vfx = Instantiate(prefab, transform);
            vfx.SetPool(pool);
            return vfx;
        }
    }
}
