public class FinishViveSetupStep : WizardStepBase, IWizardStep
{
    public string helpText => "Great! All Vive trackers should be set now. Try them out. You can fine tune them in the Trackers Settings menu later, or restart the wizard.\n\nPress Next when you are ready to continue.";

    public FinishViveSetupStep(EmbodyContext context)
        : base(context)
    {
    }

    public override void Enter()
    {
        base.Enter();

        context.embody.activeJSON.val = true;
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
