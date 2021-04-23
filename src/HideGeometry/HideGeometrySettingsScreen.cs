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
        CreateText(new JSONStorableString("", "Hides the face and hair from the VR camera when possessing a model, so it's still visible in mirrors.\n\nHair and clothing visibility is determined based on their tags and label."), true);

        CreateToggle(_hideGeometry.hideFaceJSON, true).label = "Hide face (skin, eyes, eyelashes)";
        CreateToggle(_hideGeometry.hideHairJSON, true).label = "Hide hair";
        CreateToggle(_hideGeometry.hideClothingJSON, true).label = "Hide clothing (improved eyes, glasses)";
    }
}
