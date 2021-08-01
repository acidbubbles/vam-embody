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

        CreateSpacer().height = 20f;
        CreateTitle("Look Targets");

        CreateToggle(_eyeTarget.trackMirrorsJSON).label = "Look At Mirrors";
        CreateToggle(_eyeTarget.trackWindowCameraJSON).label = "Look At Window Camera";

        CreateSpacer().height = 20f;
        CreateTitle("Field Of View");

        CreateSlider(_eyeTarget.frustumJSON).label = "Field Of View Angle";

        CreateTitle("Eye Saccade", true);

        CreateSlider(_eyeTarget.saccadeMinDurationJSON, true).label = "Saccade Min. Duration";
        CreateSlider(_eyeTarget.saccadeMaxDurationJSON, true).label = "Saccade Max. Duration";
        CreateSlider(_eyeTarget.saccadeRangeJSON, true).label = "Saccade Range";

        CreateTitle("MacGruber PostMagic", true);

        CreateToggle(_eyeTarget.controlAutoFocusPoint, true).label = "Control AutoFocusPoint";
    }
}
