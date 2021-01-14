public interface IWizardStep
{
    string helpText { get; }
    void Run();
}
