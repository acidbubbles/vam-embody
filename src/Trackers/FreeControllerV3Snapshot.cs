using UnityEngine;

public class FreeControllerV3Snapshot
{
    public FreeControllerV3 controller;
    private Vector3 _position;
    private Quaternion _rotation;

    public static FreeControllerV3Snapshot Snap(FreeControllerV3 controller)
    {
        var controlTransform = controller.control.transform;
        return new FreeControllerV3Snapshot
        {
            controller = controller,
            _position = controlTransform.position,
            _rotation = controlTransform.rotation
        };
    }

    public void Restore()
    {
        var controlTransform = controller.control.transform;
        controlTransform.position = _position;
        controlTransform.rotation = _rotation;
    }
}
