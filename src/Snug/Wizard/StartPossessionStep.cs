public class StartPossessionStep : IWizardStep
{
    public string helpText => "We will now start possession. Press select when ready.";
    public void Run(SnugWizardContext context)
    {
        // TODO: We do not want that. We want to enable head _only_, make sure the model is standing straight, and enable hide geometry and offset camera too.
        // TODO: We want Snug to work sitting too. We only need the upper body to stand straight.
        context.trackers.enabledJSON.val = true;
    }
}
