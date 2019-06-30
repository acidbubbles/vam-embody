#define POV_DIAGNOSTICS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    private DAZCharacterSelector _selector;
    private JSONStorableFloat _cameraRecessJSON;
    private JSONStorableFloat _cameraUpDownJSON;
    private JSONStorableFloat _clipDistanceJSON;
    private JSONStorableBool _possessedOnlyJSON;
    private JSONStorableBool _hideSkinJSON;
    private JSONStorableBool _hideHair;

    private SkinBehavior _skinBehavior;
    private HairBehavior _hairBehavior;
    // For change detection purposes
    private DAZCharacter _character;
    private DAZHairGroup _hair;


    // Whether the PoV effects are currently active, i.e. in possession mode
    private bool _lastActive;
    // Whether operations could not be completed because some items were not yet ready
    private bool _dirty;

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

    private void OnPostRender(Camera cam)
    {
        if (cam.name != "MonitorRig") return;
        if (_skinBehavior != null)
            _skinBehavior.OnRenderObject2();
        if (_hairBehavior != null)
            _hairBehavior.OnWillRenderObject2();
    }

    private void OnPreRender(Camera cam)
    {
        if (cam.name != "MonitorRig") return;
        if (_skinBehavior != null)
            _skinBehavior.OnWillRenderObject2();
        if (_hairBehavior != null)
            _hairBehavior.OnWillRenderObject2();
    }

    private void InitControls()
    {
        try
        {
            {
                _cameraRecessJSON = new JSONStorableFloat("Camera Recess", 0.054f, 0f, .2f, false);
                RegisterFloat(_cameraRecessJSON);
                var recessSlider = CreateSlider(_cameraRecessJSON, false);
                recessSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    ApplyCameraPosition(_lastActive);
                });
            }

            {
                _cameraUpDownJSON = new JSONStorableFloat("Camera UpDown", 0f, -0.2f, 0.2f, false);
                RegisterFloat(_cameraUpDownJSON);
                var upDownSlider = CreateSlider(_cameraUpDownJSON, false);
                upDownSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    ApplyCameraPosition(_lastActive);
                });
            }

            {
                _clipDistanceJSON = new JSONStorableFloat("Clip Distance", 0.01f, 0.01f, .2f, false);
                RegisterFloat(_clipDistanceJSON);
                var clipSlider = CreateSlider(_clipDistanceJSON, false);
                clipSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    ApplyCameraPosition(_lastActive);
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
                _hideSkinJSON = new JSONStorableBool("Hide face", true);
                RegisterBool(_hideSkinJSON);
                var skinStrategyPopup = CreateToggle(_hideSkinJSON, true);
                skinStrategyPopup.toggle.onValueChanged.AddListener(delegate (bool val)
                {
                    _dirty = true;
                });
            }

            {
                _hideHair = new JSONStorableBool("Hide hair", true);
                RegisterBool(_hideHair);
                var hairStrategyPopup = CreateToggle(_hideHair, true);
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

    public void OnDisable()
    {
        try
        {
            _lastActive = false;
            _dirty = false;
            ApplyAll(false);
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to disable Improved PoV: " + e);
        }
    }

    public void OnDestroy()
    {
        Camera.onPreRender -= OnPreRender;
        Camera.onPostRender -= OnPostRender;
        OnDisable();
    }

    public void Update()
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
        else if (_lastActive && _selector.selectedCharacter != _character && _skinBehavior != null)
        {
            _skinBehavior.OnDestroy();
            _skinBehavior = null;
            ApplyAll(true);
        }
        else if (_lastActive && _selector.selectedHairGroup != _hair)
        {
            SuperController.LogMessage("Hair changed");
            _hairBehavior.OnDestroy();
            _hairBehavior = null;
            ApplyAll(true);
        }
    }

    bool once;
    private void ApplyAll(bool active)
    {
        // Try again next frame
        if (_selector.selectedCharacter?.skin == null)
        {
            _dirty = true;
            return;
        }

        ApplyCameraPosition(active);
        ApplyPossessorMeshVisibility(active);

        if (UpdateBehavior(ref _skinBehavior, active && _hideSkinJSON.val))
        {
            if (!_skinBehavior.Configure(_selector.selectedCharacter.skin))
                _skinBehavior = null;
            _character = _selector.selectedCharacter;
        }

        if (UpdateBehavior(ref _hairBehavior, active && _hideHair.val))
        {
            if (!_hairBehavior.Configure(_selector.selectedHairGroup))
                _hairBehavior = null;
            _hair = _selector.selectedHairGroup;
        }
    }

    private bool UpdateBehavior<T>(ref T behavior, bool active)
     where T : IBehavior, new()
    {
        if (behavior == null && active)
        {
            behavior = new T();
            return true;
        }

        if (behavior != null && !active)
        {
            behavior.OnDestroy();
            behavior = default(T);
        }

        return false;
    }

    private void ApplyCameraPosition(bool active)
    {
        try
        {
            _mainCamera.nearClipPlane = active ? _clipDistanceJSON.val : 0.01f;

            var cameraRecess = active ? _cameraRecessJSON.val : 0;
            var cameraUpDown = active ? _cameraUpDownJSON.val : 0;
            var pos = _possessor.transform.position;
            _mainCamera.transform.position = pos - _mainCamera.transform.rotation * Vector3.forward * cameraRecess - _mainCamera.transform.rotation * Vector3.down * cameraUpDown;
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

    public interface IBehavior
    {
        void OnDestroy();
        void OnWillRenderObject2();
        void OnRenderObject2();
    }

    public class SkinBehavior : IBehavior
    {
        public class SkinShaderMaterialReference
        {
            public Material material;
            public Shader originalShader;
            public float originalAlphaAdjust;
            public Color originalColor;
            public Color originalSpecColor;
            public int originalRenderQueue;

            public static SkinShaderMaterialReference FromMaterial(Material material)
            {
                var materialRef = new SkinShaderMaterialReference();
                materialRef.material = material;
                materialRef.originalShader = material.shader;
                materialRef.originalAlphaAdjust = material.GetFloat("_AlphaAdjust");
                materialRef.originalColor = material.GetColor("_Color");
                materialRef.originalSpecColor = material.GetColor("_SpecColor");
                materialRef.originalRenderQueue = material.renderQueue;
                return materialRef;
            }
        }

        public static readonly string[] MaterialsToHide = new[]
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

        public static IList<Material> GetMaterialsToHide(DAZSkinV2 skin)
        {
#if (POV_DIAGNOSTICS)
            if (skin == null) throw new NullReferenceException("skin is null");
            if (skin.GPUmaterials == null) throw new NullReferenceException("skin materials are null");
#endif

            var materials = new List<Material>(MaterialsToHide.Length);

            foreach (var material in skin.GPUmaterials)
            {
                if (!MaterialsToHide.Any(materialToHide => material.name.StartsWith(materialToHide)))
                    continue;

                materials.Add(material);
            }

#if (POV_DIAGNOSTICS)
            // NOTE: Tear is not on all models
            if (materials.Count < MaterialsToHide.Length - 1)
                throw new Exception("Not enough materials found to hide. List: " + string.Join(", ", skin.GPUmaterials.Select(m => m.name).ToArray()));
#endif

            return materials;
        }

        private static Dictionary<string, Shader> ReplacementShaders = new Dictionary<string, Shader>
            {
                // Opaque materials
                { "Custom/Subsurface/GlossCullComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossSeparateAlphaComputeBuff") },
                { "Custom/Subsurface/GlossNMCullComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossNMSeparateAlphaComputeBuff") },
                { "Custom/Subsurface/GlossNMDetailCullComputeBuff", Shader.Find("Custom/Subsurface/TransparentGlossNMDetailNoCullSeparateAlphaComputeBuff") },
                { "Custom/Subsurface/CullComputeBuff", Shader.Find("Custom/Subsurface/TransparentSeparateAlphaComputeBuff") },

                // Transparent materials
                { "Custom/Subsurface/TransparentGlossNoCullSeparateAlphaComputeBuff", null },
                { "Custom/Subsurface/TransparentGlossComputeBuff", null },
                { "Custom/Subsurface/TransparentComputeBuff", null },
                { "Custom/Subsurface/AlphaMaskComputeBuff", null },
                { "Marmoset/Transparent/Simple Glass/Specular IBLComputeBuff", null },
            };

        private DAZSkinV2 _skin;
        private List<SkinShaderMaterialReference> _materialRefs;

        public bool Configure(DAZSkinV2 skin)
        {
            _skin = skin;
            _materialRefs = new List<SkinShaderMaterialReference>();

            foreach (var material in GetMaterialsToHide(skin))
            {
#if (IMPROVED_POV)
                if(material == null)
                    throw new InvalidOperationException("Attempts to apply the shader strategy on a destroyed material.");

                if (material.GetInt(SkinShaderMaterialReference.ImprovedPovEnabledShaderKey) == 1)
                    throw new InvalidOperationException("Attempts to apply the shader strategy on a skin that already has the plugin enabled (shader key).");
#endif

                var materialInfo = SkinShaderMaterialReference.FromMaterial(material);

                Shader shader;
                if (!ReplacementShaders.TryGetValue(material.shader.name, out shader))
                    SuperController.LogError("Missing replacement shader: '" + material.shader.name + "'");

                if (shader != null) material.shader = shader;

                _materialRefs.Add(materialInfo);
            }

            // This is a hack to force a refresh of the shaders cache
            skin.BroadcastMessage("OnApplicationFocus", true);
            return true;
        }

        public void OnDestroy()
        {
            foreach (var material in _materialRefs)
                material.material.shader = material.originalShader;

            _materialRefs = null;

            // This is a hack to force a refresh of the shaders cache
            _skin.BroadcastMessage("OnApplicationFocus", true);
        }

        public void OnWillRenderObject2()
        {
            if (Camera.current.name == "MonitorRig")
            {
                foreach (var materialRef in _materialRefs)
                {
                    var material = materialRef.material;
                    material.SetFloat("_AlphaAdjust", -1f);
                    material.SetColor("_Color", new Color(0f, 0f, 0f, 0f));
                    material.SetColor("_SpecColor", new Color(0f, 0f, 0f, 0f));
                }
            }
            /*else{
                                foreach (var materialRef in _materialRefs)
                {
                    var material = materialRef.material;
                    material.SetFloat("_AlphaAdjust", materialRef.originalAlphaAdjust);
                    material.SetColor("_Color", materialRef.originalColor);
                    material.SetColor("_SpecColor", materialRef.originalSpecColor);
                }
            }*/
        }

        public void OnRenderObject2()
        {
            if (Camera.current.name == "MonitorRig")
            {
                foreach (var materialRef in _materialRefs)
                {
                    var material = materialRef.material;
                    material.SetFloat("_AlphaAdjust", materialRef.originalAlphaAdjust);
                    material.SetColor("_Color", materialRef.originalColor);
                    material.SetColor("_SpecColor", materialRef.originalSpecColor);
                }
            }
            //  else{
            //                     foreach (var materialRef in _materialRefs)
            //     {
            //         var material = materialRef.material;
            //         material.SetFloat("_AlphaAdjust", -1f);
            //         material.SetColor("_Color", new Color(0f, 0f, 0f, 0f));
            //         material.SetColor("_SpecColor", new Color(0f, 0f, 0f, 0f));
            //     }
            // }
        }
    }

    public class HairBehavior : IBehavior
    {
        private Material _material;
        private float _standWidth;

        public bool Configure(DAZHairGroup hair)
        {
            // NOTE: Only applies to SimV2 hair
            _material = hair.GetComponentInChildren<MeshRenderer>()?.material;
            if (_material == null)
                return false;
            _standWidth = _material.GetFloat("_StandWidth");
            return true;
        }

        public void OnDestroy()
        {
            _material = null;
        }

        int ctr;
        public void OnWillRenderObject2()
        {
            if (Camera.current.name == "MonitorRig")
                _material.SetFloat("_StandWidth", 0f);
            // else
            //                 _material.SetFloat("_StandWidth", _standWidth);
        }

        public void OnRenderObject2()
        {
            if (Camera.current.name == "MonitorRig")
                _material.SetFloat("_StandWidth", _standWidth);
            // else
            //                 _material.SetFloat("_StandWidth", 0f);
        }
    }
}
