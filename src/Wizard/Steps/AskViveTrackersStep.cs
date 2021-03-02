using System;
using System.Collections.Generic;
using UnityEngine;

public class AskViveTrackersStep : WizardStepBase, IWizardStep
{
    private readonly List<IWizardStep> _steps;
    private readonly int _useViveTrackers;
    public string helpText => @"<b>Vive trackers</b> were detected.

Possession will now be activated only for your head and hands.

Press <b>Next</b> to continue.

Skip this if you want to setup Vive trackers manually.".TrimStart();

    public AskViveTrackersStep(EmbodyContext context, List<IWizardStep> steps, int useViveTrackers)
        : base(context)
    {
        _steps = steps;
        _useViveTrackers = useViveTrackers;
    }

    public bool Apply()
    {
        foreach (var mc in context.trackers.viveTrackers)
        {
            mc.mappedControllerName = null;
            mc.controlRotation = true;
            mc.offsetControllerCustom = Vector3.zero;
            mc.rotateControllerCustom = Vector3.zero;
            mc.rotateAroundTracker = Vector3.zero;
            mc.enabled = true;
        }

        foreach (var mc in context.trackers.headAndHands)
            mc.enabled = true;

        context.trackers.previewTrackerOffsetJSON.val = true;

        var idx = _steps.IndexOf(this);
        if (idx == -1) throw new InvalidOperationException($"{nameof(AskViveTrackersStep)} was not found in the steps list");

        _steps.Insert(++idx, new RecordViveTrackersFeetStep(context));
        _steps.Insert(++idx, new RecordViveTrackersStep(context));
        _steps.Insert(++idx, new FinishViveSetupStep(context));

        return true;
    }
}
