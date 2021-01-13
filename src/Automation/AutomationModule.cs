using System;
using SimpleJSON;
using UnityEngine;

public interface IAutomationModule : IEmbodyModule
{
    KeyCode toggleKey { get; set; }
}

public class AutomationModule : EmbodyModuleBase, IAutomationModule
{
    public const string Label = "Automation";
    public override string storeId => "Automation";
    public override string label => Label;
    public override bool alwaysEnabled => true;

    public IEmbody embody { get; set; }
    public bool headPossessedInVam => !ReferenceEquals(_headControl, null) && _headControl.possessed;
    public KeyCode toggleKey { get; set; } = KeyCode.None;
    private FreeControllerV3 _headControl;
    private bool _activatedByVam;

    public override void Awake()
    {
        base.Awake();

        enabled = true;
    }

    public void Start()
    {
        _headControl = (FreeControllerV3) containingAtom.GetStorableByID("headControl");
    }

    public void Update()
    {
        // ReSharper disable once RedundantJumpStatement

        if (!embody.activeJSON.val)
        {
            if (headPossessedInVam)
            {
                embody.activeJSON.val = true;
                _activatedByVam = true;
                return;
            }

            if (!LookInputModule.singleton.inputFieldActive && toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            {
                embody.activeJSON.val = true;
                return;
            }
        }

        if (_activatedByVam && embody.activeJSON.val && !_headControl.possessed)
        {
            embody.activeJSON.val = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            embody.activeJSON.val = false;
            return;
        }
    }

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);

        jc["ToggleKey"] = toggleKey.ToString();
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);

        var toggleKeyString = jc["ToggleKey"].Value;
        if (!string.IsNullOrEmpty(toggleKeyString))
            toggleKey = (KeyCode) Enum.Parse(typeof(KeyCode), toggleKeyString);
    }

    public void Reset()
    {
        _activatedByVam = false;
    }
}
