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
}
