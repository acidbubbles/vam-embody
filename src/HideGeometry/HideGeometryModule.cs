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
    JSONStorableBool hideClothingJSON { get; }
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
    public JSONStorableBool hideClothingJSON { get; set; }

    private readonly List<IHandler> _handlers = new List<IHandler>();

    private bool _dirty;
    private bool _failedOnce;
    private int _tryAgainAttempts;
    private DAZCharacter _character;
    private int _hairHashSum;
    private int _clothingHashSum;
    private Camera _ovrCamera;
    private Camera _steamvrCamera;
    private Camera _monitorCamera;

    public override void Awake()
    {
        try
        {
            base.Awake();

            _person = containingAtom;
            _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();
            _selector = _person.GetComponentInChildren<DAZCharacterSelector>();

            _ovrCamera = SuperController.singleton.OVRCenterCamera;
            _steamvrCamera = SuperController.singleton.ViveCenterCamera;
            _monitorCamera = SuperController.singleton.MonitorCenterCamera;

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

    private bool IsPovCamera(Camera cam)
    {
        if (ReferenceEquals(cam, _ovrCamera)) return true;
        if (ReferenceEquals(cam, _steamvrCamera)) return true;
        if (ReferenceEquals(cam, _monitorCamera)) return true;
        // if(cam.name == "MiniCamera") return true;
        return false;
    }

    private void InitControls()
    {
        try
        {
            hideFaceJSON = new JSONStorableBool("HideFace", true, (bool _) => RefreshHandlers());
            hideHairJSON = new JSONStorableBool("HideHair", true, (bool _) => RefreshHandlers());
            hideClothingJSON = new JSONStorableBool("HideClothing", true, (bool _) => RefreshHandlers());
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
            handler.Dispose();
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
            MakeDirty("character", "is not yet loaded.");
            return;
        }

        if (hideFaceJSON.val)
        {
            if (!RegisterHandler(new SkinHandler(_character.skin)))
                return;
        }

        _hairHashSum = _selector.hairItems.Where(h => h.active).Aggregate(0, (s, h) => s ^ h.GetHashCode());
        if (hideHairJSON.val)
        {
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
        }

        if (hideClothingJSON.val)
        {
            _clothingHashSum = _selector.clothingItems.Where(h => h.active).Aggregate(0, (s, c) => s ^ c.GetHashCode());
            var clothes = _selector.clothingItems
                .Where(c => c.active)
                .Where(c => c.tagsArray != null && c.tagsArray.Length > 0 ? Array.IndexOf(c.tagsArray, "head") > -1 : c.displayName.IndexOf("eye", StringComparison.OrdinalIgnoreCase) > -1)
                .ToArray();
            foreach (var c in clothes)
            {
                if (!RegisterHandler(new ClothingHandler(c)))
                    return;
            }
        }

        if (!_dirty) _tryAgainAttempts = 0;
    }

    private bool RegisterHandler(IHandler handler)
    {
        _handlers.Add(handler);
        var configured = handler.Prepare();
        if (!configured)
        {
            ClearHandlers();
            return false;
        }
        return true;
    }

    public void RefreshHandlers()
    {
        if (!enabled) return;
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

            if (hideHairJSON.val)
            {
                var hairHashSum = 0;
                for (var i = 0; i < _selector.hairItems.Length; i++)
                {
                    var hair = _selector.hairItems[i];
                    if (!hair.active) continue;
                    hairHashSum ^= hair.GetHashCode();
                }

                if (_hairHashSum != hairHashSum)
                {
                    RefreshHandlers();
                    return;
                }
            }

            if (hideClothingJSON.val)
            {
                var clothingHashSum = 0;
                for (var i = 0; i < _selector.clothingItems.Length; i++)
                {
                    var clothing = _selector.clothingItems[i];
                    if (!clothing.active) continue;
                    clothingHashSum ^= clothing.GetHashCode();
                }

                if (_clothingHashSum != clothingHashSum)
                {
                    RefreshHandlers();
                    return;
                }
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
        hideClothingJSON.StoreJSON(jc);
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);

        hideFaceJSON.RestoreFromJSON(jc);
        hideHairJSON.RestoreFromJSON(jc);
        hideClothingJSON.RestoreFromJSON(jc);
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();

        hideFaceJSON.SetValToDefault();
        hideHairJSON.SetValToDefault();
        hideClothingJSON.SetValToDefault();
    }
}
