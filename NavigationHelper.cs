using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Main = LoadoutPresets.LoadoutPresets;

namespace LoadoutPresets;

internal static class NavigationHelper
{
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
    /// Sets up explicit grid navigation on a list of rows.
    /// Each row is an array of Selectables. Wires left/right within rows
    /// and up/down between rows by column index.
    /// Mirrors what the game's TabGridNavigation does.
    /// </summary>
    public static void SetupGridNavigation(List<Selectable[]> rows)
    {
        if (rows == null || rows.Count == 0) return;

        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            if (row == null || row.Length == 0) continue;

            for (int c = 0; c < row.Length; c++)
            {
                var selectable = row[c];
                if (selectable == null) continue;

                var nav = new Navigation { mode = Navigation.Mode.Explicit };

                // Left/right within the row
                if (c > 0 && row[c - 1] != null)
                    nav.selectOnLeft = row[c - 1];
                if (c < row.Length - 1 && row[c + 1] != null)
                    nav.selectOnRight = row[c + 1];

                // Up: find the same column (or closest) in the row above
                if (r > 0)
                    nav.selectOnUp = FindInRow(rows[r - 1], c);

                // Down: find the same column (or closest) in the row below
                if (r < rows.Count - 1)
                    nav.selectOnDown = FindInRow(rows[r + 1], c);

                selectable.navigation = nav;
            }
        }

        Main.Logger.LogDebug($"NavigationHelper: SetupGridNavigation wired {rows.Count} rows.");
    }

    /// <summary>
    /// Forces a canvas layout update, then converts all Button RectTransforms
    /// under the given root to explicit sizing for correct SelectionArrow cursor scaling.
    /// SelectionArrow reads RectTransform.sizeDelta to determine cursor size.
    /// Stretch-anchored elements have sizeDelta=(0,0), which makes the cursor tiny.
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

    /// <summary>
    /// Converts a stretch-anchored RectTransform to center-point anchors with explicit sizeDelta.
    /// Must be called after the parent is active and Canvas layout has been computed.
    /// </summary>
    private static void ConvertToExplicitSize(RectTransform rect)
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
    /// Finds a selectable in a row by column index, clamping to the row bounds.
    /// </summary>
    private static Selectable FindInRow(Selectable[] row, int preferredColumn)
    {
        if (row == null || row.Length == 0) return null;

        var col = Mathf.Clamp(preferredColumn, 0, row.Length - 1);
        return row[col];
    }
}
