using System.Linq;
using UnityEngine;

public class MeasureAnchorWidthStep : WizardStepBase, IWizardStep
{
    public string helpText => $"Put your hands on your real {_anchor.label} like the model is doing right now, and press Next when ready.\n\nTry to match as closely as you can the hands position of the model, but on your own body.";

    private readonly ControllerAnchorPoint _anchor;
    private readonly float _handRotate;
    private readonly FreeControllerV3 _leftHandControl;
    private readonly FreeControllerV3 _rightHandControl;
    private readonly MotionControllerWithCustomPossessPoint _leftHandMotion;
    private readonly MotionControllerWithCustomPossessPoint _rightHandMotion;
    private FreeControllerV3Snapshot _leftHandSnapshot;
    private FreeControllerV3Snapshot _rightHandSnapshot;
    private Vector3 _leftComparePointPosition;
    private Vector3 _rightComparePointPosition;

    public MeasureAnchorWidthStep(EmbodyContext context, ControllerAnchorPoint anchor, float handRotate)
        : base(context)
    {
        _anchor = anchor;
        _handRotate = handRotate;
        _leftHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "lHandControl");
        _rightHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "rHandControl");
        _leftHandMotion = context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand);
        _rightHandMotion = context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand);
    }

    public override void Enter()
    {
        _leftHandSnapshot = FreeControllerV3Snapshot.Snap(_leftHandControl);
        _rightHandSnapshot = FreeControllerV3Snapshot.Snap(_rightHandControl);
        _leftHandControl.RBHoldPositionSpring = 10000;
        _leftHandControl.RBHoldRotationSpring = 300;
        _rightHandControl.RBHoldPositionSpring = 10000;
        _rightHandControl.RBHoldRotationSpring = 300;
    }

    public override void Update()
    {
        {
            _leftHandControl.control.rotation = _anchor.bone.rotation;
            _leftHandControl.control.Rotate(_leftHandMotion.rotateControllerBase, Space.Self);
            _leftComparePointPosition = _anchor.GetInGameWorldPosition() - (_anchor.bone.transform.right * (_anchor.inGameSize.x / 2f + TrackersConstants.handsDistance / 2f));
            _leftHandControl.control.position = _leftComparePointPosition;
            _leftHandControl.control.Translate(Quaternion.Inverse(Quaternion.Euler(_leftHandMotion.rotateControllerBase)) * _leftHandMotion.offsetControllerBase, Space.Self);
            _leftHandControl.control.RotateAround(_leftComparePointPosition, _leftHandControl.control.up, _handRotate);
        }

        {
            _rightHandControl.control.rotation = _anchor.bone.rotation;
            _rightHandControl.control.Rotate(_rightHandMotion.rotateControllerBase, Space.Self);
            _rightComparePointPosition = _anchor.GetInGameWorldPosition() + (_anchor.bone.transform.right * (_anchor.inGameSize.x / 2f + TrackersConstants.handsDistance / 2f));
            _rightHandControl.control.position = _rightComparePointPosition;
            _rightHandControl.control.Translate(Quaternion.Inverse(Quaternion.Euler(_rightHandMotion.rotateControllerBase)) * _rightHandMotion.offsetControllerBase, Space.Self);
            _rightHandControl.control.RotateAround(_rightComparePointPosition, _rightHandControl.control.up, -_handRotate);
        }
    }

    public void Apply()
    {
        var inverseRotation = Quaternion.Inverse(_anchor.bone.rotation);
        var compareCenter = (_leftComparePointPosition + _rightComparePointPosition) / 2f;
        var handsCenter = (context.leftHand.position + context.rightHand.position) / 2f;
        var realLifeOffset = inverseRotation * (handsCenter - compareCenter);
        realLifeOffset.x = 0;
        _anchor.realLifeOffset = realLifeOffset;

        var realLifeWidth = Mathf.Abs((inverseRotation * context.rightHand.position).x - (inverseRotation * context.leftHand.position).x);
        _anchor.realLifeSize = _anchor.inGameSize / _anchor.inGameSize.x * realLifeWidth;
    }

    public override void Leave()
    {
        _leftHandSnapshot.Restore(true);
        _rightHandSnapshot.Restore(true);
    }
}
