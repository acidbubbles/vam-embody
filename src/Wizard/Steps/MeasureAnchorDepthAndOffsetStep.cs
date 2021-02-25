using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

public class MeasureAnchorDepthAndOffsetStep : WizardStepBase, IWizardStep
{
    private const float _handsDistance = -0.025f;
    private const float _handsOffsetY = 0.03f;
    private const float _sizeDepthMultiply = 1.1f;

    public string helpText => $"Put your <b>right hand</b> at the same height as your <b>{_anchor.label.ToLower()}</b>, slightly pressed against you.\n\nTry to <b>replicate as closely as you can</b> the model's hand position, but on your own body.\n\nPress Next to apply.";

    private readonly ControllerAnchorPoint _anchor;
    private readonly float _handRotate;
    private readonly FreeControllerV3 _leftHandControl;
    private readonly FreeControllerV3 _rightHandControl;
    private readonly FreeControllerV3 _headControl;
    private readonly FreeControllerV3 _chestControl;
    private FreeControllerV3Snapshot _leftHandSnapshot;
    private FreeControllerV3Snapshot _rightHandSnapshot;
    private FreeControllerV3Snapshot _chestSnapshot;
    private Vector3 _rightComparePointPosition;

    public MeasureAnchorDepthAndOffsetStep(EmbodyContext context, ControllerAnchorPoint anchor, float handRotate)
        : base(context)
    {
        _anchor = anchor;
        _handRotate = handRotate;
        _leftHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "lHandControl");
        _rightHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "rHandControl");
        _headControl = context.containingAtom.freeControllers.First(fc => fc.name == "headControl");
        _chestControl = context.containingAtom.freeControllers.First(fc => fc.name == "chestControl");
    }

    public override void Enter()
    {
        _leftHandSnapshot = FreeControllerV3Snapshot.Snap(_leftHandControl);
        _rightHandSnapshot = FreeControllerV3Snapshot.Snap(_rightHandControl);
        _chestSnapshot = FreeControllerV3Snapshot.Snap(_chestControl);
        _rightHandControl.RBHoldPositionSpring = 10000;
        _rightHandControl.RBHoldRotationSpring = 300;
        _leftHandControl.currentPositionState = FreeControllerV3.PositionState.Off;
        _leftHandControl.currentRotationState = FreeControllerV3.RotationState.Off;
        _chestControl.currentRotationState = FreeControllerV3.RotationState.On;
        _chestControl.control.rotation = _headControl.control.rotation;
        _chestControl.RBHoldRotationSpring = 600;
    }

    [SuppressMessage("ReSharper", "Unity.InefficientMultiplicationOrder")]
    public override void Update()
    {
        var boneTransform = _anchor.bone.transform;

        _rightHandControl.control.rotation = _anchor.bone.rotation;
        _rightHandControl.control.Rotate(HandsAdjustments.OVRRightRotate, Space.Self);
        _rightComparePointPosition = _anchor.GetInGameWorldPosition();
        _rightComparePointPosition += (boneTransform.forward * (_anchor.inGameSize.z / 2f + _handsDistance / 2f) * context.scaleChangeReceiver.scale);
        _rightComparePointPosition += (boneTransform.up * _handsOffsetY * context.scaleChangeReceiver.scale);
        _rightHandControl.control.position = _rightComparePointPosition;
        _rightHandControl.control.Translate(Quaternion.Inverse(Quaternion.Euler(HandsAdjustments.OVRRightRotate)) * HandsAdjustments.OVRRightOffset * context.scaleChangeReceiver.scale, Space.Self);
        // ReSharper disable once Unity.InefficientPropertyAccess
        _rightHandControl.control.RotateAround(_rightComparePointPosition, boneTransform.up, -90);
        _rightHandControl.control.RotateAround(_rightComparePointPosition, boneTransform.forward, _handRotate);
    }

    public bool Apply()
    {
        var inGame = _anchor.bone.InverseTransformPoint(_rightComparePointPosition);
        var real = _anchor.bone.InverseTransformPoint(context.RightHand().position);

        _anchor.realLifeSize += new Vector3(0, (real.y - inGame.y) / 2f, (real.z - inGame.z) * _sizeDepthMultiply);

        _anchor.Update();

        return true;
    }

    public override void Leave()
    {
        _leftHandSnapshot.Restore(true);
        _rightHandSnapshot.Restore(true);
        _chestSnapshot.Restore(true);
    }
}
