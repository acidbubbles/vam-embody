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

        CreateScrollablePopup(_worldScale.worldScaleMethodJSON, true);
        CreateSlider(_worldScale.playerHeightJSON, true);
        CreateButton("Record current player height (stand straight)", true).button.onClick.AddListener(RecordPlayerHeight);
    }

    private void RecordPlayerHeight()
    {
        _worldScale.playerHeightJSON.val = RecordPlayerHeightStep.GetPlayerHeight();
        _worldScale.worldScaleMethodJSON.val = WorldScaleModule.PlayerHeightMethod;
    }
}
