using System.Linq;
using System.Text;
using MeshVR;

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
        var logs = _diagnostics.logs.ToArray();
        var logsJSON = new JSONStorableString("", logs.Length == 0 ? "No errors log were recorded" : string.Join(", ", logs));
        CreateText(logsJSON, true).height = 1200f;

        var snapshotsJSON = new JSONStorableStringChooser("",
            context.diagnostics.snapshots.Select(s => s.name).ToList(),
            $"{context.diagnostics.snapshots.Count} snapshots",
            "Snapshots",
            val => ShowSnapshot(logsJSON, val));
        CreateScrollablePopup(snapshotsJSON);

        CreateButton("Load Snapshot").button.onClick.AddListener(() => LoadSnapshot(snapshotsJSON.val));
    }

    private void ShowSnapshot(JSONStorableString logsJSON, string snapshotName)
    {
        var snapshot = context.diagnostics.snapshots.FirstOrDefault(s => s.name == snapshotName);
        if (snapshot == null) return;
        var sb = new StringBuilder();
        sb.AppendLine($"<b>Snapshot</b>:\n{snapshot.name}");
        sb.AppendLine($"<b>Player Height Adjust</b>:\n{snapshot.playerHeightAdjust}");
        sb.AppendLine($"<b>World Scale</b>:\n{snapshot.worldScale}");
        sb.AppendLine($"<b>Navigation Rig</b>:\n{snapshot.navigationRig}");
        sb.AppendLine($"<b>Head</b>:\n{snapshot.head}");
        sb.AppendLine($"<b>Left Hand</b>:\n{snapshot.leftHand}");
        sb.AppendLine($"<b>Right Hand</b>:\n{snapshot.rightHand}");
        sb.AppendLine($"<b>Vive Tracker 1</b>:\n{snapshot.viveTracker1}");
        sb.AppendLine($"<b>Vive Tracker 2</b>:\n{snapshot.viveTracker2}");
        sb.AppendLine($"<b>Vive Tracker 3</b>:\n{snapshot.viveTracker3}");
        sb.AppendLine($"<b>Vive Tracker 4</b>:\n{snapshot.viveTracker4}");
        sb.AppendLine($"<b>Vive Tracker 5</b>:\n{snapshot.viveTracker5}");
        sb.AppendLine($"<b>Vive Tracker 6</b>:\n{snapshot.viveTracker6}");
        sb.AppendLine($"<b>Vive Tracker 7</b>:\n{snapshot.viveTracker7}");
        sb.AppendLine($"<b>Vive Tracker 8</b>:\n{snapshot.viveTracker8}");
        logsJSON.val = sb.ToString();
    }

    private void LoadSnapshot(string snapshotName)
    {
        var snapshot = context.diagnostics.snapshots.FirstOrDefault(s => s.name == snapshotName);
        if (snapshot == null) return;
        context.diagnostics.RestoreSnapshot(snapshot);
    }
}
