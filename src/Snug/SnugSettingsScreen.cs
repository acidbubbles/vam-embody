// #define ALLOW_SNUG_INGAME_EDIT
using System;
using System.Linq;
using UnityEngine;

public class SnugSettingsScreen : ScreenBase, IScreen
{
    private readonly ISnugModule _snug;
    public const string ScreenName = SnugModule.Label;

    private JSONStorableStringChooser _selectedAnchorsJSON;
    private JSONStorableBool _anchorActiveJSON;
    // ReSharper disable InconsistentNaming IdentifierTypo
    private JSONStorableFloat _anchorRealOffsetXJSON, _anchorRealOffsetYJSON, _anchorRealOffsetZJSON;
    private JSONStorableFloat _anchorRealSizeXJSON, _anchorRealSizeZJSON;
    #if(ALLOW_SNUG_INGAME_EDIT)
    private JSONStorableFloat _anchorInGameOffsetXJSON, _anchorInGameOffsetYJSON, _anchorInGameOffsetZJSON;
    private JSONStorableFloat _anchorInGameSizeXJSON, _anchorInGameSizeZJSON;
    #endif
    // ReSharper restore InconsistentNaming IdentifierTypo

    public SnugSettingsScreen(EmbodyContext context, ISnugModule snug)
        : base(context)
    {
        _snug = snug;
    }

    public void Show()
    {
        if (ShowNotSelected(_snug.selectedJSON.val)) return;

        if (!context.embody.activeJSON.val)
            _snug.autoSetup.AutoSetup();

        CreateToggle(_snug.previewSnugOffsetJSON).label = "Preview Offset (Real v.s. In-Game)";
        CreateToggle(_snug.disableSelfGrabJSON).label = "Disable Person Grab";
        CreateSlider(_snug.falloffDistanceJSON, false).label = "Falloff Distance";
        CreateSlider(_snug.falloffMidPointJSON, false).label = "Falloff Mid-point";
        InitAnchorsUI();

        SyncSelectedAnchorJSON("");
    }

    private void InitAnchorsUI()
    {
        _selectedAnchorsJSON = new JSONStorableStringChooser(
            "Selected Anchor",
            _snug.anchorPoints
                .Where(a => !a.locked)
                .Select(a => a.label)
                .ToList(),
            "Chest",
            "Anchor",
            SyncSelectedAnchorJSON
        );
        CreateScrollablePopup(_selectedAnchorsJSON, true);

        _anchorActiveJSON = new JSONStorableBool("Anchor Active", true, (bool _) => UpdateAnchor(0)) {isStorable = false};
        CreateToggle(_anchorActiveJSON, true);

        _anchorRealSizeXJSON = new JSONStorableFloat("Size X (Width)", 1f, UpdateAnchor, 0.01f, 1f, true) {isStorable = false};
        CreateSlider(_anchorRealSizeXJSON, true);
        _anchorRealSizeZJSON = new JSONStorableFloat("Size Z (Depth)", 1f, UpdateAnchor, 0.01f, 1f, true) {isStorable = false};
        CreateSlider(_anchorRealSizeZJSON, true);

        _anchorRealOffsetXJSON = new JSONStorableFloat("Offset X (Left/Right)", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorRealOffsetXJSON, true);
        _anchorRealOffsetYJSON = new JSONStorableFloat("Offset Y (Up/Down)", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorRealOffsetYJSON, true);
        _anchorRealOffsetZJSON = new JSONStorableFloat("Offset Z (Forw./Back.)", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorRealOffsetZJSON, true);

        #if(ALLOW_SNUG_INGAME_EDIT)
        _anchorInGameSizeXJSON = new JSONStorableFloat("In-Game Size X (Width)", 1f, UpdateAnchor, 0.01f, 1f, true) {isStorable = false};
        CreateSlider(_anchorInGameSizeXJSON, true);
        _anchorInGameSizeZJSON = new JSONStorableFloat("In-Game Size Z (Depth)", 1f, UpdateAnchor, 0.01f, 1f, true) {isStorable = false};
        CreateSlider(_anchorInGameSizeZJSON, true);

        _anchorInGameOffsetXJSON = new JSONStorableFloat("In-Game Offset X (Left/Right)", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorInGameOffsetXJSON, true);
        _anchorInGameOffsetYJSON = new JSONStorableFloat("In-Game Offset Y (Up/Down)", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorInGameOffsetYJSON, true);
        _anchorInGameOffsetZJSON = new JSONStorableFloat("In-Game Offset Z (Forw./Back.)", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorInGameOffsetZJSON, true);
        #endif
    }

    private void SyncSelectedAnchorJSON(string _)
    {
        var anchor = _snug.anchorPoints.FirstOrDefault(a => a.label == _selectedAnchorsJSON.val);
        if (anchor == null) throw new NullReferenceException($"Could not find the selected anchor {_selectedAnchorsJSON.val}");
        _anchorActiveJSON.valNoCallback = anchor.active;
        _anchorRealSizeXJSON.defaultVal = anchor.realLifeSizeDefault.x;
        _anchorRealSizeXJSON.valNoCallback = anchor.realLifeSize.x;
        _anchorRealSizeZJSON.defaultVal = anchor.realLifeSizeDefault.z;
        _anchorRealSizeZJSON.valNoCallback = anchor.realLifeSize.z;
        _anchorRealOffsetXJSON.defaultVal = anchor.realLifeOffsetDefault.x;
        _anchorRealOffsetXJSON.valNoCallback = anchor.realLifeOffset.x;
        _anchorRealOffsetYJSON.defaultVal = anchor.realLifeOffsetDefault.y;
        _anchorRealOffsetYJSON.valNoCallback = anchor.realLifeOffset.y;
        _anchorRealOffsetZJSON.defaultVal = anchor.realLifeSizeDefault.z;
        _anchorRealOffsetZJSON.valNoCallback = anchor.realLifeOffset.z;
        #if(ALLOW_SNUG_INGAME_EDIT)
        _anchorInGameSizeXJSON.defaultVal = anchor.inGameSizeDefault.x;
        _anchorInGameSizeXJSON.valNoCallback = anchor.inGameSize.x;
        _anchorInGameSizeZJSON.defaultVal = anchor.inGameSizeDefault.z;
        _anchorInGameSizeZJSON.valNoCallback = anchor.inGameSize.z;
        _anchorInGameOffsetXJSON.defaultVal = anchor.inGameOffsetDefault.x;
        _anchorInGameOffsetXJSON.valNoCallback = anchor.inGameOffset.x;
        _anchorInGameOffsetYJSON.defaultVal = anchor.inGameOffsetDefault.y;
        _anchorInGameOffsetYJSON.valNoCallback = anchor.inGameOffset.y;
        _anchorInGameOffsetZJSON.defaultVal = anchor.inGameSizeDefault.z;
        _anchorInGameOffsetZJSON.valNoCallback = anchor.inGameOffset.z;
        #endif
    }

    private void UpdateAnchor(float _)
    {
        var anchor = _snug.anchorPoints.FirstOrDefault(a => a.label == _selectedAnchorsJSON.val);
        if (anchor == null) throw new NullReferenceException($"Could not find the selected anchor {_selectedAnchorsJSON.val}");
        anchor.realLifeSize = new Vector3(_anchorRealSizeXJSON.val, 1f, _anchorRealSizeZJSON.val);
        anchor.realLifeOffset = new Vector3(_anchorRealOffsetXJSON.val, _anchorRealOffsetYJSON.val, _anchorRealOffsetZJSON.val);
        #if(ALLOW_SNUG_INGAME_EDIT)
        anchor.inGameSize = new Vector3(_anchorInGameSizeXJSON.val, 1f, _anchorInGameSizeZJSON.val);
        anchor.inGameOffset = new Vector3(_anchorInGameOffsetXJSON.val, _anchorInGameOffsetYJSON.val, _anchorInGameOffsetZJSON.val);
        #endif
        if (anchor.locked && !_anchorActiveJSON.val)
        {
            _anchorActiveJSON.valNoCallback = true;
        }
        else if (anchor.active != _anchorActiveJSON.val)
        {
            anchor.active = _anchorActiveJSON.val;
        }
        anchor.Update();
        #if(!ALLOW_SNUG_INGAME_EDIT)
        _snug.autoSetup.AutoSetup();
        #endif
    }
}
