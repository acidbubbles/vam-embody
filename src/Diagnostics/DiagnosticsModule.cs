using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface IDiagnosticsModule : IEmbodyModule
{
    Transform head { get; }
    Transform leftHand { get; }
    Transform rightHand { get; }
    Transform viveTracker1 { get; }
    Transform viveTracker2 { get; }
    Transform viveTracker3 { get; }
    Transform viveTracker4 { get; }
    Transform viveTracker5 { get; }
    Transform viveTracker6 { get; }
    Transform viveTracker7 { get; }
    Transform viveTracker8 { get; }
    IEnumerable<string> logs { get; }
    List<JSONTrackersSnapshot> snapshots { get; }
    void TakeSnapshot(string name);
    void RestoreSnapshot(JSONTrackersSnapshot snapshot);
}

public class DiagnosticsModule : EmbodyModuleBase, IDiagnosticsModule
{
    public const string Label = "Diagnostics";
    public override string storeId => "Diagnostics";
    public override string label => Label;
    public override bool skipChangeEnabledWhenActive => true;

    public Transform head { get; private set; }
    public Transform leftHand { get; private set; }
    public Transform rightHand { get; private set; }
    public Transform viveTracker1 { get; private set; }
    public Transform viveTracker2 { get; private set; }
    public Transform viveTracker3 { get; private set; }
    public Transform viveTracker4 { get; private set; }
    public Transform viveTracker5 { get; private set; }
    public Transform viveTracker6 { get; private set; }
    public Transform viveTracker7 { get; private set; }
    public Transform viveTracker8 { get; private set; }

    private bool _once;
    private bool _restored;

    private JSONArray _logs = new JSONArray();
    public List<JSONTrackersSnapshot> snapshots { get; } = new List<JSONTrackersSnapshot>();
    public IEnumerable<string> logs => _logs.Childs.Select(c => c.Value);

    public void Start()
    {
        // TODO: Make a snapshot of each trackerL
        // SuperController.singleton.viveTracker1 position and rotation
        // Validate that it has a local position/rotation of 0,0,0
        // Validate that the possess point is unused
        // Try to use the Trackers transforms and update them to match the empty GameObject for a better representation

        //context.plugin.StartCoroutine(DebugCo());

        const string prefix = "EMBODY_DEBUG#";
        head = GetDebugAtom($"{prefix}head");
        leftHand = GetDebugAtom($"{prefix}lHand");
        rightHand = GetDebugAtom($"{prefix}rHand");
        viveTracker1 = GetDebugAtom($"{prefix}viveTracker1");
        viveTracker2 = GetDebugAtom($"{prefix}viveTracker2");
        viveTracker3 = GetDebugAtom($"{prefix}viveTracker3");
        viveTracker4 = GetDebugAtom($"{prefix}viveTracker4");
        viveTracker5 = GetDebugAtom($"{prefix}viveTracker5");
        viveTracker6 = GetDebugAtom($"{prefix}viveTracker6");
        viveTracker7 = GetDebugAtom($"{prefix}viveTracker7");
        viveTracker8 = GetDebugAtom($"{prefix}viveTracker8");
    }

    public override void OnEnable()
    {
        base.OnEnable();
        Application.logMessageReceived += CaptureLog;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        Application.logMessageReceived -= CaptureLog;
    }

    private static Transform GetDebugAtom(string uid)
    {
        var atom = SuperController.singleton.GetAtomByUid(uid);
        if (atom == null || !atom.on) return null;
        return atom.mainController.control;
    }

    private void CaptureLog(string condition, string stacktrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception) return;
        if (condition == null) return;
        _logs.Add($"{Time.realtimeSinceStartup:0.00} {type} {condition} {stacktrace}");
    }

    public void TakeSnapshot(string snapshotName)
    {
        if (!enabled) return;
        snapshots.Add(new JSONTrackersSnapshot
        {
            name = $"{Time.realtimeSinceStartup:0.00} {snapshotName}",
            worldScale = SuperController.singleton.worldScale,
            playerHeightAdjust = SuperController.singleton.playerHeightAdjust,
            pluginJSON = context.plugin.GetJSON(),
            poseJSON = StorePoseJSON(),
            navigationRig = JSONTrackerSnapshot.From(SuperController.singleton.navigationRig),
            head = JSONTrackerSnapshot.From(context.head),
            leftHand = JSONTrackerSnapshot.From(context.leftHand),
            rightHand = JSONTrackerSnapshot.From(context.rightHand),
            viveTracker1 = JSONTrackerSnapshot.From(SuperController.singleton.viveTracker1),
            viveTracker2 = JSONTrackerSnapshot.From(SuperController.singleton.viveTracker2),
            viveTracker3 = JSONTrackerSnapshot.From(SuperController.singleton.viveTracker3),
            viveTracker4 = JSONTrackerSnapshot.From(SuperController.singleton.viveTracker4),
            viveTracker5 = JSONTrackerSnapshot.From(SuperController.singleton.viveTracker5),
            viveTracker6 = JSONTrackerSnapshot.From(SuperController.singleton.viveTracker6),
            viveTracker7 = JSONTrackerSnapshot.From(SuperController.singleton.viveTracker7),
            viveTracker8 = JSONTrackerSnapshot.From(SuperController.singleton.viveTracker8),
        });
    }

    private JSONArray StorePoseJSON()
    {
        var poseJSON = new JSONArray();
        var storables = containingAtom.GetStorableIDs()
            .Select(s => containingAtom.GetStorableByID(s))
            .Where(t => !t.exclude && t.gameObject.activeInHierarchy)
            .Where(t => t is FreeControllerV3 || t is DAZBone);
        foreach (var storable in storables)
        {
            poseJSON.Add(storable.GetJSON());
        }
        return poseJSON;
    }

    private void RestorePoseJSON(JSONArray poseJSON)
    {
        foreach (var storableJSON in poseJSON.Childs)
        {
            var storableId = storableJSON["id"].Value;
            if (string.IsNullOrEmpty(storableId)) continue;
            var storable = containingAtom.GetStorableByID(storableId);
            storable.PreRestore();
            storable.RestoreFromJSON(storableJSON.AsObject);
            storable.PostRestore();
        }
    }

    public void RestoreSnapshot(JSONTrackersSnapshot snapshot)
    {
        if (snapshot.pluginJSON != null)
            context.plugin.RestoreFromJSON(snapshot.pluginJSON);
        if (snapshot.poseJSON != null)
            RestorePoseJSON(snapshot.poseJSON);
    }

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);

        jc["Logs"] = _logs;
        var snapshotsJSON = new JSONArray();
        foreach (var snapshot in snapshots)
            snapshotsJSON.Add(snapshot.ToJSON());
        jc["Snapshots"] = snapshotsJSON;
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);
        if (_restored) return;
        _restored = true;
        if (jc.HasKey("Logs"))
            _logs = jc["Logs"].AsArray;
        if(jc.HasKey("Snapshots"))
            snapshots.AddRange(jc["Snapshots"].AsArray.Childs.Select(s => JSONTrackersSnapshot.FromJSON(s.AsObject)));
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();
        _logs = new JSONArray();
        snapshots.Clear();
    }

    private IEnumerator DebugCo()
    {
        if (_once) yield break;
        _once = true;

        context.trackers.previewTrackerOffsetJSON.val = true;

        /*
        Height with tracker: 1.706743
        Height without tracker: 1.666745
         */
        var withTracker = VisualCuesHelper.Cross(Color.red).transform;
            withTracker.position = new Vector3(0, 1.706743f, 0);
        var withoutTracker = VisualCuesHelper.Cross(Color.blue).transform;
            withoutTracker.position = new Vector3(0, 1.666745f, 0);

        /*
        while (context.plugin.isActiveAndEnabled)
        {
            yield return new WaitForSeconds(1);
            new PlayerMeasurements(context).MeasureHeight();
        }
        */
        /*
        context.wizard.forceReopen = false;
        yield return new WaitForSecondsRealtime(0.2f);
        screensManager.Show(WizardScreen.ScreenName);
        yield return new WaitForSecondsRealtime(0.1f);
        context.wizard.StartWizard();
        for (var i = 0; i < 2; i++)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            context.wizard.Skip();
        }
        foreach (var t in context.trackers.headAndHands) {t.enabled = false;}
        context.hideGeometry.selectedJSON.val = false;
        context.worldScale.selectedJSON.val = false;
        for (var i = 0; i < 3; i++)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            context.wizard.Next();
            context.trackers.previewTrackerOffsetJSON.val = true;
        }
        */
    }
}
