using System;
using UnityEngine;

// TODO: Probably to deprecate... or based on player height v.s. model height if I can figure out sitting model height...
public interface IAutomation : IEmbodyModule
{
    JSONStorableBool possessionActiveJSON { get; }
    JSONStorableStringChooser toggleKeyJSON { get; }
}

public class AutomationModule : EmbodyModuleBase, IAutomation
{
    public IEmbody embody { get; set; }
    public JSONStorableBool possessionActiveJSON { get; private set; }
    public JSONStorableStringChooser toggleKeyJSON { get; set; }
    private FreeControllerV3 _headControl;
    private KeyCode _toggleKey = KeyCode.None;

    public override void Init()
    {
        base.Init();

            _headControl = (FreeControllerV3) containingAtom.GetStorableByID("headControl");

            possessionActiveJSON = new JSONStorableBool("Possession Active (Auto)", false, (bool val) =>
            {
                if (embody.activeJSON.val)
                    embody.activeJSON.val = false;
            });

            var keys = Enum.GetNames(typeof(KeyCode)).ToList();
            keys.Remove(KeyCode.None.ToString());
            keys.Insert(0, KeyCode.None.ToString());
            toggleKeyJSON = new JSONStorableStringChooser("Toggle Key", keys, KeyCode.None.ToString(), "Toggle Key",
                val => { _toggleKey = (KeyCode) Enum.Parse(typeof(KeyCode), val); });
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
            if (!LookInputModule.singleton.inputFieldActive && _toggleKey != KeyCode.None && Input.GetKeyDown(_toggleKey))
                embody.activeJSON.val = true;
        }
        else if (Input.GetKeyDown(KeyCode.Escape) || _toggleKey != KeyCode.None && Input.GetKeyDown(_toggleKey))
        {
            embody.activeJSON.val = false;
        }
    }
}
