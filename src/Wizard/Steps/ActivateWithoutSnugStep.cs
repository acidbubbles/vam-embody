public class ActivateWithoutSnugStep : IWizardStep
{
    public string helpText => "We will now start possession (except hands). Press select when ready.";
    private readonly IEmbody _embody;
    private readonly ISnugModule _snug;

    public ActivateWithoutSnugStep(IEmbody embody, ISnugModule snug)
    {
        _embody = embody;
        _snug = snug;
    }

    public void Run()
    {
        #warning Skip for now
        // TODO: We do not want that. We want to enable head _only_, make sure the model is standing straight, and enable hide geometry and offset camera too.
        // TODO: We want Snug to work sitting too. We only need the upper body to stand straight.
        _embody.activeJSON.val = true;
        _snug.enabledJSON.val = false;
    }
}
