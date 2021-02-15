using System.Linq;
using UnityEngine;

public class MeasureAnchorDepthAndOffsetStep : WizardStepBase, IWizardStep
{
    public string helpText => $"Put your right hand at the same level as your {_anchor.label}, slightly pressed against you.\n\nTry to replicate as closely as you can the model's hand position, but on your own body.";

    private readonly ControllerAnchorPoint _anchor;
    private readonly float _handRotate;
    private readonly FreeControllerV3 _leftHandControl;
    private readonly FreeControllerV3 _rightHandControl;
    private readonly MotionControllerWithCustomPossessPoint _rightHandMotion;
    private FreeControllerV3Snapshot _leftHandSnapshot;
    private FreeControllerV3Snapshot _rightHandSnapshot;
    private Vector3 _comparePointPosition;

    public MeasureAnchorDepthAndOffsetStep(EmbodyContext context, ControllerAnchorPoint anchor, float handRotate)
        : base(context)
    {
        _anchor = anchor;
        _handRotate = handRotate;
        _leftHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "lHandControl");
        _rightHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "rHandControl");
        _rightHandMotion = context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand);
    }

    public override void Enter()
    {
        _leftHandSnapshot = FreeControllerV3Snapshot.Snap(_leftHandControl);
        _rightHandSnapshot = FreeControllerV3Snapshot.Snap(_rightHandControl);
        _rightHandControl.RBHoldPositionSpring = 10000;
        _rightHandControl.RBHoldRotationSpring = 300;
        _leftHandControl.currentPositionState = FreeControllerV3.PositionState.Off;
        _leftHandControl.currentRotationState = FreeControllerV3.RotationState.Off;
    }

    public override void Update()
    {
        _rightHandControl.control.eulerAngles = _anchor.bone.rotation.eulerAngles + _rightHandMotion.rotateControllerBase;
        _comparePointPosition = _anchor.GetInGameWorldPosition() + (_anchor.bone.transform.forward * (_anchor.inGameSize.z / 2f + TrackersConstants.handsDistance / 2f));
        _rightHandControl.control.position = _comparePointPosition + Quaternion.Inverse(_rightHandControl.control.rotation) * Quaternion.Euler(_rightHandMotion.rotateControllerBase) * _rightHandMotion.offsetControllerBase;
        _rightHandControl.control.RotateAround(_comparePointPosition, _anchor.bone.transform.up, -90);
        _rightHandControl.control.RotateAround(_comparePointPosition, _anchor.bone.transform.forward, _handRotate);
    }

    public void Apply()
    {
        var inGame = _anchor.bone.InverseTransformPoint(_comparePointPosition);
        var real = _anchor.bone.InverseTransformPoint(context.rightHand.position);

        _anchor.realLifeSize += new Vector3(0, (real.y - inGame.y) / 2f, real.z - inGame.z);

        _anchor.Update();
    }

    public override void Leave()
    {
        _leftHandSnapshot.Restore(true);
        _rightHandSnapshot.Restore(true);
    }
}
