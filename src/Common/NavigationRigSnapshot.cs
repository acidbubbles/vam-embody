using UnityEngine;

public class NavigationRigSnapshot
{
    private float _playerHeightAdjust;
    private Quaternion _rotation;
    private Vector3 _position;
    private Vector3 _monitorPosition;
    private Quaternion _monitorRotation;

    public static NavigationRigSnapshot Snap()
    {
        var navigationRig = SuperController.singleton.navigationRig;
        var monitorCenterCamera = SuperController.singleton.MonitorCenterCamera.transform;
        return new NavigationRigSnapshot
        {
            _playerHeightAdjust = SuperController.singleton.playerHeightAdjust,
            _position = navigationRig.position,
            _rotation = navigationRig.rotation,
            _monitorPosition = monitorCenterCamera.position,
            _monitorRotation = monitorCenterCamera.rotation,
        };
    }

    public void Restore()
    {
        var navigationRig = SuperController.singleton.navigationRig;
        SuperController.singleton.playerHeightAdjust = _playerHeightAdjust;
        navigationRig.position = _position;
        navigationRig.rotation = _rotation;
        var monitorCenterCamera = SuperController.singleton.MonitorCenterCamera.transform;
        monitorCenterCamera.position = _monitorPosition;
        monitorCenterCamera.rotation = _monitorRotation;
    }
}
