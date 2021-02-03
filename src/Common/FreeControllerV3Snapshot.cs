using UnityEngine;

public class FreeControllerV3Snapshot
{
    public FreeControllerV3 controller;
    public bool canGrabPosition;
    public bool canGrabRotation;
    private Vector3 _position;
    private Quaternion _rotation;
    private FreeControllerV3.PositionState _positionState;
    private FreeControllerV3.RotationState _rotationState;

    public static FreeControllerV3Snapshot Snap(FreeControllerV3 controller)
    {
        var controlTransform = controller.control.transform;
        return new FreeControllerV3Snapshot
        {
            controller = controller,
            _position = controlTransform.position,
            _rotation = controlTransform.rotation,
            canGrabPosition = controller.canGrabPosition,
            canGrabRotation = controller.canGrabRotation,
            _positionState = controller.currentPositionState,
            _rotationState = controller.currentRotationState
        };
    }

    public void Restore(bool restorePose)
    {
        controller.canGrabPosition = canGrabPosition;
        controller.canGrabRotation = canGrabRotation;
        controller.currentPositionState = _positionState;
        controller.currentRotationState = _rotationState;
        if (!restorePose) return;
        var controlTransform = controller.control.transform;
        controlTransform.position = _position;
        controlTransform.rotation = _rotation;
    }
}
