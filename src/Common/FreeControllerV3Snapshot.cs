using UnityEngine;

public class FreeControllerV3Snapshot
{
    public FreeControllerV3 _controller;
    private Vector3 _position;
    private Quaternion _rotation;
    public bool _canGrabPosition;
    public bool _canGrabRotation;
    private FreeControllerV3.PositionState _positionState;
    private FreeControllerV3.RotationState _rotationState;

    public static FreeControllerV3Snapshot Snap(FreeControllerV3 controller)
    {
        var controlTransform = controller.control.transform;
        return new FreeControllerV3Snapshot
        {
            _controller = controller,
            _position = controlTransform.position,
            _rotation = controlTransform.rotation,
            _canGrabPosition = controller.canGrabPosition,
            _canGrabRotation = controller.canGrabRotation,
            _positionState = controller.currentPositionState,
            _rotationState = controller.currentRotationState
        };
    }

    public void Restore(bool restorePose)
    {
        _controller.canGrabPosition = _canGrabPosition;
        _controller.canGrabRotation = _canGrabRotation;
        _controller.currentPositionState = _positionState;
        _controller.currentRotationState = _rotationState;
        if (!restorePose) return;
        var controlTransform = _controller.control.transform;
        controlTransform.position = _position;
        controlTransform.rotation = _rotation;
    }
}
