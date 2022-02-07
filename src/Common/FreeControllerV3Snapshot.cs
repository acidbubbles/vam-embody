using System;
using UnityEngine;

public class FreeControllerV3Snapshot
{
    public FreeControllerV3 controller;
    public bool canGrabPosition;
    public bool canGrabRotation;
    public float holdPositionSpring;
    public float holdRotationSpring;
    private Vector3 _position;
    private Quaternion _rotation;
    private FreeControllerV3.PositionState _positionState;
    private FreeControllerV3.RotationState _rotationState;
    public bool hidden;

    public static FreeControllerV3Snapshot Snap(FreeControllerV3 controller)
    {
        if (controller == null) throw new NullReferenceException("Controller reference not set to an instance of an object");
        var control = controller.control;
        if (control == null) throw new NullReferenceException($"Controller '{controller.name}' does not have a control");
        return new FreeControllerV3Snapshot
        {
            controller = controller,
            _position = control.position,
            _rotation = control.rotation,
            canGrabPosition = controller.canGrabPosition,
            canGrabRotation = controller.canGrabRotation,
            _positionState = controller.currentPositionState,
            _rotationState = controller.currentRotationState,
            holdPositionSpring = controller.RBHoldPositionSpring,
            holdRotationSpring = controller.RBHoldRotationSpring,
            hidden = controller.hidden,
        };
    }

    public void Restore(bool restorePose)
    {
        if (controller == null) return;
        controller.canGrabPosition = canGrabPosition;
        controller.canGrabRotation = canGrabRotation;
        controller.currentPositionState = _positionState;
        controller.currentRotationState = _rotationState;
        controller.RBHoldPositionSpring = holdPositionSpring;
        controller.RBHoldRotationSpring = holdRotationSpring;
        controller.hidden = hidden;
        if (!restorePose) return;
        var control = controller.control;
        if (control == null) return;
        control.position = _position;
        control.rotation = _rotation;
        if (controller.followWhenOff != null)
        {
            controller.followWhenOff.position = _position;
            controller.followWhenOff.rotation = _rotation;
        }
        if(controller.currentPositionState == FreeControllerV3.PositionState.Comply)
            controller.PauseComply();
    }
}
