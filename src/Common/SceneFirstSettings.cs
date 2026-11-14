using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SimpleJSON;
using UnityEngine;

// Settings marked ** in the UI: the scene's value is used when the scene sets one, otherwise the player's value from
// their profile. A profile saves what is on screen, like any other profile setting. A scene saves only the values the
// scene set, so the player's own values are never saved into a scene.
public class SceneFirstSettings
{
    // A scene save lists the settings the scene sets, so a scene can also keep a default value on purpose. Older files
    // have no list: a setting counts as set by the scene when the file holds a value other than the default.
    private const string SceneSetKey = "SceneSet";

    private abstract class Entry
    {
        public string key;
        public bool setByScene;
        public abstract bool IsDefaultLive();
        public abstract bool HoldsNonDefault(JSONClass jc);
        public abstract void ReadPlayer(JSONClass jc);
        public abstract void ReadLive(JSONClass jc);
        public abstract void WriteLive(JSONClass jc);
        public abstract void ApplyPlayer();
        public abstract void ResetPlayer();
        // Called after any change: returns true when the live value is not the one last applied, i.e. the player
        // changed it, and makes it the player's value too.
        public abstract bool CaptureIfChanged();
        // After loading: the live value, which linked settings may have adjusted, is the one applied.
        public abstract void SyncApplied();
    }

    private class Entry<T> : Entry
    {
        public Func<T> get;
        public Action<T> set;
        public T defaultVal;
        public Func<JSONNode, T> read;
        public Action<JSONClass, string, T> write;
        public Func<T, T, bool> equals;
        public T player;
        public T applied;

        public override bool IsDefaultLive() => equals(get(), defaultVal);
        public override bool HoldsNonDefault(JSONClass jc) => jc.HasKey(key) && !equals(read(jc[key]), defaultVal);
        public override void ReadPlayer(JSONClass jc) => player = jc.HasKey(key) ? read(jc[key]) : defaultVal;
        public override void ReadLive(JSONClass jc) => Apply(jc.HasKey(key) ? read(jc[key]) : defaultVal);
        public override void WriteLive(JSONClass jc) => write(jc, key, get());
        public override void ApplyPlayer() => Apply(player);
        public override void ResetPlayer() => player = defaultVal;

        private void Apply(T value)
        {
            set(value);
            applied = get();
        }

        public override bool CaptureIfChanged()
        {
            var live = get();
            if (equals(live, applied)) return false;
            applied = live;
            player = live;
            return true;
        }

        public override void SyncApplied() => applied = get();
    }

    private readonly List<Entry> _entries = new List<Entry>();
    private bool _applying;

    public void Add(JSONStorableBool storable)
    {
        Add(storable.name, () => storable.val, v => storable.val = v, storable.defaultVal,
            n => n.Value == "true",
            (jc, k, v) => jc[k] = v ? "true" : "false",
            (a, b) => a == b);
        var callback = storable.setCallbackFunction;
        storable.setCallbackFunction = val =>
        {
            if (callback != null) callback(val);
            Changed();
        };
    }

    public void Add(JSONStorableFloat storable)
    {
        Add(storable.name, () => storable.val, v => storable.val = v, storable.defaultVal,
            n => ParseFloat(n.Value, storable.defaultVal),
            (jc, k, v) => jc[k] = v.ToString(CultureInfo.InvariantCulture),
            // Not Mathf.Approximately: in VaM it reports 0 and 0 as different.
            (a, b) => Math.Abs(a - b) < 0.000001f);
        var callback = storable.setCallbackFunction;
        storable.setCallbackFunction = val =>
        {
            if (callback != null) callback(val);
            Changed();
        };
    }

    public void Add(JSONStorableStringChooser storable)
    {
        Add(storable.name, () => storable.val, v => storable.val = v, storable.defaultVal,
            n => n.Value,
            (jc, k, v) => jc[k] = v,
            (a, b) => a == b);
        var callback = storable.setCallbackFunction;
        storable.setCallbackFunction = val =>
        {
            if (callback != null) callback(val);
            Changed();
        };
    }

    // For a value without a storable; its setter must call Changed() when the player changes it.
    public void Add(string key, Func<bool> get, Action<bool> set, bool defaultVal)
    {
        Add(key, get, set, defaultVal,
            n => n.Value == "true",
            (jc, k, v) => jc[k] = v ? "true" : "false",
            (a, b) => a == b);
    }

    // For a value without a storable; its setter must call Changed() when the player changes it.
    public void Add(string key, Func<Vector3> get, Action<Vector3> set)
    {
        Add(key, get, set, Vector3.zero,
            n => n.ToVector3(Vector3.zero),
            (jc, k, v) => jc[k] = v.ToJSON(),
            (a, b) => a == b);
    }

    // For a value without a storable; its setter must call Changed() when the player changes it.
    public void Add(string key, Func<KeyCode> get, Action<KeyCode> set)
    {
        Add(key, get, set, KeyCode.None,
            n => ParseKeyCode(n.Value),
            (jc, k, v) => jc[k] = v.ToString(),
            (a, b) => a == b);
    }

    private void Add<T>(string key, Func<T> get, Action<T> set, T defaultVal, Func<JSONNode, T> read, Action<JSONClass, string, T> write, Func<T, T, bool> equals)
    {
        _entries.Add(new Entry<T>
        {
            key = key,
            get = get,
            set = set,
            defaultVal = defaultVal,
            read = read,
            write = write,
            equals = equals,
            player = defaultVal,
            applied = defaultVal,
        });
    }

    // A change made by the player (in the UI, a preset or the wizard). It is what the scene now uses, so it is saved with
    // the scene, and it becomes the player's value too. Changes made while loading are ignored.
    public void Changed()
    {
        if (_applying) return;
        foreach (var e in _entries)
        {
            if (e.CaptureIfChanged())
                e.setByScene = true;
        }
    }

    // Makes a change that is not the scene's choice (e.g. clearing personal data): the result is not marked as set by
    // the scene.
    public void ChangeWithoutMarking(Action change)
    {
        _applying = true;
        try
        {
            change();
            foreach (var e in _entries)
                e.SyncApplied();
        }
        finally
        {
            _applying = false;
        }
    }

    public void StoreJSON(JSONClass jc, bool toProfile, bool toScene)
    {
        if (toScene)
        {
            var sceneSet = new JSONArray();
            foreach (var e in _entries.Where(e => e.setByScene))
            {
                e.WriteLive(jc);
                sceneSet.Add(e.key);
            }
            jc[SceneSetKey] = sceneSet;
        }
        else if (toProfile)
        {
            foreach (var e in _entries.Where(e => !e.IsDefaultLive()))
                e.WriteLive(jc);
        }
    }

    public void RestoreFromJSON(JSONClass jc, bool fromProfile, bool fromScene)
    {
        _applying = true;
        try
        {
            if (fromProfile)
            {
                foreach (var e in _entries)
                {
                    e.ReadPlayer(jc);
                    if (!e.setByScene) e.ApplyPlayer();
                }
            }

            if (fromScene)
            {
                var sceneSet = jc.HasKey(SceneSetKey) ? new HashSet<string>(jc[SceneSetKey].AsArray.Childs.Select(c => c.Value)) : null;
                foreach (var e in _entries)
                {
                    e.setByScene = sceneSet != null ? sceneSet.Contains(e.key) : e.HoldsNonDefault(jc);
                    if (e.setByScene)
                        e.ReadLive(jc);
                    else
                        e.ApplyPlayer();
                }
            }

            foreach (var e in _entries)
                e.SyncApplied();
        }
        finally
        {
            _applying = false;
        }
    }

    public void ResetToDefault()
    {
        _applying = true;
        try
        {
            foreach (var e in _entries)
            {
                e.setByScene = false;
                e.ResetPlayer();
                e.ApplyPlayer();
            }

            foreach (var e in _entries)
                e.SyncApplied();
        }
        finally
        {
            _applying = false;
        }
    }

    private static float ParseFloat(string value, float defaultVal)
    {
        float result;
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? result : defaultVal;
    }

    private static KeyCode ParseKeyCode(string value)
    {
        if (string.IsNullOrEmpty(value)) return KeyCode.None;
        try
        {
            return (KeyCode) Enum.Parse(typeof(KeyCode), value);
        }
        catch (ArgumentException)
        {
            return KeyCode.None;
        }
    }
}
