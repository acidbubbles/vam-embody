using System;
using SimpleJSON;
using UnityEngine;

public interface IEmbodyModule
{
    JSONStorableBool enabledJSON { get; }
    MVRScript plugin { get; set; }
}

public class EmbodyModuleBase : MonoBehaviour, IEmbodyModule
{
    public JSONStorableBool enabledJSON { get; private set; }
    public MVRScript plugin { get; set; }

    protected Atom containingAtom => plugin.containingAtom;

    [Obsolete]
    protected bool needsStore
    {
        get { return plugin.needsStore; }
        set { plugin.needsStore = value; }
    }

    public virtual void Init()
    {
        enabledJSON = new JSONStorableBool("Enabled", false, val => enabled = val);
    }

    public virtual void OnEnable()
    {
        enabledJSON.valNoCallback = true;
    }

    public virtual void OnDisable()
    {
        enabledJSON.valNoCallback = false;
    }

    [Obsolete]
    protected void RegisterBool(JSONStorableBool jsb) { plugin.RegisterBool(jsb); }
    [Obsolete]
    protected void RegisterFloat(JSONStorableFloat jsf) { plugin.RegisterFloat(jsf); }
    [Obsolete]
    protected void RegisterStringChooser(JSONStorableStringChooser jss) { plugin.RegisterStringChooser(jss); }

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
