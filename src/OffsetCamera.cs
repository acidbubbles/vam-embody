using System;
using System.Collections;
using Interop;
using UnityEngine;

public class OffsetCamera : MVRScript, ICameraOffset
{
    public JSONStorableBool activeJSON { get; set; }

    private InteropProxy _interop;
    private Possessor _possessor;
    private JSONStorableFloat _cameraDepthJSON;
    private JSONStorableFloat _cameraHeightJSON;
    private JSONStorableFloat _cameraPitchJSON;
    private JSONStorableFloat _clipDistanceJSON;

    public override void Init()
    {
        _interop = new InteropProxy(this, containingAtom);
        _interop.Init();

        _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();

        activeJSON = new JSONStorableBool("Active", false, (bool val) => Refresh());
        RegisterBool(activeJSON);
        CreateToggle(activeJSON, true);

        {
            _cameraDepthJSON = new JSONStorableFloat("Camera depth", 0.054f, 0f, 0.2f, false);
            RegisterFloat(_cameraDepthJSON);
            var cameraDepthSlider = CreateSlider(_cameraDepthJSON, false);
            cameraDepthSlider.slider.onValueChanged.AddListener(delegate(float val) { Refresh(); });
        }

        {
            _cameraHeightJSON = new JSONStorableFloat("Camera height", 0f, -0.05f, 0.05f, false);
            RegisterFloat(_cameraHeightJSON);
            var cameraHeightSlider = CreateSlider(_cameraHeightJSON, false);
            cameraHeightSlider.slider.onValueChanged.AddListener(delegate(float val) { Refresh(); });
        }

        {
            _cameraPitchJSON = new JSONStorableFloat("Camera pitch", 0f, -135f, 45f, true);
            RegisterFloat(_cameraPitchJSON);
            var cameraPitchSlider = CreateSlider(_cameraPitchJSON, false);
            cameraPitchSlider.slider.onValueChanged.AddListener(delegate(float val) { Refresh(); });
        }

        {
            _clipDistanceJSON = new JSONStorableFloat("Clip distance", 0.01f, 0.01f, .2f, true);
            RegisterFloat(_clipDistanceJSON);
            var clipDistanceSlider = CreateSlider(_clipDistanceJSON, false);
            clipDistanceSlider.slider.onValueChanged.AddListener(delegate(float val) { Refresh(); });
        }
    }

    public void OnEnable()
    {
        if (_interop?.ready != true) return;

        ApplyCameraPosition(true);
    }

    public void OnDisable()
    {
        if (_interop?.ready != true) return;

        ApplyCameraPosition(false);
    }

    public void Refresh()
    {
        if (_interop?.ready != true) return;

        ApplyCameraPosition(enabledJSON.val);
    }

    private void ApplyCameraPosition(bool active)
    {
        try
        {
            var mainCamera = CameraTarget.centerTarget?.targetCamera;

            mainCamera.nearClipPlane = active ? _clipDistanceJSON.val : 0.01f;

            var cameraDepth = active ? _cameraDepthJSON.val : 0;
            var cameraHeight = active ? _cameraHeightJSON.val : 0;
            var cameraPitch = active ? _cameraPitchJSON.val : 0;
            var pos = _possessor.transform.position;
            mainCamera.transform.position = pos - mainCamera.transform.rotation * Vector3.forward * cameraDepth - mainCamera.transform.rotation * Vector3.down * cameraHeight;
            _possessor.transform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
            _possessor.transform.position = pos;
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to update camera position: " + e);
        }
    }
}
