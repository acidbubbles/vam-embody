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

        public virtual void Hide()
        {
            _root.RemoveAll();
        }

        protected CollapsibleSection CreateSection() => _root.CreateSection();
        protected UIDynamicToggle CreateToggle(JSONStorableBool jsb, bool rightSide = false) => _root.CreateToggle(jsb, rightSide);
        protected UIDynamicSlider CreateSlider(JSONStorableFloat jsf, bool rightSide = false) => _root.CreateSlider(jsf, rightSide);
        protected UIDynamicPopup CreateScrollablePopup(JSONStorableStringChooser jss, bool rightSide = false) => _root.CreateScrollablePopup(jss, rightSide);
        protected UIDynamicPopup CreateFilterablePopup(JSONStorableStringChooser jss, bool rightSide = false) => _root.CreateFilterablePopup(jss, rightSide);
        protected UIDynamicButton CreateButton(string label, bool rightSide = false) => _root.CreateButton(label, rightSide);
        protected UIDynamicTextField CreateText(JSONStorableString jss, bool rightSide = false) => _root.CreateText(jss, rightSide);
        protected UIDynamic CreateTitle(string text, bool rightSide = false) => _root.CreateTitle(text, rightSide);
        protected UIDynamic CreateSpacer(bool rightSide = false) => _root.CreateSpacer(rightSide);
    }
