﻿using System;
using System.Diagnostics.CodeAnalysis;
using SimpleJSON;
using UnityEngine;

public interface IAutomationModule : IEmbodyModule
{
    KeyCode toggleKey { get; set; }
    JSONStorableBool takeOverVamPossess { get; }
    JSONStorableBool autoArmForRecord { get; }
    void Reset();
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
#if(VAM_GT_1_20_77_0)
            if (SuperController.singleton.currentSelectMode == SuperController.SelectMode.Possess
                || SuperController.singleton.currentSelectMode == SuperController.SelectMode.PossessAndAlign
                || SuperController.singleton.currentSelectMode == SuperController.SelectMode.TwoStagePossess)
            {
                SuperController.singleton.SelectModeOff();
                context.embody.Deactivate();
                return;
            }
#endif
        }
        else
        {
            if (headPossessedInVam)
            {
                context.embody.ActivateManually();
                _activatedByVam = true;
                return;
            }

#if(VAM_GT_1_20_77_0)
            if (takeOverVamPossess.val)
            {
                if (SuperController.singleton.currentSelectMode == SuperController.SelectMode.Possess
                    || SuperController.singleton.currentSelectMode == SuperController.SelectMode.PossessAndAlign
                    || SuperController.singleton.currentSelectMode == SuperController.SelectMode.TwoStagePossess)
                {
                    if (SuperController.singleton.GetSelectedAtom() == context.containingAtom)
                    {
                        SuperController.singleton.SelectModeOff();
                        context.embody.ActivateManually();
                        return;
                    }
                }
            }
#endif

            if (!LookInputModule.singleton.inputFieldActive && toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            {
                context.embody.ActivateManually();
                return;
            }
        }

        if (_activatedByVam && context.embody.activeJSON.val && !_headControl.possessed)
        {
            context.embody.Deactivate();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            context.embody.Deactivate();
            return;
        }
    }

    public override void StoreJSON(JSONClass jc, bool toProfile, bool toScene)
    {
        base.StoreJSON(jc, toProfile, toScene);

        jc["ToggleKey"] = toggleKey.ToString();
        if (toProfile)
        {
            autoArmForRecord.val = true;
            takeOverVamPossess.val = true;
        }
    }

    public override void RestoreFromJSON(JSONClass jc, bool fromProfile, bool fromScene)
    {
        base.RestoreFromJSON(jc, fromProfile, fromScene);

        var toggleKeyString = jc["ToggleKey"].Value;
        if (!string.IsNullOrEmpty(toggleKeyString) && toggleKeyString != KeyCode.None.ToString())
            toggleKey = (KeyCode) Enum.Parse(typeof(KeyCode), toggleKeyString);

        if (fromProfile)
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
