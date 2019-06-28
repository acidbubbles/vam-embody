#define POV_DIAGNOSTICS
using System;
using System.Linq;
using UnityEngine;
using Acidbubbles.ImprovedPoV.Skin;
using Acidbubbles.ImprovedPoV.Hair;

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
        private JSONStorableStringChooser _skinStrategyJSON;
        private JSONStorableStringChooser _hairStrategyJSON;

        private MemoizedPerson _memoized;
        private IStrategy _skinStrategy;
        private IStrategy _hairStrategy;

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
                _skinStrategy = new SkinStrategyFactory().None();
                _hairStrategy = new HairStrategyFactory().None();

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
                    _skinStrategyJSON = new JSONStorableStringChooser("Skin Strategy", SkinStrategyFactory.Names, SkinStrategyFactory.Default, "Skin Strategy");
                    RegisterStringChooser(_skinStrategyJSON);
                    var skinStrategyPopup = CreatePopup(_skinStrategyJSON, true);
                    skinStrategyPopup.popup.onValueChangeHandlers = new UIPopup.OnValueChange(delegate (string val)
                    {
                        // TODO: Why is this necessary?
                        _skinStrategyJSON.val = val;
                        _dirty = true;
                    });
                }

                {
                    _hairStrategyJSON = new JSONStorableStringChooser("Hair Strategy", HairStrategyFactory.Names, HairStrategyFactory.Default, "Hair Strategy");
                    RegisterStringChooser(_hairStrategyJSON);
                    var hairStrategyPopup = CreatePopup(_hairStrategyJSON, true);
                    hairStrategyPopup.popup.onValueChangeHandlers = new UIPopup.OnValueChange(delegate (string val)
                    {
                        // TODO: Why is this necessary?
                        _hairStrategyJSON.val = val;
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

            // NOTE: We always regenerate from scratch here. There may not be any gain from caching this in practice.
            var skin = selector.selectedCharacter.skin;
            var hair = selector.selectedHairGroup;
            _memoized = new MemoizedPerson(skin, hair);

            ApplyCameraPosition();
            ApplyPossessorMeshVisibility();
            _skinStrategy = ApplyStrategy(_skinStrategy, _skinStrategyJSON.val, new SkinStrategyFactory(), _memoized);
            _hairStrategy = ApplyStrategy(_hairStrategy, _hairStrategyJSON.val, new HairStrategyFactory(), _memoized);
        }

        private IStrategy ApplyStrategy(IStrategy strategy, string selected, IStrategyFactory strategyFactory, MemoizedPerson memoized)
        {
            try
            {
                strategy?.Restore();

                if (!_active)
                    return strategyFactory.None();

                if (strategy?.Name != selected)
                    strategy = strategyFactory.Create(selected);
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to initialize and/or restore strategy: " + e);
                return strategyFactory.None();
            }

            try
            {
                strategy.Apply(memoized);
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to execute strategy " + _skinStrategy.Name + ": " + e);
            }

            return strategy;
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