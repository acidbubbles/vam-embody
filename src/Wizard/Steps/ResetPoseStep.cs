using System.Linq;
using UnityEngine;

public class ResetPoseStep : WizardStepBase, IWizardStep
{
    public string helpText => "We will now reset the pose so the model is standing straight, and only head and feet are on. Press next when ready, or skip if you'd like to run the wizard using the current pose instead.";

    public ResetPoseStep(EmbodyContext context)
        : base(context)
    {
    }

    public void Apply()
    {
        var measurements = new PersonMeasurements(context.containingAtom);
        var height = measurements.MeasureHeight();
        // TODO: Measure shoulders
        var footHalfDistance = 0.1f;
        var footYaw = 16f;
        var footPitch = 18f;
        var footFloorDistance = 0.055f;
        var headForwardOffset = 0.032f;
        var feetForwardOffset = -0.040f;
        var handsToHeightRatio = 0.75f;
        var handsForwardOffset = 0.2f;
        var handsRightOffset = 0.2f;

        var head = context.containingAtom.freeControllers.First(fc => fc.name == "headControl");
        var pelvis = context.containingAtom.freeControllers.First(fc => fc.name == "pelvisControl");
        var hip = context.containingAtom.freeControllers.First(fc => fc.name == "hipControl");
        var lFoot = context.containingAtom.freeControllers.First(fc => fc.name == "lFootControl");
        var rFoot = context.containingAtom.freeControllers.First(fc => fc.name == "rFootControl");
        var lHand = context.containingAtom.freeControllers.First(fc => fc.name == "lHandControl");
        var rHand = context.containingAtom.freeControllers.First(fc => fc.name == "rHandControl");

        var position = (pelvis.control.position + hip.control.position) / 2f;
        position.Scale(new Vector3(1f, 0f, 1f));
        var direction = (
            Vector3.ProjectOnPlane(hip.control.eulerAngles, Vector3.up).y +
            Vector3.ProjectOnPlane(pelvis.control.eulerAngles, Vector3.up).y +
            Vector3.ProjectOnPlane(head.control.eulerAngles, Vector3.up).y
        ) / 3f;

        foreach (var controller in context.containingAtom.freeControllers.Where(fc => fc.name.EndsWith("Control")))
        {
            SetState(controller, FreeControllerV3.PositionState.Off, FreeControllerV3.RotationState.Off);
        }

        SetState(head, FreeControllerV3.PositionState.On, FreeControllerV3.RotationState.On);
        head.control.localEulerAngles = new Vector3(0f, direction, 0f);
        var headEulerAngles = head.control.localEulerAngles;
        var headForward = head.control.forward;
        var headRight = head.control.right;
        head.control.position = position + new Vector3(0f, height, 0f) + headForward * headForwardOffset;
        head.RBHoldPositionSpring = 10000f;
        head.RBHoldRotationSpring = 1000f;

        SetState(lFoot, FreeControllerV3.PositionState.On, FreeControllerV3.RotationState.On);
        lFoot.control.localEulerAngles = new Vector3(footPitch, direction - footYaw, 0f);
        lFoot.control.position = position - headRight * footHalfDistance + Vector3.up * footFloorDistance + headForward * feetForwardOffset;
        lFoot.RBHoldPositionSpring = 10000f;
        lFoot.RBHoldRotationSpring = 1000f;

        SetState(rFoot, FreeControllerV3.PositionState.On, FreeControllerV3.RotationState.On);
        rFoot.control.localEulerAngles = new Vector3(footPitch, direction + footYaw, 0f);
        rFoot.control.position = position + headRight * footHalfDistance + Vector3.up * footFloorDistance + headForward * feetForwardOffset;
        rFoot.RBHoldPositionSpring = 10000f;
        rFoot.RBHoldRotationSpring = 1000f;

        SetState(lHand, FreeControllerV3.PositionState.On, FreeControllerV3.RotationState.On);
        lHand.control.localEulerAngles = headEulerAngles + context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand).baseOffsetRotation;
        lHand.control.position = position + Vector3.up * (height * handsToHeightRatio) + headForward * handsForwardOffset - headRight * handsRightOffset;
        lHand.RBHoldPositionSpring = 10000f;
        lHand.RBHoldRotationSpring = 1000f;

        SetState(rHand, FreeControllerV3.PositionState.On, FreeControllerV3.RotationState.On);
        rHand.control.localEulerAngles = headEulerAngles + context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand).baseOffsetRotation;
        rHand.control.position = position + Vector3.up * (height * handsToHeightRatio) + headForward * handsForwardOffset + headRight * handsRightOffset;
        rHand.RBHoldPositionSpring = 10000f;
        rHand.RBHoldRotationSpring = 1000f;
    }

    private static void SetState(FreeControllerV3 controller, FreeControllerV3.PositionState positionState, FreeControllerV3.RotationState rotationState)
    {
        controller.currentPositionState = positionState;
        controller.currentRotationState = rotationState;
    }
}
