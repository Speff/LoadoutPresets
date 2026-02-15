using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Main = LoadoutPresets.LoadoutPresets;

namespace LoadoutPresets;

internal static class NavigationHelper
{
    /// <summary>
    /// Registers a button's MyButtonNormal with WindowManager.activeWindow.allButtons
    /// so the game's controller navigation system recognizes it.
    /// </summary>
    public static void RegisterButton(Button button)
    {
        if (button == null) return;

        var myButton = button.GetComponent<MyButtonNormal>();
        if (myButton == null)
        {
            Main.Logger.LogWarning($"NavigationHelper: No MyButtonNormal on '{button.gameObject.name}', skipping registration.");
            return;
        }

        var activeWindow = WindowManager.activeWindow;
        if (activeWindow == null)
        {
            Main.Logger.LogWarning("NavigationHelper: No active window, skipping registration.");
            return;
        }

        if (!activeWindow.allButtons.Contains(myButton))
        {
            activeWindow.allButtons.Add(myButton);
            Main.Logger.LogDebug($"NavigationHelper: Registered '{button.gameObject.name}' with active window.");
        }
    }

    /// <summary>
    /// Sets the EventSystem's currently selected GameObject for controller focus.
    /// </summary>
    public static void SetInitialSelection(GameObject target)
    {
        if (target == null || EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(target);
        Main.Logger.LogDebug($"NavigationHelper: Set initial selection to '{target.name}'.");
    }

    /// <summary>
    /// Configures a Selectable for automatic Unity navigation (d-pad/stick).
    /// </summary>
    public static void SetAutomaticNavigation(Selectable selectable)
    {
        if (selectable == null) return;

        selectable.navigation = new Navigation
        {
            mode = Navigation.Mode.Automatic
        };
    }

    /// <summary>
    /// Clears the EventSystem selection to prevent stale references.
    /// </summary>
    public static void ClearSelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// Converts a stretch-anchored RectTransform to explicit sizing (point anchors + sizeDelta).
    /// The game's SelectionArrow reads RectTransform size to scale the cursor highlight.
    /// Stretch-anchored elements have sizeDelta=(0,0), which makes the cursor tiny.
    /// This must be called after the parent is active and Canvas layout has been computed.
    /// </summary>
    public static void ConvertToExplicitSize(RectTransform rect)
    {
        if (rect == null) return;

        var size = rect.rect.size;
        if (size.x <= 0 || size.y <= 0) return;

        var center = new Vector2(
            (rect.anchorMin.x + rect.anchorMax.x) / 2f,
            (rect.anchorMin.y + rect.anchorMax.y) / 2f
        );

        rect.anchorMin = center;
        rect.anchorMax = center;
        rect.sizeDelta = size;
    }

    /// <summary>
    /// Forces a canvas layout update, then converts all Button RectTransforms
    /// under the given root to explicit sizing for correct SelectionArrow cursor scaling.
    /// </summary>
    public static void FixButtonSizesForCursor(Transform root)
    {
        if (root == null) return;

        Canvas.ForceUpdateCanvases();

        var buttons = root.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            var rect = button.GetComponent<RectTransform>();
            if (rect != null && rect.sizeDelta == Vector2.zero)
            {
                ConvertToExplicitSize(rect);
            }
        }

        Main.Logger.LogDebug($"NavigationHelper: Converted {buttons.Length} buttons to explicit sizing under '{root.name}'.");
    }
}
