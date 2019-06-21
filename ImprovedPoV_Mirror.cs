#define POV_DIAGNOSTICS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ImprovedPoV_Mirror : MVRScript
{
    private GameObject _mirror;
    private bool _active;

    public override void Init()
    {
        try
        {
            _mirror = containingAtom.gameObject;
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
            if (_mirror == null || _active) return;

            ReplaceMirrorScriptAndCreatedObjects<MirrorReflection, ImprovedPoVMirrorReflectionDecorator>();
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
            if (!_active) return;

            ReplaceMirrorScriptAndCreatedObjects<ImprovedPoVMirrorReflectionDecorator, MirrorReflection>();
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

    private void ReplaceMirrorScriptAndCreatedObjects<TBehaviorToRemove, TBehaviorToAdd>()
        where TBehaviorToRemove : MirrorReflection
        where TBehaviorToAdd : MirrorReflection
    {
        foreach (var childMirror in _mirror.GetComponentsInChildren<TBehaviorToRemove>())
        {
            var name = childMirror.name;
            var uiTransform = childMirror.UITransform;
            var uiTransformAlt = childMirror.UITransformAlt;
            var slaveReflection = childMirror.slaveReflection;
            var reflectLayers = childMirror.m_ReflectLayers;

            var childMirrorGameObject = childMirror.gameObject;

            var childMirrorInstanceId = childMirror.GetInstanceID();
            Destroy(childMirror);

            var reflectionCameraGameObjectPrefix = "Mirror Refl Camera id" + childMirrorInstanceId + " for ";
            foreach (var childMirrorObject in SceneManager.GetActiveScene().GetRootGameObjects().Where(x => x.name.StartsWith(reflectionCameraGameObjectPrefix)))
            {
                Destroy(childMirrorObject);
            }

            var newBehavior = childMirrorGameObject.AddComponent<TBehaviorToAdd>();
            newBehavior.name = name;
            newBehavior.UITransform = uiTransform;
            newBehavior.UITransformAlt = uiTransformAlt;
            newBehavior.slaveReflection = slaveReflection;
            newBehavior.m_ReflectLayers = reflectLayers;
        }
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
            base.Awake();
            BuildMaterialsList();
        }

        public void ImprovedPoVPersonChanged()
        {
            BuildMaterialsList();
        }

        public new void OnWillRenderObject()
        {
            ShowPoVMaterials();
            base.OnWillRenderObject();
            HidePoVMaterials();
        }

        private void BuildMaterialsList()
        {
            _materials.Clear();

            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var atoms = rootGameObjects.FirstOrDefault(o => o.name == "SceneAtoms");

            foreach (var characterSelector in atoms.GetComponentsInChildren<DAZCharacterSelector>())
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

        private void ShowPoVMaterials()
        {
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
