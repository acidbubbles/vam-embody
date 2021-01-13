public class TrackersSettingsScreen : ScreenBase, IScreen
{
    private readonly ITrackersModule _trackers;
    public const string ScreenName = TrackersModule.Label;

    public TrackersSettingsScreen(EmbodyContext context, ITrackersModule trackers)
        : base(context)
    {
        _trackers = trackers;
    }

    public void Show()
    {
        if (ShowNotSelected(_trackers.selectedJSON.val)) return;

        CreateText(new JSONStorableString("", "Binds VR trackers (such as the headset or controllers) to an atom's controllers."), true);

        CreateToggle(_trackers.restorePoseAfterPossessJSON, true).label = "Restore pose after possession";
        // TODO: Bind controllers to a specific tracker, hands, head and vive tracker 1..8
    }
}
