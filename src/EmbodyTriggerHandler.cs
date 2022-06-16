using System.Collections;
using AssetBundles;
using UnityEngine;
using UnityEngine.UI;

public class EmbodyTriggerHandler : TriggerHandler
{
    public Trigger trigger { get; }
    private RectTransform _triggerActionsPrefab;
    private RectTransform _triggerActionMiniPrefab;
    private RectTransform _triggerActionDiscretePrefab;
    private RectTransform _triggerActionTransitionPrefab;

    public EmbodyTriggerHandler()
    {
        trigger = new Trigger
        {
            handler = this
        };
    }

    public void RemoveTrigger(Trigger _)
    {
    }

    public void DuplicateTrigger(Trigger _)
    {
    }

    public RectTransform CreateTriggerActionsUI()
    {
        if (_triggerActionsPrefab == null) return null;
        var rt = Object.Instantiate(_triggerActionsPrefab);

        var content = rt.Find("Content");
        var transitionTab = content.Find("Tab2");
        transitionTab.parent = null;
        Object.Destroy(transitionTab);
        var startTab = content.Find("Tab1");
        startTab.GetComponentInChildren<Text>().text = "On Activate";
        var endTab = content.Find("Tab3");
        var endTabRect = endTab.GetComponent<RectTransform>();
        endTabRect.offsetMin = new Vector2(264, endTabRect.offsetMin.y);
        endTabRect.offsetMax = new Vector2(560, endTabRect.offsetMax.y);
        endTab.GetComponentInChildren<Text>().text = "On Deactivate";

        return rt;
    }

    public RectTransform CreateTriggerActionMiniUI()
    {
        if (_triggerActionMiniPrefab == null) return null;
        var rt = Object.Instantiate(_triggerActionMiniPrefab);
        return rt;
    }

    public RectTransform CreateTriggerActionDiscreteUI()
    {
        if (_triggerActionDiscretePrefab == null) return null;
        var rt = Object.Instantiate(_triggerActionDiscretePrefab);
        return rt;
    }

    public RectTransform CreateTriggerActionTransitionUI()
    {
        return null;
    }

    public void RemoveTriggerActionUI(RectTransform rt)
    {
        if (rt != null) Object.Destroy(rt.gameObject);
    }

    public IEnumerator LoadUIAssets()
    {
        var request = AssetBundleManager.LoadAssetAsync("z_ui2", "TriggerActionsPanel", typeof(GameObject));
        if (request == null)
        {
            SuperController.LogError("Embody: Request for TriggerActionsPanel in z_ui2 assetbundle failed");
            yield break;
        }

        yield return request;
        var go = request.GetAsset<GameObject>();
        if (go == null)
        {
            SuperController.LogError("Embody: Failed to load TriggerActionsPanel asset");
        }

        _triggerActionsPrefab = go.GetComponent<RectTransform>();
        if (_triggerActionsPrefab == null)
        {
            SuperController.LogError("Embody: Failed to load TriggerActionsPanel asset");
        }

        request = AssetBundleManager.LoadAssetAsync("z_ui2", "TriggerActionMiniPanel", typeof(GameObject));
        if (request == null)
        {
            SuperController.LogError("Embody: Request for TriggerActionMiniPanel in z_ui2 assetbundle failed");
            yield break;
        }

        yield return request;
        go = request.GetAsset<GameObject>();
        if (go == null)
        {
            SuperController.LogError("Embody: Failed to load TriggerActionMiniPanel asset");
        }

        _triggerActionMiniPrefab = go.GetComponent<RectTransform>();
        if (_triggerActionMiniPrefab == null)
        {
            SuperController.LogError("Embody: Failed to load TriggerActionMiniPanel asset");
        }

        request = AssetBundleManager.LoadAssetAsync("z_ui2", "TriggerActionDiscretePanel", typeof(GameObject));
        if (request == null)
        {
            SuperController.LogError("Embody: Request for TriggerActionDiscretePanel in z_ui2 assetbundle failed");
            yield break;
        }

        yield return request;
        go = request.GetAsset<GameObject>();
        if (go == null)
        {
            SuperController.LogError("Embody: Failed to load TriggerActionDiscretePanel asset");
        }

        _triggerActionDiscretePrefab = go.GetComponent<RectTransform>();
        if (_triggerActionDiscretePrefab == null)
        {
            SuperController.LogError("Embody: Failed to load TriggerActionDiscretePanel asset");
        }

        request = AssetBundleManager.LoadAssetAsync("z_ui2", "TriggerActionTransitionPanel", typeof(GameObject));
        if (request == null)
        {
            SuperController.LogError("Embody: Request for TriggerActionTransitionPanel in z_ui2 assetbundle failed");
            yield break;
        }

        yield return request;
        go = request.GetAsset<GameObject>();
        if (go == null)
        {
            SuperController.LogError("Embody: Failed to load TriggerActionTransitionPanel asset");
        }

        _triggerActionTransitionPrefab = go.GetComponent<RectTransform>();
        if (_triggerActionTransitionPrefab == null)
        {
            SuperController.LogError("Embody: Failed to load TriggerActionTransitionPanel asset");
        }
    }
}
