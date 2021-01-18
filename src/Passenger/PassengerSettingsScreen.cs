using System.Linq;

public class PassengerSettingsScreen : ScreenBase, IScreen
{
    private readonly IPassengerModule _passenger;
    public const string ScreenName = PassengerModule.Label;

    public PassengerSettingsScreen(EmbodyContext context, IPassengerModule passenger)
        : base(context)
    {
        _passenger = passenger;
    }

    public void Show()
    {
        if (ShowNotSelected(_passenger.selectedJSON.val)) return;

        if (context.containingAtom.type == "Person")
        {
            CreateToggle(_passenger.lookAtJSON).label = "Look at eye target";
            CreateButton("Select eye target").button.onClick.AddListener(() =>
            {
                var eyeTarget = context.containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "eyeTargetControl");
                if (eyeTarget != null) SuperController.singleton.SelectController(eyeTarget);
            });

        }

        CreateSlider(_passenger.lookAtWeightJSON).label = "Look at weight";
        CreateToggle(_passenger.positionLockJSON).label = "Control camera position";
        CreateToggle(_passenger.rotationLockJSON).label = "Control camera rotation";
        CreateToggle(_passenger.rotationLockNoRollJSON).label = "Prevent camera roll";
        CreateToggle(_passenger.allowPersonHeadRotationJSON).label = "Camera controls rotation";

        CreateSlider(_passenger.rotationSmoothingJSON, true);
        CreateSlider(_passenger.rotationOffsetXjson, true);
        CreateSlider(_passenger.rotationOffsetYjson, true);
        CreateSlider(_passenger.rotationOffsetZjson, true);
        CreateSlider(_passenger.positionSmoothingJSON, true);
        CreateSlider(_passenger.positionOffsetXjson, true).valueFormat = "F4";
        CreateSlider(_passenger.positionOffsetYjson, true).valueFormat = "F4";
        CreateSlider(_passenger.positionOffsetZjson, true).valueFormat = "F4";
    }
}
