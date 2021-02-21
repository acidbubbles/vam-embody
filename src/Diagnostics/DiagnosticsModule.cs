using System.Collections;
using SimpleJSON;
using UnityEngine;

public interface IDiagnosticsModule : IEmbodyModule
{
}

public class DiagnosticsModule : EmbodyModuleBase, IDiagnosticsModule
{
    public const string Label = "Diagnostics";
    public override string storeId => "Diagnostics";
    public override string label => Label;
    public override bool skipChangeEnabledWhenActive => true;

    private bool _once;

    public override void Awake()
    {
        base.Awake();

        enabled = true;

        //context.plugin.StartCoroutine(DebugCo());
    }

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);
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
