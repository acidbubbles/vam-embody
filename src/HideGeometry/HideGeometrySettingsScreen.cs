public class HideGeometrySettingsScreen : ScreenBase, IScreen
{
    private readonly IHideGeometryModule _hideGeometry;
    public const string ScreenName = HideGeometryModule.Label;

    public HideGeometrySettingsScreen(EmbodyContext context, IHideGeometryModule hideGeometry)
        : base(context)
    {
        _hideGeometry = hideGeometry;
    }

    public void Show()
    {
        if (ShowNotSelected(_hideGeometry.selectedJSON.val)) return;

        CreateText(new JSONStorableString("", "Hides the face and hair when possessing a model, except in mirrors."), true);

        CreateToggle(_hideGeometry.hideFaceJSON, true);
        CreateToggle(_hideGeometry.hideHairJSON, true);
    }
}
