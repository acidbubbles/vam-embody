public class DeactivateStep : WizardStepBase, IWizardStep
{
    public string helpText => "Great! All Vive trackers should be set now. You can fine tune them in the Trackers Settings menu later. We'll now stop possession, press Next when ready.";

    public DeactivateStep(EmbodyContext context)
        : base(context)
    {
    }

    public void Apply()
    {
    }

    public override void Leave()
    {
        base.Leave();

        context.trackers.previewTrackerOffsetJSON.val = false;
        context.embody.activeJSON.val = false;
    }
}
