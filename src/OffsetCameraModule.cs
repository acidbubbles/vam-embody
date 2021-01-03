using System;
using UnityEngine;

public interface IOffsetCamera : IEmbodyModule
{
}

public class OffsetCameraModule : EmbodyModuleBase, IOffsetCamera
{
    public JSONStorableBool activeJSON { get; set; }

    private Possessor _possessor;
    private JSONStorableFloat _cameraDepthJSON;
    private JSONStorableFloat _cameraHeightJSON;
    private JSONStorableFloat _cameraPitchJSON;
    private JSONStorableFloat _clipDistanceJSON;

    public override void Init()
    {
        _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();

        activeJSON = new JSONStorableBool("Active", false, (bool val) => Refresh());
        RegisterBool(activeJSON);
        CreateToggle(activeJSON, true);

        _cameraDepthJSON = new JSONStorableFloat("CameraDepthAdjust", 0.054f, (float val) => Refresh(), 0f, 0.2f, false);
        RegisterFloat(_cameraDepthJSON);
        CreateSlider(_cameraDepthJSON, false).label = "Depth adjust";

        _cameraHeightJSON = new JSONStorableFloat("CameraHeightAdjust", 0f, (float val) => Refresh(), -0.05f, 0.05f, false);
        RegisterFloat(_cameraHeightJSON);
        CreateSlider(_cameraHeightJSON, false).label = "Height adjust";

        _cameraPitchJSON = new JSONStorableFloat("CameraPitchAdjust", 0f, (float val) => Refresh(), -135f, 45f, true);
        RegisterFloat(_cameraPitchJSON);
        CreateSlider(_cameraPitchJSON, false).label = "Pitch adjust";

        _clipDistanceJSON = new JSONStorableFloat("ClipDistance", 0.01f, 0.01f, .2f, true);
        RegisterFloat(_clipDistanceJSON);
        CreateSlider(_clipDistanceJSON, false).label = "Clip distance";
    }

    public void OnEnable()
    {
        ApplyCameraPosition(true);
    }

    public void OnDisable()
    {
        ApplyCameraPosition(false);
    }

    public void Refresh()
    {
        ApplyCameraPosition(enabled);
    }

    private void ApplyCameraPosition(bool active)
    {
        try
        {
            var mainCamera = CameraTarget.centerTarget.targetCamera;
            mainCamera.nearClipPlane = active ? _clipDistanceJSON.val : 0.01f;
            var mainCameraTransform = mainCamera.transform;
            var cameraDepth = active ? _cameraDepthJSON.val : 0;
            var cameraHeight = active ? _cameraHeightJSON.val : 0;
            var cameraPitch = active ? _cameraPitchJSON.val : 0;

            var possessorTransform = _possessor.transform;
            var pos = possessorTransform.position;
            var mainCameraRotation = mainCameraTransform.rotation;
            mainCameraTransform.position = pos - mainCameraRotation * Vector3.forward * cameraDepth - mainCameraRotation * Vector3.down * cameraHeight;
            possessorTransform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
            possessorTransform.position = pos;
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to update camera position: " + e);
        }
    }
}
