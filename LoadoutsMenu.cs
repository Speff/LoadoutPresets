using Il2CppInterop.Runtime.InteropTypes.Arrays;

using System;
using System.Collections.Generic;
using System.Linq;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using Assets.Scripts.Managers;

using Main = LoadoutPresets.LoadoutPresets;
using ObjectNames = LoadoutPresets.Constants.ObjectNames;

namespace LoadoutPresets;

internal static class LoadoutsMenu
{
    private static GameObject _loadoutsMenuPanel;
    private static GameObject _mainMenuPanel;
    private static Transform _loadoutListContainer;
    private static TMP_InputField _loadoutNameInput;
    private static GameObject _characterSelect;
    private static string _loadoutBeingEdited;

    public static void CreateLoadoutsMenu(Il2CppReferenceArray<GameObject> rootGameObjects)
    {
        var uiRoot = rootGameObjects.FirstOrDefault(obj => string.Equals(obj.name, ObjectNames.UI, StringComparison.OrdinalIgnoreCase));
        if (!uiRoot)
        {
            Main.Logger.LogError("LoadoutsMenu: Could not find UI root.");
            return;
        }

        var creditsMenuTransform = uiRoot.transform.Find("Tabs/W_Credits");
        if (!creditsMenuTransform)
        {
            Main.Logger.LogError("LoadoutsMenu: Could not find Credits menu template.");
            return;
        }

        _loadoutsMenuPanel = LoadoutsMenuFactory.CloneCreditsAsLoadoutsMenu(
            creditsMenuTransform.gameObject,
            uiRoot.transform.Find("Tabs")
        );
        _mainMenuPanel = _loadoutsMenuPanel.transform.parent.Find("Menu")?.gameObject;

        _loadoutListContainer = _loadoutsMenuPanel.transform.Find("WindowLayers/Content/ScrollRect/ContentEntries");
        if (_loadoutListContainer)
        {
            Main.Logger.LogDebug($"LoadoutsMenu: Found ContentEntries container");
        }
        else
        {
            Main.Logger.LogWarning("LoadoutsMenu: Could not find container. Using fallback.");
            var scrollRect = _loadoutsMenuPanel.GetComponentInChildren<ScrollRect>();
            if (scrollRect && scrollRect.content)
            {
                _loadoutListContainer = scrollRect.content;
                Main.Logger.LogDebug("LoadoutsMenu: Using ScrollRect.content as container.");
            }
        }

        _loadoutListContainer.gameObject.GetOrAddComponent<VerticalLayoutGroup>().childControlHeight = true;

        var characterSelectTransform = uiRoot.transform.Find("Tabs/Character/W_Character");
        _characterSelect = CharacterSelectFactory.CloneCharacterSelect(
            characterSelectTransform.gameObject,
            _loadoutsMenuPanel.transform
        );

        Main.Logger.LogDebug("LoadoutsMenu: Menu created successfully.");
    }

    /// <summary>
    /// Stores reference to the loadout name input field.
    /// Called by MenuFactory during menu creation.
    /// </summary>
    public static void SetLoadoutNameInput(TMP_InputField inputField)
    {
        _loadoutNameInput = inputField;
    }

    /// <summary>
    /// Opens the Loadouts menu and populates it with saved loadouts.
    /// </summary>
    public static void OpenMenu()
    {
        if (!_loadoutsMenuPanel)
        {
            Main.Logger.LogError("LoadoutsMenu: Cannot open menu - panel not initialized.");
            return;
        }

        _characterSelect.SetActive(false);
        _loadoutsMenuPanel.transform.parent.Find("Menu")?.gameObject.SetActive(false);

        if (_loadoutNameInput)
        {
            _loadoutNameInput.DeactivateInputField();
            var placeholderText = _loadoutNameInput.placeholder?.GetComponent<TextMeshProUGUI>();
            if (placeholderText)
            {
                placeholderText.text = "New loadout name...";
                placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }

        RefreshLoadoutList();
        SetDefaultLoadoutName();

        if (_mainMenuPanel)
            _mainMenuPanel.SetActive(false);

        _loadoutsMenuPanel.SetActive(true);

        NavigationHelper.FixButtonSizesForCursor(_loadoutsMenuPanel.transform);
        SetupNavigation();
        SetInitialSelection();

        Main.Logger.LogDebug("LoadoutsMenu: Menu opened successfully.");
    }

    /// <summary>
    /// Closes the Loadouts menu.
    /// </summary>
    public static void CloseMenu()
    {
        NavigationHelper.ClearSelection();

        if (_loadoutsMenuPanel)
        {
            _loadoutsMenuPanel.SetActive(false);
            Main.Logger.LogDebug("LoadoutsMenu: Menu closed.");
        }

        if (_mainMenuPanel)
        {
            _mainMenuPanel.SetActive(true);
        }

        _loadoutsMenuPanel.transform.parent.Find("Menu")?.gameObject.SetActive(true);   
    }

    public static void OpenCharacterSelect(string loadoutName)
    {
        if (!_characterSelect)
        {
            Main.Logger.LogError("LoadoutsMenu: Cannot open character select - not initialized.");
            return;
        }

        _loadoutBeingEdited = loadoutName;
        if (_characterSelect.activeSelf)
        {
            Main.Logger.LogDebug("LoadoutsMenu: Character select already open.");
            return;
        }

        Main.Logger.LogDebug($"LoadoutsMenu: Opening character select for loadout '{loadoutName}'.");

        if (!_loadoutsMenuPanel.activeSelf)
        {
            Main.Logger.LogDebug("LoadoutsMenu: Loadouts menu not open. Opening menu first.");
            OpenMenu();
        }

        _characterSelect.SetActive(true);
    }

    /// <summary>
    /// Called when a character is selected from the Character Select menu.
    /// Links the selected character to the currently editing loadout.
    /// </summary>
    /// <param name="selectedCharacter">The character that was selected.</param>
    public static void OnCharacterSelected(ECharacter selectedCharacter)
    {
        if (string.IsNullOrEmpty(_loadoutBeingEdited))
        {
            Main.Logger.LogError("LoadoutsMenu: No loadout is currently being edited.");
            return;
        }

        Main.Logger.LogDebug($"LoadoutsMenu: Character '{selectedCharacter}' selected for loadout '{_loadoutBeingEdited}'.");

        Main.UpdateLoadoutCharacter(_loadoutBeingEdited, selectedCharacter);
        CloseCharacterSelect();

        _loadoutBeingEdited = null;
    }

    public static void CloseCharacterSelect()
    {
        if (_characterSelect && _characterSelect.activeSelf)
        {
            _characterSelect.SetActive(false);
            Main.Logger.LogDebug("LoadoutsMenu: Character select closed.");
        }

        _loadoutBeingEdited = null;
    }

    public static string GetCurrentEditingLoadout() => _loadoutBeingEdited;

    public static void RefreshLoadoutList()
    {
        if (!_loadoutListContainer)
        {
            Main.Logger.LogWarning("LoadoutsMenu: Cannot refresh - container not found.");
            return;
        }

        var titleText = _loadoutsMenuPanel.transform.Find("Header/Header/T_Title")?.GetComponent<TextMeshProUGUI>();
        var allLoadouts = Main.LoadoutDatasCache().OrderBy(x => x.Name);
        
        foreach (var loadout in allLoadouts)
        {
            var linkedCharacter = loadout.LinkedCharacter.GetEnumFromDisplayName();

            LoadoutListFactory.CreateLoadoutListItem(
                loadout.Name,
                linkedCharacter,
                _loadoutListContainer,
                titleText
            );
        }

        Main.Logger.LogDebug("LoadoutsMenu: Loadout list refreshed.");
    }

    public static void AddLoadoutListItem(string loadoutName)
    {
        var item = LoadoutListFactory.CreateLoadoutListItem(
            loadoutName,
            null,
            _loadoutListContainer,
            _loadoutsMenuPanel.transform.Find("Header/Header/T_Title")?.GetComponent<TextMeshProUGUI>()
        );

        if (item != null)
        {
            NavigationHelper.FixButtonSizesForCursor(item.transform);
            SetupNavigation();
        }

        Main.Logger.LogDebug($"LoadoutsMenu: Added loadout list item for '{loadoutName}'.");
    }

    public static void RemoveLoadoutListItem(string loadoutName)
    {
        if (LoadoutListFactory.TryRemoveLoadoutListItem(loadoutName))
            SetupNavigation();

        Main.Logger.LogDebug($"LoadoutsMenu: Removed loadout list item for '{loadoutName}'.");
    }

    /// <summary>
    /// Sets the input field text to a default name "Loadout #" where # is the count of saved loadouts + 1.
    /// Also updates the wrapper button text to match.
    /// </summary>
    public static void SetDefaultLoadoutName()
    {
        if (!_loadoutNameInput || !_loadoutsMenuPanel) return;

        var count = Main.LoadoutDatasCache().Count() + 1;
        var defaultName = $"Loadout {count}";

        _loadoutNameInput.text = defaultName;
        _loadoutNameInput.DeactivateInputField();

        var wrapper = _loadoutsMenuPanel.transform.Find("WindowLayers/Content/SubHeader/B_InputFieldWrapper");
        var wrapperText = wrapper?.Find("B_InputFieldWrapper_Text_Protected")?.GetComponent<TextMeshProUGUI>();
        if (wrapperText)
            wrapperText.text = defaultName;
    }

    public static void UpdateLoadoutListItemCharacter(string loadoutName, ECharacter? linkedCharacter)
    {
        if (!LoadoutListFactory.TryUpdateLoadoutListItemCharacter(loadoutName, linkedCharacter))
            RefreshLoadoutList();
    }

    /// <summary>
    /// Builds an explicit navigation grid for all menu buttons and wires them up.
    /// Row 0: [B_Back]
    /// Row 1: [B_InputFieldWrapper, B_SaveLoadout, B_OpenFolder]
    /// Row 2+: [B_CharacterSelector, B_Load, B_Delete] per loadout
    /// </summary>
    private static void SetupNavigation()
    {
        var rows = new List<Selectable[]>();

        // Row 0: Back button
        var backButton = _loadoutsMenuPanel.transform.Find("Header/Header/B_Back")?.GetComponent<Button>();
        if (backButton)
            rows.Add(new Selectable[] { backButton });

        // Row 1: Sub-header (input wrapper, save, open folder)
        var subHeader = _loadoutsMenuPanel.transform.Find("WindowLayers/Content/SubHeader");
        if (subHeader)
        {
            var inputWrapper = subHeader.Find("B_InputFieldWrapper")?.GetComponent<Button>();
            var saveButton = subHeader.Find("B_SaveLoadout")?.GetComponent<Button>();
            var openFolder = subHeader.Find("B_OpenFolder")?.GetComponent<Button>();
            rows.Add(new Selectable[] { inputWrapper, saveButton, openFolder });
        }

        // Row 2+: Loadout items
        if (_loadoutListContainer)
        {
            for (int i = 0; i < _loadoutListContainer.childCount; i++)
            {
                var item = _loadoutListContainer.GetChild(i);
                if (!item.gameObject.activeSelf) continue;

                var charSelector = item.Find("B_CharacterSelector")?.GetComponent<Button>();
                var loadBtn = item.Find("B_Load")?.GetComponent<Button>();
                var deleteBtn = item.Find("B_Delete")?.GetComponent<Button>();
                rows.Add(new Selectable[] { charSelector, loadBtn, deleteBtn });
            }
        }

        NavigationHelper.SetupGridNavigation(rows);
    }

    /// <summary>
    /// Sets initial controller focus using the game's ButtonManager.ForceHoverButton API.
    /// Selects the first loadout's Load button, or falls back to Save if no loadouts exist.
    /// </summary>
    private static void SetInitialSelection()
    {
        // Try to select the first loadout's Load button
        if (_loadoutListContainer && _loadoutListContainer.childCount > 0)
        {
            var firstItem = _loadoutListContainer.GetChild(0);
            var loadButton = firstItem.Find("B_Load")?.GetComponent<MyButtonNormal>();
            if (loadButton)
            {
                ButtonManager.ForceHoverButton(loadButton);
                Main.Logger.LogDebug("LoadoutsMenu: Set initial selection to first loadout's Load button.");
                return;
            }
        }

        // Fallback: select the Save button
        var saveButton = _loadoutsMenuPanel.transform.Find("WindowLayers/Content/SubHeader/B_SaveLoadout")?.GetComponent<MyButtonNormal>();
        if (saveButton)
        {
            ButtonManager.ForceHoverButton(saveButton);
            Main.Logger.LogDebug("LoadoutsMenu: Set initial selection to Save button (no loadouts).");
            return;
        }

        Main.Logger.LogDebug("LoadoutsMenu: No suitable button for initial controller selection.");
    }

}