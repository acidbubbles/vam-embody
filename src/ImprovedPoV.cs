// #define POV_DIAGNOSTICS
using System;
using System.Collections.Generic;
using System.Linq;
using Handlers;
using UnityEngine;

public class ImprovedPoV : MVRScript
{
    private Atom _person;
    private Camera _mainCamera;
    private Possessor _possessor;
    private FreeControllerV3 _headControl;
    private DAZCharacterSelector _selector;
    private JSONStorableFloat _cameraDepthJSON;
    private JSONStorableFloat _cameraHeightJSON;
    private JSONStorableFloat _cameraPitchJSON;
    private JSONStorableFloat _clipDistanceJSON;
    private JSONStorableBool _autoWorldScaleJSON;
    private JSONStorableBool _possessedOnlyJSON;
    private JSONStorableBool _hideFaceJSON;
    private JSONStorableBool _hideHairJSON;

    private SkinHandler _skinHandler;
    private List<HairHandler> _hairHandlers;
    // For change detection purposes
    private DAZCharacter _character;
    private DAZHairGroup[] _hair;


    // Whether the PoV effects are currently active, i.e. in possession mode
    private bool _lastActive;
    // Requires re-generating all shaders and materials, either because last frame was not ready or because something changed
    private bool _dirty;
    // To avoid spamming errors when something failed
    private bool _failedOnce;
    // When waiting for a model to load, how long before we abandon
    private int _tryAgainAttempts;
    private float _originalWorldScale;

    public override void Init()
    {
        try
        {
            if (containingAtom?.type != "Person")
            {
                SuperController.LogError($"Please apply the ImprovedPoV plugin to the 'Person' atom you wish to possess. Currently applied on '{containingAtom.type}'.");
                DestroyImmediate(this);
                return;
            }

            _person = containingAtom;
            _mainCamera = CameraTarget.centerTarget?.targetCamera;
            _possessor = SuperController
                .FindObjectsOfType(typeof(Possessor))
                .Where(p => p.name == "CenterEye")
                .Select(p => p as Possessor)
                .FirstOrDefault();
            _headControl = (FreeControllerV3)_person.GetStorableByID("headControl");
            _selector = _person.GetComponentInChildren<DAZCharacterSelector>();

            InitControls();
            Camera.onPreRender += OnPreRender;
            Camera.onPostRender += OnPostRender;
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to initialize Improved PoV: " + e);
            DestroyImmediate(this);
        }
    }

    private void OnPreRender(Camera cam)
    {
        if (!IsPovCamera(cam)) return;

        try
        {
            if (_skinHandler != null)
                _skinHandler.BeforeRender();
            if (_hairHandlers != null)
                _hairHandlers.ForEach(x =>
                {
                    if (x != null)
                        x.BeforeRender();
                });
        }
        catch (Exception e)
        {
            if (_failedOnce) return;
            _failedOnce = true;
            SuperController.LogError("Failed to execute pre render Improved PoV: " + e);
        }
    }

    private void OnPostRender(Camera cam)
    {
        if (!IsPovCamera(cam)) return;

        try
        {
            if (_skinHandler != null)
                _skinHandler.AfterRender();
            if (_hairHandlers != null)
                _hairHandlers.ForEach(x =>
                {
                    if (x != null)
                        x.AfterRender();
                });
        }
        catch (Exception e)
        {
            if (_failedOnce) return;
            _failedOnce = true;
            SuperController.LogError("Failed to execute post render Improved PoV: " + e);
        }
    }

    private bool IsPovCamera(Camera cam)
    {
        return
            // Oculus Rift
            cam.name == "CenterEyeAnchor" ||
            // Steam VR
            cam.name == "Camera (eye)" ||
            // Desktop
            cam.name == "MonitorRig"; /* ||
            // Window Camera
            cam.name == "MiniCamera";
            */
    }

    private void InitControls()
    {
        try
        {
            {
                _cameraDepthJSON = new JSONStorableFloat("Camera depth", 0.054f, 0f, 0.2f, false);
                RegisterFloat(_cameraDepthJSON);
                var cameraDepthSlider = CreateSlider(_cameraDepthJSON, false);
                cameraDepthSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    ApplyCameraPosition(_lastActive);
                });
            }

            {
                _cameraHeightJSON = new JSONStorableFloat("Camera height", 0f, -0.05f, 0.05f, false);
                RegisterFloat(_cameraHeightJSON);
                var cameraHeightSlider = CreateSlider(_cameraHeightJSON, false);
                cameraHeightSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    ApplyCameraPosition(_lastActive);
                });
            }

            {
                _cameraPitchJSON = new JSONStorableFloat("Camera pitch", 0f, -135f, 45f, true);
                RegisterFloat(_cameraPitchJSON);
                var cameraPitchSlider = CreateSlider(_cameraPitchJSON, false);
                cameraPitchSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    ApplyCameraPosition(_lastActive);
                });
            }

            {
                _clipDistanceJSON = new JSONStorableFloat("Clip distance", 0.01f, 0.01f, .2f, true);
                RegisterFloat(_clipDistanceJSON);
                var clipDistanceSlider = CreateSlider(_clipDistanceJSON, false);
                clipDistanceSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    ApplyCameraPosition(_lastActive);
                });
            }

            {
                _autoWorldScaleJSON = new JSONStorableBool("Auto world scale", false);
                RegisterBool(_autoWorldScaleJSON);
                var autoWorldScaleToggle = CreateToggle(_autoWorldScaleJSON, true);
                autoWorldScaleToggle.toggle.onValueChanged.AddListener(delegate (bool val)
                {
                    _dirty = true;
                });
            }

            {
                var possessedOnlyDefaultValue = true;
#if (POV_DIAGNOSTICS)
                // NOTE: Easier to test when it's always on
                possessedOnlyDefaultValue = false;
#endif
                _possessedOnlyJSON = new JSONStorableBool("Activate only when possessed", possessedOnlyDefaultValue);
                RegisterBool(_possessedOnlyJSON);
                var possessedOnlyCheckbox = CreateToggle(_possessedOnlyJSON, true);
                possessedOnlyCheckbox.toggle.onValueChanged.AddListener(delegate (bool val)
                {
                    _dirty = true;
                });
            }

            {
                _hideFaceJSON = new JSONStorableBool("Hide face", true);
                RegisterBool(_hideFaceJSON);
                var hideFaceToggle = CreateToggle(_hideFaceJSON, true);
                hideFaceToggle.toggle.onValueChanged.AddListener(delegate (bool val)
                {
                    _dirty = true;
                });
            }

            {
                _hideHairJSON = new JSONStorableBool("Hide hair", true);
                RegisterBool(_hideHairJSON);
                var hideHairToggle = CreateToggle(_hideHairJSON, true);
                hideHairToggle.toggle.onValueChanged.AddListener(delegate (bool val)
                {
                    _dirty = true;
                });
            }
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to register controls: " + e);
        }
    }

    public void OnDisable()
    {
        try
        {
            _dirty = false;
            ApplyAll(false);
            _lastActive = false;
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to disable Improved PoV: " + e);
        }
    }

    public void OnDestroy()
    {
        OnDisable();
        Camera.onPreRender -= OnPreRender;
        Camera.onPostRender -= OnPostRender;
    }

    public void Update()
    {
        try
        {
            var active = _headControl.possessed || !_possessedOnlyJSON.val;

            if (!_lastActive && active)
            {
                ApplyAll(true);
                _lastActive = true;
            }
            else if (_lastActive && !active)
            {
                ApplyAll(false);
                _lastActive = false;
            }
            else if (_dirty)
            {
                _dirty = false;
                ApplyAll(_lastActive);
            }
            else if (_lastActive && _selector.selectedCharacter != _character)
            {
                _skinHandler?.Restore();
                _skinHandler = null;
                ApplyAll(true);
            }
            else if (_lastActive && !_selector.hairItems.Where(h => h.active).SequenceEqual(_hair))
            {
                // Note: This only checks if the first hair changed. It'll be good enough for most purposes, but imperfect.
                if (_hairHandlers != null)
                {
                    _hairHandlers.ForEach(x =>
                    {
                        if (x != null)
                            x.Restore();
                    });
                    _hairHandlers = null;
                }
                ApplyAll(true);
            }
        }
        catch (Exception e)
        {
            if (_failedOnce) return;
            _failedOnce = true;
            SuperController.LogError("Failed to update Improved PoV: " + e);
        }
    }

    private void ApplyAll(bool active)
    {
        // Try again next frame
        if (_selector.selectedCharacter?.skin == null)
        {
            MakeDirty("Skin not yet loaded.");
            return;
        }

        _character = _selector.selectedCharacter;
        _hair = _selector.hairItems.Where(h => h.active).ToArray();

        ApplyAutoWorldScale(active);
        ApplyCameraPosition(active);
        ApplyPossessorMeshVisibility(active);
        if (UpdateHandler(ref _skinHandler, active && _hideFaceJSON.val))
            ConfigureHandler("Skin", ref _skinHandler, _skinHandler.Configure(_character.skin));
        if (_hairHandlers == null)
            _hairHandlers = new List<HairHandler>(new HairHandler[_hair.Length]);
        for (var i = 0; i < _hairHandlers.Count; i++)
        {
            var hairHandler = _hairHandlers[i];
            if (UpdateHandler(ref hairHandler, active && _hideHairJSON.val))
                ConfigureHandler("Hair", ref hairHandler, hairHandler.Configure(_hair[i]));
            _hairHandlers[i] = hairHandler;
        }

        if (!_dirty) _tryAgainAttempts = 0;
    }

    private void MakeDirty(string reason)
    {
        _dirty = true;
        _tryAgainAttempts++;
        if (_tryAgainAttempts > 90 * 20) // Approximately 20 to 40 seconds
        {
            SuperController.LogError("Failed to apply ImprovedPoV. Reason: " + reason + ". Try reloading the plugin, or report the issue to @Acidbubbles.");
            enabled = false;
        }
    }

    private void ConfigureHandler<T>(string what, ref T handler, int result)
     where T : IHandler, new()
    {
        switch (result)
        {
            case HandlerConfigurationResult.Success:
                break;
            case HandlerConfigurationResult.CannotApply:
                handler = default(T);
                break;
            case HandlerConfigurationResult.TryAgainLater:
                handler = default(T);
                MakeDirty(what + " is still waiting for assets to be ready.");
                break;
        }
    }

    private bool UpdateHandler<T>(ref T handler, bool active)
     where T : IHandler, new()
    {
        if (handler == null && active)
        {
            handler = new T();
            return true;
        }

        if (handler != null && active)
        {
            handler.Restore();
            handler = new T();
            return true;
        }

        if (handler != null && !active)
        {
            handler.Restore();
            handler = default(T);
        }

        return false;
    }

    private void ApplyCameraPosition(bool active)
    {
        try
        {
            _mainCamera.nearClipPlane = active ? _clipDistanceJSON.val : 0.01f;

            var cameraDepth = active ? _cameraDepthJSON.val : 0;
            var cameraHeight = active ? _cameraHeightJSON.val : 0;
            var cameraPitch = active ? _cameraPitchJSON.val : 0;
            var pos = _possessor.transform.position;
            _mainCamera.transform.position = pos - _mainCamera.transform.rotation * Vector3.forward * cameraDepth - _mainCamera.transform.rotation * Vector3.down * cameraHeight;
            _possessor.transform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
            _possessor.transform.position = pos;
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to update camera position: " + e);
        }
    }

    private void ApplyPossessorMeshVisibility(bool active)
    {
        try
        {
            var meshActive = !active;

            _possessor.gameObject.transform.Find("Capsule")?.gameObject.SetActive(meshActive);
            _possessor.gameObject.transform.Find("Sphere1")?.gameObject.SetActive(meshActive);
            _possessor.gameObject.transform.Find("Sphere2")?.gameObject.SetActive(meshActive);
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to update possessor mesh visibility: " + e);
        }
    }

    private void ApplyAutoWorldScale(bool active)
    {
        if (!active)
        {
            if (_originalWorldScale != 0f && SuperController.singleton.worldScale != _originalWorldScale)
            {
                SuperController.singleton.worldScale = _originalWorldScale;
                _originalWorldScale = 0f;
            }
            return;
        }

        if (!_autoWorldScaleJSON.val) return;

        if (_originalWorldScale == 0f)
        {
            _originalWorldScale = SuperController.singleton.worldScale;
        }

        var eyes = _person.GetComponentsInChildren<LookAtWithLimits>();
        var lEye = eyes.FirstOrDefault(eye => eye.name == "lEye");
        var rEye = eyes.FirstOrDefault(eye => eye.name == "rEye");
        if (lEye == null || rEye == null)
            return;
        var atomEyeDistance = Vector3.Distance(lEye.transform.position, rEye.transform.position);

        var rig = GameObject.FindObjectOfType<OVRCameraRig>();
        if (rig == null)
            return;
        var rigEyesDistance = Vector3.Distance(rig.leftEyeAnchor.transform.position, rig.rightEyeAnchor.transform.position);

        var scale = atomEyeDistance / rigEyesDistance;
        var worldScale = SuperController.singleton.worldScale * scale;

        if (SuperController.singleton.worldScale != worldScale)
            SuperController.singleton.worldScale = worldScale;

        var yAdjust = _possessor.autoSnapPoint.position.y - _headControl.possessPoint.position.y;

        if (yAdjust != 0)
            SuperController.singleton.playerHeightAdjust = SuperController.singleton.playerHeightAdjust - yAdjust;
    }
}
