using UnityEngine;

public class ActivateHeadAndHandsStep : WizardStepBase, IWizardStep
{
    public string helpText => "We will now start possession of head and hands, and unmap all vive trackers. Press next when ready.";

    private readonly EmbodySelectionSnapshot _snapshot;

    public ActivateHeadAndHandsStep(EmbodyContext context, EmbodySelectionSnapshot snapshot)
        : base(context)
    {
        _snapshot = snapshot;
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

        context.worldScale.selectedJSON.val = _snapshot.worldScale;
        context.hideGeometry.selectedJSON.val = true;
        context.trackers.selectedJSON.val = true;
        context.eyeTarget.selectedJSON.val = _snapshot.eyeTarget;

        context.embody.activeJSON.val = true;
    }
}
