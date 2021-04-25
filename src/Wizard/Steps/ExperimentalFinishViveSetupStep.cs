public class ExperimentalFinishViveSetupStep : WizardStepBase, IWizardStep
{
    public string helpText => @"
<b>Vive trackers setup is complete</b>.

Try them out! You can fine tune them in the <i>Trackers Settings</i> menu later, or restart the wizard and try again.

If you exit the wizard to make adjustments, you can return and skip previous step.

Press <b>Next</b> when you are ready to continue.".TrimStart();

    public ExperimentalFinishViveSetupStep(EmbodyContext context)
        : base(context)
    {
    }

    public override void Enter()
    {
        base.Enter();

        context.embody.activeJSON.val = true;
    }

    public bool Apply()
    {
        context.diagnostics.TakeSnapshot($"{nameof(FinishSnugSetupStep)}.{nameof(Apply)}");
        return true;
    }

    public override void Leave(bool final)
    {
        base.Leave(final);

        context.trackers.previewTrackerOffsetJSON.val = false;
        context.embody.activeJSON.val = false;
    }
}
