public class EnableSnugStep : IWizardStep
{
    public string helpText => "We will now enable hands possession with Snug. Press select when ready.";
    private readonly IEmbody _embody;
    private readonly ISnugModule _snug;

    public EnableSnugStep(IEmbody embody, ISnugModule snug)
    {
        _embody = embody;
        _snug = snug;
    }

    public void Run()
    {
        // TODO: We do not want that. We want to enable head _only_, make sure the model is standing straight, and enable hide geometry and offset camera too.
        // TODO: We want Snug to work sitting too. We only need the upper body to stand straight.
        if (_embody.activeJSON.val)
            _snug.enabledJSON.val = true;
    }
}
