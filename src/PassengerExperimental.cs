using System.Linq;
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

        // var x = SuperController.singleton.centerCameraTarget.transform;
        // while(true)
        // {
        //     SuperController.LogMessage($"{x.name}");
        //     x = x.parent;
        //     if(x == null) break;
        // }
        // SuperController.LogMessage($"DONE");
    }

    public void OnDisable()
    {
        if (_activeJSON.val) _activeJSON.val = false;
    }

    private void Activate()
    {
        _link = containingAtom.rigidbodies.First(rb => rb.name == "head");

        _cameraRig = SuperController.singleton.GetAtomByUid("[CameraRig]").transform.GetChild(0);
        _cameraRigParent = _cameraRig.transform.parent;

        _cameraRigRotationBackup = _cameraRig.transform.localRotation;
        _cameraRigPositionBackup = _cameraRig.transform.localPosition;

        _cameraRig.GetComponentInChildren<OVRCameraRig>().enabled = false;
        _cameraRig.transform.SetParent(_link.transform, true);

        // for (var i = 0; i < _cameraRig.transform.childCount; i++)
        // {
        //     var c = _cameraRig.transform.GetChild(i);
        //     SuperController.LogMessage($"{c}");
        //     foreach (var x in c.GetComponents<MonoBehaviour>())
        //         SuperController.LogMessage($"  {x}");
        // }
    }

    private void Deactivate()
    {
        _cameraRig.transform.SetParent(_cameraRigParent, true);
        _cameraRig.GetComponentInChildren<OVRCameraRig>().enabled = true;
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

        PositionCamera();
    }

    public void LateUpdate()
    {
        if (!_activeJSON.val) return;

        PositionCamera();
    }

    public void PositionCamera()
    {
        Transform centerTarget = SuperController.singleton.centerCameraTarget.transform;
        _cameraRig.transform.localPosition = centerTarget.InverseTransformPoint(_cameraRig.transform.position);
        _cameraRig.transform.localRotation = Quaternion.Inverse(centerTarget.rotation) * _cameraRig.transform.rotation;
    }
}
