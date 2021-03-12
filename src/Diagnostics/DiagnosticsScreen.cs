using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class DiagnosticsScreen : ScreenBase, IScreen
{
    public const string ScreenName = DiagnosticsModule.Label;

    private readonly IDiagnosticsModule _diagnostics;

    private readonly JSONStorableBool _restoreWorldStateJSON = new JSONStorableBool("Restore Navigation Rig", false);

    public DiagnosticsScreen(EmbodyContext context, IDiagnosticsModule diagnostics)
        : base(context)
    {
        _diagnostics = diagnostics;
    }

    public void Show()
    {
        CreateToggle(_diagnostics.enabledJSON).label = "Record Diagnostics Data";

        var logs = _diagnostics.logs.ToArray();
        var logsJSON = new JSONStorableString("", logs.Length == 0 ? "Enabling diagnostics will record all VaM errors and your physical position during possession.\n\n<b>ONLY enable this if Acidbubbles asks you to.</b>\n\nAlso keep in mind this will give information about your height and body size, if you are not comfortable sharing this information, please keep diagnostics off." : string.Join(", ", logs));
        CreateText(logsJSON, true).height = 1200f;

        if (!Input.GetKey(KeyCode.LeftControl)) return;

        CreateButton("Show Logged Errors").button.onClick.AddListener(() => logsJSON.val = logs.Length == 0 ? "No errors log were recorded" : string.Join(", ", logs));

        var snapshotsJSON = new JSONStorableStringChooser("",
            new List<string>(),
            $"",
            "Snapshots",
            val => ShowSnapshot(logsJSON, val));
        RefreshSnapshots(snapshotsJSON);
        CreateScrollablePopup(snapshotsJSON);

        CreateButton("Remove Fake Trackers").button.onClick.AddListener(() =>
        {
            context.diagnostics.RemoveFakeTrackers();
        });
        CreateToggle(_diagnostics.attachToHeadJSON).label = "Trackers Attached To Head";
        CreateButton("Create Fake Trackers").button.onClick.AddListener(() =>
        {
            context.diagnostics.CreateFakeTrackers(context.diagnostics.snapshots.FirstOrDefault(s => s.name == snapshotsJSON.val));
        });
        CreateToggle(_restoreWorldStateJSON);
        CreateButton("Load Snapshot").button.onClick.AddListener(() =>
        {
            var snapshot = FindSnapshot(snapshotsJSON);
            if (snapshot == null)
            {
                logsJSON.val = "Select a snapshot first";
                return;
            }
            context.diagnostics.RestoreSnapshot(snapshot, _restoreWorldStateJSON.val);
        });
        CreateButton("Load Fake Trackers").button.onClick.AddListener(() =>
        {
            var snapshot = FindSnapshot(snapshotsJSON);
            if (snapshot == null)
            {
                logsJSON.val = "Select a snapshot first";
                return;
            }
            context.diagnostics.RestoreTrackers(snapshot);
        });
        CreateButton("Take Snapshot").button.onClick.AddListener(() =>
        {
            context.diagnostics.TakeSnapshot("ManualSnapshot");
            RefreshSnapshots(snapshotsJSON);
            logsJSON.val = "";
        });
        CreateButton("Delete Snapshot").button.onClick.AddListener(() =>
        {
            var snapshot = FindSnapshot(snapshotsJSON);
            if (snapshot == null)
            {
                logsJSON.val = "Select a snapshot first";
                return;
            }
            context.diagnostics.snapshots.Remove(snapshot);
            snapshotsJSON.choices = context.diagnostics.snapshots.Select(s => s.name).ToList();
            snapshotsJSON.val = $"{context.diagnostics.snapshots.Count} snapshots";
            logsJSON.val = "";
        });
        CreateButton("Record Vive Trackers").button.onClick.AddListener(() =>
        {
            new RecordViveTrackersStep(context).Apply();
        });
    }

    private void RefreshSnapshots(JSONStorableStringChooser snapshotsJSON)
    {
        snapshotsJSON.choices = context.diagnostics.snapshots.Select(s => s.name).ToList();
        snapshotsJSON.val = $"{context.diagnostics.snapshots.Count} snapshots";
    }

    private EmbodyDebugSnapshot FindSnapshot(JSONStorableStringChooser snapshotsJSON)
    {
        return context.diagnostics.snapshots.FirstOrDefault(s => s.name == snapshotsJSON.val);
    }

    private void ShowSnapshot(JSONStorableString logsJSON, string snapshotName)
    {
        var snapshot = context.diagnostics.snapshots.FirstOrDefault(s => s.name == snapshotName);
        if (snapshot == null) return;
        var sb = new StringBuilder();
        sb.AppendLine($"<b>Snapshot</b>:\n{snapshot.name}");
        sb.AppendLine($"<b>Pose</b>:\n{((snapshot.poseJSON?.Count ?? 0) > 0 ? $"{snapshot.poseJSON.Count} nodes" : "None")}");
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
}
