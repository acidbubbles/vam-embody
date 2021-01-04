using System.Linq;

public class PassengerSettingsScreen : ScreenBase, IScreen
{
    private readonly IPassenger _passenger;
    public const string ScreenName = "Passenger";

    public PassengerSettingsScreen(MVRScript plugin, IPassenger passenger)
        : base(plugin)
    {
        _passenger = passenger;
    }

    public void Show()
    {
        CreateFilterablePopup(_passenger.linkJSON).popupPanelHeight = 600f;
        if (plugin.containingAtom.type == "Person")
        {
            CreateToggle(_passenger.lookAtJSON).label = "Look at eye target";
            CreateButton("Select eye target").button.onClick.AddListener(() =>
            {
                var eyeTarget = plugin.containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "eyeTargetControl");
                if (eyeTarget != null) SuperController.singleton.SelectController(eyeTarget);
            });

        }

        CreateSlider(_passenger.lookAtWeightJSON).label = "Look at weight";
        CreateToggle(_passenger.positionLockJSON).label = "Control camera position";
        CreateToggle(_passenger.rotationLockJSON).label = "Control camera rotation";
        CreateToggle(_passenger.rotationLockNoRollJSON).label = "Prevent camera roll";

        var followerPopup = CreateFilterablePopup(_passenger.possessRotationLink);
        followerPopup.popupPanelHeight = 600f;
        // TODO: Re-enable
        // followerPopup.popup.onOpenPopupHandlers += () =>
        // {
        //     possessRotationLink.choices = getPossessRotationChoices();
        // };

        CreateSlider(_passenger.rotationSmoothingJSON);
        CreateSlider(_passenger.rotationOffsetXjson);
        CreateSlider(_passenger.rotationOffsetYjson);
        CreateSlider(_passenger.rotationOffsetZjson);
        CreateSlider(_passenger.positionSmoothingJSON);
        CreateSlider(_passenger.positionOffsetXjson).valueFormat = "F4";
        CreateSlider(_passenger.positionOffsetYjson).valueFormat = "F4";
        CreateSlider(_passenger.positionOffsetZjson).valueFormat = "F4";
        CreateSlider(_passenger.eyesToHeadDistanceJSON);
    }
}
