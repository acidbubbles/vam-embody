#define POV_DIAGNOSTICS
using System;
using System.Linq;
using UnityEngine;

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
        private FreeControllerV3 _headControl;

        private JSONStorableFloat _cameraRecessJSON;
        private JSONStorableFloat _cameraUpDownJSON;
        private JSONStorableFloat _clipDistanceJSON;
        private JSONStorableBool _possessedOnlyJSON;
        private JSONStorableBool _skinStrategyJSON;
        private JSONStorableBool _hairStrategyJSON;

        private SkinStrategy _skinStrategy;
        private HairStrategy _hairStrategy;

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

                InitControls();
                _valid = true;
                _enabled = true;
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

            var possessed = _headControl.possessed || !_possessedOnlyJSON.val;

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
            else
            {
                // TODO: Check if the skin changed
                /*
                if(_skinStrategy != null)
                    _skinStrategy.Update();
                if(_hairStrategy != null)
                    _hairStrategy.Update();
                */
            }
        }

        private void InitControls()
        {
            try
            {
                {
                    _cameraRecessJSON = new JSONStorableFloat("Camera Recess", 0.06f, 0f, .2f, false);
                    RegisterFloat(_cameraRecessJSON);
                    var recessSlider = CreateSlider(_cameraRecessJSON, false);
                    recessSlider.slider.onValueChanged.AddListener(delegate (float val)
                    {
                        ApplyCameraPosition();
                    });
                }

                {
                    _cameraUpDownJSON = new JSONStorableFloat("Camera UpDown", 0f, -0.2f, 0.2f, false);
                    RegisterFloat(_cameraUpDownJSON);
                    var upDownSlider = CreateSlider(_cameraUpDownJSON, false);
                    upDownSlider.slider.onValueChanged.AddListener(delegate (float val)
                    {
                        ApplyCameraPosition();
                    });
                }

                {
                    _clipDistanceJSON = new JSONStorableFloat("Clip Distance", 0.01f, 0.01f, .2f, false);
                    RegisterFloat(_clipDistanceJSON);
                    var clipSlider = CreateSlider(_clipDistanceJSON, false);
                    clipSlider.slider.onValueChanged.AddListener(delegate (float val)
                    {
                        ApplyCameraPosition();
                    });
                }

                {
                    var possessedOnlyDefaultValue = true;
#if (POV_DIAGNOSTICS)
                    // NOTE: Easier to test when it's always on
                    possessedOnlyDefaultValue = false;
#endif
                    _possessedOnlyJSON = new JSONStorableBool("Possessed Only", possessedOnlyDefaultValue);
                    RegisterBool(_possessedOnlyJSON);
                    var possessedOnlyCheckbox = CreateToggle(_possessedOnlyJSON, true);
                    possessedOnlyCheckbox.toggle.onValueChanged.AddListener(delegate (bool val)
                    {
                        _dirty = true;
                    });
                }

                {
                    _skinStrategyJSON = new JSONStorableBool("Hide Face", true);
                    RegisterBool(_skinStrategyJSON);
                    var skinStrategyPopup = CreateToggle(_skinStrategyJSON, true);
                    skinStrategyPopup.toggle.onValueChanged.AddListener(delegate (bool val)
                    {
                        _dirty = true;
                    });
                }

                {
                    _hairStrategyJSON = new JSONStorableBool("Hide Hair", true);
                    RegisterBool(_hairStrategyJSON);
                    var hairStrategyPopup = CreateToggle(_hairStrategyJSON, true);
                    hairStrategyPopup.toggle.onValueChanged.AddListener(delegate (bool val)
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

        private void ApplyAll()
        {
            var selector = _person.GetComponentInChildren<DAZCharacterSelector>();

            // Try again next frame
            if (selector == null || selector.selectedCharacter == null)
            {
                _dirty = true;
                return;
            }

            ApplyCameraPosition();
            ApplyPossessorMeshVisibility();

            var renderers = _person.GetComponentsInChildren<Renderer>();

            if (UpdateBehavior(ref _skinStrategy, _active && _skinStrategyJSON.val, renderers))
                _skinStrategy.Configure(selector.selectedCharacter.skin);

            if (UpdateBehavior(ref _hairStrategy, _active && _hairStrategyJSON.val, renderers))
                _hairStrategy.Configure(selector.selectedHairGroup);
        }

        private bool UpdateBehavior<T>(ref T behavior, bool active, Renderer[] renderers)
         where T : MonoBehaviour
        {
            if (behavior == null && active)
            {
                var hairRenderer = renderers.FirstOrDefault(r => r.gameObject.name == "Render");
                if (hairRenderer == null) throw new NullReferenceException("Did not find the hair Render");
                behavior = hairRenderer.gameObject.AddComponent<T>();
                if (behavior == null) throw new NullReferenceException("Could not add the hair strategy");
                return true;
            }

            if (behavior != null && !active)
            {
                Destroy(behavior);
                behavior = null;
            }

            return false;
        }

        private void ApplyCameraPosition()
        {
            try
            {
                _mainCamera.nearClipPlane = _active ? _clipDistanceJSON.val : 0.01f;

                var cameraRecess = _active ? _cameraRecessJSON.val : 0;
                var cameraUpDown = _active ? _cameraUpDownJSON.val : 0;
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