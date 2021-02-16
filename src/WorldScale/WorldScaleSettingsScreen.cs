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
        if (ShowNotSelected(_worldScale.selectedJSON.val)) return;

        CreateText(new JSONStorableString("", "Changes the world scale based on your real height and the in-game model height.\n\nUse the Record Player Height button while standing straight to get accurate world scale."), true);

        CreateScrollablePopup(_worldScale.worldScaleMethodJSON, true);
        CreateSlider(_worldScale.playerHeightJSON, true).label = "Player Height";
        CreateButton("Record Player Height (stand straight)", true).button.onClick.AddListener(RecordPlayerHeight);
    }

    private void RecordPlayerHeight()
    {
        _worldScale.playerHeightJSON.val = SuperController.singleton.heightAdjustTransform.InverseTransformPoint(context.trackers.motionControls.First(mc => mc.name == MotionControlNames.Head).currentMotionControl.position).y;
        _worldScale.worldScaleMethodJSON.val = WorldScaleModule.PlayerHeightMethod;
    }
}
