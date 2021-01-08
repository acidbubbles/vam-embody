using System;
using SimpleJSON;
using UnityEngine;

public interface IEmbodyModule
{
    string storeId { get; }
    string label { get; }
    JSONStorableBool enabledJSON { get; }
    JSONStorableBool selectedJSON { get; }
    MVRScript plugin { get; set; }
    JSONStorableBool activeJSON { get; set; }
}

public abstract class EmbodyModuleBase : MonoBehaviour, IEmbodyModule
{
    public abstract string storeId { get; }
    public abstract string label { get; }
    public JSONStorableBool selectedJSON { get; private set; }
    public JSONStorableBool enabledJSON { get; private set; }
    public MVRScript plugin { get; set; }
    public JSONStorableBool activeJSON { get; set; }

    protected Atom containingAtom => plugin.containingAtom;
    protected virtual bool shouldBeSelectedByDefault => false;

    public virtual void Awake()
    {
        selectedJSON = new JSONStorableBool("Selected", shouldBeSelectedByDefault, val =>
        {
            if (activeJSON.val)
                enabledJSON.val = val;
        });
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

    public virtual void StoreJSON(JSONClass jc)
    {
        selectedJSON.StoreJSON(jc);
    }

    public virtual void RestoreFromJSON(JSONClass jc)
    {
        selectedJSON.RestoreFromJSON(jc);
    }
}
