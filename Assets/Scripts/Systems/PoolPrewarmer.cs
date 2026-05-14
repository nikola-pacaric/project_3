using System.Collections.Generic;
using UnityEngine.Pool;

namespace Warblade.Systems
{
    /// <summary>
    /// Pre-creates pooled objects so first-use spikes do not happen during gameplay.
    /// </summary>
    public static class PoolPrewarmer
    {
        /// <summary>
        /// Gets and releases a number of pooled objects immediately.
        /// </summary>
        public static void Prewarm<T>(IObjectPool<T> pool, int count) where T : class
        {
            if (pool == null || count <= 0)
            {
                return;
            }

            List<T> instances = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                instances.Add(pool.Get());
            }

            for (int i = instances.Count - 1; i >= 0; i--)
            {
                pool.Release(instances[i]);
            }
        }
    }
}
