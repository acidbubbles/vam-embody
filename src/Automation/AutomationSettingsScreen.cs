public class AutomationSettingsScreen : ScreenBase, IScreen
{
    private readonly IAutomation _automation;
    public const string ScreenName = "Automation";

    public AutomationSettingsScreen(MVRScript plugin, IAutomation automation)
        : base(plugin)
    {
        _automation = automation;
    }

    public void Show()
    {
        CreateToggle(_automation.possessionActiveJSON).toggle.interactable = false;

        var toggleKeyPopup = CreateFilterablePopup(_automation.toggleKeyJSON);
        toggleKeyPopup.popupPanelHeight = 600f;
    }
}
