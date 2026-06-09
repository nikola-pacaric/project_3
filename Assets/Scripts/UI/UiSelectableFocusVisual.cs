using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Warblade.UI
{
    [DisallowMultipleComponent]
    internal sealed class UiSelectableFocusVisual : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private static readonly Color FocusOutlineColor = new Color(0f, 0.85f, 1f, 0.95f);
        private static readonly Vector2 FocusOutlineDistance = new Vector2(4f, -4f);
        private const float FocusScale = 1.18f;

        private Selectable _selectable;
        private Graphic _targetGraphic;
        private RectTransform _targetRectTransform;
        private Outline _outline;
        private Vector3 _originalScale = Vector3.one;
        private bool _hasOriginalScale;

        public void Configure(Selectable selectable)
        {
            _selectable = selectable;
            _targetGraphic = selectable != null ? selectable.targetGraphic : null;
            ResolveReferences();
            SetFocused(false);
        }

        private void Awake()
        {
            ResolveReferences();
            SetFocused(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetFocused(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetFocused(false);
        }

        private void ResolveReferences()
        {
            if (_selectable == null)
            {
                _selectable = GetComponent<Selectable>();
            }

            if (_targetGraphic == null && _selectable != null)
            {
                _targetGraphic = _selectable.targetGraphic;
            }

            if (_targetGraphic == null)
            {
                return;
            }

            if (_targetRectTransform == null)
            {
                _targetRectTransform = _targetGraphic.rectTransform;
            }

            if (!_hasOriginalScale && _targetRectTransform != null)
            {
                _originalScale = _targetRectTransform.localScale;
                _hasOriginalScale = true;
            }

            if (_outline == null)
            {
                _outline = _targetGraphic.GetComponent<Outline>();
                if (_outline == null)
                {
                    _outline = _targetGraphic.gameObject.AddComponent<Outline>();
                }

                _outline.effectColor = FocusOutlineColor;
                _outline.effectDistance = FocusOutlineDistance;
                _outline.useGraphicAlpha = false;
            }
        }

        private void SetFocused(bool isFocused)
        {
            ResolveReferences();

            bool shouldShow = isFocused
                && _selectable != null
                && _selectable.IsActive()
                && _selectable.IsInteractable();

            if (_outline != null)
            {
                _outline.enabled = shouldShow;
            }

            if (_targetRectTransform != null && _hasOriginalScale)
            {
                _targetRectTransform.localScale = shouldShow
                    ? _originalScale * FocusScale
                    : _originalScale;
            }
        }
    }
}
