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
        CreateToggle(_passenger.exitOnMenuOpen).label = "Exit On Menu Open";
        if (context.containingAtom.type == "Person")
        {
            CreateToggle(_passenger.lookAtJSON).label = "Look At Eye Target";
            CreateButton("Select Eye Target").button.onClick.AddListener(() =>
            {
                var eyeTarget = context.containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "eyeTargetControl");
                if (eyeTarget != null) SuperController.singleton.SelectController(eyeTarget);
            });

        }

        if (context.containingAtom.type == "Person")
            CreateSlider(_passenger.lookAtWeightJSON).label = "Look At Weight";
        CreateToggle(_passenger.positionLockJSON).label = "Control Camera Position";
        CreateToggle(_passenger.rotationLockJSON).label = "Control Camera Rotation";
        CreateToggle(_passenger.rotationLockNoRollJSON).label = "Prevent Camera Roll";
        CreateToggle(_passenger.allowPersonHeadRotationJSON).label = "Camera Controls Rotation";
        CreateSlider(_passenger.eyesToHeadDistanceOffsetJSON).label = "Head-eyes Distance Offset";

        CreateSlider(_passenger.rotationSmoothingJSON, true);
        CreateSlider(new JSONStorableFloat(
            "Rotation X",
            0f,
            (float val) => _passenger.rotationOffset = new Vector3(val, _passenger.rotationOffset.y, _passenger.rotationOffset.z),
            -180f,
            180f
        ) { valNoCallback = _passenger.rotationOffset.x }, true);
        CreateSlider(new JSONStorableFloat(
            "Rotation Y",
            0f,
            (float val) => _passenger.rotationOffset = new Vector3(_passenger.rotationOffset.x, val, _passenger.rotationOffset.z),
            -180f,
            180f
        ) { valNoCallback = _passenger.rotationOffset.y }, true);
        CreateSlider(new JSONStorableFloat(
            "Rotation Z",
            0f,
            (float val) => _passenger.rotationOffset = new Vector3(_passenger.rotationOffset.x, _passenger.rotationOffset.y, val),
            -180f,
            180f
        ) { valNoCallback = _passenger.rotationOffset.z }, true);
        CreateSlider(_passenger.positionSmoothingJSON, true);
        CreateSlider(new JSONStorableFloat(
            "Position X",
            0f,
            (float val) => _passenger.positionOffset = new Vector3(val, _passenger.positionOffset.y, _passenger.positionOffset.z),
            -2f,
            2f,
            false
        ) { valNoCallback = _passenger.positionOffset.x }, true).valueFormat = "F4";
        CreateSlider(new JSONStorableFloat(
            "Position Y",
            0f,
            (float val) => _passenger.positionOffset = new Vector3(_passenger.positionOffset.x, val, _passenger.positionOffset.z),
            -2f,
            2f,
            false
        ) { valNoCallback = _passenger.positionOffset.y }, true).valueFormat = "F4";
        CreateSlider(new JSONStorableFloat(
            "Position Z",
            0f,
            (float val) => _passenger.positionOffset = new Vector3(_passenger.positionOffset.x, _passenger.positionOffset.y, val),
            -2f,
            2f,
            false
        ) { valNoCallback = _passenger.positionOffset.z }, true).valueFormat = "F4";
    }
}
