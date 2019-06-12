#define POV_DIAGNOSTICS
using System;
using System.Linq;
using UnityEngine;
#if(POV_DIAGNOSTICS)
using System.Collections.Generic;
#endif

namespace Acidbubbles.VAM.Plugins
{
    /// <summary>
    /// Improved POV handling so that possession actually feels right.
    /// Credits to https://www.reddit.com/user/ShortRecognition/ for the original HeadPossessDepthFix from which
    /// this plugin took heavy inspiration: https://www.reddit.com/r/VAMscenes/comments/9z9b71/script_headpossessdepthfix/
    /// </summary>
    public class ImprovedPOV : MVRScript
    {
        private Atom _person;
        private Camera _mainCamera;
        private Possessor _possessor;

        private JSONStorableFloat _cameraRecess;
        private JSONStorableFloat _cameraUpDown;

        public override void Init()
        {
            try
            {
                pluginLabelJSON.val = "Improved POV Plugin - by Acidbubbles";
                if (!AssertPerson(containingAtom)) return;

                _person = containingAtom;
                _mainCamera = CameraTarget.centerTarget.targetCamera;
                _possessor = SuperController.FindObjectsOfType(typeof(Possessor)).Where(p => p.name == "CenterEye").Select(p => p as Possessor).First();

                RegisterPossessor();
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to initialize Improved POV: " + e);
            }
        }

        protected void FixedUpdate()
        {
            if (_person == null)
                return;
        }

        private static bool AssertPerson(Atom containingAtom)
        {
            if (containingAtom.type == "Person")
                return true;

            SuperController.LogError($"Please apply the ImprovedPOV plugin to the 'Person' atom you wish to possess. Currently applied on '{containingAtom.type}'.");
            return false;
        }

        private void RegisterPossessor()
        {
            try
            {
                _cameraRecess = new JSONStorableFloat("Camera Recess", 0.0f, 0f, .2f, false);
                RegisterFloat(_cameraRecess);
                var recessSlider = CreateSlider(_cameraRecess);
                recessSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    MoveHead();
                });

                _cameraUpDown = new JSONStorableFloat("Camera UpDown", 0f, -0.2f, 0.2f, false);
                RegisterFloat(_cameraUpDown);
                var upDownSlider = CreateSlider(_cameraUpDown);
                upDownSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    MoveHead();
                });

                var clipDistance = new JSONStorableFloat("Clip Distance", 0.01f, 0.01f, .2f, true);
                RegisterFloat(clipDistance);
                var clipSlider = CreateSlider(clipDistance, true);
                clipSlider.slider.onValueChanged.AddListener(delegate (float val)
                {
                    _mainCamera.nearClipPlane = val;
                });
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to register possessor: " + e);
            }
        }

        private void MoveHead()
        {
            var pos = _possessor.transform.position;
            _mainCamera.transform.position = pos - _mainCamera.transform.rotation * Vector3.forward * _cameraRecess.val - _mainCamera.transform.rotation * Vector3.down * _cameraUpDown.val - _mainCamera.transform.rotation * Vector3.right;
            _possessor.transform.position = pos;

        }

        #if(POV_DIAGNOSTICS)
        #region Debugging

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

        #endregion
        #endif
    }
}