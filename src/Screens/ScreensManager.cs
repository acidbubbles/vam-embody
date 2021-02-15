using System.Collections.Generic;

public interface IScreensManager
{
    bool Show(string screenName);
}

public class ScreensManager : IScreensManager
{
    public JSONStorableStringChooser screensJSON { get; }

    private readonly List<string> _screenNames = new List<string>();
    private readonly Dictionary<string, IScreen> _screens = new Dictionary<string, IScreen>();
    private string _currentScreenName;
    private UIDynamicButton _backButton;
    private string _mainScreenName;

    public ScreensManager()
    {
        screensJSON = new JSONStorableStringChooser("Screens", _screenNames, "", "Screen", val =>
        {
            if (!Show(val)) screensJSON.valNoCallback = _currentScreenName;
        });
    }

    public void Add(string screenName, IScreen screen)
    {
        screen.screensManager = this;
        _screenNames.Add(screenName);
        _screens.Add(screenName, screen);
    }

    public void Init(MVRScript plugin, string mainScreen)
    {
        _mainScreenName = mainScreen;
        _backButton = plugin.CreateButton("< Back");
        _backButton.button.onClick.AddListener(() => Show(mainScreen));
        _backButton.height = 100f;
    }

    public bool Show(string screenName)
    {
        if (screenName == _currentScreenName || string.IsNullOrEmpty(screenName)) return false;

        IScreen screen;
        if (_currentScreenName != null)
        {
            if (_screens.TryGetValue(_currentScreenName, out screen))
                screen.Hide();
            _currentScreenName = null;
            screensJSON.valNoCallback = screenName;
        }

        if (!_screens.TryGetValue(screenName, out screen))
            return false;

        screen.Show();
        _currentScreenName = screenName;
        screensJSON.valNoCallback = screenName;
        _backButton.button.interactable = screenName != _mainScreenName;
        _backButton.label = _backButton.button.interactable ? "< Back" : "Welcome to Embody <3";

        return true;
    }
}
