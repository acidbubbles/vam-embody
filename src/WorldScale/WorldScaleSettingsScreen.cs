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
        CreateText(new JSONStorableString("", "Changes the world scale based on your real height and the in-game model height.\n\nUse the Record Player Height button while standing straight to get accurate world scale.\n\nOptionally put a controller down before to get a more precise reading."), true);

        CreateScrollablePopup(_worldScale.worldScaleMethodJSON).label = "Method*";
        CreateSlider(_worldScale.playerHeightJSON).label = "Player Height*";
        CreateButton("Record Player Height (stand straight)").button.onClick.AddListener(RecordPlayerHeight);

        CreateToggle(_worldScale.useProfileJSON, true, "*Use Profile <i>(Not Saved In Scene)</i>", "*Use Profile <i>(Saved In Scene)</i>");

        CreateSpacer(true).height = 20f;

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
        context.diagnostics.TakeSnapshot($"{nameof(WorldScaleSettingsScreen)}.{nameof(RecordPlayerHeight)}.Before");
        _worldScale.playerHeightJSON.val = new PlayerMeasurements(context).MeasureHeight();
        _worldScale.worldScaleMethodJSON.val = WorldScaleModule.PlayerHeightMethod;
        context.diagnostics.TakeSnapshot($"{nameof(WorldScaleSettingsScreen)}.{nameof(RecordPlayerHeight)}.Before");
    }
}
