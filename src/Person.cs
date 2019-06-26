#define POV_DIAGNOSTICS
using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace Acidbubbles.ImprovedPoV
{
    /// <summary>
    /// Improved PoV Version 0.0.0
    /// Possession that actually feels right.
    /// Source: https://github.com/acidbubbles/vam-improved-pov
    /// </summary>
    public class Person : MVRScript
    {

        private Atom _person;
        private Camera _mainCamera;
        private Possessor _possessor;
        private IStrategy _strategyImpl;
        private DAZSkinV2 _skin;

        private FreeControllerV3 _headControl;
        private JSONStorableFloat _cameraRecess;
        private JSONStorableFloat _cameraUpDown;
        private JSONStorableFloat _clipDistance;
        private JSONStorableBool _possessedOnly;
        private JSONStorableStringChooser _strategy;

        // Whether the current configuration is valid, which otherwise prevents enabling and using the plugin
        private bool _valid;
        // Whether the PoV effects are currently active, i.e. in possession mode
        private bool _active;
        // Whether the script is currently enabled, i.e. not destroyed or disabled in Person/Plugin
        private bool _enabled;
        // Whether operations could not be completed because some items were not yet ready
        private bool _dirty;

        public override void Init()
        {
            try
            {
                if (containingAtom?.type != "Person")
                {
                    _valid = false;
                    SuperController.LogError($"Please apply the ImprovedPoV plugin to the 'Person' atom you wish to possess. Currently applied on '{containingAtom.type}'.");
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
                _strategyImpl = new NoStrategy();

                InitControls();
                _valid = true;
                _enabled = true;

                // TODO: Figure out when's the best time to actually run this script
                // TODO: Can we detect whenever an atom is added?
                MirrorReflectionReplacer.Attach();
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to initialize Improved PoV: " + e);
            }
        }

        public void OnEnable()
        {
            try
            {
                if (!_valid || _enabled) return;

                _enabled = true;
                ApplyAll();
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to enable Improved PoV: " + e);
            }
        }

        public void OnDisable()
        {
            try
            {
                if (!_enabled) return;

                _active = false;
                _enabled = false;
                ApplyAll();
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to disable Improved PoV: " + e);
            }
        }

        public void OnDestroy()
        {
            OnDisable();
        }

        public void Update()
        {
            if (!_enabled) return;

            var possessed = _headControl.possessed || !_possessedOnly.val;

            if (!_active && possessed)
            {
                _active = true;
                ApplyAll();
            }
            else if (_active && !possessed)
            {
                _active = false;
                ApplyAll();
            }
            else if (_dirty)
            {
                _dirty = false;
                ApplyAll();
            }
        }

        private void InitControls()
        {
            try
            {
                _cameraRecess = new JSONStorableFloat("Camera Recess", 0.06f, 0f, .2f, false);
                RegisterFloat(_cameraRecess);
                var recessSlider = CreateSlider(_cameraRecess, false);
                recessSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    ApplyCameraPosition();
                });

                _cameraUpDown = new JSONStorableFloat("Camera UpDown", 0f, -0.2f, 0.2f, false);
                RegisterFloat(_cameraUpDown);
                var upDownSlider = CreateSlider(_cameraUpDown, false);
                upDownSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    ApplyCameraPosition();
                });

                _clipDistance = new JSONStorableFloat("Clip Distance", 0.01f, 0.01f, .2f, false);
                RegisterFloat(_clipDistance);
                var clipSlider = CreateSlider(_clipDistance, false);
                clipSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    ApplyCameraPosition();
                });

                var possessedOnlyDefaultValue = true;
#if (POV_DIAGNOSTICS)
                // NOTE: Easier to test when it's always on
                possessedOnlyDefaultValue = false;
#endif
                _possessedOnly = new JSONStorableBool("Possessed Only", possessedOnlyDefaultValue);
                RegisterBool(_possessedOnly);
                var possessedOnlyCheckbox = CreateToggle(_possessedOnly, true);
                possessedOnlyCheckbox.toggle.onValueChanged.AddListener(delegate (bool val)
                {
                    _dirty = true;
                });

                var strategies = new List<string> { NoStrategy.Name, MaterialsEnabledStrategy.Name, ShaderStrategy.Name };
                _strategy = new JSONStorableStringChooser("Strategy", strategies, ShaderStrategy.Name, "Strategy");
                RegisterStringChooser(_strategy);
                var strategyPopup = CreatePopup(_strategy, true);
                strategyPopup.popup.onValueChangeHandlers = new UIPopup.OnValueChange(delegate (string val)
                {
                    // TODO: Why is this necessary?
                    _strategy.val = val;
                    _dirty = true;
                });
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to register controls: " + e);
            }
        }

        private void ApplyAll()
        {
            ApplyCameraPosition();
            ApplyFaceStrategy();
            ApplyPossessorMeshVisibility();
        }

        private void ApplyFaceStrategy()
        {
            DAZSkinV2 skin;

            try
            {
                skin = _person.GetComponentInChildren<DAZCharacterSelector>()?.selectedCharacter?.skin;

                if (skin == null)
                {
                    _dirty = true;
                    return;
                }

                if (!_active)
                {
                    if (_strategyImpl.Name != NoStrategy.Name)
                    {
                        _strategyImpl.Restore(skin);
                        _strategyImpl = new NoStrategy();
                    }
                    return;
                }

                if (_strategyImpl.Name != _strategy.val || skin != _skin)
                {
                    _strategyImpl.Restore(skin);
                    _strategyImpl = StrategyFactory.CreateStrategy(_strategy.val);
                    _skin = skin;
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to initialize and/or restore strategy: " + e);
                return;
            }

            try
            {
                _strategyImpl.Apply(skin);
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to execute strategy " + _strategyImpl.Name + ": " + e);
            }
        }

        private void ApplyCameraPosition()
        {
            try
            {
                _mainCamera.nearClipPlane = _active ? _clipDistance.val : 0.01f;

                var cameraRecess = _active ? _cameraRecess.val : 0;
                var cameraUpDown = _active ? _cameraUpDown.val : 0;
                var pos = _possessor.transform.position;
                _mainCamera.transform.position = pos - _mainCamera.transform.rotation * Vector3.forward * cameraRecess - _mainCamera.transform.rotation * Vector3.down * cameraUpDown;
                _possessor.transform.position = pos;
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to update camera position: " + e);
            }
        }

        private void ApplyPossessorMeshVisibility()
        {
            try
            {
                var meshActive = !_active;

                _possessor.gameObject.transform.Find("Capsule")?.gameObject.SetActive(meshActive);
                _possessor.gameObject.transform.Find("Sphere1")?.gameObject.SetActive(meshActive);
                _possessor.gameObject.transform.Find("Sphere2")?.gameObject.SetActive(meshActive);
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to update possessor mesh visibility: " + e);
            }
        }

    }
}