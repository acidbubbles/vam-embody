using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface IDiagnosticsModule : IEmbodyModule
{
    IEnumerable<string> logs { get; }
}

public class DiagnosticsModule : EmbodyModuleBase, IDiagnosticsModule
{
    public const string Label = "Diagnostics";
    public override string storeId => "Diagnostics";
    public override string label => Label;
    public override bool skipChangeEnabledWhenActive => true;

    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    public Transform viveTracker1;
    public Transform viveTracker2;
    public Transform viveTracker3;
    public Transform viveTracker4;
    public Transform viveTracker5;
    public Transform viveTracker6;
    public Transform viveTracker7;
    public Transform viveTracker8;

    private bool _once;

    private readonly JSONArray _logs = new JSONArray();
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

    private void CaptureLog(string condition, string stacktrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception) return;
        _logs.Add($"{Time.realtimeSinceStartup:0.00} {type} {condition} {stacktrace}");
    }

    private static Transform GetDebugAtom(string uid)
    {
        var atom = SuperController.singleton.GetAtomByUid(uid);
        if (atom == null || !atom.on) return null;
        return atom.mainController.control;
    }

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);

        jc["Logs"] = _logs;
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();
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
