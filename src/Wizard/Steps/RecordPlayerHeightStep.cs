using System.Linq;

public class RecordPlayerHeightStep : WizardStepBase, IWizardStep
{
    public string helpText => context.trackers.selectedJSON.val && context.trackers.viveTrackers.Any(mc => mc.enabled && mc.SyncMotionControl())
        ? "We will now <b>measure your height</b>.\n\nPlease <b>place one Vive tracker on the ground</b>, <b>stand straight</b>, and press Next when ready."
        : "We will now <b>measure your height</b>.\n\nThis will improve automatic <b>world scale</b>, making your body height feel right.\n\nStand straight, and press Next when ready.";

    public RecordPlayerHeightStep(EmbodyContext context)
        : base(context)
    {
    }

    public void Apply()
    {
        var viveTrackers = context.trackers.selectedJSON.val ? context.trackers.viveTrackers.Where(t => t.enabled && t.SyncMotionControl()).ToList() : null;
        if (viveTrackers == null || viveTrackers.Count == 0)
            context.worldScale.playerHeightJSON.val = GetPlayerHeight();
        else
            context.worldScale.playerHeightJSON.val = SuperController.singleton.centerCameraTarget.transform.position.y - viveTrackers.Min(vt => vt.currentMotionControl.position.y);
        context.worldScale.worldScaleMethodJSON.val = WorldScaleModule.PlayerHeightMethod;
    }

    public static float GetPlayerHeight()
    {
        return SuperController.singleton.heightAdjustTransform.InverseTransformPoint(SuperController.singleton.centerCameraTarget.transform.position).y;
    }
}
