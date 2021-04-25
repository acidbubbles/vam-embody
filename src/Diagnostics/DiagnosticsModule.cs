using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    List<EmbodyDebugSnapshot> snapshots { get; }
    void Log(string message);
    void TakeSnapshot(string name);
    void RestoreSnapshot(EmbodyDebugSnapshot snapshot, bool restoreWorldState);
    void RemoveFakeTrackers();
    void CreateFakeTrackers(EmbodyDebugSnapshot snapshot);
}

public class DiagnosticsModule : EmbodyModuleBase, IDiagnosticsModule
{
    public const string Label = "Diagnostics";
    private const string _embodyDebugPrefix = "EMBODY_DEBUG#";
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
    public List<EmbodyDebugSnapshot> snapshots { get; } = new List<EmbodyDebugSnapshot>();
    public IEnumerable<string> logs => _logs.Childs.Select(c => c.Value);

    public void Start()
    {
        BindFakeTrackers();

        // context.plugin.StartCoroutine(DebugCo());
    }

    private void BindFakeTrackers()
    {
        RemoveFakeTrackers();

        head = GetDebugAtom($"{_embodyDebugPrefix}{MotionControlNames.Head}");
        leftHand = GetDebugAtom($"{_embodyDebugPrefix}{MotionControlNames.LeftHand}");
        rightHand = GetDebugAtom($"{_embodyDebugPrefix}{MotionControlNames.RightHand}");
        viveTracker1 = GetDebugAtom($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}1");
        viveTracker2 = GetDebugAtom($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}2");
        viveTracker3 = GetDebugAtom($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}3");
        viveTracker4 = GetDebugAtom($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}4");
        viveTracker5 = GetDebugAtom($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}5");
        viveTracker6 = GetDebugAtom($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}6");
        viveTracker7 = GetDebugAtom($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}7");
        viveTracker8 = GetDebugAtom($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}8");
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private IEnumerator DebugCo()
    {
        if (_once) yield break;
        _once = true;

        yield return 0;

        context.embody.activeJSON.val = true;

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

    public override void OnEnable()
    {
        base.OnEnable();
        Application.logMessageReceived += CaptureLog;
        BindFakeTrackers();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        Application.logMessageReceived -= CaptureLog;
    }

    public void OnDestroy()
    {
        RemoveFakeTrackers();
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
        _logs.Add($"[ERR] {Time.realtimeSinceStartup:0.00} {type} {condition} {stacktrace}");
    }

    public void RemoveFakeTrackers()
    {
        if (head != null && head.name.StartsWith(_embodyDebugPrefix)) Destroy(head.gameObject);
        head = null;
        if (leftHand != null && leftHand.name.StartsWith(_embodyDebugPrefix)) Destroy(leftHand.gameObject);
        leftHand = null;
        if (rightHand != null && rightHand.name.StartsWith(_embodyDebugPrefix)) Destroy(rightHand.gameObject);
        rightHand = null;
        if (viveTracker1 != null && viveTracker1.name.StartsWith(_embodyDebugPrefix)) Destroy(viveTracker1.gameObject);
        viveTracker1 = null;
        if (viveTracker2 != null && viveTracker2.name.StartsWith(_embodyDebugPrefix)) Destroy(viveTracker2.gameObject);
        viveTracker2 = null;
        if (viveTracker3 != null && viveTracker3.name.StartsWith(_embodyDebugPrefix)) Destroy(viveTracker3.gameObject);
        viveTracker3 = null;
        if (viveTracker4 != null && viveTracker4.name.StartsWith(_embodyDebugPrefix)) Destroy(viveTracker4.gameObject);
        viveTracker4 = null;
        if (viveTracker5 != null && viveTracker5.name.StartsWith(_embodyDebugPrefix)) Destroy(viveTracker5.gameObject);
        viveTracker5 = null;
        if (viveTracker6 != null && viveTracker6.name.StartsWith(_embodyDebugPrefix)) Destroy(viveTracker6.gameObject);
        viveTracker6 = null;
        if (viveTracker7 != null && viveTracker7.name.StartsWith(_embodyDebugPrefix)) Destroy(viveTracker7.gameObject);
        viveTracker7 = null;
        if (viveTracker8 != null && viveTracker8.name.StartsWith(_embodyDebugPrefix)) Destroy(viveTracker8.gameObject);
        viveTracker8 = null;
    }

    public void CreateFakeTrackers(EmbodyDebugSnapshot snapshot)
    {
        RemoveFakeTrackers();

        if (snapshot == null) return;

        head = CreateFakeTrackers("head", snapshot.head);
        leftHand = CreateFakeTrackers("leftHand", snapshot.leftHand);
        rightHand = CreateFakeTrackers("rightHand", snapshot.rightHand);
        viveTracker1 = CreateFakeTrackers("viveTracker1", snapshot.viveTracker1);
        viveTracker2 = CreateFakeTrackers("viveTracker2", snapshot.viveTracker2);
        viveTracker3 = CreateFakeTrackers("viveTracker3", snapshot.viveTracker3);
        viveTracker4 = CreateFakeTrackers("viveTracker4", snapshot.viveTracker4);
        viveTracker5 = CreateFakeTrackers("viveTracker5", snapshot.viveTracker5);
        viveTracker6 = CreateFakeTrackers("viveTracker6", snapshot.viveTracker6);
        viveTracker7 = CreateFakeTrackers("viveTracker7", snapshot.viveTracker7);
        viveTracker8 = CreateFakeTrackers("viveTracker8", snapshot.viveTracker8);
    }

    private static Transform CreateFakeTrackers(string suffix, EmbodyTransformDebugSnapshot snapshot)
    {
        if (snapshot == null) return null;
        var go = new GameObject(_embodyDebugPrefix + suffix);
        go.transform.SetParent(SuperController.singleton.worldScaleTransform, false);
        go.transform.position = snapshot.position;
        go.transform.eulerAngles = snapshot.rotation;
        VisualCuesHelper.Cross(Color.white).transform.SetParent(go.transform, false);
        return go.transform;
    }

    public void Log(string message)
    {
        if (!enabled) return;
        _logs.Add($"[INF] {Time.realtimeSinceStartup:0.00} {message}");
    }

    public void TakeSnapshot(string snapshotName)
    {
        if (!enabled) return;
        Log(snapshotName);
        var pluginJSON = new JSONClass();
        context.embody.StoreJSON(pluginJSON, true);
        pluginJSON.Remove(storeId);
        snapshots.Add(new EmbodyDebugSnapshot
        {
            name = $"{Time.realtimeSinceStartup:0.00} {snapshotName}",
            vrMode = GetVRMode(),
            worldScale = SuperController.singleton.worldScale,
            playerHeightAdjust = SuperController.singleton.playerHeightAdjust,
            pluginJSON = pluginJSON,
            poseJSON = StorePoseJSON(),
            navigationRig = EmbodyTransformDebugSnapshot.From(SuperController.singleton.navigationRig),
            head = EmbodyTransformDebugSnapshot.From(context.head),
            leftHand = EmbodyTransformDebugSnapshot.From(context.LeftHand()),
            rightHand = EmbodyTransformDebugSnapshot.From(context.RightHand()),
            viveTracker1 = EmbodyTransformDebugSnapshot.From(SuperController.singleton.viveTracker1),
            viveTracker2 = EmbodyTransformDebugSnapshot.From(SuperController.singleton.viveTracker2),
            viveTracker3 = EmbodyTransformDebugSnapshot.From(SuperController.singleton.viveTracker3),
            viveTracker4 = EmbodyTransformDebugSnapshot.From(SuperController.singleton.viveTracker4),
            viveTracker5 = EmbodyTransformDebugSnapshot.From(SuperController.singleton.viveTracker5),
            viveTracker6 = EmbodyTransformDebugSnapshot.From(SuperController.singleton.viveTracker6),
            viveTracker7 = EmbodyTransformDebugSnapshot.From(SuperController.singleton.viveTracker7),
            viveTracker8 = EmbodyTransformDebugSnapshot.From(SuperController.singleton.viveTracker8),
        });
    }

    private static string GetVRMode()
    {
        if (SuperController.singleton.isOVR)
            return "Oculus";
        if (SuperController.singleton.isOpenVR)
            return "Vive";
        return "Desktop";
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

    private void RestorePoseJSON(JSONNode poseJSON)
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

    public void RestoreSnapshot(EmbodyDebugSnapshot snapshot, bool restoreWorldState)
    {
        if (snapshot.pluginJSON != null)
            context.plugin.RestoreFromJSON(snapshot.pluginJSON);
        context.worldScale.selectedJSON.val = false;
        context.hideGeometry.selectedJSON.val = false;
        context.trackers.previewTrackerOffsetJSON.val = true;
        if (snapshot.poseJSON != null)
            RestorePoseJSON(snapshot.poseJSON);
        if (snapshot.worldScale > 0 && restoreWorldState)
            SuperController.singleton.worldScale = snapshot.worldScale;
        if (restoreWorldState)
            SuperController.singleton.playerHeightAdjust = snapshot.playerHeightAdjust;
        if (snapshot.navigationRig != null && restoreWorldState)
        {
            SuperController.singleton.navigationRig.position = snapshot.navigationRig.position;
            SuperController.singleton.navigationRig.eulerAngles = snapshot.navigationRig.rotation;
        }
        CreateFakeTrackers(snapshot);
    }

    public override void StoreJSON(JSONClass jc, bool includeProfile)
    {
        base.StoreJSON(jc, includeProfile);

        if (_logs.Count > 0)
            jc["Logs"] = _logs;
        if (snapshots.Count > 0)
        {
            var snapshotsJSON = new JSONArray();
            foreach (var snapshot in snapshots)
                snapshotsJSON.Add(snapshot.ToJSON());
            jc["Snapshots"] = snapshotsJSON;
        }
    }

    public override void RestoreFromJSON(JSONClass jc, bool fromDefaults)
    {
        base.RestoreFromJSON(jc, fromDefaults);
        if (fromDefaults) return;
        if (_restored) return;
        _restored = true;
        if (jc.HasKey("Logs"))
            _logs = jc["Logs"].AsArray;
        if(jc.HasKey("Snapshots"))
            snapshots.AddRange(jc["Snapshots"].AsArray.Childs.Select(s => EmbodyDebugSnapshot.FromJSON(s.AsObject)));
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();
        _logs = new JSONArray();
        snapshots.Clear();
    }
}
