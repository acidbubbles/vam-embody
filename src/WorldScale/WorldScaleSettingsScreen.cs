using System.Linq;

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
        CreateText(new JSONStorableString("", "Changes the world scale based on your real height and the in-game model height.\n\nUse the Record Player Height button while standing straight to get accurate world scale."), true);

        CreateToggle(_worldScale.importDefaultsOnLoad, true).label = "Use Defaults On Load";
        CreateScrollablePopup(_worldScale.worldScaleMethodJSON, true);
        CreateSlider(_worldScale.playerHeightJSON, true).label = "Player Height";
        CreateButton("Record Player Height (stand straight)", true).button.onClick.AddListener(RecordPlayerHeight);

        CreateSpacer().height = 20f;

        var showPersonHeight = CreateButton("Show Person Height", true);
        showPersonHeight.button.onClick.AddListener(() =>
        {
            var height = new PersonMeasurements(context).MeasureHeight();
            showPersonHeight.label = $"Height: {height:0.00} (World Scale: {height / context.worldScale.playerHeightJSON.val:0.00})";
        });
        var applyWorldScale = CreateButton("Apply World Scale", true);
        applyWorldScale.button.onClick.AddListener(() =>
        {
            context.worldScale.ApplyWorldScale();
        });
    }

    private void RecordPlayerHeight()
    {
        _worldScale.playerHeightJSON.val = new PlayerMeasurements(context).MeasureHeight();
        _worldScale.worldScaleMethodJSON.val = WorldScaleModule.PlayerHeightMethod;
    }
}
