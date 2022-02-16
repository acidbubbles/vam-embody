using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Handlers;
using SimpleJSON;
using UnityEngine;

public interface IHideGeometryModule : IEmbodyModule
{
    JSONStorableBool hideFaceJSON { get; }
    JSONStorableBool hideHairJSON { get; }
    JSONStorableBool hideClothingJSON { get; }
    JSONStorableBool[] hideFaceMaterials { get; }
}

public class HideGeometryModule : EmbodyModuleBase, IHideGeometryModule
{
    public const string Label = "Hide Geometry";
    public override string storeId => "HideGeometry";
    public override string label => Label;

    private Atom _person;
    private Possessor _possessor;
    private DAZCharacterSelector _selector;
    public JSONStorableBool hideFaceJSON { get; set; }
    public JSONStorableBool hideHairJSON { get; set; }
    public JSONStorableBool hideClothingJSON { get; set; }

    public JSONStorableBool[] hideFaceMaterials { get; } = new[]
    {
        new JSONStorableBool("Lacrimals", true),
        new JSONStorableBool("Pupils", true),
        new JSONStorableBool("Lips", true),
        new JSONStorableBool("Gums", true),
        new JSONStorableBool("Irises", true),
        new JSONStorableBool("Teeth", true),
        new JSONStorableBool("Face", true),
        new JSONStorableBool("Head", true),
        new JSONStorableBool("InnerMouth", true),
        new JSONStorableBool("Tongue", true),
        new JSONStorableBool("EyeReflection", true),
        new JSONStorableBool("Nostrils", true),
        new JSONStorableBool("Cornea", true),
        new JSONStorableBool("Eyelashes", true),
        new JSONStorableBool("Sclera", true),
        new JSONStorableBool("Ears", true),
        new JSONStorableBool("Tear", true),
    };

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

    public override void InitStorables()
    {
        base.InitStorables();
        selectedJSON.defaultVal = true;
        selectedJSON.valNoCallback = selectedJSON.defaultVal;

        hideFaceJSON = new JSONStorableBool("HideFace", true, (bool _) => RefreshHandlers());
        hideHairJSON = new JSONStorableBool("HideHair", true, (bool _) => RefreshHandlers());
        hideClothingJSON = new JSONStorableBool("HideClothing", true, (bool _) => RefreshHandlers());

        foreach (var hideFaceMaterial in hideFaceMaterials)
        {
            hideFaceMaterial.setCallbackFunction = _ => RefreshHandlers();
        }
    }

    public override void InitReferences()
    {
        base.InitReferences();

        _person = containingAtom;
        _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();
        _selector = _person.GetComponentInChildren<DAZCharacterSelector>();

        _ovrCamera = SuperController.singleton.OVRCenterCamera;
        _steamvrCamera = SuperController.singleton.ViveCenterCamera;
        _monitorCamera = SuperController.singleton.MonitorCenterCamera;
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
            if (!RegisterHandler(new SkinHandler(_character.skin, hideFaceMaterials.Where(x => x.val).Select(x => x.name), hideFaceMaterials.Length)))
                return;
        }

        _hairHashSum = _selector.hairItems.Where(h => h.active).Aggregate(0, (s, h) => s ^ h.GetHashCode());
        if (hideHairJSON.val)
        {
            var hair = _selector.hairItems
                .Where(h => h != null)
                .Where(h => h.active)
                .Where(IsHeadHair)
                .Where(HairHandler.Supports)
                .ToArray();
            foreach (var h in hair)
            {
                if (!RegisterHandler(new HairHandler(h)))
                    return;
            }
        }
        else
        {
            var hair = _selector.hairItems
                .Where(h => h != null)
                .Where(h => h.active)
                .Where(IsEyesHair)
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
                .Where(IsHeadClothing)
                .ToArray();
            foreach (var c in clothes)
            {
                if (!RegisterHandler(new ClothingHandler(c)))
                    return;
            }
        }

        if (!_dirty) _tryAgainAttempts = 0;
    }

    private static bool IsHeadHair(DAZHairGroup h)
    {
        var hasTags = h.tagsArray != null && h.tagsArray.Length > 0;
        if (hasTags)
        {
            if (
                Array.IndexOf(h.tagsArray, "head") > -1 ||
                Array.IndexOf(h.tagsArray, "face") > -1
            ) return true;
            // Oops, forgot to tag
            if (
                Array.IndexOf(h.tagsArray, "arms") == -1 &&
                Array.IndexOf(h.tagsArray, "full body") == -1 &&
                Array.IndexOf(h.tagsArray, "genital") == -1 &&
                Array.IndexOf(h.tagsArray, "legs") == -1 &&
                Array.IndexOf(h.tagsArray, "torso") == -1
            ) return true;
        }
        if (h.displayName.IndexOf("lash", StringComparison.OrdinalIgnoreCase) > -1) return true;
        if (h.displayName.IndexOf("eyebrow", StringComparison.OrdinalIgnoreCase) > -1) return true;
        return !hasTags;
    }

    private static bool IsEyesHair(DAZHairGroup h)
    {
        var hasTags = h.tagsArray != null && h.tagsArray.Length > 0;
        if (hasTags)
        {
            if (
                Array.IndexOf(h.tagsArray, "face") > -1
            ) return true;
        }
        if (h.displayName.IndexOf("lash", StringComparison.OrdinalIgnoreCase) > -1) return true;
        if (h.displayName.IndexOf("eyebrow", StringComparison.OrdinalIgnoreCase) > -1) return true;
        return false;
    }

    private static bool IsHeadClothing(DAZClothingItem c)
    {
        if (c.displayName.IndexOf("eye", StringComparison.OrdinalIgnoreCase) > -1) return true;
        if (c.displayName.IndexOf("lashes", StringComparison.OrdinalIgnoreCase) > -1) return true;
        if (c.displayName.IndexOf("brows", StringComparison.OrdinalIgnoreCase) > -1) return true;
        if (c.displayName.IndexOf("hair", StringComparison.OrdinalIgnoreCase) > -1) return true;
        if (c.displayName.IndexOf("face", StringComparison.OrdinalIgnoreCase) > -1) return true;
        if (c.displayName.IndexOf("lips", StringComparison.OrdinalIgnoreCase) > -1) return true;
        if (c.tagsArray == null) return false;
        if (c.tagsArray.Length == 0) return false;
        if (Array.IndexOf(c.tagsArray, "head") > -1) return true;
        if (Array.IndexOf(c.tagsArray, "glasses") > -1) return true;
        return false;
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

    [SuppressMessage("ReSharper", "RedundantJumpStatement")]
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

    public override void StoreJSON(JSONClass jc, bool toProfile, bool toScene)
    {
        base.StoreJSON(jc, toProfile, toScene);

        hideFaceJSON.StoreJSON(jc);
        hideHairJSON.StoreJSON(jc);
        hideClothingJSON.StoreJSON(jc);
        foreach (var hideFaceMaterial in hideFaceMaterials) hideFaceMaterial.StoreJSON(jc);
    }

    public override void RestoreFromJSON(JSONClass jc, bool fromProfile, bool fromScene)
    {
        base.RestoreFromJSON(jc, fromProfile, fromScene);

        hideFaceJSON.RestoreFromJSON(jc);
        hideHairJSON.RestoreFromJSON(jc);
        hideClothingJSON.RestoreFromJSON(jc);
        foreach (var hideFaceMaterial in hideFaceMaterials) hideFaceMaterial.RestoreFromJSON(jc);
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();

        hideFaceJSON.SetValToDefault();
        hideHairJSON.SetValToDefault();
        hideClothingJSON.SetValToDefault();
        foreach (var hideFaceMaterial in hideFaceMaterials) hideFaceMaterial.SetValToDefault();
    }
}
