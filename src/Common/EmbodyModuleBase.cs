using SimpleJSON;
using UnityEngine;

public interface IEmbodyModule
{
    string storeId { get; }
    string label { get; }
    bool skipChangeEnabledWhenActive { get; }
    JSONStorableBool enabledJSON { get; }
    JSONStorableBool selectedJSON { get; }
    EmbodyContext context { get; set; }
    JSONStorableBool activeJSON { get; set; }

    bool Validate();
    void PreActivate();
    void PostDeactivate();
    void ResetToDefault();
}

public abstract class EmbodyModuleBase : MonoBehaviour, IEmbodyModule
{
    public abstract string storeId { get; }
    public abstract string label { get; }
    public virtual bool skipChangeEnabledWhenActive => false;
    public JSONStorableBool selectedJSON { get; private set; }
    public JSONStorableBool enabledJSON { get; private set; }
    public EmbodyContext context { get; set; }
    public JSONStorableBool activeJSON { get; set; }

    protected Atom containingAtom => context.plugin.containingAtom;
    protected virtual bool shouldBeSelectedByDefault => false;

    public virtual void Awake()
    {
        selectedJSON = new JSONStorableBool("Selected", shouldBeSelectedByDefault, (bool val) => context.embody.Refresh());
        enabledJSON = new JSONStorableBool("Enabled", false, val => enabled = val);
    }

    public virtual bool Validate()
    {
        return true;
    }

    public virtual void PreActivate()
    {
    }

    public virtual void PostDeactivate()
    {
    }

    public virtual void OnEnable()
    {
        enabledJSON.valNoCallback = true;
    }

    public virtual void OnDisable()
    {
        enabledJSON.valNoCallback = false;
    }

    public virtual void StoreJSON(JSONClass jc, bool includeProfile)
    {
        selectedJSON.StoreJSON(jc);
    }

    public virtual void RestoreFromJSON(JSONClass jc, bool fromDefaults)
    {
        if (!fromDefaults)
            selectedJSON.RestoreFromJSON(jc);
    }

    public virtual void ResetToDefault()
    {
        selectedJSON.val = shouldBeSelectedByDefault;
    }
}
