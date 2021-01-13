public class EmbodyWizardSettingsScreen : ScreenBase, IScreen
{
    private readonly IEmbodyWizard _wizard;
    public const string ScreenName = EmbodyWizardModule.Label;

    public EmbodyWizardSettingsScreen(EmbodyContext context, IEmbodyWizard wizard)
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
