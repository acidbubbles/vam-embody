using System.Collections.Generic;

public class ScreensManager
{
    public JSONStorableStringChooser screensJSON { get; }

    private readonly List<string> _screenNames = new List<string>();
    private readonly Dictionary<string, IScreen> _screens = new Dictionary<string, IScreen>();
    private string _currentScreenName;

    public ScreensManager()
    {
        screensJSON = new JSONStorableStringChooser("Screens", _screenNames, "", "Screen", Show);
    }

    public void Add(string screenName, IScreen screen)
    {
        _screenNames.Add(screenName);
        _screens.Add(screenName, screen);
    }

    public void Show(string screenName)
    {
        IScreen screen;
        if (_currentScreenName != null)
        {
            if (_screens.TryGetValue(_currentScreenName, out screen))
                screen.Hide();
            _currentScreenName = null;
            screensJSON.valNoCallback = screenName;
        }

        if (!string.IsNullOrEmpty(screenName))
        {
            if (_screens.TryGetValue(screenName, out screen))
                screen.Show();
            _currentScreenName = screenName;
            screensJSON.valNoCallback = screenName;
        }
    }
}
