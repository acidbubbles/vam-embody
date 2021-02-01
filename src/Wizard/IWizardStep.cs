public interface IWizardStep
{
    string helpText { get; }
    void Setup();
    void Update();
    void Apply();
}
