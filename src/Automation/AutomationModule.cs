using System;
using System.Diagnostics.CodeAnalysis;
using SimpleJSON;
using UnityEngine;

public interface IAutomationModule : IEmbodyModule
{
    KeyCode toggleKey { get; set; }
    JSONStorableBool takeOverVamPossess { get; }
    JSONStorableBool autoArmForRecord { get; }
}

public class AutomationModule : EmbodyModuleBase, IAutomationModule
{
    public const string Label = "Automation";
    public override string storeId => "Automation";
    public override string label => Label;
    public override bool skipChangeEnabledWhenActive => true;

    public JSONStorableBool takeOverVamPossess { get; } = new JSONStorableBool("TakeOverVamPossess", true);
    public JSONStorableBool autoArmForRecord { get; } = new JSONStorableBool("AutoArmForRecord", false);
    public KeyCode toggleKey { get; set; } = KeyCode.None;

    private bool headPossessedInVam => !ReferenceEquals(_headControl, null) && _headControl.possessed;
    private FreeControllerV3 _headControl;
    private bool _activatedByVam;

    public void Start()
    {
        _headControl = (FreeControllerV3) containingAtom.GetStorableByID("headControl");
    }

    [SuppressMessage("ReSharper", "RedundantJumpStatement")]
    public void Update()
    {
        if (context.embody.activeJSON.val)
        {
            if (SuperController.singleton.currentSelectMode == SuperController.SelectMode.Possess
                || SuperController.singleton.currentSelectMode == SuperController.SelectMode.PossessAndAlign
                || SuperController.singleton.currentSelectMode == SuperController.SelectMode.TwoStagePossess)
            {
                SuperController.singleton.SelectModeOff();
                context.embody.activeJSON.val = false;
                return;
            }
        }
        else
        {
            if (headPossessedInVam)
            {
                context.embody.activeJSON.val = true;
                _activatedByVam = true;
                return;
            }

            if (takeOverVamPossess.val)
            {
                if (SuperController.singleton.currentSelectMode == SuperController.SelectMode.Possess
                    || SuperController.singleton.currentSelectMode == SuperController.SelectMode.PossessAndAlign
                    || SuperController.singleton.currentSelectMode == SuperController.SelectMode.TwoStagePossess)
                {
                    if (SuperController.singleton.GetSelectedAtom() == context.containingAtom)
                    {
                        SuperController.singleton.SelectModeOff();
                        context.embody.activeJSON.val = true;
                        return;
                    }
                }
            }

            if (!LookInputModule.singleton.inputFieldActive && toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            {
                context.embody.activeJSON.val = true;
                return;
            }
        }

        if (_activatedByVam && context.embody.activeJSON.val && !_headControl.possessed)
        {
            context.embody.activeJSON.val = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            context.embody.activeJSON.val = false;
            return;
        }
    }

    public override void StoreJSON(JSONClass jc, bool includeProfile)
    {
        base.StoreJSON(jc, includeProfile);

        jc["ToggleKey"] = toggleKey.ToString();
        if (includeProfile)
        {
            autoArmForRecord.val = true;
            takeOverVamPossess.val = true;
        }
    }

    public override void RestoreFromJSON(JSONClass jc, bool fromDefaults)
    {
        base.RestoreFromJSON(jc, fromDefaults);

        var toggleKeyString = jc["ToggleKey"].Value;
        if (!string.IsNullOrEmpty(toggleKeyString) && toggleKeyString != KeyCode.None.ToString())
            toggleKey = (KeyCode) Enum.Parse(typeof(KeyCode), toggleKeyString);

        if (fromDefaults)
        {
            autoArmForRecord.RestoreFromJSON(jc);
            takeOverVamPossess.RestoreFromJSON(jc);
        }
    }

    public void Reset()
    {
        _activatedByVam = false;
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();
        toggleKey = KeyCode.None;
        takeOverVamPossess.SetValToDefault();
        autoArmForRecord.SetValToDefault();
    }
}
