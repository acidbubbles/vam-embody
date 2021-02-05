using System.Linq;
using UnityEngine;

public class ResetPoseStep : WizardStepBase, IWizardStep
{
    public string helpText => "We will now reset the pose. Press next when ready, or skip if you'd like to run the wizard using the current pose.";

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
        var footPitch = 15f;
        var footFloorDistance = 0.06f;
        var headForward = 0.01f;

        var head = context.containingAtom.freeControllers.First(fc => fc.name == "headControl");
        var lFoot = context.containingAtom.freeControllers.First(fc => fc.name == "lFootControl");
        var rFoot = context.containingAtom.freeControllers.First(fc => fc.name == "rFootControl");

        var position = (lFoot.control.position + rFoot.control.position) / 2f;
        position.Scale(new Vector3(1f, 0f, 1f));
        var direction = (lFoot.control.eulerAngles.y + rFoot.control.eulerAngles.y + head.control.eulerAngles.y) / 3f;
        foreach (var controller in context.containingAtom.freeControllers.Where(fc => fc.name.EndsWith("Control")))
        {
            SetState(controller, FreeControllerV3.PositionState.Off, FreeControllerV3.RotationState.Off);
        }

        SetState(head, FreeControllerV3.PositionState.On, FreeControllerV3.RotationState.On);
        head.control.localEulerAngles = new Vector3(0f, direction, 0f);
        head.control.position = position + new Vector3(0f, height, 0f) + head.control.forward * headForward;
        head.RBHoldPositionSpring = 10000f;
        head.RBHoldRotationSpring = 1000f;

        SetState(lFoot, FreeControllerV3.PositionState.On, FreeControllerV3.RotationState.On);
        lFoot.control.localEulerAngles = new Vector3(footPitch, direction - footYaw, 0f);
        lFoot.control.position = position - lFoot.control.right * footHalfDistance + Vector3.up * footFloorDistance;
        lFoot.RBHoldPositionSpring = 10000f;
        lFoot.RBHoldRotationSpring = 1000f;

        SetState(rFoot, FreeControllerV3.PositionState.On, FreeControllerV3.RotationState.On);
        rFoot.control.localEulerAngles = new Vector3(footPitch, direction + footYaw, 0f);
        rFoot.control.position = position + rFoot.control.right * footHalfDistance + Vector3.up * footFloorDistance;
        rFoot.RBHoldPositionSpring = 10000f;
        rFoot.RBHoldRotationSpring = 1000f;
    }

    private static void SetState(FreeControllerV3 controller, FreeControllerV3.PositionState positionState, FreeControllerV3.RotationState rotationState)
    {
        controller.currentPositionState = positionState;
        controller.currentRotationState = rotationState;
    }
}
