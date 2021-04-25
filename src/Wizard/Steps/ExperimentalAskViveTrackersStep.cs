using System;
using System.Collections.Generic;

public class ExperimentalAskViveTrackersStep : WizardStepBase, IWizardStep
{
    private readonly List<IWizardStep> _steps;

    public string helpText => @"<b>Vive trackers</b> were detected.

Possession will now be activated only for your head and hands.

Press <b>Next</b> to continue.

Skip this if you want to setup Vive trackers manually.".TrimStart();

    public ExperimentalAskViveTrackersStep(EmbodyContext context, List<IWizardStep> steps)
        : base(context)
    {
        _steps = steps;
    }

    public bool Apply()
    {
        foreach (var mc in context.trackers.viveTrackers)
        {
            mc.ResetToDefault();
        }

        foreach (var mc in context.trackers.headAndHands)
            mc.enabled = true;

        context.trackers.previewTrackerOffsetJSON.val = true;

        var idx = _steps.IndexOf(this);
        if (idx == -1) throw new InvalidOperationException($"{nameof(ExperimentalAskViveTrackersStep)} was not found in the steps list");

        _steps.Insert(++idx, new ExperimentalRecordViveTrackersFeetStep(context));
        _steps.Insert(++idx, new ExperimentalRecordViveTrackersStep(context));
        _steps.Insert(++idx, new ExperimentalFinishViveSetupStep(context));

        return true;
    }
}
