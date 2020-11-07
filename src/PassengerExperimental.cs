using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PassengerExperimental : MVRScript
{
    private JSONStorableBool _activeJSON;
    private Rigidbody _link;
    private Transform _cameraRig;
    private Transform _cameraRigParent;
    private Quaternion _cameraRigRotationBackup;
    private Vector3 _cameraRigPositionBackup;

    public override void Init()
    {
        _activeJSON = new JSONStorableBool("Active", false, val =>
        {
            if (val)
                Activate();
            else
                Deactivate();
        });
        RegisterBool(_activeJSON);
        CreateToggle(_activeJSON);
    }

    public void OnDisable()
    {
        if (_activeJSON.val) _activeJSON.val = false;
    }

    private void Activate()
    {
        _link = containingAtom.rigidbodies.First(rb => rb.name == "head");

        // GlobalSceneOptions.singleton.disableNavigation = true;

        _cameraRig = SuperController.singleton.centerCameraTarget.transform.parent;
        _cameraRigParent = _cameraRig.transform.parent;

        _cameraRigRotationBackup = _cameraRig.transform.localRotation;
        _cameraRigPositionBackup = _cameraRig.transform.localPosition;

        _cameraRig.transform.SetParent(_link.transform, false);
    }

    private void Deactivate()
    {
        // GlobalSceneOptions.singleton.disableNavigation = false;

        _cameraRig.transform.SetParent(_cameraRigParent, true);
        _cameraRig.transform.localRotation = _cameraRigRotationBackup;
        _cameraRig.transform.localPosition = _cameraRigPositionBackup;

        _cameraRig = null;
        _cameraRigParent = null;

        _link = null;
    }

    public void Update()
    {
        if (!_activeJSON.val)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                _activeJSON.val = true;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
        {
            _activeJSON.val = false;
            return;
        }
    }

    public void FixedUpdate()
    {
        if (!_activeJSON.val) return;

        PositionCamera();
    }

    [MethodImpl(256)]
    public void PositionCamera()
    {
        _cameraRig.localPosition = Vector3.zero;
    }
}
