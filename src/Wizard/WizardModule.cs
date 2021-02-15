using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public interface IWizard : IEmbodyModule
{
    WizardStatusChangedEvent statusChanged { get; }
    JSONStorableString statusJSON { get; }
    bool isRunning { get; }
    bool forceReopen { get; set; }
    void StartWizard();
    void StopWizard(string message = null);
    void Next();
    void Skip();
}

public class WizardStatusChangedEvent : UnityEvent<bool> { }

public class WizardModule : EmbodyModuleBase, IWizard
{
    public const string WizardIntroMessage = "This wizard will walk you through the main Embody features, help you configure them so you can make the most out of this plugin.\n\n. When you are ready, select Start Wizard. Once started, you can use Next to apply the current step, or Skip to ignore it and move on.\n\nOnce done you can always fine tune your profile later manually.";

    public const string Label = "Wizard";
    public override string storeId => "Wizard";
    public override string label => Label;
    public override bool skipChangeEnabledWhenActive => true;

    public WizardStatusChangedEvent statusChanged { get; } = new WizardStatusChangedEvent();
    public JSONStorableString statusJSON { get; } = new JSONStorableString("WizardStatus", "");
    public bool isRunning => _coroutine != null;
    public bool forceReopen { get; set; } = true;

    private Coroutine _coroutine;
    private IWizardStep _step;
    private bool _next;
    private bool _skip;

    public override void Awake()
    {
        base.Awake();

        statusJSON.val = WizardIntroMessage;
    }

    public void StartWizard()
    {
        if (_coroutine != null) StopWizard();
        enabledJSON.val = true;
        _coroutine = StartCoroutine(WizardCoroutine());
        statusChanged.Invoke(true);
    }

    public void StopWizard(string message = null)
    {
        if (!enabledJSON.val) return;

        StopCoroutine(_coroutine);
        _coroutine = null;

        if (_step != null)
        {
            try
            {
                _step.Leave();
            }
            catch (Exception exc)
            {
                SuperController.LogError($"Embody: Wizard {_step}.Leave failed: {exc}");
            }
            _step = null;
        }

        statusJSON.val = message ?? WizardIntroMessage;
        _next = false;
        _skip = false;
        statusChanged.Invoke(false);

        // TODO: This ugly fix is for when you're canceling during Snug setup, we need some way to restore the hands after. Maybe a cleanup step would be better.
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand).enabled = true;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand).enabled = true;

        enabledJSON.val = false;
    }

    public void Next()
    {
        _next = true;
    }

    public void Skip()
    {
        _skip = true;
    }

    private IEnumerator WizardCoroutine()
    {
        context.embody.activeJSON.val = false;
        context.Initialize();

        yield return 0;

        SuperController.singleton.worldScale = 1f;

        context.embody.presetsJSON.val = "Improved Possession";

        var steps = BuildSteps();

        for (var i = 0; i < steps.Count; i++)
        {
            _step = steps[i];
            statusJSON.val = $"Step {i + 1} / {steps.Count}\n\n{_step.helpText}";
            try
            {
                _step.Enter();
            }
            catch (Exception exc)
            {
                SuperController.LogError($"Embody: Wizard {_step}.Enter failed: {exc}");
                StopWizard("An error prevented the wizard from finishing");
                yield break;
            }

            while (!AreAnyStartRecordKeysDown())
            {
                ReopenIfClosed();

                if (_skip)
                    break;

                yield return 0;

                try
                {
                    _step.Update();
                }
                catch (Exception exc)
                {
                    SuperController.LogError($"Embody: Wizard {_step}.Update failed: {exc}");
                    StopWizard("An error prevented the wizard from finishing");
                    yield break;
                }
            }

            ReopenIfClosed();

            try
            {
                if (_skip)
                    _skip = false;
                else
                    _step.Apply();
            }
            catch (Exception exc)
            {
                SuperController.LogError($"Embody: Wizard {_step}.Apply failed: {exc}");
                StopWizard("An error prevented the wizard from finishing");
                yield break;
            }
            finally
            {
                try
                {
                    _step?.Leave();
                }
                catch (Exception exc)
                {
                    SuperController.LogError($"Embody: Wizard {_step}.Leave failed: {exc}");
                    StopWizard("An error prevented the wizard from finishing");
                }
            }

            yield return 0;
        }

        StopWizard("All done! You can now activate Embody.\n\nYou can tweak your settings or start this wizard again. You can make this setup your default in the Import/Export screen. Default settings will automatically apply whenever you load this plugin on an atom.");
    }

    private void ReopenIfClosed()
    {
        if (!forceReopen) return;
        if (context.plugin.UITransform.gameObject.activeInHierarchy) return;
        if (!SuperController.singleton.mainHUD.gameObject.activeInHierarchy) return;
        SuperController.singleton.SelectController(containingAtom.mainController, false, false, false);
        SuperController.singleton.ShowMainHUD(false);
        var selector = containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
        selector.SetActiveTab("Plugins");
        context.plugin.UITransform.gameObject.SetActive(true);
    }

    private List<IWizardStep> BuildSteps()
    {
        var steps = new List<IWizardStep>();

        // NOTE: We use Count because we want to sync all available motion controls, not only the first one
        var useViveTrackers = context.trackers.viveTrackers.Count(t => t.SyncMotionControl());

        steps.Add(new ResetPoseStep(context, useViveTrackers == 0));

        steps.Add(new RecordPlayerHeightStep(context.worldScale));

        if (useViveTrackers > 0)
            steps.Add(new AskViveTrackersStep(context, steps, useViveTrackers));

        steps.Add(new AskSnugStep(context, steps));

        steps.Add(new MakeDefaultsStep(context));

        return steps;
    }


    private bool AreAnyStartRecordKeysDown()
    {
        if (_next || _skip)
        {
            _next = false;
            return true;
        }
        if (Input.GetKey(KeyCode.Escape))
        {
            _skip = true;
            return true;
        }
        var sc = SuperController.singleton;
        if (sc.isOVR)
        {
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.Touch)) return true;
            if (OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.Touch)) return true;
        }
        if (sc.isOpenVR)
        {
            if (sc.selectAction.stateDown) return true;
        }
        if (Input.GetKeyDown(KeyCode.Space)) return true;
        return false;
    }
}
