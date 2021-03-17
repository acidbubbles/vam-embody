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
        CreateText(new JSONStorableString("", "Moves the eye target so you will be looking back when looking at mirrors and window camera.\n\nFor advanced features, check out the Glance plugin by AcidBubbles."), true);

        CreateSpacer().height = 40f;

        CreateToggle(_eyeTarget.trackMirrorsJSON).label = "Look At Mirrors";

        CreateSpacer().height = 40f;

        CreateSlider(_eyeTarget.frustrumJSON).label = "Field Of View";
        CreateToggle(_eyeTarget.trackWindowCameraJSON).label = "Look At Window Camera";

        CreateSpacer(true).height = 40f;

        CreateSlider(_eyeTarget.shakeMinDurationJSON, true).label = "Shake Min. Duration (s)";
        CreateSlider(_eyeTarget.shakeMaxDurationJSON, true).label = "Shake Max. Duration (s)";
        CreateSlider(_eyeTarget.shakeRangeJSON, true).label = "Shake Range";
    }
}
