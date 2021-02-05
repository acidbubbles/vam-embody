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

    public virtual void Enter()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void Leave()
    {
    }
}
