public class RecordViveTrackersStep : WizardStepBase, IWizardStep
{
    public string helpText => "Take the same pose as the person you want to possess, and press Next";

    public RecordViveTrackersStep(EmbodyContext context)
        : base(context)
    {

    }

    public void Apply()
    {
        var autoSetup = new TrackerAutoSetup(context.containingAtom);
        foreach (var mc in context.trackers.viveTrackers)
        {
            if (!mc.SyncMotionControl()) continue;
            autoSetup.AttachToClosestNode(mc);
        }
    }

    public override void Update()
    {
        // TODO: Enable and disable the preview
        // TODO: Draw lines connecting trackers with their closest target
    }
}
