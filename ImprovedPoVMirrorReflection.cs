// MirrorReflection
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

[ExecuteInEditMode]
public class ImprovedPoVMirrorReflection : MVRScript
{
	// Acidbubbles: Whether this is the real thing, or just the plugin
	private bool _actualMirror;
	// /Acidbubbles

	public MirrorReflection slaveReflection;

	protected JSONStorableBool disablePixelLightsJSON;

	[SerializeField]
	protected bool _disablePixelLights;

	protected JSONStorableStringChooser textureSizeJSON;

	protected int _oldReflectionTextureSize;

	[SerializeField]
	protected int _textureSize = 1024;

	protected JSONStorableStringChooser antiAliasingJSON;

	protected int _oldAntiAliasing;

	[SerializeField]
	protected int _antiAliasing = 8;

	protected JSONStorableFloat reflectionOpacityJSON;

	[SerializeField]
	protected float _reflectionOpacity = 0.5f;

	protected JSONStorableFloat reflectionBlendJSON;

	[SerializeField]
	protected float _reflectionBlend = 1f;

	protected JSONStorableFloat surfaceTexturePowerJSON;

	[SerializeField]
	protected float _surfaceTexturePower = 1f;

	protected JSONStorableFloat specularIntensityJSON;

	[SerializeField]
	protected float _specularIntensity = 1f;

	protected JSONStorableColor reflectionColorJSON;

	protected HSVColor _currentReflectionHSVColor;

	protected Color _currentReflectionColor;

	public static bool globalEnabled = true;

	public Transform altObjectWhenMirrorDisabled;

	public bool useSameMaterialWhenMirrorDisabled;

	public float m_ClipPlaneOffset;

	public LayerMask m_ReflectLayers = -1;

	public bool m_UseObliqueClip = true;

	public bool renderBackside;

	protected Hashtable m_ReflectionCameras = new Hashtable();

	protected RenderTexture m_ReflectionTextureLeft;

	protected RenderTexture m_ReflectionTextureRight;

	protected static bool s_InsideRendering;

	public bool disablePixelLights
	{
		get
		{
			return _disablePixelLights;
		}
		set
		{
			if (disablePixelLightsJSON != null)
			{
				disablePixelLightsJSON.val = value;
			}
			else if (_disablePixelLights != value)
			{
				SyncDisablePixelLights(value);
			}
		}
	}

	public int textureSize
	{
		get
		{
			return _textureSize;
		}
		set
		{
			if (textureSizeJSON != null)
			{
				textureSizeJSON.val = value.ToString();
			}
			else if (_textureSize != value && (value == 512 || value == 1024 || value == 2048 || value == 4096))
			{
				SetTextureSizeFromString(value.ToString());
			}
		}
	}

	public int antiAliasing
	{
		get
		{
			return _antiAliasing;
		}
		set
		{
			if (antiAliasingJSON != null)
			{
				antiAliasingJSON.val = value.ToString();
			}
			else if (_antiAliasing != value && (value == 1 || value == 2 || value == 4 || value == 8))
			{
				SetAntialiasingFromString(value.ToString());
			}
		}
	}

	public float reflectionOpacity
	{
		get
		{
			return _reflectionOpacity;
		}
		set
		{
			if (reflectionOpacityJSON != null)
			{
				reflectionOpacityJSON.val = value;
			}
			else if (_reflectionOpacity != value)
			{
				SyncReflectionOpacity(value);
			}
		}
	}

	public float reflectionBlend
	{
		get
		{
			return _reflectionBlend;
		}
		set
		{
			if (reflectionBlendJSON != null)
			{
				reflectionBlendJSON.val = value;
			}
			else if (_reflectionBlend != value)
			{
				SyncReflectionBlend(value);
			}
		}
	}

	public float surfaceTexturePower
	{
		get
		{
			return _surfaceTexturePower;
		}
		set
		{
			if (surfaceTexturePowerJSON != null)
			{
				surfaceTexturePowerJSON.val = value;
			}
			else if (_surfaceTexturePower != value)
			{
				SyncSurfaceTexturePower(value);
			}
		}
	}

	public float specularIntensity
	{
		get
		{
			return _specularIntensity;
		}
		set
		{
			if (specularIntensityJSON != null)
			{
				specularIntensityJSON.val = value;
			}
			else if (_specularIntensity != value)
			{
				SyncSpecularIntensity(value);
			}
		}
	}

	protected void SyncDisablePixelLights(bool b)
	{
		_disablePixelLights = b;
		if (slaveReflection != null)
		{
			slaveReflection.disablePixelLights = b;
		}
	}

	protected void SetTextureSizeFromString(string size)
	{
		try
		{
			int num = int.Parse(size);
			if (num == 512 || num == 1024 || num == 2048 || num == 4096)
			{
				_textureSize = num;
				if (slaveReflection != null)
				{
					slaveReflection.textureSize = _textureSize;
				}
			}
			else
			{
				if (textureSizeJSON != null)
				{
					textureSizeJSON.valNoCallback = _textureSize.ToString();
				}
				Debug.LogError("Attempted to set texture size to " + size + " which is not a valid value of 512, 1024, 2048, 4096");
			}
		}
		catch (FormatException)
		{
			Debug.LogError("Attempted to set texture size to " + size + " which is not a valid integer");
		}
	}

	protected void SetAntialiasingFromString(string aa)
	{
		try
		{
			int num = int.Parse(aa);
			if (num == 1 || num == 2 || num == 4 || num == 8)
			{
				_antiAliasing = num;
				if (slaveReflection != null)
				{
					slaveReflection.antiAliasing = _antiAliasing;
				}
			}
			else
			{
				if (antiAliasingJSON != null)
				{
					antiAliasingJSON.valNoCallback = _antiAliasing.ToString();
				}
				Debug.LogError("Attempted to set antialiasing to " + aa + " which is not a valid value of 1, 2, 4, or 8");
			}
		}
		catch (FormatException)
		{
			Debug.LogError("Attempted to set antialiasing to " + aa + " which is not a valid integer");
		}
	}

	protected bool MaterialHasProp(string propName)
	{
		Renderer component = GetComponent<Renderer>();
		if (component != null)
		{
			Material material = (!Application.isPlaying) ? component.sharedMaterial : component.material;
			if (material.HasProperty(propName))
			{
				return true;
			}
		}
		return false;
	}

	protected void SetMaterialProp(string propName, float propValue)
	{
		Renderer component = GetComponent<Renderer>();
		if (!(component != null))
		{
			return;
		}
		Material[] array = (!Application.isPlaying) ? component.sharedMaterials : component.materials;
		Material[] array2 = array;
		foreach (Material material in array2)
		{
			if (material.HasProperty(propName))
			{
				material.SetFloat(propName, propValue);
			}
		}
	}

	protected void SyncReflectionOpacity(float f)
	{
		_reflectionOpacity = f;
		SetMaterialProp("_ReflectionOpacity", _reflectionOpacity);
		if (slaveReflection != null)
		{
			slaveReflection.reflectionOpacity = f;
		}
	}

	protected void SyncReflectionBlend(float f)
	{
		_reflectionBlend = f;
		SetMaterialProp("_ReflectionBlendTexPower", _reflectionBlend);
		if (slaveReflection != null)
		{
			slaveReflection.reflectionBlend = f;
		}
	}

	protected void SyncSurfaceTexturePower(float f)
	{
		_surfaceTexturePower = f;
		SetMaterialProp("_MainTexPower", _surfaceTexturePower);
		if (slaveReflection != null)
		{
			slaveReflection.surfaceTexturePower = f;
		}
	}

	protected void SyncSpecularIntensity(float f)
	{
		_specularIntensity = f;
		SetMaterialProp("_SpecularIntensity", _specularIntensity);
		if (slaveReflection != null)
		{
			slaveReflection.specularIntensity = f;
		}
	}

	public void SetReflectionMaterialColor(Color c)
	{
		Renderer component = GetComponent<Renderer>();
		if (!(component != null))
		{
			return;
		}
		Material[] array = (!Application.isPlaying) ? component.sharedMaterials : component.materials;
		Material[] array2 = array;
		foreach (Material material in array2)
		{
			if (material.HasProperty("_ReflectionColor"))
			{
				material.SetColor("_ReflectionColor", c);
			}
		}
	}

	protected void SyncReflectionColor(float h, float s, float v)
	{
		_currentReflectionHSVColor.H = h;
		_currentReflectionHSVColor.S = s;
		_currentReflectionHSVColor.V = v;
		_currentReflectionColor = HSVColorPicker.HSVToRGB(h, s, v);
		SetReflectionMaterialColor(_currentReflectionColor);
		if (slaveReflection != null)
		{
			slaveReflection.SetReflectionColor(_currentReflectionHSVColor);
		}
	}

	public void SetReflectionColor(HSVColor hsvColor)
	{
		if (reflectionColorJSON != null)
		{
			reflectionColorJSON.val = hsvColor;
		}
		else
		{
			SyncReflectionColor(hsvColor.H, hsvColor.S, hsvColor.V);
		}
	}

	private void RenderMirror(Camera reflectionCamera)
	{
		Vector3 position = base.transform.position;
		Vector3 up = base.transform.up;
		float w = 0f - Vector3.Dot(up, position) - m_ClipPlaneOffset;
		Vector4 plane = new Vector4(up.x, up.y, up.z, w);
		Matrix4x4 reflectionMat = Matrix4x4.zero;
		CalculateReflectionMatrix(ref reflectionMat, plane);
		reflectionCamera.worldToCameraMatrix *= reflectionMat;
		if (m_UseObliqueClip)
		{
			Vector4 clipPlane = CameraSpacePlane(reflectionCamera, position, up, 1f);
			reflectionCamera.projectionMatrix = reflectionCamera.CalculateObliqueMatrix(clipPlane);
		}
		reflectionCamera.cullingMask = (-17 & m_ReflectLayers.value);
		GL.invertCulling = true;
		reflectionCamera.transform.position = reflectionCamera.cameraToWorldMatrix.GetPosition();
		reflectionCamera.transform.rotation = reflectionCamera.cameraToWorldMatrix.GetRotation();
		reflectionCamera.Render();
		GL.invertCulling = false;
	}

	public void OnWillRenderObject()
	{
		// Acidbubbles: Only for actual mirror
		if(!_actualMirror) return;
		// /Acidbubbles

		Renderer component = GetComponent<Renderer>();
		if (!base.enabled || !(bool)component || !(bool)component.sharedMaterial || !component.enabled || !globalEnabled)
		{
			return;
		}
		Camera current = Camera.current;
		if (!(bool)current)
		{
			return;
		}
		Vector3 rhs = current.transform.position - base.transform.position;
		if (!renderBackside)
		{
			float num = Vector3.Dot(base.transform.up, rhs);
			if (num <= 0.001f)
			{
				return;
			}
		}
		if (s_InsideRendering)
		{
			return;
		}
		s_InsideRendering = true;
		Camera reflectionCamera;
		CreateMirrorObjects(current, out reflectionCamera);
		int pixelLightCount = QualitySettings.pixelLightCount;
		if (_disablePixelLights)
		{
			QualitySettings.pixelLightCount = 0;
		}
		UpdateCameraModes(current, reflectionCamera);
		// Acidbubbles: Show materials that were hidden by the ImprovedPOV plugin
		ShowPoVMaterials();
		// /Acidbubbles
		Vector3 position = current.transform.position;
		if (current.stereoEnabled)
		{
			if (current.stereoTargetEye == StereoTargetEyeMask.Both)
			{
				reflectionCamera.ResetWorldToCameraMatrix();
				if (CameraTarget.rightTarget != null && CameraTarget.rightTarget.targetCamera != null && current.transform.parent != null)
				{
					reflectionCamera.transform.position = current.transform.parent.TransformPoint(InputTracking.GetLocalPosition(XRNode.RightEye));
					reflectionCamera.transform.rotation = current.transform.parent.rotation * InputTracking.GetLocalRotation(XRNode.RightEye);
					reflectionCamera.worldToCameraMatrix = CameraTarget.rightTarget.worldToCameraMatrix;
					reflectionCamera.projectionMatrix = CameraTarget.rightTarget.projectionMatrix;
				}
				else
				{
					reflectionCamera.transform.position = current.transform.parent.TransformPoint(InputTracking.GetLocalPosition(XRNode.RightEye));
					reflectionCamera.transform.rotation = current.transform.parent.rotation * InputTracking.GetLocalRotation(XRNode.RightEye);
					reflectionCamera.worldToCameraMatrix = current.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
					reflectionCamera.projectionMatrix = current.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
				}
				reflectionCamera.targetTexture = m_ReflectionTextureRight;
				RenderMirror(reflectionCamera);
				reflectionCamera.ResetWorldToCameraMatrix();
				if (CameraTarget.leftTarget != null && CameraTarget.leftTarget.targetCamera != null && current.transform.parent != null)
				{
					reflectionCamera.transform.position = current.transform.parent.TransformPoint(InputTracking.GetLocalPosition(XRNode.LeftEye));
					reflectionCamera.transform.rotation = current.transform.parent.rotation * InputTracking.GetLocalRotation(XRNode.LeftEye);
					reflectionCamera.worldToCameraMatrix = CameraTarget.leftTarget.worldToCameraMatrix;
					reflectionCamera.projectionMatrix = CameraTarget.leftTarget.projectionMatrix;
				}
				else
				{
					reflectionCamera.transform.position = current.transform.parent.TransformPoint(InputTracking.GetLocalPosition(XRNode.LeftEye));
					reflectionCamera.transform.rotation = current.transform.parent.rotation * InputTracking.GetLocalRotation(XRNode.LeftEye);
					reflectionCamera.worldToCameraMatrix = current.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
					reflectionCamera.projectionMatrix = current.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
				}
				position = reflectionCamera.transform.position;
				reflectionCamera.targetTexture = m_ReflectionTextureLeft;
				RenderMirror(reflectionCamera);
			}
			else if (current.stereoTargetEye == StereoTargetEyeMask.Left)
			{
				reflectionCamera.ResetWorldToCameraMatrix();
				reflectionCamera.transform.position = current.transform.position;
				reflectionCamera.transform.rotation = current.transform.rotation;
				reflectionCamera.worldToCameraMatrix = current.worldToCameraMatrix;
				reflectionCamera.projectionMatrix = current.projectionMatrix;
				reflectionCamera.targetTexture = m_ReflectionTextureLeft;
				RenderMirror(reflectionCamera);
			}
			else if (current.stereoTargetEye == StereoTargetEyeMask.Right)
			{
				reflectionCamera.ResetWorldToCameraMatrix();
				reflectionCamera.transform.position = current.transform.position;
				reflectionCamera.transform.rotation = current.transform.rotation;
				reflectionCamera.worldToCameraMatrix = current.worldToCameraMatrix;
				reflectionCamera.projectionMatrix = current.projectionMatrix;
				reflectionCamera.targetTexture = m_ReflectionTextureLeft;
				RenderMirror(reflectionCamera);
			}
		}
		else
		{
			reflectionCamera.ResetWorldToCameraMatrix();
			reflectionCamera.transform.position = current.transform.position;
			reflectionCamera.transform.rotation = current.transform.rotation;
			reflectionCamera.worldToCameraMatrix = current.worldToCameraMatrix;
			reflectionCamera.projectionMatrix = current.projectionMatrix;
			reflectionCamera.targetTexture = m_ReflectionTextureLeft;
			RenderMirror(reflectionCamera);
		}
		Material[] array = (!Application.isPlaying) ? component.sharedMaterials : component.materials;
		Vector4 value = default(Vector4);
		value.x = position.x;
		value.y = position.y;
		value.z = position.z;
		value.w = 0f;
		Material[] array2 = array;
		foreach (Material material in array2)
		{
			if (material.HasProperty("_ReflectionTex"))
			{
				material.SetTexture("_ReflectionTex", m_ReflectionTextureLeft);
			}
			if (material.HasProperty("_LeftReflectionTex"))
			{
				material.SetTexture("_LeftReflectionTex", m_ReflectionTextureLeft);
			}
			if (material.HasProperty("_RightReflectionTex"))
			{
				material.SetTexture("_RightReflectionTex", m_ReflectionTextureRight);
			}
			if (material.HasProperty("_LeftCameraPosition"))
			{
				material.SetVector("_LeftCameraPosition", value);
			}
		}
		if (_disablePixelLights)
		{
			QualitySettings.pixelLightCount = pixelLightCount;
		}
		// Acidbubbles: Hide materials that were temporarily shown for the ImprovedPoV plugin
		HidePoVMaterials();
		// /Acidbubbles
		s_InsideRendering = false;
	}

	// Acidbubbles: PoV Material show/hide functions
	public struct ShownSkin
	{
		public Material material;
		public Shader shaderToRestore;
	}
	private List<ShownSkin> _shownMaterials = new List<ShownSkin>();
	private bool _debugOutputOnce;
	private bool _failedOnce;

    private void ShowPoVMaterials()
    {
        try
        {
            // TODO: Initialize the shader in Init and keep it instead of using Find every render
            var shader = Shader.Find("Custom/Subsurface/GlossNMCullComputeBuff");
            if (shader == null)
            {
                SuperController.LogMessage("Shader show not found");
                return;
            }
            // TODO: Cache the list of characters and materials to hide, and update the list using a broadcast message
            foreach (var characterSelector in GameObject.FindObjectsOfType<DAZCharacterSelector>())
            {
                var skin = characterSelector.selectedCharacter.skin;
				var previousMaterialsContainerName = "ImprovedPoV container for skin " + skin.GetInstanceID();

                var previousMaterialsContainer = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(o => o.name == previousMaterialsContainerName);
				if(previousMaterialsContainer == null) continue;

                var previousMaterials = previousMaterialsContainer.GetComponent<MeshRenderer>().materials;

                if (!_debugOutputOnce)
                {
                    SuperController.LogMessage("Mirror debugging info (once)");
                    _debugOutputOnce = true;
                    for (var index = 0; index < skin.GPUmaterials.Length; index++)
                    {
                        var material = skin.GPUmaterials[index];
                        SuperController.LogMessage("Mirror material: " + material.name + ", shader: " + material.shader.name);
                    }
                }

                foreach (var material in skin.GPUmaterials)
                {
                    // NOTE: The new material would be called "Eyes (Instance)"
                    var previousMaterial = previousMaterials.FirstOrDefault(m => m.name.StartsWith(material.name));
                    if (previousMaterial == null) continue;

                    // TODO: Use a fixed array since we know how many materials should be in there, avoiding re-allocating a new list every time
                    _shownMaterials.Add(new ShownSkin { material = material, shaderToRestore = material.shader });
                    // material.shader = previousMaterial.shader;
                    material.SetColor("_Color", new Color(0, 1, 0, 0));
                    // material.SetColor("_Color", previousMaterial.GetColor("_Color"));
                    // material.SetColor("_SpecColor", previousMaterial.GetColor("_SpecColor"));
                }
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
            foreach (var shown in _shownMaterials)
            {
				var material = shown.material;
                // material.shader = shown.shaderToRestore;
				material.SetColor("_Color", new Color(1, 0, 0, 0));
				// material.SetColor("_SpecColor", Color.black);
            }
            _shownMaterials.Clear();
        }
        catch (Exception e)
        {
            if (_failedOnce) return;
            _failedOnce = true;
            SuperController.LogError("Failed to hide PoV materials: " + e);
        }
	}
	// /Acidbubbles

	protected void UpdateCameraModes(Camera src, Camera dest)
	{
		if (dest == null)
		{
			return;
		}
		dest.backgroundColor = src.backgroundColor;
		if (src.clearFlags == CameraClearFlags.Skybox)
		{
			Skybox skybox = src.GetComponent(typeof(Skybox)) as Skybox;
			Skybox skybox2 = dest.GetComponent(typeof(Skybox)) as Skybox;
			if (!(bool)skybox || !(bool)skybox.material)
			{
				skybox2.enabled = false;
			}
			else
			{
				skybox2.enabled = true;
				skybox2.material = skybox.material;
			}
		}
		dest.farClipPlane = src.farClipPlane;
		dest.nearClipPlane = src.nearClipPlane;
		dest.orthographic = src.orthographic;
		dest.fieldOfView = src.fieldOfView;
		dest.aspect = src.aspect;
		dest.orthographicSize = src.orthographicSize;
	}

	protected void CreateMirrorObjects(Camera currentCamera, out Camera reflectionCamera)
	{
		reflectionCamera = null;
		if (!(bool)m_ReflectionTextureRight || !(bool)m_ReflectionTextureLeft || _oldReflectionTextureSize != _textureSize || _oldAntiAliasing != _antiAliasing)
		{
			if ((bool)m_ReflectionTextureLeft)
			{
				UnityEngine.Object.DestroyImmediate(m_ReflectionTextureLeft);
			}
			m_ReflectionTextureLeft = new RenderTexture(_textureSize, _textureSize, 24);
			m_ReflectionTextureLeft.name = "__MirrorReflectionLeft" + GetInstanceID();
			m_ReflectionTextureLeft.antiAliasing = _antiAliasing;
			m_ReflectionTextureLeft.isPowerOfTwo = true;
			m_ReflectionTextureLeft.hideFlags = HideFlags.DontSave;
			if ((bool)m_ReflectionTextureRight)
			{
				UnityEngine.Object.DestroyImmediate(m_ReflectionTextureRight);
			}
			m_ReflectionTextureRight = new RenderTexture(_textureSize, _textureSize, 24);
			m_ReflectionTextureRight.name = "__MirrorReflectionRight" + GetInstanceID();
			m_ReflectionTextureRight.antiAliasing = _antiAliasing;
			m_ReflectionTextureRight.isPowerOfTwo = true;
			m_ReflectionTextureRight.hideFlags = HideFlags.DontSave;
			_oldReflectionTextureSize = _textureSize;
			_oldAntiAliasing = _antiAliasing;
		}
		reflectionCamera = (m_ReflectionCameras[currentCamera] as Camera);
		if (!(bool)reflectionCamera)
		{
			GameObject gameObject = new GameObject("Mirror Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox));
			reflectionCamera = gameObject.GetComponent<Camera>();
			reflectionCamera.enabled = false;
			reflectionCamera.transform.position = base.transform.position;
			reflectionCamera.transform.rotation = base.transform.rotation;
			reflectionCamera.gameObject.AddComponent<FlareLayer>();
			gameObject.hideFlags = HideFlags.DontSave;
			m_ReflectionCameras[currentCamera] = reflectionCamera;
		}
	}

	protected static float sgn(float a)
	{
		if (a > 0f)
		{
			return 1f;
		}
		if (a < 0f)
		{
			return -1f;
		}
		return 0f;
	}

	protected Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
	{
		Vector3 point = pos + normal * m_ClipPlaneOffset;
		Matrix4x4 worldToCameraMatrix = cam.worldToCameraMatrix;
		Vector3 lhs = worldToCameraMatrix.MultiplyPoint(point);
		Vector3 rhs = worldToCameraMatrix.MultiplyVector(normal).normalized * sideSign;
		return new Vector4(rhs.x, rhs.y, rhs.z, 0f - Vector3.Dot(lhs, rhs));
	}

	protected static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
	{
		reflectionMat.m00 = 1f - 2f * plane[0] * plane[0];
		reflectionMat.m01 = -2f * plane[0] * plane[1];
		reflectionMat.m02 = -2f * plane[0] * plane[2];
		reflectionMat.m03 = -2f * plane[3] * plane[0];
		reflectionMat.m10 = -2f * plane[1] * plane[0];
		reflectionMat.m11 = 1f - 2f * plane[1] * plane[1];
		reflectionMat.m12 = -2f * plane[1] * plane[2];
		reflectionMat.m13 = -2f * plane[3] * plane[1];
		reflectionMat.m20 = -2f * plane[2] * plane[0];
		reflectionMat.m21 = -2f * plane[2] * plane[1];
		reflectionMat.m22 = 1f - 2f * plane[2] * plane[2];
		reflectionMat.m23 = -2f * plane[3] * plane[2];
		reflectionMat.m30 = 0f;
		reflectionMat.m31 = 0f;
		reflectionMat.m32 = 0f;
		reflectionMat.m33 = 1f;
	}

	// Acidbubbles
	private void PrintDebugStatus()
	{
        SuperController.LogMessage("Root objects: " + string.Join("; ", SceneManager.GetActiveScene().GetRootGameObjects().Select(x => x.name).ToArray()));
		SuperController.LogMessage("Original mirrors: " + string.Join("; ", GameObject.FindObjectsOfType<MirrorReflection>().Select(x => GetDebugHierarchy(x.gameObject)).ToArray()));
		SuperController.LogMessage("PoV mirrors: " + string.Join("; ", GameObject.FindObjectsOfType<ImprovedPoVMirrorReflection>().Select(x => GetDebugHierarchy(x.gameObject)).ToArray()));
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

	private void ReplaceMirrorScriptAndCreatedObjects<TBefore, TAfter>(GameObject target)
		where TBefore: MonoBehaviour
		where TAfter: MonoBehaviour
	{
			foreach(var childMirror in target.GetComponentsInChildren<TBefore>().Where(x => !x.name.StartsWith("plugin#"))){
				var childMirrorGameObject = childMirror.gameObject;
				SuperController.LogMessage("- Removing child mirror from " + GetDebugHierarchy(childMirrorGameObject));
				var childMirrorInstanceId = childMirror.GetInstanceID();
				Destroy(childMirror);
				foreach(var childMirrorObject in SceneManager.GetActiveScene().GetRootGameObjects().Where(x => x.name.StartsWith("Mirror Refl Camera id" + childMirrorInstanceId + " for " )))
				{
					SuperController.LogMessage("-   Removing object " + childMirrorObject.name);
					Destroy(childMirrorObject);
				}
				SuperController.LogMessage("- Adding new child mirror on " + GetDebugHierarchy(childMirrorGameObject));
				childMirrorGameObject.AddComponent<TAfter>();
			}
	}
	// /Acidbubbles

	public override void Init()
	{
		// Acidbubbles: Disable the original MirrorReflection in the actual mirror children
		try{
			PrintDebugStatus();
			if(containingAtom == null && gameObject == null){
				SuperController.LogMessage("Mirror Plugin Empty Init");
                return; // It will be called again
            }
			if(containingAtom != null) {
				// We are now in the plugin. Let's inject the component on the mirror itself.
				var pluginTarget = containingAtom.gameObject;
				SuperController.LogMessage("Mirror Plugin Init " + pluginTarget.name + " " + GetInstanceID());
				ReplaceMirrorScriptAndCreatedObjects<MirrorReflection, ImprovedPoVMirrorReflection>(pluginTarget);
				PrintDebugStatus();
				return;
			}
			if(gameObject.name.StartsWith("plugin#")){
				// This is applied on the plugin gameobject, we don't want to initalize a mirror here
				SuperController.LogMessage("Mirror Plugin GameObject Init " + gameObject.name + " " + GetInstanceID());
				return;
			}
			// This is the real thing!
			SuperController.LogMessage("Real Mirror Component Init " + gameObject.name + " " + GetInstanceID());
			_actualMirror = true;
		}
		catch(Exception e)
		{
			SuperController.LogError("Failed to initialize ImprovedPoVMirrorReflection" + e);
		}
		// /Acidbubbles

		Material material = null;
		Renderer component = GetComponent<Renderer>();
		if (component != null)
		{
			Material[] materials = component.materials;
			if (materials != null)
			{
				material = materials[0];
			}
			if (material != null && material.HasProperty("_ReflectionColor"))
			{
				Color color = material.GetColor("_ReflectionColor");
				_currentReflectionHSVColor = HSVColorPicker.RGBToHSV(color.r, color.g, color.b);
			}
			else
			{
				_currentReflectionHSVColor = default(HSVColor);
				_currentReflectionHSVColor.H = 1f;
				_currentReflectionHSVColor.S = 1f;
				_currentReflectionHSVColor.V = 1f;
			}
			SyncReflectionColor(_currentReflectionHSVColor.H, _currentReflectionHSVColor.S, _currentReflectionHSVColor.V);
			reflectionColorJSON = new JSONStorableColor("reflectionColor", _currentReflectionHSVColor, SyncReflectionColor);
			RegisterColor(reflectionColorJSON);
			if (material != null && material.HasProperty("_ReflectionOpacity"))
			{
				SyncReflectionOpacity(material.GetFloat("_ReflectionOpacity"));
				reflectionOpacityJSON = new JSONStorableFloat("reflectionOpacity", _reflectionOpacity, SyncReflectionOpacity, 0f, 1f);
				RegisterFloat(reflectionOpacityJSON);
			}
			if (material != null && material.HasProperty("_ReflectionBlendTexPower"))
			{
				SyncReflectionBlend(material.GetFloat("_ReflectionBlendTexPower"));
				reflectionBlendJSON = new JSONStorableFloat("reflectionBlend", _reflectionBlend, SyncReflectionBlend, 0f, 2f);
				RegisterFloat(reflectionBlendJSON);
			}
			if (material != null && material.HasProperty("_MainTexPower"))
			{
				SyncSurfaceTexturePower(material.GetFloat("_MainTexPower"));
				surfaceTexturePowerJSON = new JSONStorableFloat("surfaceTexturePower", _surfaceTexturePower, SyncSurfaceTexturePower, 0f, 1f);
				RegisterFloat(surfaceTexturePowerJSON);
			}
			if (material != null && material.HasProperty("_SpecularIntensity"))
			{
				SyncSpecularIntensity(material.GetFloat("_SpecularIntensity"));
				specularIntensityJSON = new JSONStorableFloat("specularIntensity", _specularIntensity, SyncSpecularIntensity, 0f, 2f);
				RegisterFloat(specularIntensityJSON);
			}
			List<string> list = new List<string>();
			list.Add("1");
			list.Add("2");
			list.Add("4");
			list.Add("8");
			antiAliasingJSON = new JSONStorableStringChooser("antiAliasing", list, _antiAliasing.ToString(), "Anti-aliasing", SetAntialiasingFromString);
			RegisterStringChooser(antiAliasingJSON);
			disablePixelLightsJSON = new JSONStorableBool("disablePixelLights", _disablePixelLights, SyncDisablePixelLights);
			RegisterBool(disablePixelLightsJSON);
			List<string> list2 = new List<string>();
			list2.Add("512");
			list2.Add("1024");
			list2.Add("2048");
			list2.Add("4096");
			textureSizeJSON = new JSONStorableStringChooser("textureSize", list2, _textureSize.ToString(), "Texture Size", SetTextureSizeFromString);
			RegisterStringChooser(textureSizeJSON);
		}
	}

	public override void InitUI()
	{
		// Acidbubbles: Only for actual mirror
		if(!_actualMirror) return;
		// /Acidbubbles

		if (!(UITransform != null))
		{
			return;
		}
		MirrorReflectionUI componentInChildren = UITransform.GetComponentInChildren<MirrorReflectionUI>();
		if (componentInChildren != null)
		{
			disablePixelLightsJSON.toggle = componentInChildren.disablePixelLightsToggle;
			reflectionColorJSON.colorPicker = componentInChildren.reflectionColorPicker;
			if (reflectionOpacityJSON != null)
			{
				reflectionOpacityJSON.slider = componentInChildren.reflectionOpacitySlider;
			}
			else if (componentInChildren.reflectionOpacityContainer != null)
			{
				componentInChildren.reflectionOpacityContainer.gameObject.SetActive(false);
			}
			if (reflectionBlendJSON != null)
			{
				reflectionBlendJSON.slider = componentInChildren.reflectionBlendSlider;
			}
			else if (componentInChildren.reflectionBlendContainer != null)
			{
				componentInChildren.reflectionBlendContainer.gameObject.SetActive(false);
			}
			if (surfaceTexturePowerJSON != null)
			{
				surfaceTexturePowerJSON.slider = componentInChildren.surfaceTexturePowerSlider;
			}
			else if (componentInChildren.surfaceTexturePowerContainer != null)
			{
				componentInChildren.surfaceTexturePowerContainer.gameObject.SetActive(false);
			}
			if (specularIntensityJSON != null)
			{
				specularIntensityJSON.slider = componentInChildren.specularIntensitySlider;
			}
			else if (componentInChildren.specularIntensityContainer != null)
			{
				componentInChildren.specularIntensityContainer.gameObject.SetActive(false);
			}
			antiAliasingJSON.popup = componentInChildren.antiAliasingPopup;
			textureSizeJSON.popup = componentInChildren.textureSizePopup;
		}
	}

	public override void InitUIAlt()
	{
		// Acidbubbles: Only for actual mirror
		if(!_actualMirror) return;
		// /Acidbubbles

		if (!(UITransformAlt != null))
		{
			return;
		}
		MirrorReflectionUI componentInChildren = UITransformAlt.GetComponentInChildren<MirrorReflectionUI>();
		if (componentInChildren != null)
		{
			disablePixelLightsJSON.toggleAlt = componentInChildren.disablePixelLightsToggle;
			reflectionColorJSON.colorPickerAlt = componentInChildren.reflectionColorPicker;
			if (reflectionOpacityJSON != null)
			{
				reflectionOpacityJSON.sliderAlt = componentInChildren.reflectionOpacitySlider;
			}
			else if (componentInChildren.reflectionOpacityContainer != null)
			{
				componentInChildren.reflectionOpacityContainer.gameObject.SetActive(false);
			}
			if (reflectionBlendJSON != null)
			{
				reflectionBlendJSON.sliderAlt = componentInChildren.reflectionBlendSlider;
			}
			else if (componentInChildren.reflectionBlendContainer != null)
			{
				componentInChildren.reflectionBlendContainer.gameObject.SetActive(false);
			}
			if (surfaceTexturePowerJSON != null)
			{
				surfaceTexturePowerJSON.sliderAlt = componentInChildren.surfaceTexturePowerSlider;
			}
			else if (componentInChildren.surfaceTexturePowerContainer != null)
			{
				componentInChildren.surfaceTexturePowerContainer.gameObject.SetActive(false);
			}
			if (specularIntensityJSON != null)
			{
				specularIntensityJSON.sliderAlt = componentInChildren.specularIntensitySlider;
			}
			else if (componentInChildren.specularIntensityContainer != null)
			{
				componentInChildren.specularIntensityContainer.gameObject.SetActive(false);
			}
			antiAliasingJSON.popupAlt = componentInChildren.antiAliasingPopup;
			textureSizeJSON.popupAlt = componentInChildren.textureSizePopup;
		}
	}

	private void OnDestroy() {
		OnDisable();
	}

	private void OnDisable()
	{
		// Acidbubbles: For the plugin, disable the children POV mirrors, otherwise disable normally
		if(containingAtom != null) {
			SuperController.LogMessage("Mirror Plugin Disable " + containingAtom.gameObject.name + " " + GetInstanceID());
			ReplaceMirrorScriptAndCreatedObjects<ImprovedPoVMirrorReflection, MirrorReflection>(containingAtom.gameObject);
			return;
		}
		if(!_actualMirror) return;
		SuperController.LogMessage("Actual Mirror Disable " + containingAtom.gameObject.name + " " + GetInstanceID());
		// /Acidbubbles

		if ((bool)m_ReflectionTextureRight)
		{
			UnityEngine.Object.DestroyImmediate(m_ReflectionTextureRight);
			m_ReflectionTextureRight = null;
		}
		if ((bool)m_ReflectionTextureLeft)
		{
			UnityEngine.Object.DestroyImmediate(m_ReflectionTextureLeft);
			m_ReflectionTextureLeft = null;
		}
		IDictionaryEnumerator enumerator = m_ReflectionCameras.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				UnityEngine.Object.DestroyImmediate(((Camera)((DictionaryEntry)enumerator.Current).Value).gameObject);
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		m_ReflectionCameras.Clear();
	}

	protected override void Awake()
	{
		if (!awakecalled)
		{
			base.Awake();
			if (Application.isPlaying)
			{
				Init();
				InitUI();
				InitUIAlt();
			}
		}
	}

	private void Update()
	{
		// Acidbubbles: Only for actual mirror
		if(!_actualMirror) return;

		Renderer component = GetComponent<Renderer>();
		if (component != null)
		{
			component.enabled = (globalEnabled || useSameMaterialWhenMirrorDisabled);
		}
		if (useSameMaterialWhenMirrorDisabled)
		{
			if (globalEnabled)
			{
				return;
			}
			Material[] array = (!Application.isPlaying) ? component.sharedMaterials : component.materials;
			Material[] array2 = array;
			foreach (Material material in array2)
			{
				if (material.HasProperty("_ReflectionTex"))
				{
					material.SetTexture("_ReflectionTex", null);
				}
				if (material.HasProperty("_LeftReflectionTex"))
				{
					material.SetTexture("_LeftReflectionTex", null);
				}
				if (material.HasProperty("_RightReflectionTex"))
				{
					material.SetTexture("_RightReflectionTex", null);
				}
			}
		}
		else if (altObjectWhenMirrorDisabled != null)
		{
			altObjectWhenMirrorDisabled.gameObject.SetActive(!globalEnabled);
		}
	}
}
