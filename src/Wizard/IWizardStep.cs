public interface IWizardStep
{
    string helpText { get; }
    void Enter();
    void Update();
    void Apply();
    void Leave();
}
