public class EyeTargetSettingsScreen : ScreenBase, IScreen
{
    private readonly IEyeTarget _eyeTarget;
    public const string ScreenName = "EyeTarget";

    public EyeTargetSettingsScreen(MVRScript plugin, IEyeTarget eyeTarget)
        : base(plugin)
    {
        _eyeTarget = eyeTarget;
    }

    public void Show()
    {
        // TODO: Enable or disable
    }
}
