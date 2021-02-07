public class DeactivateStep : WizardStepBase, IWizardStep
{
    public string helpText => "Great! We'll now stop possession. Press next when ready.";

    public DeactivateStep(EmbodyContext context)
        : base(context)
    {
    }

    public void Apply()
    {
        context.trackers.previewTrackerOffsetJSON.val = false;
        context.embody.activeJSON.val = false;
    }
}
