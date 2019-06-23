#define POV_DIAGNOSTICS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            newBehavior.name = name;
            newBehavior.UITransform = uiTransform;
            newBehavior.UITransformAlt = uiTransformAlt;
            newBehavior.slaveReflection = slaveReflection;
            newBehavior.m_ReflectLayers = reflectLayers;
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
            if (!active) return;
            StartCoroutine(BuildMaterialsListCoroutine());
        }

        public IEnumerator BuildMaterialsListCoroutine()
        {
            _materials.Clear();
            var activeScene = SceneManager.GetActiveScene();
            GameObject[] rootGameObjects = null;
            DAZCharacterSelector[] selectors = null;
            yield return new WaitUntil(() =>
            {
                rootGameObjects = activeScene.GetRootGameObjects();
                var atoms = rootGameObjects.FirstOrDefault(o => o.name == "SceneAtoms");
                if (atoms == null) return false;
                selectors = atoms.GetComponentsInChildren<DAZCharacterSelector>();
                return selectors != null;
            });
            BuildMaterialsList(rootGameObjects, selectors);
        }

        private void BuildMaterialsList(GameObject[] rootGameObjects, DAZCharacterSelector[] selectors)
        {
            foreach (var characterSelector in selectors)
            {
                var skin = characterSelector.selectedCharacter.skin;
                var previousMaterialsContainerName = "ImprovedPoV container for skin " + skin.GetInstanceID();

                var previousMaterialsContainer = rootGameObjects.FirstOrDefault(o => o.name == previousMaterialsContainerName);
                if (previousMaterialsContainer == null) continue;

                var previousMaterials = previousMaterialsContainer.GetComponent<MeshRenderer>().materials;

                foreach (var material in skin.GPUmaterials)
                {
                    // NOTE: The new material would be called "Eyes (Instance)"
                    var previousMaterial = previousMaterials.FirstOrDefault(m => m.name.StartsWith(material.name));
                    if (previousMaterial == null) continue;

                    _materials.Add(new MaterialReference { Previous = previousMaterial, Current = material });
                }
            }
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
