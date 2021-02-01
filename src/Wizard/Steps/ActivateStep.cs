public class ActivateStep : WizardStepBase, IWizardStep
{
    public string helpText => "We will now start possession. Press next when ready.";
    private readonly IEmbody _embody;

    public ActivateStep(IEmbody embody)
    {
        _embody = embody;
    }

    public void Apply()
    {
        _embody.activeJSON.val = true;
    }
}
