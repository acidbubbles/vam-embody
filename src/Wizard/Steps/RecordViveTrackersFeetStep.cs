using System.Linq;

public class RecordViveTrackersFeetStep : WizardStepBase, IWizardStep
{
    public string helpText => "Align your feet to match the model feet's position and angle as closely as possible, and select Next.";

    public RecordViveTrackersFeetStep(EmbodyContext context)
        : base(context)
    {

    }

    public void Apply()
    {
        var autoSetup = new TrackerAutoSetup(context.containingAtom);
        var feet = context.containingAtom.freeControllers.Where(fc => fc.name.EndsWith("FootControl")).ToList();
        foreach (var mc in context.trackers.viveTrackers)
        {
            if (!mc.SyncMotionControl()) continue;
            autoSetup.AttachToClosestNode(mc, feet);
        }
        context.Refresh();
    }

    public override void Update()
    {
        // TODO: Enable and disable the preview
        // TODO: Draw lines connecting trackers with their closest target
    }
}
