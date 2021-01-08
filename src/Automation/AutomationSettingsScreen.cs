public class AutomationSettingsScreen : ScreenBase, IScreen
{
    private readonly IAutomationModule _automation;
    public const string ScreenName = AutomationModule.Label;

    public AutomationSettingsScreen(MVRScript plugin, IAutomationModule automation)
        : base(plugin)
    {
        _automation = automation;
    }

    public void Show()
    {
        base.CreateModuleUI(_automation);

        CreateText(new JSONStorableString("", "This module automatically enables/disabled Embody when VaM possession is activated. You can also press <b>Esc</b> to exit Embody at any time."), true);
        CreateToggle(_automation.possessionActiveJSON, true).toggle.interactable = false;

        CreateSpacer(true);

        var toggleKeyPopup = CreateFilterablePopup(_automation.toggleKeyJSON);
        toggleKeyPopup.popupPanelHeight = 600f;
    }
}
