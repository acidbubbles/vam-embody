﻿using System;
using SimpleJSON;
using UnityEngine;

public interface IOffsetCameraModule : IEmbodyModule
{
    JSONStorableFloat cameraDepthJSON { get; }
    JSONStorableFloat cameraHeightJSON { get; }
    JSONStorableFloat cameraPitchJSON { get; }
    JSONStorableFloat clipDistanceJSON { get; }
}

public class OffsetCameraModule : EmbodyModuleBase, IOffsetCameraModule
{
    public const string Label = "Offset Camera";
    public override string storeId => "OffsetCamera";
    public override string label => Label;

    public JSONStorableFloat cameraDepthJSON { get; set; }
    public JSONStorableFloat cameraHeightJSON { get; set; }
    public JSONStorableFloat cameraPitchJSON { get; set; }
    public JSONStorableFloat clipDistanceJSON { get; set; }

    private Possessor _possessor;

    public override void InitStorables()
    {
        base.InitStorables();

        cameraDepthJSON = new JSONStorableFloat("CameraDepthAdjust", 0.054f, (float val) => Refresh(), 0f, 0.2f, false);
        cameraHeightJSON = new JSONStorableFloat("CameraHeightAdjust", 0f, (float val) => Refresh(), -0.05f, 0.05f, false);
        cameraPitchJSON = new JSONStorableFloat("CameraPitchAdjust", 0f, (float val) => Refresh(), -135f, 45f, true);
        clipDistanceJSON = new JSONStorableFloat("ClipDistance", 0.01f, (float val) => Refresh(), 0.01f, .2f, true);
    }

    public override void InitReferences()
    {
        base.InitReferences();

        _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();
    }

    public override void OnEnable()
    {
        base.OnEnable();

        ApplyCameraPosition(true);
    }

    public override void OnDisable()
    {
        base.OnDisable();

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
            if (_possessor != null)
            {
                var mainCamera = CameraTarget.centerTarget.targetCamera;
                mainCamera.nearClipPlane = active ? clipDistanceJSON.val : 0.01f;
                var mainCameraTransform = mainCamera.transform;
                var cameraDepth  = active ? cameraDepthJSON.val : 0;
                var cameraHeight = active ? cameraHeightJSON.val : 0;
                var cameraPitch  = active ? cameraPitchJSON.val : 0;
                var possessorTransform = _possessor.transform;
                var pos                = possessorTransform.position;
                var mainCameraRotation = mainCameraTransform.rotation;
                mainCameraTransform.position = pos - mainCameraRotation * Vector3.forward * cameraDepth - mainCameraRotation * Vector3.down * cameraHeight;
                possessorTransform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
                possessorTransform.position = pos;
            }
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to update camera position: " + e);
        }
    }

    public override void StoreJSON(JSONClass jc, bool toProfile, bool toScene)
    {
        base.StoreJSON(jc, toProfile, toScene);

        cameraDepthJSON.StoreJSON(jc);
        cameraHeightJSON.StoreJSON(jc);
        cameraPitchJSON.StoreJSON(jc);
        clipDistanceJSON.StoreJSON(jc);
    }

    public override void RestoreFromJSON(JSONClass jc, bool fromProfile, bool fromScene)
    {
        base.RestoreFromJSON(jc, fromProfile, fromScene);

        cameraDepthJSON.RestoreFromJSON(jc);
        cameraHeightJSON.RestoreFromJSON(jc);
        cameraPitchJSON.RestoreFromJSON(jc);
        clipDistanceJSON.RestoreFromJSON(jc);
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();

        cameraDepthJSON.SetValToDefault();
        cameraHeightJSON.SetValToDefault();
        cameraPitchJSON.SetValToDefault();
        clipDistanceJSON.SetValToDefault();
    }
}
