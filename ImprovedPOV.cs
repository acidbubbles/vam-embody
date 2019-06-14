﻿#define POV_DIAGNOSTICS
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
        private FreeControllerV3 _headControl;
        private JSONStorableFloat _cameraRecess;
        private JSONStorableFloat _cameraUpDown;
        private JSONStorableBool _hideFace;
        private JSONStorableBool _possessedOnly;
        private bool _active;
        private bool _destroyed;

        public override void Init()
        {
            try
            {
                pluginLabelJSON.val = PluginLabel;
                if (!AssertPerson(containingAtom)) return;

                _person = containingAtom;
                _mainCamera = CameraTarget.centerTarget.targetCamera;
                _possessor = SuperController.FindObjectsOfType(typeof(Possessor)).Where(p => p.name == "CenterEye").Select(p => p as Possessor).First();
                _headControl = (FreeControllerV3)_person.GetStorableByID("headControl");

#if (POV_DIAGNOSTICS)
                SuperController.LogMessage("PoV Person: " + GetDebugHierarchy(_person.gameObject));
#endif

                InitControls();
                ApplyAll();
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to initialize Improved PoV: " + e);
            }
        }

        public void OnDestroy()
        {
            _destroyed = true;
            ApplyAll();
        }

        public void Update()
        {
            if (_destroyed) return;
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
        }

        private bool AssertPerson(Atom containingAtom)
        {
            if (containingAtom.type == "Person")
                return true;

            SuperController.LogError($"Please apply the ImprovedPoV plugin to the 'Person' atom you wish to possess. Currently applied on '{containingAtom.type}'.");
            return false;
        }

        private void InitControls()
        {
            try
            {
                _cameraRecess = new JSONStorableFloat("Camera Recess", 0.05f, 0f, .2f, false);
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

                var clipDistance = new JSONStorableFloat("Clip Distance", 0.01f, 0.01f, .2f, false);
                RegisterFloat(clipDistance);
                var clipSlider = CreateSlider(clipDistance, false);
                clipSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    _mainCamera.nearClipPlane = val;
                });

                _hideFace = new JSONStorableBool("Hide Face", true);
                RegisterBool(_hideFace);
                var hideFaceCheckbox = CreateToggle(_hideFace, true);
                hideFaceCheckbox.toggle.onValueChanged.AddListener(delegate (bool val)
                {
                    ApplyFaceMaterialsEnabled();
                });

                _possessedOnly = new JSONStorableBool("Possessed Only", true);
                RegisterBool(_possessedOnly);
                var possessedOnlyCheckbox = CreateToggle(_possessedOnly, true);
                possessedOnlyCheckbox.toggle.onValueChanged.AddListener(delegate (bool val)
                {
                    ApplyAll();
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
            ApplyFaceMaterialsEnabled();
            ApplyPossessorMeshVisibility();
        }

        private void ApplyFaceMaterialsEnabled()
        {
            try
            {
                var skin = _person.GetComponentInChildren<DAZCharacterSelector>().selectedCharacter.skin;
                var enabled = _destroyed || !_active || !_hideFace.val;

                for (int i = 0; i < skin.GPUmaterials.Length; i++)
                {
                    Material mat = skin.GPUmaterials[i];
                    if (MaterialsToHide.Any(materialToHide => mat.name.StartsWith(materialToHide)))
                    {
                        skin.materialsEnabled[i] = enabled;
                    }
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to toggle face materials: " + e);
            }
        }

#if (POV_DIAGNOSTICS)

        private void ApplyFaceShaders()
        {
            try
            {
                var skin = _person.GetComponentInChildren<DAZCharacterSelector>().selectedCharacter.skin;
                var standardShader = Shader.Find("Standard");
                // https://docs.unity3d.com/ScriptReference/Material-shader.html
                // https://forum.unity.com/threads/transparency-with-standard-surface-shader.394551/
                // https://answers.unity.com/questions/244837/shader-help-adding-transparency-to-a-shader.html
                // TODO: Print out which shaders are used by the mirror and by the face
                foreach (var material in skin.GPUmaterials)
                {
                    if (MaterialsToHide.Contains(material.name))
                    {
                        // TODO: Transparent (should also work)
                        material.shader = standardShader;
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                        material.SetFloat("_Mode", 2.0f);

                        // TODO: Cutoff (desired effect)
                        // material.SetOverrideTag("RenderType", "Cutout");
                        // material.EnableKeyword("_ALPHATEST_ON");
                        // material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    }
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to update face shaders: " + e);
            }
        }

#endif

        private void ApplyCameraPosition()
        {
            try
            {
                var pos = _possessor.transform.position;
                _mainCamera.transform.position = pos - _mainCamera.transform.rotation * Vector3.forward * _cameraRecess.val - _mainCamera.transform.rotation * Vector3.down * _cameraUpDown.val;
                _possessor.transform.position = pos;
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to update camera position: " + e);
            }
        }

        private void ApplyPossessorMeshVisibility()
        {
            var meshActive = _destroyed || !_active;

            _possessor.gameObject.transform.Find("Capsule").gameObject.SetActive(meshActive);
            _possessor.gameObject.transform.Find("Sphere1").gameObject.SetActive(meshActive);
            _possessor.gameObject.transform.Find("Sphere2").gameObject.SetActive(meshActive);
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