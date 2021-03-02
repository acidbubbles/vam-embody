using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

public class MeasureAnchorWidthStep : WizardStepBase, IWizardStep
{
    private const float _handsDistance = -0.030f;
    private const float _handsOffsetY = 0.03f;
    private const float _widthMultiplier = 1.2f;

    public string helpText => $@"Put your <b>hands</b> on your real <b>{_anchor.label.ToLower()}</b>, like the model is doing right now.

Press Next to apply.

Try to match as closely as you can the hands position of the model, but on your own body. Check that your <b>feet are aligned</b>, and try to center your hands depth-wise without moving forward.".TrimStart();

    private readonly ControllerAnchorPoint _anchor;
    private readonly float _handRotate;
    private readonly bool _ignoreDepth;
    private readonly FreeControllerV3 _leftHandControl;
    private readonly FreeControllerV3 _rightHandControl;
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

    [SuppressMessage("ReSharper", "Unity.InefficientMultiplicationOrder")]
    public override void Update()
    {
        var boneTransform = _anchor.bone.transform;

        {
            _leftHandControl.control.rotation = _anchor.bone.rotation;
            _leftHandControl.control.Rotate(HandsAdjustments.OVRLeftRotate, Space.Self);
            _leftComparePointPosition = _anchor.GetInGameWorldPosition();
            _leftComparePointPosition -= (boneTransform.right * (_anchor.inGameSize.x / 2f + _handsDistance / 2f) * context.scaleChangeReceiver.scale);
            _leftComparePointPosition += (boneTransform.up * _handsOffsetY * context.scaleChangeReceiver.scale);
            _leftHandControl.control.position = _leftComparePointPosition;
            _leftHandControl.control.Translate(Quaternion.Inverse(Quaternion.Euler(HandsAdjustments.OVRLeftRotate)) * HandsAdjustments.OVRLeftOffset * context.scaleChangeReceiver.scale, Space.Self);
            _leftHandControl.control.RotateAround(_leftComparePointPosition, _leftHandControl.control.up, _handRotate);
            // _leftHandControl.control.Translate(_anchor.bone.up * _handsOffsetY, Space.World);
        }

        {
            _rightHandControl.control.rotation = _anchor.bone.rotation;
            _rightHandControl.control.Rotate(HandsAdjustments.OVRRightRotate, Space.Self);
            _rightComparePointPosition = _anchor.GetInGameWorldPosition();
            _rightComparePointPosition += (boneTransform.right * (_anchor.inGameSize.x / 2f + _handsDistance / 2f) * context.scaleChangeReceiver.scale);
            _rightComparePointPosition += (boneTransform.up * _handsOffsetY * context.scaleChangeReceiver.scale);
            _rightHandControl.control.position = _rightComparePointPosition;
            _rightHandControl.control.Translate(Quaternion.Inverse(Quaternion.Euler(HandsAdjustments.OVRRightRotate)) * HandsAdjustments.OVRRightOffset * context.scaleChangeReceiver.scale, Space.Self);
            _rightHandControl.control.RotateAround(_rightComparePointPosition, _rightHandControl.control.up, -_handRotate);
            // _rightHandControl.control.Translate(_anchor.bone.up * _handsOffsetY, Space.World);
        }
    }

    public bool Apply()
    {
        var inverseRotation = Quaternion.Inverse(_anchor.bone.rotation);
        var compareCenter = (_leftComparePointPosition + _rightComparePointPosition) / 2f;
        var leftHandPosition = context.LeftHand().position;
        var rightHandPosition = context.RightHand().position;
        var handsCenter = (leftHandPosition + rightHandPosition) / 2f;
        var realLifeOffset = inverseRotation * (handsCenter - compareCenter);
        realLifeOffset.x = 0; // We never want sideways offset
        realLifeOffset.z = _ignoreDepth ? 0f : realLifeOffset.z *  0.6f; // It's hard to get z right, especially for the chest, so let's tone down how much we trust this.
        _anchor.realLifeOffset = realLifeOffset;

        var realLifeWidth = Mathf.Abs((inverseRotation * rightHandPosition).x - (inverseRotation * leftHandPosition).x);
        _anchor.realLifeSize = _anchor.inGameSize / _anchor.inGameSize.x * (realLifeWidth * _widthMultiplier);
        _anchor.realLifeSize = new Vector3(_anchor.realLifeSize.x, 1f, _anchor.realLifeSize.z);

        context.diagnostics.TakeSnapshot($"{nameof(MeasureAnchorWidthStep)}[{_anchor.id}].{nameof(Apply)}");

        return true;
    }

    public override void Leave(bool final)
    {
        _leftHandSnapshot.Restore(true);
        _rightHandSnapshot.Restore(true);
    }
}
