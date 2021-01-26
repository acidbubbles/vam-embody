public class RecordPlayerHeightStep : IWizardStep
{
    public string helpText => "We will record your height.\n\nStand straight, and press next when ready.";

    private readonly IWorldScaleModule _worldScale;

    public RecordPlayerHeightStep(IWorldScaleModule worldScale)
    {
        _worldScale = worldScale;
    }

    public void Run()
    {
        _worldScale.playerHeightJSON.val = GetPlayerHeight();
        _worldScale.worldScaleMethodJSON.val = WorldScaleModule.PlayerHeightMethod;
    }

    public static float GetPlayerHeight()
    {
        return SuperController.singleton.heightAdjustTransform.InverseTransformPoint(SuperController.singleton.centerCameraTarget.transform.position).y;
    }
}
