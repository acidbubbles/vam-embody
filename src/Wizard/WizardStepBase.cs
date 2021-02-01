public abstract class WizardStepBase
{
    protected readonly EmbodyContext context;

    protected WizardStepBase()
    {
    }

    protected WizardStepBase(EmbodyContext context)
    {
        this.context = context;
    }

    public virtual void Setup()
    {
    }

    public virtual void Update()
    {
    }
}
