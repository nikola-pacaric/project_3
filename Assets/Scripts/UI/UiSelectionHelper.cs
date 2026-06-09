using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Warblade.UI
{
    internal static class UiSelectionHelper
    {
        private static readonly Stack<GameObject> SelectionStack = new Stack<GameObject>();

        public static void ClearSelectionStack()
        {
            SelectionStack.Clear();
        }

        public static void SelectNextFrame(MonoBehaviour owner, GameObject target)
        {
            if (owner != null && owner.isActiveAndEnabled)
            {
                owner.StartCoroutine(SelectAfterFrame(target));
                return;
            }

            Select(target);
        }

        public static void ApplyPanelNavigation(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            List<Selectable> selectables = GetSelectableNavigationOrder(root);
            for (int i = 0; i < selectables.Count; i++)
            {
                Selectable selectable = selectables[i];
                Navigation navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = i > 0 ? selectables[i - 1] : null;
                navigation.selectOnDown = i < selectables.Count - 1 ? selectables[i + 1] : null;
                navigation.selectOnLeft = null;
                navigation.selectOnRight = null;
                selectable.navigation = navigation;

                ColorBlock colors = selectable.colors;
                colors.selectedColor = colors.highlightedColor;
                selectable.colors = colors;

                if (selectable is Slider)
                {
                    UiSelectableFocusVisual focusVisual = selectable.GetComponent<UiSelectableFocusVisual>();
                    if (focusVisual == null)
                    {
                        focusVisual = selectable.gameObject.AddComponent<UiSelectableFocusVisual>();
                    }

                    focusVisual.Configure(selectable);
                }
            }
        }

        private static List<Selectable> GetSelectableNavigationOrder(GameObject root)
        {
            Selectable[] allSelectables = root.GetComponentsInChildren<Selectable>(true);
            List<Selectable> selectables = new List<Selectable>();

            for (int i = 0; i < allSelectables.Length; i++)
            {
                Selectable selectable = allSelectables[i];
                if (selectable == null || !selectable.gameObject.activeSelf || !selectable.interactable)
                {
                    continue;
                }

                selectables.Add(selectable);
            }

            selectables.Sort(CompareSelectablesForVerticalNavigation);
            return selectables;
        }

        private static int CompareSelectablesForVerticalNavigation(Selectable left, Selectable right)
        {
            Vector3 leftPosition = left.transform.position;
            Vector3 rightPosition = right.transform.position;

            int yComparison = rightPosition.y.CompareTo(leftPosition.y);
            if (yComparison != 0)
            {
                return yComparison;
            }

            return leftPosition.x.CompareTo(rightPosition.x);
        }

        public static void PushSelectionAndSelectNextFrame(
            MonoBehaviour owner,
            GameObject target,
            GameObject fallbackReturnSelection = null)
        {
            PushCurrentSelection(fallbackReturnSelection);
            SelectNextFrame(owner, target);
        }

        public static void RestorePreviousSelectionNextFrame(MonoBehaviour owner, GameObject fallbackSelection = null)
        {
            if (owner != null && owner.isActiveAndEnabled)
            {
                owner.StartCoroutine(RestoreAfterFrame(fallbackSelection));
                return;
            }

            RestorePreviousSelection(fallbackSelection);
        }

        private static IEnumerator SelectAfterFrame(GameObject target)
        {
            yield return null;
            Select(target);
        }

        private static IEnumerator RestoreAfterFrame(GameObject fallbackSelection)
        {
            yield return null;
            RestorePreviousSelection(fallbackSelection);
        }

        private static void PushCurrentSelection(GameObject fallbackSelection)
        {
            GameObject selection = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

            if (!CanSelect(selection))
            {
                selection = fallbackSelection;
            }

            if (CanSelect(selection))
            {
                SelectionStack.Push(selection);
            }
        }

        private static void RestorePreviousSelection(GameObject fallbackSelection)
        {
            while (SelectionStack.Count > 0)
            {
                GameObject selection = SelectionStack.Pop();
                if (CanSelect(selection))
                {
                    Select(selection);
                    return;
                }
            }

            Select(fallbackSelection);
        }

        private static void Select(GameObject target)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            eventSystem.SetSelectedGameObject(null);

            if (CanSelect(target))
            {
                eventSystem.SetSelectedGameObject(target);
            }
        }

        private static bool CanSelect(GameObject target)
        {
            if (target == null || !target.activeInHierarchy)
            {
                return false;
            }

            Selectable selectable = target.GetComponent<Selectable>();
            return selectable != null && selectable.IsActive() && selectable.IsInteractable();
        }
    }
}
