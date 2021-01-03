using System;
using SimpleJSON;
using UnityEngine;

public interface IEmbodyModule
{
}

public class EmbodyModuleBase : MonoBehaviour, IEmbodyModule
{
    public MVRScript plugin;

    protected Atom containingAtom => plugin.containingAtom;

    [Obsolete]
    protected bool needsStore
    {
        get { return plugin.needsStore; }
        set { plugin.needsStore = value; }
    }

    public virtual void Init()
    {
    }

    protected void RegisterBool(JSONStorableBool jsb) { plugin.RegisterBool(jsb); }
    protected void RegisterFloat(JSONStorableFloat jsf) { plugin.RegisterFloat(jsf); }
    protected void RegisterStringChooser(JSONStorableStringChooser jss) { plugin.RegisterStringChooser(jss); }

    protected UIDynamicToggle CreateToggle(JSONStorableBool jsb, bool rightSide = false) { return plugin.CreateToggle(jsb, rightSide); }
    protected UIDynamicSlider CreateSlider(JSONStorableFloat jsf, bool rightSide = false) { return plugin.CreateSlider(jsf, rightSide); }
    protected UIDynamicPopup CreateScrollablePopup(JSONStorableStringChooser jss, bool rightSide = false) { return plugin.CreateScrollablePopup(jss, rightSide); }
    protected UIDynamicPopup CreateFilterablePopup(JSONStorableStringChooser jss, bool rightSide = false) { return plugin.CreateFilterablePopup(jss, rightSide); }
    protected UIDynamicButton CreateButton(string label, bool rightSide = false) { return plugin.CreateButton(label, rightSide); }
    protected UIDynamic CreateSpacer(bool rightSide = false) { return plugin.CreateSpacer(rightSide); }

    [Obsolete]
    protected void SaveJSON(JSONClass jc, string saveName) => plugin.SaveJSON(jc, saveName);
    [Obsolete]
    protected JSONNode LoadJSON(string saveName) => plugin.LoadJSON(saveName);

    [Obsolete]
    public virtual JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
        return plugin.GetJSON(includePhysical, includeAppearance, forceStore);
    }

    public virtual void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
    {
        plugin.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
    }
}
