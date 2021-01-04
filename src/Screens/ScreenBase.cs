    using System.Collections.Generic;

    public interface IScreen
    {
        void Show();
        void Hide();
    }

    public abstract class ScreenBase
    {
        protected readonly MVRScript plugin;
        private readonly List<UIDynamicToggle> _toggles = new List<UIDynamicToggle>();
        private readonly List<UIDynamicSlider> _sliders = new List<UIDynamicSlider>();
        private readonly List<UIDynamicPopup> _popups = new List<UIDynamicPopup>();
        private readonly List<UIDynamicButton> _buttons = new List<UIDynamicButton>();
        private readonly List<UIDynamic> _spacers = new List<UIDynamic>();

        protected ScreenBase(MVRScript plugin)
        {
            this.plugin = plugin;
        }

        public void Hide()
        {
            foreach (var toggle in _toggles) plugin.RemoveToggle(toggle);
            _toggles.Clear();

            foreach (var slider in _sliders) plugin.RemoveSlider(slider);
            _sliders.Clear();

            foreach (var popup in _popups) plugin.RemovePopup(popup);
            _popups.Clear();

            foreach (var button in _buttons) plugin.RemoveButton(button);
            _buttons.Clear();

            foreach (var spacer in _spacers) plugin.RemoveSpacer(spacer);
            _spacers.Clear();
        }

        protected UIDynamicToggle CreateToggle(JSONStorableBool jsb, bool rightSide = false) { return _toggles.AddAndReturn(plugin.CreateToggle(jsb, rightSide)); }
        protected UIDynamicSlider CreateSlider(JSONStorableFloat jsf, bool rightSide = false) { return _sliders.AddAndReturn(plugin.CreateSlider(jsf, rightSide)); }
        protected UIDynamicPopup CreateScrollablePopup(JSONStorableStringChooser jss, bool rightSide = false) { return _popups.AddAndReturn(plugin.CreateScrollablePopup(jss, rightSide)); }
        protected UIDynamicPopup CreateFilterablePopup(JSONStorableStringChooser jss, bool rightSide = false) { return _popups.AddAndReturn(plugin.CreateFilterablePopup(jss, rightSide)); }
        protected UIDynamicButton CreateButton(string label, bool rightSide = false) { return _buttons.AddAndReturn(plugin.CreateButton(label, rightSide)); }
        protected UIDynamic CreateSpacer(bool rightSide = false) { return _spacers.AddAndReturn(plugin.CreateSpacer(rightSide)); }
    }
