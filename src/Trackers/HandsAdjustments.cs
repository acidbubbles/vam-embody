using UnityEngine;

public static class HandsAdjustments
{
    public static readonly Vector3 OVRLeftOffset = new Vector3(-0.03247f, -0.03789f, -0.10116f);
    public static readonly Vector3 OVRLeftRotate = new Vector3(-90, 90, 0);
    public static readonly Vector3 OVRRightOffset = Vector3.Scale(OVRLeftOffset, new Vector3(-1f, 1f, 1f));
    public static readonly Vector3 OVRRightRotate = Vector3.Scale(OVRLeftRotate, new Vector3(1f, -1f, -1f));

    private static readonly Vector3 _viveLeftOffset = new Vector3(-0.03247f, -0.08473f, -0.13298f);
    private static readonly Vector3 _viveLeftRotate = new Vector3(-90, 90, 0);
    private static readonly Vector3 _viveRightOffset = Vector3.Scale(_viveLeftOffset, new Vector3(-1f, 1f, 1f));
    private static readonly Vector3 _viveRightRotate = Vector3.Scale(_viveLeftRotate, new Vector3(1f, -1f, -1f));

    private static readonly Vector3 _leapLeftOffset = new Vector3(0f, -0.02341545f, 0f);
    private static readonly Vector3 _leapLeftRotate = new Vector3(180f, 90f, 6f);
    private static readonly Vector3 _leapRightOffset = Vector3.Scale(_leapLeftOffset, new Vector3(-1f, 1f, 1f));
    private static readonly Vector3 _leapRightRotate = Vector3.Scale(_leapLeftRotate, new Vector3(1f, -1f, -1f));

    public static void ConfigureHand(MotionControllerWithCustomPossessPoint motionControl, bool isRight)
    {
        var t = motionControl.currentMotionControl;
        if (t == SuperController.singleton.touchObjectLeft)
        {
            motionControl.offsetControllerBase = OVRLeftOffset;
            motionControl.rotateControllerBase = OVRLeftRotate;
        }
        else if (t == SuperController.singleton.touchObjectRight)
        {
            motionControl.offsetControllerBase = OVRRightOffset;
            motionControl.rotateControllerBase = OVRRightRotate;
        }
        else if (t == SuperController.singleton.viveObjectLeft)
        {
            motionControl.offsetControllerBase = _viveLeftOffset;
            motionControl.rotateControllerBase = _viveLeftRotate;
        }
        else if (t == SuperController.singleton.viveObjectRight)
        {
            motionControl.offsetControllerBase = _viveRightOffset;
            motionControl.rotateControllerBase = _viveRightRotate;
        }
        else if (t == SuperController.singleton.leapHandLeft)
        {
            motionControl.offsetControllerBase = _leapLeftOffset;
            motionControl.rotateControllerBase = _leapLeftRotate;
        }
        else if (t == SuperController.singleton.leapHandRight)
        {
            motionControl.offsetControllerBase = _leapRightOffset;
            motionControl.rotateControllerBase = _leapRightRotate;
        }
        else if(!isRight)
        {
            // TODO: Copied from Oculus controller
            motionControl.offsetControllerBase = OVRLeftOffset;
            motionControl.rotateControllerBase = OVRLeftRotate;
        }
        else
        {
            // TODO: Copied from Oculus controller
            motionControl.offsetControllerBase = OVRRightOffset;
            motionControl.rotateControllerBase = OVRRightRotate;
        }
    }
}
