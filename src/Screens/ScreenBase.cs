    using System.Collections.Generic;

    public interface IScreen
    {
        IScreensManager screensManager { get; set; }
        void Show();
        void Hide();
    }

    public abstract class ScreenBase
    {
        public IScreensManager screensManager { get; set; }

        protected readonly EmbodyContext context;

        private readonly List<UIDynamicToggle> _toggles = new List<UIDynamicToggle>();
        private readonly List<UIDynamicSlider> _sliders = new List<UIDynamicSlider>();
        private readonly List<UIDynamicPopup> _popups = new List<UIDynamicPopup>();
        private readonly List<UIDynamicButton> _buttons = new List<UIDynamicButton>();
        private readonly List<UIDynamicTextField> _textFields = new List<UIDynamicTextField>();
        private readonly List<UIDynamic> _spacers = new List<UIDynamic>();

        protected ScreenBase(EmbodyContext context)
        {
            this.context = context;
        }

        protected bool ShowNotSelected(bool selected)
        {
            if (selected) return false;

            CreateText(new JSONStorableString("", "This module is disabled. Go to the main screen to select it."), true);

            return true;
        }

        public virtual void Hide()
        {
            foreach (var toggle in _toggles) context.plugin.RemoveToggle(toggle);
            _toggles.Clear();

            foreach (var slider in _sliders) context.plugin.RemoveSlider(slider);
            _sliders.Clear();

            foreach (var popup in _popups) context.plugin.RemovePopup(popup);
            _popups.Clear();

            foreach (var button in _buttons) context.plugin.RemoveButton(button);
            _buttons.Clear();

            foreach (var button in _textFields) context.plugin.RemoveTextField(button);
            _textFields.Clear();

            foreach (var spacer in _spacers) context.plugin.RemoveSpacer(spacer);
            _spacers.Clear();
        }

        protected UIDynamicToggle CreateToggle(JSONStorableBool jsb, bool rightSide = false) { return _toggles.AddAndReturn(context.plugin.CreateToggle(jsb, rightSide)); }
        protected UIDynamicSlider CreateSlider(JSONStorableFloat jsf, bool rightSide = false) { return _sliders.AddAndReturn(context.plugin.CreateSlider(jsf, rightSide)); }
        protected UIDynamicPopup CreateScrollablePopup(JSONStorableStringChooser jss, bool rightSide = false) { return _popups.AddAndReturn(context.plugin.CreateScrollablePopup(jss, rightSide)); }
        protected UIDynamicPopup CreateFilterablePopup(JSONStorableStringChooser jss, bool rightSide = false) { return _popups.AddAndReturn(context.plugin.CreateFilterablePopup(jss, rightSide)); }
        protected UIDynamicButton CreateButton(string label, bool rightSide = false) { return _buttons.AddAndReturn(context.plugin.CreateButton(label, rightSide)); }
        protected UIDynamicTextField CreateText(JSONStorableString jss, bool rightSide = false) { return _textFields.AddAndReturn(context.plugin.CreateTextField(jss, rightSide)); }
        protected UIDynamic CreateSpacer(bool rightSide = false) { return _spacers.AddAndReturn(context.plugin.CreateSpacer(rightSide)); }
    }
