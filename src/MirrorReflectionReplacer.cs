#define POV_DIAGNOSTICS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Acidbubbles.ImprovedPoV
{
    /// <summary>
    /// Improved PoV Version 0.0.0
    /// Possession that actually feels right.
    /// Source: https://github.com/acidbubbles/vam-improved-pov
    /// </summary>
    public static class MirrorReflectionReplacer
    {
        private static bool _registered;

        public static void Attach()
        {
            if (_registered) return;

            // TODO: Test whether the singleton instance changes; if it does, re-register, otherwise do not re-register
            SuperController.singleton.onAtomUIDsChangedHandlers += OnAtomUIDsChanged;
            ScanAndReplace();
            _registered = true;
        }

        private static void OnAtomUIDsChanged(List<string> atomUIDs)
        {
            ScanAndReplace();
        }

        public static void ScanAndReplace()
        {
            try
            {
                // TODO: Optimize this by only browsing objects we know are mirrors
                foreach (var mirror in SuperController.singleton.GetAtoms())
                {
                    ReplaceMirrorScriptAndCreatedObjects(mirror.gameObject);
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to scan and replace MirrorReflection " + e);
            }
        }

        private static void ReplaceMirrorScriptAndCreatedObjects(GameObject mirror)
        {
            foreach (var behavior in mirror.GetComponentsInChildren<MirrorReflection>())
            {
                // Already replaced
                if (behavior.GetType() != typeof(MirrorReflection))
                    continue;

                ReplaceMirrorScriptAndCreatedObjects(behavior);
            }
        }

        private static void ReplaceMirrorScriptAndCreatedObjects(MirrorReflection originalBehavior)
        {
            var childMirrorGameObject = originalBehavior.gameObject;
            var childMirrorInstanceId = originalBehavior.GetInstanceID();

            var name = originalBehavior.name;
            var atom = originalBehavior.containingAtom;
            if (atom != null)
                atom.UnregisterAdditionalStorable(originalBehavior);
            var newBehavior = childMirrorGameObject.AddComponent<MirrorReflectionDecorator>();
            if (newBehavior == null) throw new NullReferenceException("newBehavior");
            newBehavior.CopyFrom(originalBehavior);
            UnityEngine.Object.DestroyImmediate(originalBehavior);
            newBehavior.name = name;
            if (atom != null)
                atom.RegisterAdditionalStorable(newBehavior);

            var reflectionCameraGameObjectPrefix = "Mirror Refl Camera id" + childMirrorInstanceId + " for ";
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var childMirrorObject in rootObjects.Where(x => x.name.StartsWith(reflectionCameraGameObjectPrefix)))
            {
                UnityEngine.GameObject.DestroyImmediate(childMirrorObject);
            }
        }
    }
}
