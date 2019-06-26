#define POV_DIAGNOSTICS
using System;
using System.Collections;
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
    public class MirrorReflectionDecorator : MirrorReflection
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
                foreach (var reference in State.current.GetAllMaterials())
                {
                    reference.MakeVisible();
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
                foreach (var reference in State.current.GetAllMaterials())
                {
                    reference.MakeInvisible();
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
