public class EyeTargetSettingsScreen : ScreenBase, IScreen
{
    private readonly IEyeTargetModule _eyeTarget;
    public const string ScreenName = EyeTargetModule.Label;

    public EyeTargetSettingsScreen(EmbodyContext context, IEyeTargetModule eyeTarget)
        : base(context)
    {
        _eyeTarget = eyeTarget;
    }

    public void Show()
    {
        if (ShowNotSelected(_eyeTarget.selectedJSON.val)) return;

        CreateText(new JSONStorableString("", "Moves the eye target so you will be looking back when looking at mirrors."), true);
    }
}
