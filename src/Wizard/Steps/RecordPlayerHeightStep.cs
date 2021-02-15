public class RecordPlayerHeightStep : WizardStepBase, IWizardStep
{
    public string helpText => "We will now record your height. This will improve automatic world scale, making your body height feel right.\n\nStand straight, and press Next when ready.";

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
