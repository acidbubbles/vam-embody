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
        CreateText(new JSONStorableString("", "Moves the eye target so you will be looking back when looking at mirrors, and automatically looks at other objects in front of the head."), true);

        CreateSpacer().height = 40f;

        CreateToggle(_eyeTarget.trackMirrorsJSON).label = "Look At Mirrors";

        CreateSpacer().height = 40f;

        CreateSlider(_eyeTarget.frustrumJSON).label = "Objects Field Of View Angle Filter";

        CreateToggle(_eyeTarget.trackWindowCameraJSON).label = "Look At Window Camera";
        CreateToggle(_eyeTarget.trackPersonsJSON).label = "Look At Other Persons";
        CreateToggle(_eyeTarget.trackSelfHandsJSON).label = "Look At Self (Hands)";
        CreateToggle(_eyeTarget.trackSelfGenitalsJSON).label = "Look At Self (Genitals)";
        CreateToggle(_eyeTarget.trackObjectsJSON).label = "Look At Objects (Controls)";

        CreateSlider(_eyeTarget.gazeMinDurationJSON, true).label = "Gaze Min. Duration (s)";
        CreateSlider(_eyeTarget.gazeMaxDurationJSON, true).label = "Gaze Max. Duration (s)";

        CreateSpacer(true).height = 40f;

        CreateSlider(_eyeTarget.shakeMinDurationJSON, true).label = "Shake Min. Duration (s)";
        CreateSlider(_eyeTarget.shakeMaxDurationJSON, true).label = "Shake Max. Duration (s)";
        CreateSlider(_eyeTarget.shakeRangeJSON, true).label = "Shake Range";
    }
}
