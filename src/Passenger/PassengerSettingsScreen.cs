using System.Linq;
using UnityEngine;

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
        CreateSlider(new JSONStorableFloat(
            "Rotation X",
            0f,
            (float val) => _passenger.rotationOffset = new Vector3(val, _passenger.rotationOffset.y, _passenger.rotationOffset.z),
            -180f,
            180f
        ), true);
        CreateSlider(new JSONStorableFloat(
            "Rotation Y",
            0f,
            (float val) => _passenger.rotationOffset = new Vector3(_passenger.rotationOffset.x, val, _passenger.rotationOffset.z),
            -180f,
            180f
        ), true);
        CreateSlider(new JSONStorableFloat(
            "Rotation Z",
            0f,
            (float val) => _passenger.rotationOffset = new Vector3(_passenger.rotationOffset.x, _passenger.rotationOffset.y, val),
            -180f,
            180f
        ), true);
        CreateSlider(_passenger.positionSmoothingJSON, true);
        CreateSlider(new JSONStorableFloat(
            "Position X",
            0f,
            (float val) => _passenger.positionOffset = new Vector3(val, _passenger.positionOffset.y, _passenger.positionOffset.z),
            -2f,
            2f,
            false
        ), true).valueFormat = "F4";
        CreateSlider(new JSONStorableFloat(
            "Position Y",
            0f,
            (float val) => _passenger.positionOffset = new Vector3(_passenger.positionOffset.x, val, _passenger.positionOffset.z),
            -2f,
            2f,
            false
        ), true).valueFormat = "F4";
        CreateSlider(new JSONStorableFloat(
            "Position Z",
            0f,
            (float val) => _passenger.positionOffset = new Vector3(_passenger.positionOffset.x, _passenger.positionOffset.y, val),
            -2f,
            2f,
            false
        ), true).valueFormat = "F4";
    }
}
