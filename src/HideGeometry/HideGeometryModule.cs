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

    private readonly List<IHandler> _handlers = new List<IHandler>();
    // For change detection purposes
    private DAZCharacter _character;

    // Requires re-generating all shaders and materials, either because last frame was not ready or because something changed
    private bool _dirty;
    // To avoid spamming errors when something failed
    private bool _failedOnce;
    // When waiting for a model to load, how long before we abandon
    private int _tryAgainAttempts;
    private int _hairHashSum;
    private int _clothingHashSum;

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
            for (var i = 0; i < _handlers.Count; i++)
                _handlers[i].BeforeRender();
        }
        catch (Exception e)
        {
            if (_failedOnce) return;
            _failedOnce = true;
            SuperController.LogError($"Embody: Failed to execute {nameof(OnGeometryPreRender)}: {e}");
        }
    }

    private void OnGeometryPostRender(Camera cam)
    {
        // TODO: Instead only register on cameras we need on enable?
        if (!IsPovCamera(cam)) return;

        try
        {
            for (var i = 0; i < _handlers.Count; i++)
                _handlers[i].AfterRender();
        }
        catch (Exception e)
        {
            if (_failedOnce) return;
            _failedOnce = true;
            SuperController.LogError($"Embody: Failed to execute {nameof(OnGeometryPostRender)}: {e}");
        }
    }

    private static bool IsPovCamera(Camera cam)
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

        ApplyPossessorMeshVisibility(false);

        RegisterHandlers();

        Camera.onPreRender += OnGeometryPreRender;
        Camera.onPostRender += OnGeometryPostRender;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        Camera.onPreRender -= OnGeometryPreRender;
        Camera.onPostRender -= OnGeometryPostRender;

        ApplyPossessorMeshVisibility(true);

        ClearHandlers();
    }

    public void ClearHandlers()
    {
        foreach (var handler in _handlers)
            handler.Restore();
        _handlers.Clear();
        _character = null;
        _hairHashSum = 0;
        _clothingHashSum = 0;
        _dirty = false;
    }

    public void RegisterHandlers()
    {
        _dirty = false;

        // ReSharper disable once Unity.NoNullPropagation
        if (ReferenceEquals(_selector.selectedCharacter?.skin, null))
        {
            MakeDirty("skin", "is not yet loaded.");
            return;
        }

        _character = _selector.selectedCharacter;
        if (_character == null)
        {
            enabled = false;
            return;
        }

        if (!RegisterHandler(new SkinHandler(_character.skin)))
            return;

        _hairHashSum = _selector.hairItems.Where(h => h.active).Aggregate(0, (s, h) => s ^ h.GetHashCode());
        var hair = _selector.hairItems
            .Where(h => h != null)
            .Where(h => h.active)
            .Where(h => h.tagsArray == null || h.tagsArray.Length == 0 || Array.IndexOf(h.tagsArray, "head") > -1 || Array.IndexOf(h.tagsArray, "face") > -1)
            .Where(HairHandler.Supports)
            .ToArray();
        foreach (var h in hair)
        {
            if (!RegisterHandler(new HairHandler(h)))
                return;
        }

        _clothingHashSum = _selector.clothingItems.Where(h => h.active).Aggregate(0, (s, c) => s ^ c.GetHashCode());
        var clothes = _selector.clothingItems
            .Where(c => c.active)
            .Where(c => c.tagsArray != null && Array.IndexOf(c.tagsArray, "head") > -1)
            .ToArray();
        foreach (var c in clothes)
        {
            if (!RegisterHandler(new ClothingHandler(c)))
                return;
        }

        if (!_dirty) _tryAgainAttempts = 0;
    }

    private bool RegisterHandler(IHandler handler)
    {
        _handlers.Add(handler);
        var configured = handler.Configure();
        if (!configured)
        {
            ClearHandlers();
            return false;
        }
        return true;
    }

    public void RefreshHandlers()
    {
        ClearHandlers();
        RegisterHandlers();
    }

    public void Update()
    {
        try
        {
            if (_dirty)
            {
                RegisterHandlers();
                return;
            }

            if (_selector.selectedCharacter != _character)
            {
                RefreshHandlers();
                return;
            }
            // TODO: Validate if this actually works, and if it does use loop instead of linq
            if (_hairHashSum != _selector.hairItems.Where(h => h.active).Aggregate(0, (s, h) => s ^ h.GetHashCode()))
            {
                RefreshHandlers();
                return;
            }
            if (_clothingHashSum != _selector.clothingItems.Where(h => h.active).Aggregate(0, (s, c) => s ^ c.GetHashCode()))
            {
                RefreshHandlers();
                return;
            }
        }
        catch (Exception e)
        {
            if (_failedOnce) return;
            _failedOnce = true;
            SuperController.LogError("Failed to update HideGeometry: " + e);
        }
    }

    private void MakeDirty(string what, string reason)
    {
        _dirty = true;
        _tryAgainAttempts++;
        if (_tryAgainAttempts > 90 * 20) // Approximately 20 to 40 seconds
        {
            SuperController.LogError($"Failed to apply HideGeometry. Reason: {what} {reason}. Try reloading the plugin, or report the issue to @Acidbubbles.");
            enabled = false;
        }
    }

    private void ApplyPossessorMeshVisibility(bool active)
    {
        try
        {
            var possessorTransform = _possessor.gameObject.transform;
            possessorTransform.Find("Capsule")?.gameObject.SetActive(active);
            possessorTransform.Find("Sphere1")?.gameObject.SetActive(active);
            possessorTransform.Find("Sphere2")?.gameObject.SetActive(active);
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
