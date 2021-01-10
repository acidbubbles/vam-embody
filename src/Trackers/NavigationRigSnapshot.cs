using UnityEngine;

public class NavigationRigSnapshot
{
    private float _playerHeightAdjust;
    private Quaternion _rotation;
    private Vector3 _position;

    public static NavigationRigSnapshot Snap()
    {
        var navigationRig = SuperController.singleton.navigationRig;
        return new NavigationRigSnapshot
        {
            _position = navigationRig.position,
            _rotation = navigationRig.rotation,
            _playerHeightAdjust = SuperController.singleton.playerHeightAdjust
        };
    }

    public void Restore()
    {
        var navigationRig = SuperController.singleton.navigationRig;
        navigationRig.position = _position;
        navigationRig.rotation = _rotation;
        SuperController.singleton.playerHeightAdjust = _playerHeightAdjust;
    }
}
