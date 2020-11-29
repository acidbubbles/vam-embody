using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using Interop;
using MeshVR;
using UnityEngine;

public class PassengerExperimental : MVRScript
{
    private InteropProxy _interop;
    private JSONStorableBool _activeJSON;
    private Rigidbody _link;
    private Transform _cameraRig;
    private Transform _cameraRigParent;
    private Quaternion _cameraRigRotationBackup;
    private Vector3 _cameraRigPositionBackup;
    private bool _ready;
    private FreeControllerV3 _lookAt;

    public override void Init()
    {
        _interop = new InteropProxy(containingAtom);
        _activeJSON = new JSONStorableBool("Active", false, val =>
        {
            if (!_ready) return;
            if (val)
                Activate();
            else
                Deactivate();
        });
        RegisterBool(_activeJSON);
        CreateToggle(_activeJSON);
        SuperController.singleton.StartCoroutine(InitDeferred());
    }

    private IEnumerator InitDeferred()
    {
        yield return new WaitForEndOfFrame();
        _interop.Connect();
        _ready = true;
        if (_activeJSON.val) Activate();
    }

    public void OnDisable()
    {
        if (_activeJSON.val) _activeJSON.val = false;
    }

    private void Activate()
    {
        try
        {
            _link = containingAtom.rigidbodies.First(rb => rb.name == "head");
            _lookAt = containingAtom.freeControllers.First(fc => fc.name == "eyeTargetControl");

            _cameraRig = SuperController.singleton.centerCameraTarget.transform.parent.GetComponentInChildren<Camera>().transform;
            var cameraRigTransform = _cameraRig.transform;
            _cameraRigParent = cameraRigTransform.parent;

            _cameraRigRotationBackup = cameraRigTransform.localRotation;
            _cameraRigPositionBackup = cameraRigTransform.localPosition;

            cameraRigTransform.SetParent(_link.transform, false);

            if (_interop.improvedPoV?.possessedOnlyJSON != null)
                _interop.improvedPoV.possessedOnlyJSON.val = false;

            GlobalSceneOptions.singleton.disableNavigation = true;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"Embody: Failed to activate Passenger.\n{exc}");
            Deactivate();
        }
    }

    private void Deactivate()
    {
        GlobalSceneOptions.singleton.disableNavigation = false;

        if (_cameraRig != null)
        {
            var cameraRigTransform = _cameraRig.transform;
            cameraRigTransform.SetParent(_cameraRigParent, false);
            cameraRigTransform.localRotation = _cameraRigRotationBackup;
            cameraRigTransform.localPosition = _cameraRigPositionBackup;

            _cameraRig = null;
            _cameraRigParent = null;

            _link = null;
        }

        if (_interop.improvedPoV?.possessedOnlyJSON != null)
            _interop.improvedPoV.possessedOnlyJSON.val = true;
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
        _cameraRig.LookAt(_lookAt.transform);
    }
}
