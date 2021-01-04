public class OffsetCameraSettingsScreen : ScreenBase, IScreen
{
    private readonly IOffsetCamera _offsetCamera;
    public const string ScreenName = "Offset Camera";

    public OffsetCameraSettingsScreen(MVRScript plugin, IOffsetCamera offsetCamera)
        : base(plugin)
    {
        _offsetCamera = offsetCamera;
    }

    public void Show()
    {
        CreateSlider(_offsetCamera.cameraDepthJSON, false).label = "Depth adjust";
        CreateSlider(_offsetCamera.cameraHeightJSON, false).label = "Height adjust";
        CreateSlider(_offsetCamera.cameraPitchJSON, false).label = "Pitch adjust";
        CreateSlider(_offsetCamera.clipDistanceJSON, false).label = "Clip distance";
    }
}
