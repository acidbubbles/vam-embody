public class ActivateStep : IWizardStep
{
    public string helpText => "We will now start possession. Press select when ready.";
    private readonly IEmbody _embody;
    private readonly ISnugModule _snug;

    public ActivateStep(IEmbody embody)
    {
        _embody = embody;
    }

    public void Run()
    {
        _embody.activeJSON.val = true;
    }
}
