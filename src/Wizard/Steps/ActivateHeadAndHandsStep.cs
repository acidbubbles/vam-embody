using UnityEngine;

public class ActivateHeadAndHandsStep : WizardStepBase, IWizardStep
{
    public string helpText => "We will now start possession of head and hands, and unmap all vive trackers. Press next when ready.";

    public ActivateHeadAndHandsStep(EmbodyContext context)
        : base(context)
    {
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
    }
}
