using System;
using SimpleJSON;
using UnityEngine;

public interface IEmbodyModule
{
    JSONStorableBool enabledJSON { get; }
    MVRScript plugin { get; set; }
}

public abstract class EmbodyModuleBase : MonoBehaviour, IEmbodyModule
{
    public abstract string storeId { get; }
    public JSONStorableBool includedJSON { get; private set; }
    public JSONStorableBool enabledJSON { get; private set; }
    public MVRScript plugin { get; set; }

    protected Atom containingAtom => plugin.containingAtom;

    public virtual void Init()
    {
        // TODO: When changed, it should disable and re-enable Embody (use a Unity event or go through plugin)
        includedJSON = new JSONStorableBool("Included", false, val => enabled = val);
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
        includedJSON.StoreJSON(jc);
    }

    public virtual void RestoreFromJSON(JSONClass jc)
    {
        includedJSON.RestoreFromJSON(jc);
    }
}
