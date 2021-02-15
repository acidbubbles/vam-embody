using System;
using System.Collections.Generic;
using UnityEngine;

public class AskViveTrackersStep : WizardStepBase, IWizardStep
{
    private readonly List<IWizardStep> _steps;
    private readonly int _useViveTrackers;
    public string helpText => "Vive trackers were detected. If you want to configure them so they are automatically used during possession, press Next. You can also Skip this if you don't want Embody to configure your Vive trackers.";

    public AskViveTrackersStep(EmbodyContext context, List<IWizardStep> steps, int useViveTrackers)
        : base(context)
    {
        _steps = steps;
        _useViveTrackers = useViveTrackers;
    }

    public void Apply()
    {
        foreach (var mc in context.trackers.viveTrackers)
        {
            mc.mappedControllerName = null;
            mc.controlRotation = true;
            mc.customOffset = Vector3.zero;
            mc.customOffsetRotation = Vector3.zero;
            mc.possessPointRotation = Vector3.zero;
            mc.enabled = true;
        }

        foreach (var mc in context.trackers.headAndHands)
            mc.enabled = true;

        context.embody.activeJSON.val = true;
        context.trackers.previewTrackerOffsetJSON.val = true;

        var idx = _steps.IndexOf(this);
        if (idx == -1) throw new InvalidOperationException($"{nameof(AskViveTrackersStep)} was not found in the steps list");

        if (_useViveTrackers > 1)
            _steps.Insert(++idx, new RecordViveTrackersFeetStep(context));
        if (_useViveTrackers == 1 || _useViveTrackers > 2)
            _steps.Insert(++idx, new RecordViveTrackersStep(context));
        _steps.Insert(++idx, new DeactivateStep(context));
    }
}
