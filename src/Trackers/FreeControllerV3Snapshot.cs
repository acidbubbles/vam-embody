using UnityEngine;

public class FreeControllerV3Snapshot
{
    private FreeControllerV3 _controller;
    private Vector3 _position;
    private Quaternion _rotation;
    private bool _canGrabPosition;
    private bool _canGrabRotation;

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
        };
    }

    public void Restore()
    {
        var controlTransform = _controller.control.transform;
        controlTransform.position = _position;
        controlTransform.rotation = _rotation;
        _controller.canGrabPosition = _canGrabPosition;
        _controller.canGrabRotation = _canGrabRotation;
    }
}
