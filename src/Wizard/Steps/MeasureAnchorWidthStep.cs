using System.Linq;
using UnityEngine;

public class MeasureAnchorWidthStep : WizardStepBase, IWizardStep
{
    public string helpText => $"Possession activated. Now put your hands on your real {_anchor.label}, and Press next when ready.";

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
            _leftHandControl.control.eulerAngles = _anchor.bone.rotation.eulerAngles + _leftHandMotion.baseOffsetRotation;
            _leftComparePointPosition = _anchor.GetInGameWorldPosition() - (_anchor.bone.transform.right * (_anchor.inGameSize.x / 2f + TrackersConstants.handsDistance / 2f));
            _leftHandControl.control.position = _leftComparePointPosition + Quaternion.Inverse(_leftHandControl.control.rotation) * Quaternion.Euler(_leftHandMotion.baseOffsetRotation) * _leftHandMotion.baseOffset;
            _leftHandControl.control.RotateAround(_leftComparePointPosition, _leftHandControl.control.up, _handRotate);
        }

        {
             _rightHandControl.control.eulerAngles = _anchor.bone.rotation.eulerAngles + _rightHandMotion.baseOffsetRotation;
            _rightComparePointPosition = _anchor.GetInGameWorldPosition() + (_anchor.bone.transform.right * (_anchor.inGameSize.x / 2f + TrackersConstants.handsDistance / 2f));
            _rightHandControl.control.position = _rightComparePointPosition + Quaternion.Inverse(_rightHandControl.control.rotation) * Quaternion.Euler(_rightHandMotion.baseOffsetRotation) * _rightHandMotion.baseOffset;
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
