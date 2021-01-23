using UnityEngine;
using UnityEngine.UI;

namespace Common
{
    public static class UIDynamicPopupExtensions
    {
        public static UIDynamicPopup AddNav(this UIDynamicPopup popup, MVRScript owner, bool filterable = false)
        {
            popup.popup.labelText.alignment = TextAnchor.UpperCenter;
            var labelTextRect = popup.popup.labelText.GetComponent<RectTransform>();
            float btnAnchorOffsetMaxY;
            if (filterable)
            {
                popup.popup.labelText.GetComponent<RectTransform>().anchorMax = new Vector2(0.03f, 0.91f);
                btnAnchorOffsetMaxY = 70;
            }
            else
            {
                popup.popup.labelText.fontSize = 24;
                labelTextRect.anchorMax = new Vector2(0.03f, 0.95f);
                btnAnchorOffsetMaxY = 65f;
            }

            {
                var btn = Object.Instantiate(owner.manager.configurableButtonPrefab, popup.transform, false);
                Object.Destroy(btn.GetComponent<LayoutElement>());
                btn.GetComponent<UIDynamicButton>().label = "<";
                btn.GetComponent<UIDynamicButton>().button.onClick.AddListener(() => { popup.popup.SetPreviousValue(); });
                var prevBtnRect = btn.GetComponent<RectTransform>();
                prevBtnRect.pivot = new Vector2(0, 0);
                prevBtnRect.anchoredPosition = new Vector2(10f, 0);
                prevBtnRect.sizeDelta = new Vector2(0f, 0f);
                prevBtnRect.offsetMin = new Vector2(5f, 5f);
                prevBtnRect.offsetMax = new Vector2(80f, btnAnchorOffsetMaxY);
                prevBtnRect.anchorMin = new Vector2(0f, 0f);
                prevBtnRect.anchorMax = new Vector2(0f, 0f);
            }

            {
                var btn = Object.Instantiate(owner.manager.configurableButtonPrefab, popup.transform, false);
                Object.Destroy(btn.GetComponent<LayoutElement>());
                btn.GetComponent<UIDynamicButton>().label = ">";
                btn.GetComponent<UIDynamicButton>().button.onClick.AddListener(() => { popup.popup.SetNextValue(); });
                var prevBtnRect = btn.GetComponent<RectTransform>();
                prevBtnRect.pivot = new Vector2(0, 0);
                prevBtnRect.anchoredPosition = new Vector2(10f, 0);
                prevBtnRect.sizeDelta = new Vector2(0f, 0f);
                prevBtnRect.offsetMin = new Vector2(82f, 5f);
                prevBtnRect.offsetMax = new Vector2(157f, btnAnchorOffsetMaxY);
                prevBtnRect.anchorMin = new Vector2(0f, 0f);
                prevBtnRect.anchorMax = new Vector2(0f, 0f);
            }

            return popup;
        }
    }
}
