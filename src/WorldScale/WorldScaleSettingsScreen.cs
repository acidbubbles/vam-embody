public class WorldScaleSettingsScreen : ScreenBase, IScreen
{
    private readonly IWorldScaleModule _worldScale;
    public const string ScreenName = WorldScaleModule.Label;

    public WorldScaleSettingsScreen(EmbodyContext context, IWorldScaleModule worldScale)
        : base(context)
    {
        _worldScale = worldScale;
    }

    public void Show()
    {
        if (ShowNotSelected(_worldScale.selectedJSON.val)) return;

        CreateText(new JSONStorableString("", "Changes the world scale based on your measurements and the person's measurement."), true);
    }
}
