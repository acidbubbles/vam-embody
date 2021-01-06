public class WorldScaleSettingsScreen : ScreenBase, IScreen
{
    private readonly IWorldScale _worldScale;
    public const string ScreenName = "World Scale";

    public WorldScaleSettingsScreen(MVRScript plugin, IWorldScale worldScale)
        : base(plugin)
    {
        _worldScale = worldScale;
    }

    public void Show()
    {
        CreateToggle(_worldScale.enabledJSON, true);
    }
}
