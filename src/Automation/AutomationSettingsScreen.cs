using System;
using System.Collections.Generic;
using UnityEngine;

public class AutomationSettingsScreen : ScreenBase, IScreen
{
    private static List<string> _keys;

    public const string ScreenName = AutomationModule.Label;

    private readonly IAutomationModule _automation;

    public AutomationSettingsScreen(MVRScript plugin, IAutomationModule automation)
        : base(plugin)
    {
        _automation = automation;
    }

    public void Show()
    {
        CreateText(
            new JSONStorableString("",
                "This module automatically enables/disabled Embody when VaM possession is activated. You can also press <b>Esc</b> to exit Embody at any time."), true);

        var toggleKeyJSON = new JSONStorableStringChooser("Toggle Key", GetKeys(), KeyCode.None.ToString(), "Toggle Key",
            val => { _automation.toggleKey = (KeyCode) Enum.Parse(typeof(KeyCode), val); });
        var toggleKeyPopup = CreateFilterablePopup(toggleKeyJSON, true);
        toggleKeyPopup.popupPanelHeight = 700f;
    }

    private static List<string> GetKeys()
    {
        if (_keys != null) return _keys;

        _keys = Enum.GetNames(typeof(KeyCode)).ToList();
        _keys.Remove(KeyCode.Mouse0.ToString());
        _keys.Remove(KeyCode.Escape.ToString());
        _keys.Remove(KeyCode.None.ToString());
        _keys.Insert(0, KeyCode.None.ToString());
        return _keys;
    }
}
