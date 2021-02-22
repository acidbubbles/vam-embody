using System.Linq;

public class DiagnosticsScreen : ScreenBase, IScreen
{
    public const string ScreenName = DiagnosticsModule.Label;

    private readonly IDiagnosticsModule _diagnostics;

    public DiagnosticsScreen(EmbodyContext context, IDiagnosticsModule diagnostics)
        : base(context)
    {
        _diagnostics = diagnostics;
    }

    public void Show()
    {
        CreateText(new JSONStorableString("", string.Join(", ", _diagnostics.logs.ToArray())), true).height = 700f;
    }
}
