using System;
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

        // context.plugin.StartCoroutine(DebugCo());
    }

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
        _logs.Add($"[ERR] {Time.realtimeSinceStartup:0.00} {type} {condition} {stacktrace}");
    }

    public void RemoveFakeTrackers()
    {
        head = null;
        SuperController.singleton.RemoveAtom(SuperController.singleton.GetAtomByUid($"{_embodyDebugPrefix}{MotionControlNames.Head}"));
        leftHand = null;
        SuperController.singleton.RemoveAtom(SuperController.singleton.GetAtomByUid($"{_embodyDebugPrefix}{MotionControlNames.LeftHand}"));
        rightHand = null;
        SuperController.singleton.RemoveAtom(SuperController.singleton.GetAtomByUid($"{_embodyDebugPrefix}{MotionControlNames.RightHand}"));
        viveTracker1 = null;
        SuperController.singleton.RemoveAtom(SuperController.singleton.GetAtomByUid($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}1"));
        viveTracker2 = null;
        SuperController.singleton.RemoveAtom(SuperController.singleton.GetAtomByUid($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}2"));
        viveTracker3 = null;
        SuperController.singleton.RemoveAtom(SuperController.singleton.GetAtomByUid($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}3"));
        viveTracker4 = null;
        SuperController.singleton.RemoveAtom(SuperController.singleton.GetAtomByUid($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}4"));
        viveTracker5 = null;
        SuperController.singleton.RemoveAtom(SuperController.singleton.GetAtomByUid($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}5"));
        viveTracker6 = null;
        SuperController.singleton.RemoveAtom(SuperController.singleton.GetAtomByUid($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}6"));
        viveTracker7 = null;
        SuperController.singleton.RemoveAtom(SuperController.singleton.GetAtomByUid($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}7"));
        viveTracker8 = null;
        SuperController.singleton.RemoveAtom(SuperController.singleton.GetAtomByUid($"{_embodyDebugPrefix}{MotionControlNames.ViveTrackerPrefix}8"));
    }

    public void CreateFakeTrackers(EmbodyDebugSnapshot snapshot)
    {
        StartCoroutine(CreateFakeTrackersCo(snapshot));
    }

    public IEnumerator CreateFakeTrackersCo(EmbodyDebugSnapshot snapshot)
    {
        var e = CreateFakeTrackerCo(MotionControlNames.Head, "headControl", t => head = t, "Head", false);
        while (e.MoveNext())
            yield return e.Current;

        if (snapshot == null || snapshot.leftHand != null)
            StartCoroutine(CreateFakeTrackerCo(MotionControlNames.LeftHand, "lHandControl", t => leftHand = t));
        if (snapshot == null || snapshot.rightHand != null)
            StartCoroutine(CreateFakeTrackerCo(MotionControlNames.RightHand, "rHandControl", t => rightHand = t));
        if (snapshot == null || snapshot.viveTracker1 != null)
            StartCoroutine(CreateFakeTrackerCo($"{MotionControlNames.ViveTrackerPrefix}1", "lFootControl", t => viveTracker1 = t));
        if (snapshot == null || snapshot.viveTracker2 != null)
            StartCoroutine(CreateFakeTrackerCo($"{MotionControlNames.ViveTrackerPrefix}2", "rFootControl", t => viveTracker2 = t));
        if (snapshot == null || snapshot.viveTracker3 != null)
            StartCoroutine(CreateFakeTrackerCo($"{MotionControlNames.ViveTrackerPrefix}3", "lKneeControl", t => viveTracker3 = t));
        if (snapshot == null || snapshot.viveTracker4 != null)
            StartCoroutine(CreateFakeTrackerCo($"{MotionControlNames.ViveTrackerPrefix}4", "rKneeControl", t => viveTracker4 = t));
        if (snapshot == null || snapshot.viveTracker5 != null)
            StartCoroutine(CreateFakeTrackerCo($"{MotionControlNames.ViveTrackerPrefix}5", "hipControl", t => viveTracker5 = t));
        if (snapshot == null || snapshot.viveTracker6 != null)
            StartCoroutine(CreateFakeTrackerCo($"{MotionControlNames.ViveTrackerPrefix}6", "chestControl", t => viveTracker6 = t));
        if (snapshot == null || snapshot.viveTracker7 != null)
            StartCoroutine(CreateFakeTrackerCo($"{MotionControlNames.ViveTrackerPrefix}7", "lElbowControl", t => viveTracker7 = t));
        if (snapshot == null || snapshot.viveTracker8 != null)
            StartCoroutine(CreateFakeTrackerCo($"{MotionControlNames.ViveTrackerPrefix}8", "rElbowControl", t => viveTracker8 = t));
    }

    private IEnumerator CreateFakeTrackerCo(string trackerName, string controlName, Func<Transform, Transform> assign, string icon = null, bool attachToHead = true)
    {
        var atomUid = $"{_embodyDebugPrefix}{trackerName}";
        var atom = SuperController.singleton.GetAtomByUid(atomUid);
        if (atom == null)
        {
            var enumerator = SuperController.singleton.AddAtomByType("GrabPoint", atomUid);
            while (enumerator.MoveNext())
                yield return enumerator.Current;
            atom = SuperController.singleton.GetAtomByUid(atomUid);
        }
        var t = assign(atom.mainController.control);
        if (icon != null)
        {
            var grabPoint = atom.GetStorableByID("GrabPoint");
            if (grabPoint != null)
            {
                grabPoint.SetStringChooserParamValue("grabIconType", icon);
            }
        }

        var controller = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == controlName);
        if (controller != null)
        {
            t.SetPositionAndRotation(controller.control.position, controller.control.rotation);
            var tracker = context.trackers.motionControls.FirstOrDefault(mc => mc.name == trackerName);
            if(tracker != null)
                t.Rotate(tracker.rotateControllerCombined, Space.Self);
        }

        if (attachToHead)
        {
            var c = atom.mainController;
            c.SetLinkToAtom($"{_embodyDebugPrefix}{MotionControlNames.Head}");
            c.SetLinkToRigidbodyObject("control");
            c.currentPositionState = FreeControllerV3.PositionState.ParentLink;
            c.currentRotationState = FreeControllerV3.RotationState.ParentLink;
        }
    }

    public void Log(string message)
    {
        _logs.Add($"[INF] {Time.realtimeSinceStartup:0.00} {message}");
    }

    public void TakeSnapshot(string snapshotName)
    {
        if (!enabled) return;
        var pluginJSON = context.plugin.GetJSON();
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
        RestoreFakeTracker(head, snapshot.head);
        RestoreFakeTracker(leftHand, snapshot.leftHand);
        RestoreFakeTracker(rightHand, snapshot.rightHand);
        RestoreFakeTracker(viveTracker1, snapshot.viveTracker1);
        RestoreFakeTracker(viveTracker2, snapshot.viveTracker2);
        RestoreFakeTracker(viveTracker3, snapshot.viveTracker3);
        RestoreFakeTracker(viveTracker4, snapshot.viveTracker4);
        RestoreFakeTracker(viveTracker5, snapshot.viveTracker5);
        RestoreFakeTracker(viveTracker6, snapshot.viveTracker6);
        RestoreFakeTracker(viveTracker7, snapshot.viveTracker7);
        RestoreFakeTracker(viveTracker8, snapshot.viveTracker8);
    }

    private void RestoreFakeTracker(Transform fake, EmbodyTransformDebugSnapshot snapshot)
    {
        if (fake == null) return;
        var controller = fake.GetComponent<FreeControllerV3>();
        if (snapshot == null)
        {
            SuperController.singleton.RemoveAtom(controller.containingAtom);
            return;
        }
        controller.currentPositionState = FreeControllerV3.PositionState.On;
        controller.currentRotationState = FreeControllerV3.RotationState.On;
        controller.control.position = snapshot.position;
        controller.control.eulerAngles = snapshot.rotation;
        controller.currentPositionState = FreeControllerV3.PositionState.ParentLink;
        controller.currentRotationState = FreeControllerV3.RotationState.ParentLink;
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
            snapshots.AddRange(jc["Snapshots"].AsArray.Childs.Select(s => EmbodyDebugSnapshot.FromJSON(s.AsObject)));
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();
        _logs = new JSONArray();
        snapshots.Clear();
    }
}
