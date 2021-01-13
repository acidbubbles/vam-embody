public class WizardScreen : ScreenBase, IScreen
{
    private readonly IWizard _wizard;
    public const string ScreenName = WizardModule.Label;

    public WizardScreen(EmbodyContext context, IWizard wizard)
        : base(context)
    {
        _wizard = wizard;
    }

    public void Show()
    {
        CreateText(new JSONStorableString("", "Automatically configures optimal settings for the current person and the selected modules."), true);

        var setupButton = CreateButton("Setup Wizard");
        setupButton.button.onClick.AddListener(() => _wizard.StartWizard());

        var stopButton = CreateButton("Stop Wizard");
        stopButton.button.onClick.AddListener(() =>
        {
            _wizard.StopWizard();
        } );

        CreateText(new JSONStorableString("", "Automatically configures optimal settings for the current person and the selected modules."), true);
    }
}
