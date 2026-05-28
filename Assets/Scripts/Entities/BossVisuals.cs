using UnityEngine;
using Warblade.Data;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    public class BossVisuals : MonoBehaviour
    {
        private SpriteRenderer[] _spriteRenderers;
        private Color[] _baseSpriteColors;
        private Animator[] _animators;

        internal void Initialize(SpriteRenderer[] spriteRenderers)
        {
            if (_baseSpriteColors != null)
            {
                return;
            }

            _spriteRenderers = spriteRenderers == null || spriteRenderers.Length == 0
                ? GetComponentsInChildren<SpriteRenderer>(true)
                : spriteRenderers;

            _baseSpriteColors = new Color[_spriteRenderers == null ? 0 : _spriteRenderers.Length];
            for (int i = 0; i < _baseSpriteColors.Length; i++)
            {
                _baseSpriteColors[i] = _spriteRenderers[i] == null ? Color.white : _spriteRenderers[i].color;
            }

            _animators = GetComponentsInChildren<Animator>(true);
        }

        internal void PauseAnimationAtFirstFrame()
        {
            if (_animators == null)
            {
                return;
            }

            for (int i = 0; i < _animators.Length; i++)
            {
                Animator animator = _animators[i];
                if (animator == null)
                {
                    continue;
                }

                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        internal void PlayAnimation()
        {
            if (_animators == null)
            {
                return;
            }

            for (int i = 0; i < _animators.Length; i++)
            {
                Animator animator = _animators[i];
                if (animator != null)
                {
                    animator.speed = 1f;
                }
            }
        }

        internal void ApplyCycleVisuals(CycleScalingState cycleScaling)
        {
            if (_spriteRenderers == null || _baseSpriteColors == null)
            {
                return;
            }

            int count = Mathf.Min(_spriteRenderers.Length, _baseSpriteColors.Length);
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer spriteRenderer = _spriteRenderers[i];
                if (spriteRenderer == null)
                {
                    continue;
                }

                Color color = Color.Lerp(_baseSpriteColors[i], cycleScaling.TintColor, cycleScaling.TintStrength);
                color.a = _baseSpriteColors[i].a;
                spriteRenderer.color = color;
            }
        }
    }
}
