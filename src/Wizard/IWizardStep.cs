public interface IWizardStep
{
    string helpText { get; }
    string lastError { get; set; }
    void Enter();
    void Update();
    bool Apply();
    void Leave(bool final);
}
