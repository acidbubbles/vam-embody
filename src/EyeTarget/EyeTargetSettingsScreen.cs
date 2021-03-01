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

        CreateToggle(_eyeTarget.trackMirrorsJSON, true).label = "Look At Mirrors";

        CreateSlider(_eyeTarget.frustrumJSON, true).label = "Objects Field Of View Angle Filter";

        CreateToggle(_eyeTarget.trackWindowCameraJSON, true).label = "Look At Window Camera";
        CreateToggle(_eyeTarget.trackPersonsJSON, true).label = "Look At Other Persons";
        CreateToggle(_eyeTarget.trackSelfHandsJSON, true).label = "Look At Self (Hands)";
        CreateToggle(_eyeTarget.trackSelfGenitalsJSON, true).label = "Look At Self (Genitals)";
        CreateToggle(_eyeTarget.trackObjectsJSON, true).label = "Look At Objects (Controls)";
    }
}
