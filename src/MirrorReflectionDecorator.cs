#define POV_DIAGNOSTICS
using System;
using System.Collections.Generic;

namespace Acidbubbles.ImprovedPoV
{
    /// <summary>
    /// Improved PoV Version 0.0.0
    /// Possession that actually feels right.
    /// Source: https://github.com/acidbubbles/vam-improved-pov
    /// </summary>
    public class MirrorReflectionDecorator : MirrorReflection
    {
        private MemoizedPerson _person;

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
            // TODO: Check what this is, we might keep a reference to a deleted gameobject
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

        public void ImprovedPoVSkinUpdated(Dictionary<string, object> value)
        {
            _person = MemoizedPerson.FromBroadcastable(value);
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
            _person?.BeforeMirrorRender();
            base.OnWillRenderObject();
            _person?.AfterMirrorRender();
        }
    }
}
