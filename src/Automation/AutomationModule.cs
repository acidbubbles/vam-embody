using System;
using SimpleJSON;
using UnityEngine;

public interface IAutomationModule : IEmbodyModule
{
    JSONStorableBool possessionActiveJSON { get; }
    KeyCode toggleKey { get; set; }
}

public class AutomationModule : EmbodyModuleBase, IAutomationModule
{
    public const string Label = "Automation";
    public override string storeId => "Automation";
    public override string label => Label;

    public IEmbody embody { get; set; }
    public JSONStorableBool possessionActiveJSON { get; private set; }
    public JSONStorableStringChooser toggleKeyJSON { get; set; }
    private FreeControllerV3 _headControl;
    public KeyCode toggleKey { get; set; } = KeyCode.None;

    public override void Awake()
    {
        base.Awake();

        possessionActiveJSON = new JSONStorableBool("Possession Active (Auto)", false, (bool val) =>
        {
            if (embody.activeJSON.val)
                embody.activeJSON.val = false;
        });


        enabled = true;
    }

    public void Start()
    {
        _headControl = (FreeControllerV3) containingAtom.GetStorableByID("headControl");
    }

    public override void OnEnable()
    {
        base.OnEnable();

    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    public void Update()
    {
        // TODO: Extract this into another module, with it's own config screen
        if (_headControl != null)
        {
            if (_headControl.possessed)
            {
                possessionActiveJSON.val = true;
                return;
            }

            if (possessionActiveJSON.val && !_headControl.possessed)
            {
                possessionActiveJSON.val = false;
                return;
            }
        }

        if (!embody.activeJSON.val)
        {
            if (!LookInputModule.singleton.inputFieldActive && toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
                embody.activeJSON.val = true;
        }
        else if (Input.GetKeyDown(KeyCode.Escape) || toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            embody.activeJSON.val = false;
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
}
