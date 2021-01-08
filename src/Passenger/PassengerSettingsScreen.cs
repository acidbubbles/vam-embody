using System.Linq;

public class PassengerSettingsScreen : ScreenBase, IScreen
{
    private readonly IPassengerModule _passenger;
    public const string ScreenName = PassengerModule.Label;

    public PassengerSettingsScreen(MVRScript plugin, IPassengerModule passenger)
        : base(plugin)
    {
        _passenger = passenger;
    }

    public void Show()
    {
        if (ShowNotSelected(_passenger.selectedJSON.val)) return;

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

        CreateSlider(_passenger.rotationSmoothingJSON, true);
        CreateSlider(_passenger.rotationOffsetXjson, true);
        CreateSlider(_passenger.rotationOffsetYjson, true);
        CreateSlider(_passenger.rotationOffsetZjson, true);
        CreateSlider(_passenger.positionSmoothingJSON, true);
        CreateSlider(_passenger.positionOffsetXjson, true).valueFormat = "F4";
        CreateSlider(_passenger.positionOffsetYjson, true).valueFormat = "F4";
        CreateSlider(_passenger.positionOffsetZjson, true).valueFormat = "F4";
        CreateSlider(_passenger.eyesToHeadDistanceJSON, true);
    }
}
