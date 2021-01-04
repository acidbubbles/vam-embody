public class HideGeometrySettingsScreen : ScreenBase, IScreen
{
    private readonly IHideGeometry _hideGeometry;
    public const string ScreenName = "Hide Geometry";

    public HideGeometrySettingsScreen(MVRScript plugin, IHideGeometry hideGeometry)
        : base(plugin)
    {
        _hideGeometry = hideGeometry;
    }

    public void Show()
    {
        CreateToggle(_hideGeometry.enabledJSON, true);
        CreateToggle(_hideGeometry.hideFaceJSON, true);
        CreateToggle(_hideGeometry.hideHairJSON, true);
    }
}
