#define POV_DIAGNOSTICS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Improved PoV Version 0.0.0
/// Possession that actually feels right.
/// Assign this script to a mirror so person's faces with ImprovedPoV_Person are visible in that mirror
/// Source: https://github.com/acidbubbles/vam-improved-pov
/// </summary>
public class ImprovedPoV_Mirror : MVRScript
{
    private GameObject _mirror;
    private ImprovedPoVMirrorReflectionDecorator[] _behaviors;
    private bool _active;

    public override void Init()
    {
        try
        {
            if (containingAtom == null) throw new NullReferenceException("No containing atom");
            _mirror = containingAtom.gameObject;
            _behaviors = ReplaceMirrorScriptAndCreatedObjects();
            OnEnable();
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to initialize Improved PoV Mirror" + e);
        }
    }

    public void OnEnable()
    {
        try
        {
            if (_mirror == null || _behaviors == null || _active) return;

            foreach (var behavior in _behaviors)
            {
                behavior.active = true;
                behavior.ImprovedPoVPersonChanged();
            }

            _active = true;
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to enable Improved PoV Mirror: " + e);
        }
    }

    public void OnDisable()
    {
        try
        {
            if (!_active || _behaviors == null) return;

            foreach (var behavior in _behaviors)
                behavior.active = false;

            _active = false;
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to disable Improved PoV Mirror: " + e);
        }
    }

    public void OnDestroy()
    {
        OnDisable();
    }

    private ImprovedPoVMirrorReflectionDecorator[] ReplaceMirrorScriptAndCreatedObjects()
    {
        var behaviors = new List<ImprovedPoVMirrorReflectionDecorator>();
        foreach (var childMirror in _mirror.GetComponentsInChildren<MirrorReflection>())
        {
            if (childMirror is ImprovedPoVMirrorReflectionDecorator) continue;

            var childMirrorGameObject = childMirror.gameObject;

            var childMirrorInstanceId = childMirror.GetInstanceID();

            var name = childMirror.name;
            var atom = childMirror.containingAtom;
            if (atom != null)
                atom.UnregisterAdditionalStorable(childMirror);
            var newBehavior = childMirrorGameObject.AddComponent<ImprovedPoVMirrorReflectionDecorator>();
            if (newBehavior == null) throw new NullReferenceException("newBehavior");
            newBehavior.CopyFrom(childMirror);
            DestroyImmediate(childMirror);
            newBehavior.name = name;
            if (atom != null)
                atom.RegisterAdditionalStorable(newBehavior);

            var reflectionCameraGameObjectPrefix = "Mirror Refl Camera id" + childMirrorInstanceId + " for ";
            foreach (var childMirrorObject in SceneManager.GetActiveScene().GetRootGameObjects().Where(x => x.name.StartsWith(reflectionCameraGameObjectPrefix)))
            {
                DestroyImmediate(childMirrorObject);
            }

            behaviors.Add(newBehavior);
        }

        return behaviors.ToArray();
    }

#if (POV_DIAGNOSTICS)
    public class Utils
    {
        public static void PrintDebugStatus()
        {
            SuperController.LogMessage("Root objects: " + string.Join("; ", SceneManager.GetActiveScene().GetRootGameObjects().Select(x => x.name).ToArray()));
            SuperController.LogMessage("Original mirrors: " + string.Join("; ", GameObject.FindObjectsOfType<MirrorReflection>().Select(x => GetDebugHierarchy(x.gameObject)).ToArray()));
            SuperController.LogMessage("PoV mirrors: " + string.Join("; ", GameObject.FindObjectsOfType<ImprovedPoVMirrorReflectionDecorator>().Select(x => GetDebugHierarchy(x.gameObject)).ToArray()));
        }

        public static string GetDebugHierarchy(GameObject o)
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
    }
#endif

    public class ImprovedPoVMirrorReflectionDecorator : MirrorReflection
    {
        // Attempts before stopping trying to find a character. Approximating 90 frames per second, for 20 seconds.
        public const int MAX_ATTEMPTS = 90 * 10;

        private bool _isWaitingForMaterials;
        public bool active;
        public void CopyFrom(MirrorReflection original)
        {
            // Copy all public fields
            UITransform = original.UITransform;
            UITransformAlt = original.UITransformAlt;
            altObjectWhenMirrorDisabled = original.altObjectWhenMirrorDisabled;
            containingAtom = original.containingAtom;
            exclude = original.exclude;
            m_ClipPlaneOffset = original.m_ClipPlaneOffset;
            m_ReflectLayers = original.m_ReflectLayers;
            m_UseObliqueClip = original.m_UseObliqueClip;
            needsStore = original.needsStore;
            onlyStoreIfActive = original.onlyStoreIfActive;
            overrideId = original.overrideId;
            renderBackside = original.renderBackside;
            slaveReflection = original.slaveReflection;
            useSameMaterialWhenMirrorDisabled = original.useSameMaterialWhenMirrorDisabled;

            disablePixelLightsJSON = original.GetBoolJSONParam("disablePixelLights");
            if (disablePixelLightsJSON != null)
            {
                RegisterBool(disablePixelLightsJSON);
                disablePixelLightsJSON.setCallbackFunction = SyncDisablePixelLights;
            }
            textureSizeJSON = original.GetStringChooserJSONParam("textureSize");
            if (textureSizeJSON != null)
            {
                RegisterStringChooser(textureSizeJSON);
                textureSizeJSON.setCallbackFunction = SetTextureSizeFromString;
            }
            antiAliasingJSON = original.GetStringChooserJSONParam("antiAliasing");
            if (antiAliasingJSON != null)
            {
                RegisterStringChooser(antiAliasingJSON);
                antiAliasingJSON.setCallbackFunction = SetAntialiasingFromString;
            }
            reflectionOpacityJSON = original.GetFloatJSONParam("reflectionOpacity");
            if (reflectionOpacityJSON != null)
            {
                RegisterFloat(reflectionOpacityJSON);
                reflectionOpacityJSON.setCallbackFunction = SyncReflectionOpacity;
            }
            reflectionBlendJSON = original.GetFloatJSONParam("reflectionBlend");
            if (reflectionBlendJSON != null)
            {
                RegisterFloat(reflectionBlendJSON);
                reflectionBlendJSON.setCallbackFunction = SyncReflectionBlend;
            }
            surfaceTexturePowerJSON = original.GetFloatJSONParam("surfaceTexturePower");
            if (surfaceTexturePowerJSON != null)
            {
                RegisterFloat(surfaceTexturePowerJSON);
                surfaceTexturePowerJSON.setCallbackFunction = SyncSurfaceTexturePower;
            }
            specularIntensityJSON = original.GetFloatJSONParam("specularIntensity");
            if (specularIntensityJSON != null)
            {
                RegisterFloat(specularIntensityJSON);
            }
            reflectionColorJSON = original.GetColorJSONParam("reflectionColor");
            if (reflectionColorJSON != null)
            {
                RegisterColor(reflectionColorJSON);
                reflectionColorJSON.setCallbackFunction = SyncReflectionColor;
            }
        }

        // NOTE: 16 is the amount of materials we need to hide in ImprovedPoV. Usually, one character only will have the PoV active
        private List<MaterialReference> _materials = new List<MaterialReference>(16);
        private bool _failedOnce;

        public struct MaterialReference
        {
            public Material Current;
            public Material Previous;
        }

        protected override void Awake()
        {
            if (awakecalled) return;
            awakecalled = true;
            // NOTE: We skip all MirrorReflection initialization, since we'll just copy everything from the previous mirror
            // base.Awake();
            InitJSONStorable();
        }

        public void ImprovedPoVPersonChanged()
        {
            if (!active || _isWaitingForMaterials) return;

            _isWaitingForMaterials = true;
            StartCoroutine(BuildMaterialsListCoroutine());
        }

        public IEnumerator BuildMaterialsListCoroutine()
        {
            _materials.Clear();

            var attempts = 0;

            yield return new WaitUntil(() =>
            {
                try
                {
                    if (attempts++ > MAX_ATTEMPTS) return true;

                    return BuildMaterialsList();
                }
                catch (Exception exc)
                {
                    SuperController.LogError("Failed waiting for materials for ImprovedPoV mirror: " + exc);
                    return true;
                }
            });

            _isWaitingForMaterials = false;

            // Allow crashing again now that we have fresh data to work with
            _failedOnce = false;
        }

        private bool BuildMaterialsList()
        {
            var activeScene = SceneManager.GetActiveScene();
            var rootGameObjects = activeScene.GetRootGameObjects();

            var atoms = rootGameObjects.FirstOrDefault(o => !(o == null) && o.name == "SceneAtoms");
            if (atoms == null) return false;

            var selectors = atoms.GetComponentsInChildren<DAZCharacterSelector>();
            if (selectors == null || selectors.Length == 0) return false;

            var skins = selectors.Select(s => s.selectedCharacter?.skin).Where(s => !(s == null)).ToArray();
            if (skins.Length != selectors.Length) return false;

            foreach (var skin in skins)
            {
                if (skin == null || skin.GPUmaterials == null) continue;

                var previousMaterialsContainerName = "ImprovedPoV container for skin " + skin.GetInstanceID();

                var previousMaterialsContainer = rootGameObjects.FirstOrDefault(o => !(o == null) && o.name == previousMaterialsContainerName);

                // This character face is not hidden, skip
                if (previousMaterialsContainer == null)
                {
                    // The shader was destroyed, or not yet initialized
                    if (!ReferenceEquals(gameObject, null))
                        return false;

                    continue;
                }

                var previousMaterials = previousMaterialsContainer.GetComponent<MeshRenderer>()?.materials;
                if (previousMaterials == null) throw new NullReferenceException("Unable to get materials for skin");

                foreach (var material in skin.GPUmaterials)
                {
                    if (material == null) continue;

                    // NOTE: The new material would be called "Eyes (Instance)"
                    var previousMaterial = previousMaterials.FirstOrDefault(m => m.name.StartsWith(material.name));
                    if (previousMaterial == null) continue;

                    _materials.Add(new MaterialReference { Previous = previousMaterial, Current = material });
                }
            }

            return true;
        }

        public new void OnWillRenderObject()
        {
            ShowPoVMaterials();
            base.OnWillRenderObject();
            HidePoVMaterials();
        }

        private void ShowPoVMaterials()
        {
            if (!active) return;

            try
            {
                foreach (var reference in _materials)
                {
                    var material = reference.Current;
                    var previousMaterial = reference.Previous;
                    // NOTE: We cannot rely on Broadcast for some reason, so we must always make sure not to overwrite the shader restore
                    if (previousMaterial.renderQueue == 5000)
                    {
                        _materials.Clear();
                        return;
                    }
                    material.SetFloat("_AlphaAdjust", previousMaterial.GetFloat("_AlphaAdjust"));
                    material.SetColor("_Color", previousMaterial.GetColor("_Color"));
                    material.SetColor("_SpecColor", previousMaterial.GetColor("_SpecColor"));
                }
            }
            catch (Exception e)
            {
                if (_failedOnce) return;
                _failedOnce = true;
                SuperController.LogError("Failed to show PoV materials: " + e);
            }
        }

        private void HidePoVMaterials()
        {
            if (!active) return;

            try
            {
                foreach (var reference in _materials)
                {
                    var material = reference.Current;
                    var previousMaterial = reference.Previous;
                    // NOTE: We cannot rely on Broadcast for some reason, so we must always make sure not to overwrite the shader restore
                    if (previousMaterial.renderQueue == 5000)
                    {
                        _materials.Clear();
                        return;
                    }
                    material.SetFloat("_AlphaAdjust", -1f);
                    material.SetColor("_Color", new Color(0f, 0f, 0f, 0f));
                    material.SetColor("_SpecColor", new Color(0f, 0f, 0f, 0f));
                }
            }
            catch (Exception e)
            {
                if (_failedOnce) return;
                _failedOnce = true;
                SuperController.LogError("Failed to hide PoV materials: " + e);
            }
        }
    }
}
