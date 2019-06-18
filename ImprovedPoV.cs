#define POV_DIAGNOSTICS
using System;
using System.Linq;
using UnityEngine;
#if (POV_DIAGNOSTICS)
using System.Collections.Generic;
#endif

namespace Acidbubbles.VaM.Plugins
{
    /// <summary>
    /// Improved PoV handling so that possession actually feels right.
    /// Source: https://github.com/acidbubbles/vam-improved-pov
    /// Credits to https://www.reddit.com/user/ShortRecognition/ for the original HeadPossessDepthFix from which
    /// this plugin took heavy inspiration: https://www.reddit.com/r/VaMscenes/comments/9z9b71/script_headpossessdepthfix/
    /// Thanks for Marko, VAMDeluxe, LFE for your previous help on Discord
    /// </summary>
    public class ImprovedPoV : MVRScript
    {
        private const string PluginLabel = "Improved PoV Plugin - by Acidbubbles";

        private static readonly string[] MaterialsToHide = new[]
        {
            "Lacrimals",
            "Pupils",
            "Lips",
            "Gums",
            "Irises",
            "Teeth",
            "Face",
            "InnerMouth",
            "Tongue",
            "EyeReflection",
            "Nostrils",
            "Cornea",
            "Eyelashes",
            "Sclera",
            "Ears",
            "Tear"
        };

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
                if (string.IsNullOrEmpty(pluginLabelJSON.val))
                    pluginLabelJSON.val = PluginLabel;

                if (containingAtom.type != "Person")
                {
                    _valid = false;
                    SuperController.LogError($"Please apply the ImprovedPoV plugin to the 'Person' atom you wish to possess. Currently applied on '{containingAtom.type}'.");
                    return;
                }

                _person = containingAtom;
                _mainCamera = CameraTarget.centerTarget.targetCamera;
                _possessor = SuperController.FindObjectsOfType(typeof(Possessor)).Where(p => p.name == "CenterEye").Select(p => p as Possessor).First();
                _headControl = (FreeControllerV3)_person.GetStorableByID("headControl");
                _strategyImpl = new NoStrategy();

                InitControls();
                _valid = true;
                _enabled = true;
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to initialize Improved PoV: " + e);
            }
        }

        public void OnDisable()
        {
            if (!_enabled) return;

            _active = false;
            _enabled = false;
            ApplyAll();
        }

        public void OnEnable()
        {
            if (!_valid || _enabled) return;

            _enabled = true;
            ApplyAll();
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

                _possessedOnly = new JSONStorableBool("Possessed Only", true);
                RegisterBool(_possessedOnly);
                var possessedOnlyCheckbox = CreateToggle(_possessedOnly, true);
                possessedOnlyCheckbox.toggle.onValueChanged.AddListener(delegate (bool val)
                {
                    ApplyAll();
                });

                var strategies = new List<string> { NoStrategy.Name, MaterialsEnabledStrategy.Name, ShaderStrategy.Name };
                _strategy = new JSONStorableStringChooser("Strategy", strategies, MaterialsEnabledStrategy.Name, "Strategy");
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

        private DAZSkinV2 GetSkin()
        {
            if (_skin != null) return _skin;
            var skin = _person.GetComponentInChildren<DAZCharacterSelector>()?.selectedCharacter?.skin;
            _skin = skin;
            return skin;
        }

        private void ApplyFaceStrategy()
        {
            DAZSkinV2 skin;

            try
            {
                skin = GetSkin();
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

                if (_strategyImpl.Name != _strategy.val)
                {
                    _strategyImpl.Restore(skin);
                    _strategyImpl = CreateStrategy(_strategy.val);
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

        private static IStrategy CreateStrategy(string val)
        {
            switch (val)
            {
                case MaterialsEnabledStrategy.Name:
                    return new MaterialsEnabledStrategy();
                case ShaderStrategy.Name:
                    return new ShaderStrategy();
                case NoStrategy.Name:
                    return new NoStrategy();
                default:
                    throw new InvalidOperationException("Invalid strategy: '" + val + "'");
            }
        }

        public interface IStrategy
        {
            string Name { get; }
            void Apply(DAZSkinV2 skin);
            void Restore(DAZSkinV2 skin);
        }

        public class ShaderStrategy : IStrategy
        {
            public const string Name = "Shaders (allows mirrors)";

            private static Dictionary<string, Shader> ReplacementShaders = new Dictionary<string, Shader>
            {
                // Opaque materials
                { "Custom/Subsurface/GlossCullComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossSeparateAlphaComputeBuff") },
                { "Custom/Subsurface/GlossNMCullComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossNMSeparateAlphaComputeBuff") },
                { "Custom/Subsurface/CullComputeBuff", Shader.Find("Custom/Subsurface/TransparentSeparateAlphaComputeBuff") },

                // Transparent materials
                { "Custom/Subsurface/TransparentGlossNoCullSeparateAlphaComputeBuff", null },
                { "Custom/Subsurface/TransparentGlossComputeBuff", null },
                { "Custom/Subsurface/AlphaMaskComputeBuff", null },
                { "Marmoset/Transparent/Simple Glass/Specular IBLComputeBuff", null },
            };

            private GameObject _previousMaterialsContainer;

            string IStrategy.Name
            {
                get { return Name; }
            }

            public void Apply(DAZSkinV2 skin)
            {
                Apply(skin, true);
            }

            public void Apply(DAZSkinV2 skin, bool save)
            {
                // Check if already applied
                if (_previousMaterialsContainer != null)
                    return;

                _previousMaterialsContainer = new GameObject("ImprovedPoV container for skin " + skin.GetInstanceID());
                var previousMaterialsRenderer = _previousMaterialsContainer.AddComponent<MeshRenderer>();
                if (previousMaterialsRenderer == null) throw new NullReferenceException("Failed to add the MeshRenderer component");
                var previousMaterials = new List<Material>();

                foreach (var material in GetMaterialsToHide(skin))
                {
                    var materialClone = new Material(material);
                    previousMaterials.Add(materialClone);

                    Shader shader;
                    if (!ReplacementShaders.TryGetValue(material.shader.name, out shader))
                        SuperController.LogError("Missing replacement shader: '" + material.shader.name + "'");
                    if(shader != null)
                        material.shader = shader;
                    material.SetFloat("_AlphaAdjust", -1f);
                    material.SetColor("_Color", new Color(0f, 0f, 0f, 1f));
                    material.SetColor("_SpecColor", new Color(0f, 0f, 0f, 1f));
                }

                previousMaterialsRenderer.materials = previousMaterials.ToArray();

                // This is a hack to force a refresh of the shaders cache
                skin.BroadcastMessage("OnApplicationFocus", true);
            }

            public void Restore(DAZSkinV2 skin)
            {
                // Already restored (abnormal)
                if (_previousMaterialsContainer == null) throw new InvalidOperationException("Attempt to Restore but the previous material container does not exist");

                var previousMaterials = _previousMaterialsContainer.GetComponent<MeshRenderer>().materials;
                if (previousMaterials == null) throw new NullReferenceException("previousMaterials");

                foreach (var material in GetMaterialsToHide(skin))
                {
                    // NOTE: The new material would be called "Eyes (Instance)"
                    var previousMaterial = previousMaterials.FirstOrDefault(m => m.name.StartsWith(material.name));
                    if (previousMaterial == null) throw new NullReferenceException("Failed to find material " + material.name + " in previous materials list: " + string.Join(", ", previousMaterials.Select(m => m.name).ToArray()));
                    material.shader = previousMaterial.shader;
                    material.SetFloat("_AlphaAdjust", previousMaterial.GetFloat("_AlphaAdjust"));
                    material.SetColor("_Color", previousMaterial.GetColor("_Color"));
                    material.SetColor("_SpecColor", previousMaterial.GetColor("_SpecColor"));
                }
                Destroy(_previousMaterialsContainer);

                // This is a hack to force a refresh of the shaders cache
                skin.BroadcastMessage("OnApplicationFocus", true);
            }

            private IList<Material> GetMaterialsToHide(DAZSkinV2 skin)
            {
                var materials = new List<Material>(MaterialsToHide.Length);

                foreach (var material in skin.GPUmaterials)
                {
                    if (!MaterialsToHide.Any(materialToHide => material.name.StartsWith(materialToHide)))
                        continue;

                    materials.Add(material);
                }

                return materials;
            }
        }

        public class MaterialsEnabledStrategy : IStrategy
        {
            public const string Name = "Materials Enabled (performance)";

            string IStrategy.Name
            {
                get { return Name; }
            }

            public void Apply(DAZSkinV2 skin)
            {
                UpdateMaterialsEnabled(skin, false);
            }

            public void Restore(DAZSkinV2 skin)
            {
                UpdateMaterialsEnabled(skin, true);
            }

            public void UpdateMaterialsEnabled(DAZSkinV2 skin, bool enabled)
            {

                for (int i = 0; i < skin.GPUmaterials.Length; i++)
                {
                    Material mat = skin.GPUmaterials[i];
                    if (MaterialsToHide.Any(materialToHide => mat.name.StartsWith(materialToHide)))
                    {
                        skin.materialsEnabled[i] = enabled;
                    }
                }
            }
        }

        public class NoStrategy : IStrategy
        {
            public const string Name = "None (face mesh visible)";

            string IStrategy.Name
            {
                get { return Name; }
            }

            public void Apply(DAZSkinV2 skin)
            {
            }

            public void Restore(DAZSkinV2 skin)
            {
            }
        }

#if (POV_DIAGNOSTICS)

        private static void DumpSceneGameObjects()
        {
            foreach (var o in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                PrintTree(1, o.gameObject);
            }
        }

        private static void PrintTree(int indent, GameObject o)
        {
            var exclude = new[] { "Morph", "Cloth", "Hair" };
            if (exclude.Any(x => o.gameObject.name.Contains(x))) { return; }
            SuperController.LogMessage("|" + new String(' ', indent) + " [" + o.tag + "] " + o.name);
            for (int i = 0; i < o.transform.childCount; i++)
            {
                var under = o.transform.GetChild(i).gameObject;
                PrintTree(indent + 4, under);
            }
        }

        private static string GetDebugHierarchy(GameObject o)
        {
            var items = new List<string>(new[] { o.name });
            GameObject parent = o;
            for (int i = 0; i < 100; i++)
            {
                parent = parent.transform.parent?.gameObject;
                if (parent == null || parent == o) break;
                items.Insert(0, parent.gameObject.name);
            }
            return string.Join(" -> ", items.ToArray());
        }

#endif
    }
}