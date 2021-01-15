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
        private readonly CollapsibleSection _root;

        protected ScreenBase(EmbodyContext context)
        {
            this.context = context;
            _root = new CollapsibleSection(context.plugin);
        }

        protected bool ShowNotSelected(bool selected)
        {
            if (selected) return false;

            CreateText(new JSONStorableString("", "This module is disabled. Go to the main screen to select it."), true);

            return true;
        }

        public virtual void Hide()
        {
            _root.RemoveAll();
        }

        protected CollapsibleSection CreateSection() => _root.CreateSection();
        protected UIDynamicToggle CreateToggle(JSONStorableBool jsb, bool rightSide = false) { return _root.CreateToggle(jsb, rightSide); }
        protected UIDynamicSlider CreateSlider(JSONStorableFloat jsf, bool rightSide = false) { return _root.CreateSlider(jsf, rightSide); }
        protected UIDynamicPopup CreateScrollablePopup(JSONStorableStringChooser jss, bool rightSide = false) { return _root.CreateScrollablePopup(jss, rightSide); }
        protected UIDynamicPopup CreateFilterablePopup(JSONStorableStringChooser jss, bool rightSide = false) { return _root.CreateFilterablePopup(jss, rightSide); }
        protected UIDynamicButton CreateButton(string label, bool rightSide = false) { return _root.CreateButton(label, rightSide); }
        protected UIDynamicTextField CreateText(JSONStorableString jss, bool rightSide = false) { return _root.CreateText(jss, rightSide); }
        protected UIDynamic CreateSpacer(bool rightSide = false) { return _root.CreateSpacer(rightSide); }
    }
