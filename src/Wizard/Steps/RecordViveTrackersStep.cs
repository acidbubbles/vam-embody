public class RecordViveTrackersStep : WizardStepBase, IWizardStep
{
    public string helpText => @"
Your Vive trackers will be mapped to the closest control. Their offset and rotation will be recorded.

<b>Align</b> to the pose as best as you can

Press <b>Next</b> to record, skip to leave vive trackers off.

Note that you'll be able to adjust your trackers later in the <i>Configure Trackers</i> screen.
".TrimStart();

    public RecordViveTrackersStep(EmbodyContext context)
        : base(context)
    {

    }

    public bool Apply()
    {
        context.diagnostics.TakeSnapshot($"{nameof(RecordViveTrackersStep)}.{nameof(Apply)}.Before");
        var autoSetup = new TrackerAutoSetup(context);
        lastError = autoSetup.AlignAllNow();
        context.diagnostics.TakeSnapshot($"{nameof(RecordViveTrackersStep)}.{nameof(Apply)}.After");

        return true;
    }
}
