using System.Linq;
using SimpleJSON;

public interface ITrackersModule : IEmbodyModule
{
}

public class TrackersModule : EmbodyModuleBase, ITrackersModule
{
    private FreeControllerV3 _headControl;
    public const string Label = "Trackers";

    public override string storeId => "Trackers";
    public override string label => Label;
    protected override bool shouldBeSelectedByDefault => true;

    public override void Awake()
    {
        base.Awake();

        _headControl = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "headControl");
    }

    public override void OnEnable()
    {
        base.OnEnable();

        if (_headControl.possessed)
            enabled = false;

        // TODO: Do the parenting, see how vam does it (however we might want to allow offsets)
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);
    }
}
