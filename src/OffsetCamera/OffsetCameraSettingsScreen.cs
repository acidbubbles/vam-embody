public class OffsetCameraSettingsScreen : ScreenBase, IScreen
{
    private readonly IOffsetCameraModule _offsetCamera;
    public const string ScreenName = OffsetCameraModule.Label;

    public OffsetCameraSettingsScreen(MVRScript plugin, IOffsetCameraModule offsetCamera)
        : base(plugin)
    {
        _offsetCamera = offsetCamera;
    }

    public void Show()
    {
        if (ShowNotSelected(_offsetCamera.selectedJSON.val)) return;

        CreateText(new JSONStorableString("", "Adjusts the camera position, useful when using the built-in VaM possession to move the eyes closer to the model's eyes."), true);

        CreateSlider(_offsetCamera.cameraDepthJSON, true).label = "Depth adjust";
        CreateSlider(_offsetCamera.cameraHeightJSON, true).label = "Height adjust";
        CreateSlider(_offsetCamera.cameraPitchJSON, true).label = "Pitch adjust";
        CreateSlider(_offsetCamera.clipDistanceJSON, true).label = "Clip distance";
    }
}
