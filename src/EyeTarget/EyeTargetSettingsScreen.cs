public class EyeTargetSettingsScreen : ScreenBase, IScreen
{
    private readonly IEyeTarget _eyeTarget;
    public const string ScreenName = "Eye Target";

    public EyeTargetSettingsScreen(MVRScript plugin, IEyeTarget eyeTarget)
        : base(plugin)
    {
        _eyeTarget = eyeTarget;
    }

    public void Show()
    {
        CreateToggle(_eyeTarget.enabledJSON, true);
        // TODO: Enable or disable
    }
}
