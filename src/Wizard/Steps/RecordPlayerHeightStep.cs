public class RecordPlayerHeightStep : WizardStepBase, IWizardStep
{
    public string helpText => "We will now <b>measure your height</b>.\n\nThis will improve automatic <b>world scale</b>, making your body height feel right.\n\nStand straight, and press Next when ready.";

    private readonly IWorldScaleModule _worldScale;

    public RecordPlayerHeightStep(IWorldScaleModule worldScale)
    {
        _worldScale = worldScale;
    }

    public void Apply()
    {
        _worldScale.playerHeightJSON.val = GetPlayerHeight();
        _worldScale.worldScaleMethodJSON.val = WorldScaleModule.PlayerHeightMethod;
    }

    public static float GetPlayerHeight()
    {
        return SuperController.singleton.heightAdjustTransform.InverseTransformPoint(SuperController.singleton.centerCameraTarget.transform.position).y;
    }
}
