public class RecordViveTrackersStep : WizardStepBase, IWizardStep
{
    public string helpText => "Now <b>take the same pose</b> as the model, and <b>align all vive trackers</b> as closely as possible to your model's position.\n\nPress Next when you are ready.\n\nEach Vive tracker will be assigned a control on the model, and their relative position will be recorded.";

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
        context.Refresh();
    }

    public override void Update()
    {
        // TODO: Enable and disable the preview
        // TODO: Draw lines connecting trackers with their closest target
    }
}
