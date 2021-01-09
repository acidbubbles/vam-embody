// TODO: Hide Hunting Succubus's eyes and cua hair

using System;
using System.Collections.Generic;
using System.Linq;
using Handlers;
using SimpleJSON;
using UnityEngine;

public interface IHideGeometryModule : IEmbodyModule
{
    JSONStorableBool hideFaceJSON { get; }
    JSONStorableBool hideHairJSON { get; }
}

public class HideGeometryModule : EmbodyModuleBase, IHideGeometryModule
{
    public const string Label = "Hide Geometry";
    public override string storeId => "HideGeometry";
    public override string label => Label;
    protected override bool shouldBeSelectedByDefault => true;

    private Atom _person;
    private Possessor _possessor;
    private DAZCharacterSelector _selector;
    public JSONStorableBool hideFaceJSON { get; set; }
    public JSONStorableBool hideHairJSON { get; set; }

    private SkinHandler _skinHandler;
    private List<HairHandler> _hairHandlers;
    // For change detection purposes
    private DAZCharacter _character;
    private DAZHairGroup[] _hair;

    // Requires re-generating all shaders and materials, either because last frame was not ready or because something changed
    private bool _dirty;
    // To avoid spamming errors when something failed
    private bool _failedOnce;
    // When waiting for a model to load, how long before we abandon
    private int _tryAgainAttempts;

    public override void Awake()
    {
        try
        {
            base.Awake();

            _person = containingAtom;
            _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();
            _selector = _person.GetComponentInChildren<DAZCharacterSelector>();

            InitControls();
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to initialize HideGeometry: " + e);
        }
    }

    private void OnGeometryPreRender(Camera cam)
    {
        if (!IsPovCamera(cam)) return;

        try
        {
            _skinHandler?.BeforeRender();
            _hairHandlers?.ForEach(x =>
            {
                x?.BeforeRender();
            });
        }
        catch (Exception e)
        {
            if (_failedOnce) return;
            _failedOnce = true;
            SuperController.LogError("Failed to execute pre render HideGeometry: " + e);
        }
    }

    private void OnGeometryPostRender(Camera cam)
    {
        // TODO: Instead only register on cameras we need on enable?
        if (!IsPovCamera(cam)) return;

        try
        {
            _skinHandler?.AfterRender();
            _hairHandlers?.ForEach(x =>
            {
                x?.AfterRender();
            });
        }
        catch (Exception e)
        {
            if (_failedOnce) return;
            _failedOnce = true;
            SuperController.LogError("Failed to execute post render HideGeometry: " + e);
        }
    }

    private bool IsPovCamera(Camera cam)
    {
        return
            // Oculus Rift
            cam.name == "CenterEyeAnchor" ||
            // Steam VR
            cam.name == "Camera (eye)" ||
            // Desktop
            cam.name == "MonitorRig"; /* ||
            // Window Camera
            cam.name == "MiniCamera";
            */
    }

    private void InitControls()
    {
        try
        {
            hideFaceJSON = new JSONStorableBool("Hide face", true, (bool _) => _dirty = true);
            hideHairJSON = new JSONStorableBool("Hide hair", true, (bool _) => _dirty = true);
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to register controls: " + e);
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();

        Camera.onPreRender += OnGeometryPreRender;
        Camera.onPostRender += OnGeometryPostRender;

        ApplyAll(true);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        Camera.onPreRender -= OnGeometryPreRender;
        Camera.onPostRender -= OnGeometryPostRender;

        if (ReferenceEquals(_person, null)) return;

        _dirty = false;
        ApplyAll(false);
    }

    public void Update()
    {
        try
        {
            if (_dirty)
            {
                _dirty = false;
                ApplyAll(true);
            }
            else if (_selector.selectedCharacter != _character)
            {
                _skinHandler?.Restore();
                _skinHandler = null;
                ApplyAll(true);
            }
            else if (!_selector.hairItems.Where(h => h.active).SequenceEqual(_hair))
            {
                // Note: This only checks if the first hair changed. It'll be good enough for most purposes, but imperfect.
                if (_hairHandlers != null)
                {
                    _hairHandlers.ForEach(x =>
                    {
                        x?.Restore();
                    });
                    _hairHandlers = null;
                }
                ApplyAll(true);
            }
        }
        catch (Exception e)
        {
            if (_failedOnce) return;
            _failedOnce = true;
            SuperController.LogError("Failed to update HideGeometry: " + e);
        }
    }

    private void ApplyAll(bool active)
    {
        // Try again next frame
        // ReSharper disable once Unity.NoNullPropagation
        if (ReferenceEquals(_selector.selectedCharacter?.skin, null))
        {
            MakeDirty("Skin not yet loaded.");
            return;
        }

        _character = _selector.selectedCharacter;
        _hair = _selector.hairItems
            .Where(h => h != null)
            .Where(h => h.active)
            .Where(h => h.tagsArray == null || h.tagsArray.Length == 0 || h.tagsArray.Contains("head") || h.tagsArray.Contains("face"))
            .ToArray();

        ApplyPossessorMeshVisibility(active);
        if (UpdateHandler(ref _skinHandler, active && hideFaceJSON.val))
            ConfigureHandler("Skin", ref _skinHandler, _skinHandler.Configure(_character.skin));
        if (_hairHandlers == null)
            _hairHandlers = new List<HairHandler>(new HairHandler[_hair.Length]);
        for (var i = 0; i < _hairHandlers.Count; i++)
        {
            var hairHandler = _hairHandlers[i];
            if (UpdateHandler(ref hairHandler, active && hideHairJSON.val))
                ConfigureHandler("Hair", ref hairHandler, hairHandler.Configure(_hair[i]));
            _hairHandlers[i] = hairHandler;
        }

        if (!_dirty) _tryAgainAttempts = 0;
    }

    private void MakeDirty(string reason)
    {
        _dirty = true;
        _tryAgainAttempts++;
        if (_tryAgainAttempts > 90 * 20) // Approximately 20 to 40 seconds
        {
            SuperController.LogError("Failed to apply HideGeometry. Reason: " + reason + ". Try reloading the plugin, or report the issue to @Acidbubbles.");
            enabled = false;
        }
    }

    private void ConfigureHandler<T>(string what, ref T handler, int result)
     where T : IHandler, new()
    {
        switch (result)
        {
            case HandlerConfigurationResult.Success:
                break;
            case HandlerConfigurationResult.CannotApply:
                handler = default(T);
                break;
            case HandlerConfigurationResult.TryAgainLater:
                handler = default(T);
                MakeDirty(what + " is still waiting for assets to be ready.");
                break;
        }
    }

    private bool UpdateHandler<T>(ref T handler, bool active)
     where T : IHandler, new()
    {
        if (handler == null && active)
        {
            handler = new T();
            return true;
        }

        if (handler != null && active)
        {
            handler.Restore();
            handler = new T();
            return true;
        }

        if (handler != null && !active)
        {
            handler.Restore();
            handler = default(T);
        }

        return false;
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

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);

        hideFaceJSON.StoreJSON(jc);
        hideHairJSON.StoreJSON(jc);
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);

        hideFaceJSON.RestoreFromJSON(jc);
        hideHairJSON.RestoreFromJSON(jc);
    }
}
