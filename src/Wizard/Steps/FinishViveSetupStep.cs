public class FinishViveSetupStep : WizardStepBase, IWizardStep
{
    public string helpText => @"
<b>Vive trackers setup is complete</b>. Try them out. You can fine tune them in the <i>Trackers Settings</i> menu later, or restart the wizard and try again.

Press Next when you are ready to continue.".TrimStart();

    public FinishViveSetupStep(EmbodyContext context)
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

    public override void Leave()
    {
        base.Leave();

        context.trackers.previewTrackerOffsetJSON.val = false;
        context.embody.activeJSON.val = false;
    }
}
