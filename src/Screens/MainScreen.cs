using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainScreen : ScreenBase, IScreen
{
    private readonly IEmbodyModule[] _modules;
    private readonly EmbodyWizard _embodyWizard;
    public const string ScreenName = "Embody (Main)";

    public MainScreen(EmbodyContext context, IEmbodyModule[] modules, EmbodyWizard embodyWizard)
        : base(context)
    {
        _modules = modules;
        _embodyWizard = embodyWizard;
    }

    public void Show()
    {
        CreateSpacer().height = 10f;
        CreateButton($"Import / Export Settings...").button.onClick.AddListener(() => screensManager.Show(ImportExportScreen.ScreenName));
        CreateButton("Setup Wizard").button.onClick.AddListener(() => context.StartCoroutine(_embodyWizard.Wizard()));

        CreateText(new JSONStorableString("", @"
Select modules you want to activate when Embody is activated.

Configure each module option in the Screen menu.
".Trim()), true);

        var presetsJSON = new JSONStorableStringChooser("Presets", new List<string>
        {
            "Improved Possession",
            "Snug",
            "Passenger",
        }, "", "Apply a preset", (string val) =>
        {
            foreach (var module in _modules)
            {
                module.selectedJSON.val = false;
            }
            switch (val)
            {
                case "Improved Possession":
                    SelectModule("HideGeometry", true);
                    SelectModule("OffsetCamera", true);
                    SelectModule("WorldScale", true);
                    SelectModule("EyeTarget", true);
                    break;
                case "Snug":
                    SelectModule("HideGeometry", true);
                    SelectModule("OffsetCamera", true);
                    SelectModule("Snug", true);
                    SelectModule("WorldScale", true);
                    SelectModule("EyeTarget", true);
                    break;
                case "Passenger":
                    SelectModule("HideGeometry", true);
                    SelectModule("Passenger", true);
                    SelectModule("WorldScale", true);
                    SelectModule("EyeTarget", true);
                    break;
            }
        });
        CreateScrollablePopup(presetsJSON, true);

        foreach (var module in _modules)
        {
            if (module.storeId == "Automation") continue;
            var selectToggle = CreateToggle(module.selectedJSON, true);
            selectToggle.label = $"Select {module.label}";
            var configureButton = CreateButton($"Configure {module.label}...", true);
            configureButton.buttonText.alignment = TextAnchor.MiddleLeft;
            configureButton.buttonText.GetComponent<RectTransform>().offsetMin = new Vector2(60, 0f);
            configureButton.buttonColor = new Color(0.8f, 0.7f, 0.8f);
            configureButton.button.onClick.AddListener(() => screensManager.Show(module.label));
            configureButton.gameObject.SetActive(module.selectedJSON.val);
            selectToggle.toggle.onValueChanged.AddListener(val => configureButton.gameObject.SetActive(val));
        }
    }

    private void SelectModule(string storeId, bool selected)
    {
        var module = _modules.FirstOrDefault(m => m.storeId == storeId);
        if (module == null) return;
        module.selectedJSON.val = selected;
    }
}
