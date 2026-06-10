using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Warblade.UI
{
    internal sealed class FirstRunControlsHintOverlay : MonoBehaviour
    {
        private const string RuntimeObjectName = "FirstRunControlsHintOverlay";
        private const int SortingOrder = 32760;

        private static FirstRunControlsHintOverlay _instance;

        private CanvasGroup _canvasGroup;
        private TMP_FontAsset _fontAsset;
        private Button _confirmButton;
        private Action _onConfirmed;

        public static FirstRunControlsHintOverlay Show(TMP_FontAsset fontAsset, Action onConfirmed)
        {
            if (_instance == null)
            {
                _instance = CreateRuntimeInstance();
            }

            _instance.ShowInternal(fontAsset, onConfirmed);
            return _instance;
        }

        private static FirstRunControlsHintOverlay CreateRuntimeInstance()
        {
            GameObject overlayObject = new GameObject(
                RuntimeObjectName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));

            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            StretchToParent(overlayRect);

            Canvas canvas = overlayObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;

            FirstRunControlsHintOverlay overlay = overlayObject.AddComponent<FirstRunControlsHintOverlay>();
            overlay._canvasGroup = overlayObject.GetComponent<CanvasGroup>();
            overlay.BuildUi(overlayRect);
            overlay.SetVisible(false);
            return overlay;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void ShowInternal(TMP_FontAsset fontAsset, Action onConfirmed)
        {
            _fontAsset = fontAsset;
            _onConfirmed = onConfirmed;
            ApplyFontToChildren();
            SetVisible(true);
            SelectConfirmButton();
        }

        private void Confirm()
        {
            Action callback = _onConfirmed;
            _onConfirmed = null;
            SetVisible(false);
            callback?.Invoke();
        }

        private void BuildUi(RectTransform overlayRect)
        {
            Image dimmer = CreateImage("Dimmer", overlayRect, new Color(0f, 0f, 0f, 0.74f));
            StretchToParent(dimmer.rectTransform);

            GameObject panelObject = new GameObject(
                "ControlsHintPanel",
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline),
                typeof(VerticalLayoutGroup));
            panelObject.transform.SetParent(overlayRect, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(720f, 500f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.02f, 0.07f, 0.12f, 0.96f);

            Outline outline = panelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0.85f, 1f, 0.7f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;

            VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(42, 42, 34, 34);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateText(panelRect, "CONTROLS", 38f, FontStyles.UpperCase, TextAlignmentOptions.Center, 52f);
            CreateText(panelRect, "Move Left:  Left Arrow / A", 28f, FontStyles.Normal, TextAlignmentOptions.Left, 40f);
            CreateText(panelRect, "Move Right:  Right Arrow / D", 28f, FontStyles.Normal, TextAlignmentOptions.Left, 40f);
            CreateText(panelRect, "Fire:  Left Ctrl / Space", 28f, FontStyles.Normal, TextAlignmentOptions.Left, 40f);
            CreateText(panelRect, "Pause:  P / Escape", 28f, FontStyles.Normal, TextAlignmentOptions.Left, 40f);
            CreateText(panelRect, "One press fires one shot. Autofire only works while the bonus is active.", 22f, FontStyles.Normal, TextAlignmentOptions.Center, 78f);

            Button confirmButton = CreateButton(panelRect, "Start Run");
            confirmButton.onClick.AddListener(Confirm);
            UiButtonClickSound clickSound = confirmButton.gameObject.AddComponent<UiButtonClickSound>();
            clickSound.Configure(confirmButton);

            UiSelectableFocusVisual focusVisual = confirmButton.gameObject.AddComponent<UiSelectableFocusVisual>();
            focusVisual.Configure(confirmButton);

            _confirmButton = confirmButton;
        }

        private TMP_Text CreateText(
            Transform parent,
            string text,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            float height)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);

            TMP_Text tmpText = textObject.GetComponent<TMP_Text>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.fontStyle = fontStyle;
            tmpText.alignment = alignment;
            tmpText.color = Color.white;
            tmpText.textWrappingMode = TextWrappingModes.Normal;

            LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = height;

            return tmpText;
        }

        private Button CreateButton(Transform parent, string label)
        {
            GameObject buttonObject = new GameObject(
                "StartRunButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.95f, 0.78f, 0.22f, 0.94f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(1f, 0.93f, 0.48f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.82f, 0.58f, 0.15f, 1f);
            colors.disabledColor = new Color(0.22f, 0.22f, 0.22f, 0.6f);
            button.colors = colors;

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 280f;
            layoutElement.preferredHeight = 62f;

            TMP_Text buttonText = CreateText(buttonObject.transform, label, 28f, FontStyles.UpperCase, TextAlignmentOptions.Center, 62f);
            buttonText.color = new Color(0.02f, 0.04f, 0.08f, 1f);

            RectTransform textRect = buttonText.rectTransform;
            StretchToParent(textRect);

            return button;
        }

        private static Image CreateImage(string objectName, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private void ApplyFontToChildren()
        {
            if (_fontAsset == null)
            {
                return;
            }

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].font = _fontAsset;
            }
        }

        private void SetVisible(bool isVisible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = isVisible ? 1f : 0f;
                _canvasGroup.interactable = isVisible;
                _canvasGroup.blocksRaycasts = isVisible;
            }

            gameObject.SetActive(isVisible);
        }

        private void SelectConfirmButton()
        {
            if (_confirmButton == null || EventSystem.current == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_confirmButton.gameObject);
        }
    }
}
