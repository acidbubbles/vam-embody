public class StartPossessionStep : IWizardStep
{
    public string helpText => "We will now start possession. Press select when ready.";
    public void Run(SnugWizardContext context)
    {
        // TODO: We should technically enable Embody. Review this.
        context.trackers.enabledJSON.val = true;
    }
}
