public class AskSnugStep : WizardStepBase, IWizardStep
{
    public string helpText => "Do you want to setup Snug? This will dynamically adjust your hands so despite body proportion differences, your in-game hands position will match your own in relation to your body.";

    public AskSnugStep(EmbodyContext context)
        : base(context)
    {
    }

    public void Apply()
    {
        context.snug.selectedJSON.val = true;
    }
}
