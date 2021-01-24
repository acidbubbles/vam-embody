using SimpleJSON;
using UnityEngine;

public interface IEmbodyModule
{
    string storeId { get; }
    string label { get; }
    bool alwaysEnabled { get; }
    JSONStorableBool enabledJSON { get; }
    JSONStorableBool selectedJSON { get; }
    EmbodyContext context { get; set; }
    JSONStorableBool activeJSON { get; set; }

    bool BeforeEnable();
}

public abstract class EmbodyModuleBase : MonoBehaviour, IEmbodyModule
{
    public abstract string storeId { get; }
    public abstract string label { get; }
    public virtual bool alwaysEnabled => false;
    public JSONStorableBool selectedJSON { get; private set; }
    public JSONStorableBool enabledJSON { get; private set; }
    public EmbodyContext context { get; set; }
    public JSONStorableBool activeJSON { get; set; }

    protected Atom containingAtom => context.plugin.containingAtom;
    protected virtual bool shouldBeSelectedByDefault => false;

    public virtual void Awake()
    {
        selectedJSON = new JSONStorableBool("Selected", shouldBeSelectedByDefault, (bool val) =>
        {
            if (!activeJSON.val) return;
            activeJSON.val = false;
            activeJSON.val = true;
        });
        enabledJSON = new JSONStorableBool("Enabled", false, val => enabled = val);
    }

    public virtual bool BeforeEnable()
    {
        return true;
    }

    public virtual void OnEnable()
    {
        enabledJSON.valNoCallback = true;
    }

    public virtual void OnDisable()
    {
        enabledJSON.valNoCallback = false;
    }

    public virtual void StoreJSON(JSONClass jc)
    {
        selectedJSON.StoreJSON(jc);
    }

    public virtual void RestoreFromJSON(JSONClass jc)
    {
        selectedJSON.RestoreFromJSON(jc);
    }
}
