public class EyeTargetSettingsScreen : ScreenBase, IScreen
{
    private readonly IEyeTargetModule _eyeTarget;
    public const string ScreenName = EyeTargetModule.Label;

    public EyeTargetSettingsScreen(MVRScript plugin, IEyeTargetModule eyeTarget)
        : base(plugin)
    {
        _eyeTarget = eyeTarget;
    }

    public void Show()
    {
        if (ShowNotSelected(_eyeTarget.selectedJSON.val)) return;

        CreateText(new JSONStorableString("", "Moves the eye target so you will be looking back when looking at mirrors."), true);
    }
}
