using System.Linq;

public class RecordPlayerHeightStep : WizardStepBase, IWizardStep
{
    private readonly MotionControllerWithCustomPossessPoint _headMotionControl;

    public string helpText => context.trackers.selectedJSON.val && context.trackers.viveTrackers.Any(mc => mc.enabled && mc.SyncMotionControl())
        ? "We will now <b>measure your height</b>.\n\nPlease <b>place one Vive tracker on the ground</b>, <b>stand straight</b>, and press Next when ready."
        : "We will now <b>measure your height</b>.\n\nThis will improve automatic <b>world scale</b>, making your body height feel right.\n\nStand straight, and press Next when ready.";

    public RecordPlayerHeightStep(EmbodyContext context)
        : base(context)
    {
        _headMotionControl = context.trackers.motionControls.First(mc => mc.name == MotionControlNames.Head);
    }

    public void Apply()
    {
        var viveTrackers = context.trackers.selectedJSON.val ? context.trackers.viveTrackers.Where(t => t.enabled && t.SyncMotionControl()).ToList() : null;
        if (viveTrackers == null || viveTrackers.Count == 0)
            context.worldScale.playerHeightJSON.val = SuperController.singleton.heightAdjustTransform.InverseTransformPoint(_headMotionControl.currentMotionControl.position).y;
        else
            context.worldScale.playerHeightJSON.val = _headMotionControl.currentMotionControl.position.y - viveTrackers.Min(vt => vt.currentMotionControl.position.y);
        context.worldScale.worldScaleMethodJSON.val = WorldScaleModule.PlayerHeightMethod;
    }
}
