using System.Collections.Generic;

public class CollapsibleSection
{
    private readonly MVRScript _plugin;
    private readonly List<CollapsibleSection> _childSections = new List<CollapsibleSection>();
    private readonly List<UIDynamicToggle> _toggles = new List<UIDynamicToggle>();
    private readonly List<UIDynamicSlider> _sliders = new List<UIDynamicSlider>();
    private readonly List<UIDynamicPopup> _popups = new List<UIDynamicPopup>();
    private readonly List<UIDynamicButton> _buttons = new List<UIDynamicButton>();
    private readonly List<UIDynamicTextField> _textFields = new List<UIDynamicTextField>();
    private readonly List<UIDynamic> _spacers = new List<UIDynamic>();

    public CollapsibleSection(MVRScript plugin)
    {
        _plugin = plugin;
    }

    public void RemoveAll()
    {
        foreach (var section in _childSections) section.RemoveAll();
        _childSections.Clear();

        foreach (var toggle in _toggles) _plugin.RemoveToggle(toggle);
        _toggles.Clear();

        foreach (var slider in _sliders) _plugin.RemoveSlider(slider);
        _sliders.Clear();

        foreach (var popup in _popups) _plugin.RemovePopup(popup);
        _popups.Clear();

        foreach (var button in _buttons) _plugin.RemoveButton(button);
        _buttons.Clear();

        foreach (var button in _textFields) _plugin.RemoveTextField(button);
        _textFields.Clear();

        foreach (var spacer in _spacers) _plugin.RemoveSpacer(spacer);
        _spacers.Clear();
    }

    public CollapsibleSection CreateSection() { return _childSections.AddAndReturn(new CollapsibleSection(_plugin)); }
    public UIDynamicToggle CreateToggle(JSONStorableBool jsb, bool rightSide = false) { return _toggles.AddAndReturn(_plugin.CreateToggle(jsb, rightSide)); }
    public void RemoveToggle(UIDynamicToggle component) { _plugin.RemoveToggle(component); _toggles.Remove(component); }
    public UIDynamicSlider CreateSlider(JSONStorableFloat jsf, bool rightSide = false) { return _sliders.AddAndReturn(_plugin.CreateSlider(jsf, rightSide)); }
    public void RemoveSlider(UIDynamicSlider component) { _plugin.RemoveSlider(component); _sliders.Remove(component); }
    public UIDynamicPopup CreateScrollablePopup(JSONStorableStringChooser jss, bool rightSide = false) { return _popups.AddAndReturn(_plugin.CreateScrollablePopup(jss, rightSide)); }
    public UIDynamicPopup CreateFilterablePopup(JSONStorableStringChooser jss, bool rightSide = false) { return _popups.AddAndReturn(_plugin.CreateFilterablePopup(jss, rightSide)); }
    public void RemoveFilterablePopup(UIDynamicPopup component) { _plugin.RemovePopup(component); _popups.Remove(component); }
    public UIDynamicButton CreateButton(string label, bool rightSide = false) { return _buttons.AddAndReturn(_plugin.CreateButton(label, rightSide)); }
    public void RemoveButton(UIDynamicButton component) { _plugin.RemoveButton(component); _buttons.Remove(component); }
    public UIDynamicTextField CreateText(JSONStorableString jss, bool rightSide = false) { return _textFields.AddAndReturn(_plugin.CreateTextField(jss, rightSide)); }
    public void RemoveText(UIDynamicTextField component) { _plugin.RemoveTextField(component); _textFields.Remove(component); }
    public UIDynamic CreateSpacer(bool rightSide = false) { return _spacers.AddAndReturn(_plugin.CreateSpacer(rightSide)); }
    public void RemoveSpacer(UIDynamic component) { _plugin.RemoveSpacer(component); _spacers.Remove(component); }
}
