public abstract class WizardStepBase
{
    protected readonly EmbodyContext context;

    public string lastError { get; set; }

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
