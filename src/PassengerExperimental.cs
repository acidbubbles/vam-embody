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
    private JSONStorableStringChooser _linkJSON;
    private JSONStorableBool _lookAtJSON;

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

        var defaultLink = containingAtom.type == "Person" ? "head" : "object";
        var links = containingAtom.linkableRigidbodies.Select(c => c.name).ToList();
        _linkJSON = new JSONStorableStringChooser("Target Controller", links, defaultLink, "Camera controller", (string val) => Refresh());
        RegisterStringChooser(_linkJSON);
        CreateScrollablePopup(_linkJSON).popupPanelHeight = 600f;

        _lookAtJSON = new JSONStorableBool("Look At Eye Target", false, (bool val) => Refresh());
        if (containingAtom.type == "Person")
        {
            RegisterBool(_lookAtJSON);
            CreateToggle(_lookAtJSON);
        }

        SuperController.singleton.StartCoroutine(InitDeferred());
    }

    private void Refresh()
    {
        if (_activeJSON?.val != true) return;
        _activeJSON.val = false;
        _activeJSON.val = true;
    }

    private IEnumerator InitDeferred()
    {
        yield return new WaitForEndOfFrame();
        _interop.Connect();
        _ready = true;
        if (_activeJSON.val)
            Activate();
    }

    public void OnDisable()
    {
        if (_activeJSON.val) _activeJSON.val = false;
    }

    private void Activate()
    {
        try
        {
            _link = containingAtom.rigidbodies.First(rb => rb.name == _linkJSON.val);

            if (!CanActivate())
            {
                _activeJSON.valNoCallback = false;
                return;
            }

            if (_lookAtJSON.val)
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
            _activeJSON.valNoCallback = false;
            Deactivate();
        }
    }

    private bool CanActivate()
    {
        if (_link == null)
        {
            SuperController.LogError("Embody: Could not find the specified link.");
            return false;
        }

        var userPreferences = SuperController.singleton.GetAtomByUid("CoreControl").gameObject.GetComponent<UserPreferences>();
        if (userPreferences.useHeadCollider)
        {
            SuperController.LogError("Embody: Do not enable the head collider with Passenger, they do not work together!");
            return false;
        }

        var linkController = containingAtom.GetStorableByID(_link.name.EndsWith("Control") ? _link.name : $"{_link.name}Control") as FreeControllerV3;
        if (linkController != null && linkController.possessed)
        {
            SuperController.LogError($"Embody: Cannot activate Passenger while the target rigidbody {_link.name} is being possessed. Use the 'Active' checkbox or trigger instead of using built-in Virt-A-Mate possession.");
            return false;
        }

        return true;
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
        }

        if (_interop.improvedPoV?.possessedOnlyJSON != null)
            _interop.improvedPoV.possessedOnlyJSON.val = true;

        _link = null;
        _lookAt = null;
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
        if (_lookAt)
            _cameraRig.LookAt(_lookAt.transform);
    }
}
