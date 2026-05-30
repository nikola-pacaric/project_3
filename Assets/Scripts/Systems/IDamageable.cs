using UnityEngine;

namespace Warblade.Systems
{
    public interface IDamageable
    {
        void TakeDamage(int amount);
    }

    /// <summary>
    /// Receives damage with the world position where the hit was detected.
    /// </summary>
    public interface IHitPointDamageable : IDamageable
    {
        void TakeDamage(int amount, Vector3 hitPoint);
    }
}
