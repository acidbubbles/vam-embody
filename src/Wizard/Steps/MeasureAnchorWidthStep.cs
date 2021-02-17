using System.Linq;
using UnityEngine;

public class MeasureAnchorWidthStep : WizardStepBase, IWizardStep
{
    private const float _handsDistance = -0.030f;

    public string helpText => $"Put your <b>hands</b> on your real <b>{_anchor.label.ToLower()}</b>, like the model is doing right now.\n\nPress Next to apply.\n\nTry to match as closely as you can the hands position of the model, but on your own body. Check that your feet are aligned, and try to center your hands to the center of your body (depth).";

    private readonly ControllerAnchorPoint _anchor;
    private readonly float _handRotate;
    private readonly bool _ignoreDepth;
    private readonly FreeControllerV3 _leftHandControl;
    private readonly FreeControllerV3 _rightHandControl;
    private readonly MotionControllerWithCustomPossessPoint _leftHandMotion;
    private readonly MotionControllerWithCustomPossessPoint _rightHandMotion;
    private FreeControllerV3Snapshot _leftHandSnapshot;
    private FreeControllerV3Snapshot _rightHandSnapshot;
    private Vector3 _leftComparePointPosition;
    private Vector3 _rightComparePointPosition;

    public MeasureAnchorWidthStep(EmbodyContext context, ControllerAnchorPoint anchor, float handRotate, bool ignoreDepth = false)
        : base(context)
    {
        _anchor = anchor;
        _handRotate = handRotate;
        _ignoreDepth = ignoreDepth;
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
            _leftComparePointPosition = _anchor.GetInGameWorldPosition() - (_anchor.bone.transform.right * (_anchor.inGameSize.x / 2f + _handsDistance / 2f) * context.scaleChangeReceiver.scale);
            _leftHandControl.control.position = _leftComparePointPosition;
            _leftHandControl.control.Translate(Quaternion.Inverse(Quaternion.Euler(_leftHandMotion.rotateControllerBase)) * _leftHandMotion.offsetControllerBase * context.scaleChangeReceiver.scale, Space.Self);
            _leftHandControl.control.RotateAround(_leftComparePointPosition, _leftHandControl.control.up, _handRotate);
        }

        {
            _rightHandControl.control.rotation = _anchor.bone.rotation;
            _rightHandControl.control.Rotate(_rightHandMotion.rotateControllerBase, Space.Self);
            _rightComparePointPosition = _anchor.GetInGameWorldPosition() + (_anchor.bone.transform.right * (_anchor.inGameSize.x / 2f + _handsDistance / 2f) * context.scaleChangeReceiver.scale);
            _rightHandControl.control.position = _rightComparePointPosition;
            _rightHandControl.control.Translate(Quaternion.Inverse(Quaternion.Euler(_rightHandMotion.rotateControllerBase)) * _rightHandMotion.offsetControllerBase * context.scaleChangeReceiver.scale, Space.Self);
            _rightHandControl.control.RotateAround(_rightComparePointPosition, _rightHandControl.control.up, -_handRotate);
        }
    }

    public void Apply()
    {
        var inverseRotation = Quaternion.Inverse(_anchor.bone.rotation);
        var compareCenter = (_leftComparePointPosition + _rightComparePointPosition) / 2f;
        var leftHandPosition = context.leftHand.position;
        var rightHandPosition = context.rightHand.position;
        var handsCenter = (leftHandPosition + rightHandPosition) / 2f;
        var realLifeOffset = inverseRotation * (handsCenter - compareCenter);
        realLifeOffset.x = 0; // We never want sideways offset
        realLifeOffset.z = _ignoreDepth ? 0f : realLifeOffset.z *  0.4f; // It's hard to get z right, especially for the chest, so let's tone down how much we trust this.
        _anchor.realLifeOffset = realLifeOffset;

        var realLifeWidth = Mathf.Abs((inverseRotation * rightHandPosition).x - (inverseRotation * leftHandPosition).x);
        _anchor.realLifeSize = _anchor.inGameSize / _anchor.inGameSize.x * realLifeWidth;
    }

    public override void Leave()
    {
        _leftHandSnapshot.Restore(true);
        _rightHandSnapshot.Restore(true);
    }
}
