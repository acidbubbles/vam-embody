using System.Collections.Generic;
using System.Linq;

public class RecordViveTrackersStep : WizardStepBase, IWizardStep
{
    public string helpText => @"Now <b>take the same pose</b> as the model, and <b>align all vive trackers</b> as closely as possible to your model's position.

Press Next when you are ready.

Each Vive tracker will be assigned a control on the model, and their relative position will be recorded.".TrimStart();

    private NavigationRigSnapshot _navigationRigSnapshot;
    private float _heightAdjust;

    public RecordViveTrackersStep(EmbodyContext context)
        : base(context)
    {

    }

    public override void Enter()
    {
        base.Enter();

        _navigationRigSnapshot = NavigationRigSnapshot.Snap();
        context.embody.activeJSON.val = true;
        _heightAdjust = SuperController.singleton.playerHeightAdjust;
        /*
        This method creates less movement noise, but it's harder to precisely align eyes
        context.worldScale.enabledJSON.val = true;
        SuperController.singleton.AlignRigAndController(
            context.containingAtom.freeControllers.First(fc => fc.name == "headControl"),
            context.trackers.motionControls.First(mc => mc.name == MotionControlNames.Head),
            false);
        */
    }

    public override void Update()
    {
        SuperController.singleton.playerHeightAdjust = _heightAdjust;
    }

    public bool Apply()
    {
        context.diagnostics.TakeSnapshot($"{nameof(RecordViveTrackersStep)}.{nameof(Apply)}.Before");
        var autoSetup = new TrackerAutoSetup(context.containingAtom);
        var hashSet = new HashSet<string>();
        foreach (var mc in context.trackers.viveTrackers)
        {
            if (!mc.SyncMotionControl()) continue;
            autoSetup.AttachToClosestNode(mc);
            if (!hashSet.Add(mc.mappedControllerName))
            {
                lastError = $"The same controller was bound more than once: {mc.mappedControllerName}";
                context.diagnostics.Log(lastError);
                return false;
            }
        }
        context.Refresh();
        context.diagnostics.TakeSnapshot($"{nameof(RecordViveTrackersStep)}.{nameof(Apply)}.After");

        return true;
    }

    public override void Leave()
    {
        base.Leave();

        context.embody.activeJSON.val = false;
        /*
        context.worldScale.enabledJSON.val = false;
        _navigationRigSnapshot?.Restore();
        */
    }
}
