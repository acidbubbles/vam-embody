using System;
using System.Linq;
using UnityEngine;

public class SnugSettingsScreen : ScreenBase, IScreen
{
    private readonly ISnugModule _snug;
    public const string ScreenName = SnugModule.Label;

    private JSONStorableStringChooser _selectedAnchorsJSON;
    private JSONStorableFloat _handOffsetXJSON, _handOffsetYJSON, _handOffsetZJSON;
    private JSONStorableFloat _handRotateXJSON, _handRotateYJSON, _handRotateZJSON;
    private JSONStorableFloat _anchorVirtSizeXJSON, _anchorVirtSizeZJSON;
    private JSONStorableFloat _anchorVirtOffsetXJSON, _anchorVirtOffsetYJSON, _anchorVirtOffsetZJSON;
    private JSONStorableFloat _anchorPhysOffsetXJSON, _anchorPhysOffsetYJSON, _anchorPhysOffsetZJSON;
    private JSONStorableFloat _anchorPhysSizeXJSON, _anchorPhysSizeZJSON;
    private JSONStorableBool _anchorActiveJSON;

    public SnugSettingsScreen(EmbodyContext context, ISnugModule snug)
        : base(context)
    {
        _snug = snug;
    }

    public void Show()
    {
        if (ShowNotSelected(_snug.selectedJSON.val)) return;

        CreateToggle(_snug.previewSnugOffsetJSON).label = "Preview Offset (Real v.s. In-Game)";
        CreateButton("Arm hands for record").button.onClick.AddListener(() =>
        {
            SuperController.singleton.ArmAllControlledControllersForRecord();
            context.containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "lHandControl").GetComponent<MotionAnimationControl>().armedForRecord = true;
            context.containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "rHandControl").GetComponent<MotionAnimationControl>().armedForRecord = true;
        });
        CreateToggle(_snug.disableSelectionJSON).label = "Disable Person Grab";
        CreateSpacer().height = 10f;
        CreateSpacer().height = 10f;
        InitHandsSettingsUI();
        InitAnchorsUI();

        SyncSelectedAnchorJSON("");
    }

    private void InitHandsSettingsUI()
    {
        CreateSlider(_snug.falloffJSON, false);
    }

    private void InitAnchorsUI()
    {
        _selectedAnchorsJSON = new JSONStorableStringChooser("Selected Anchor", _snug.anchorPoints.Select(a => a.Label).ToList(), "Chest", "Anchor", SyncSelectedAnchorJSON)
            {isStorable = false};
        CreateScrollablePopup(_selectedAnchorsJSON, true);

        _anchorActiveJSON = new JSONStorableBool("Anchor Active", true, (bool _) => UpdateAnchor(0)) {isStorable = false};
        CreateToggle(_anchorActiveJSON, true);

        _anchorPhysSizeXJSON = new JSONStorableFloat("Size X (Width)", 1f, UpdateAnchor, 0.01f, 1f, true) {isStorable = false};
        CreateSlider(_anchorPhysSizeXJSON, true);
        _anchorPhysSizeZJSON = new JSONStorableFloat("Size Z (Depth)", 1f, UpdateAnchor, 0.01f, 1f, true) {isStorable = false};
        CreateSlider(_anchorPhysSizeZJSON, true);

        _anchorPhysOffsetXJSON = new JSONStorableFloat("Offset X (Left/Right)", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorPhysOffsetXJSON, true);
        _anchorPhysOffsetYJSON = new JSONStorableFloat("Offset Y (Up/Down)", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorPhysOffsetYJSON, true);
        _anchorPhysOffsetZJSON = new JSONStorableFloat("Offset Z (Forw./Back.)", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorPhysOffsetZJSON, true);

        // TODO: Since we don't need this, should even try saving it at all?
        /*
        _anchorVirtSizeXJSON = new JSONStorableFloat("In-Game Size X", 1f, UpdateAnchor, 0.01f, 1f, true) {isStorable = false};
        CreateSlider(_anchorVirtSizeXJSON, true);
        _anchorVirtSizeZJSON = new JSONStorableFloat("In-Game Size Z", 1f, UpdateAnchor, 0.01f, 1f, true) {isStorable = false};

        CreateSlider(_anchorVirtSizeZJSON, true);
        _anchorVirtOffsetXJSON = new JSONStorableFloat("In-Game Offset X", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorVirtOffsetXJSON, true);
        _anchorVirtOffsetYJSON = new JSONStorableFloat("In-Game Offset Y", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorVirtOffsetYJSON, true);
        _anchorVirtOffsetZJSON = new JSONStorableFloat("In-Game Offset Z", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorVirtOffsetZJSON, true);
        */
    }

    private void SyncSelectedAnchorJSON(string _)
    {
        var anchor = _snug.anchorPoints.FirstOrDefault(a => a.Label == _selectedAnchorsJSON.val);
        if (anchor == null) throw new NullReferenceException($"Could not find the selected anchor {_selectedAnchorsJSON.val}");
        _anchorActiveJSON.valNoCallback = anchor.Active;
        _anchorPhysSizeXJSON.valNoCallback = anchor.RealLifeSize.x;
        _anchorPhysSizeZJSON.valNoCallback = anchor.RealLifeSize.z;
        _anchorPhysOffsetXJSON.valNoCallback = anchor.RealLifeOffset.x;
        _anchorPhysOffsetYJSON.valNoCallback = anchor.RealLifeOffset.y;
        _anchorPhysOffsetZJSON.valNoCallback = anchor.RealLifeOffset.z;
        /*
        _anchorVirtSizeXJSON.valNoCallback = anchor.InGameSize.x;
        _anchorVirtSizeZJSON.valNoCallback = anchor.InGameSize.z;
        _anchorVirtOffsetXJSON.valNoCallback = anchor.InGameOffset.x;
        _anchorVirtOffsetYJSON.valNoCallback = anchor.InGameOffset.y;
        _anchorVirtOffsetZJSON.valNoCallback = anchor.InGameOffset.z;
        */
    }

    private void UpdateAnchor(float _)
    {
        var anchor = _snug.anchorPoints.FirstOrDefault(a => a.Label == _selectedAnchorsJSON.val);
        if (anchor == null) throw new NullReferenceException($"Could not find the selected anchor {_selectedAnchorsJSON.val}");
        anchor.InGameSize = new Vector3(_anchorVirtSizeXJSON.val, 1f, _anchorVirtSizeZJSON.val);
        anchor.InGameOffset = new Vector3(_anchorVirtOffsetXJSON.val, _anchorVirtOffsetYJSON.val, _anchorVirtOffsetZJSON.val);
        anchor.RealLifeSize = new Vector3(_anchorPhysSizeXJSON.val, 1f, _anchorPhysSizeZJSON.val);
        anchor.RealLifeOffset = new Vector3(_anchorPhysOffsetXJSON.val, _anchorPhysOffsetYJSON.val, _anchorPhysOffsetZJSON.val);
        if (anchor.Locked && !_anchorActiveJSON.val)
        {
            _anchorActiveJSON.valNoCallback = true;
        }
        else if (anchor.Active != _anchorActiveJSON.val)
        {
            anchor.Active = _anchorActiveJSON.val;
            anchor.Update();
        }

        anchor.Update();
    }
}
