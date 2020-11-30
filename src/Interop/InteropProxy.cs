using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Interop
{
    public class InteropProxy
    {
        private readonly Atom _containingAtom;

        public bool active
        {
            get { return _activeJSON.val; }
            set { _activeJSON.val = value; }
        }

        public bool ready;
        public IHideGeometry hideGeometry;
        public IPassenger passenger;
        public ICameraOffset cameraOffset;
        public IWorldScale worldScale;

        private readonly MVRScript _this;
        private JSONStorableBool _activeJSON;

        public InteropProxy(MVRScript @this, Atom containingAtom)
        {
            _this = @this;
            _containingAtom = containingAtom;
        }

        public void Init()
        {
            _activeJSON = new JSONStorableBool("Active", false, (bool val) =>
            {
                try
                {
                    _this.enabledJSON.val = val;
                }
                catch (Exception exc)
                {
                    if (val)
                    {
                        SuperController.LogError($"Embody: Failed to activate: {exc}");
                        _this.enabledJSON.val = false;
                    }
                    else
                    {
                        SuperController.LogError($"Embody: Failed to deactivate: {exc}");
                    }
                }
            }) {isStorable = false};
            _this.RegisterBool(_activeJSON);
            _this.CreateToggle(_activeJSON);
            _this.enabledJSON.isStorable = false;
            _this.enabledJSON.setCallbackFunction += (bool val) => _activeJSON.val = val;
            _this.enabledJSON.val = false;

            StartInitDeferred();
        }

        public void StartInitDeferred()
        {
            SuperController.singleton.StartCoroutine(InitDeferred());
        }

        private IEnumerator InitDeferred()
        {
            yield return new WaitForEndOfFrame();
            if (_containingAtom == null) yield break;
            foreach (var plugin in _containingAtom.GetStorableIDs().Select(s => _containingAtom.GetStorableByID(s)).OfType<IEmbodyPlugin>())
            {
                if (TryAssign(plugin, ref hideGeometry)) continue;
                if (TryAssign(plugin, ref passenger)) continue;
                if (TryAssign(plugin, ref worldScale)) continue;
                if (TryAssign(plugin, ref cameraOffset)) continue;
            }
            ready = true;
        }

        public void SelectPlugin(IEmbodyPlugin plugin)
        {
            SuperController.singleton.SelectController(_containingAtom.mainController);
            var selector = _containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
            selector.SetActiveTab("Plugins");
            ((MVRScript) plugin).UITransform.gameObject.SetActive(true);
        }


        private bool TryAssign<T>(IEmbodyPlugin plugin, ref T field)
            where T : class, IEmbodyPlugin
        {
            var cast = plugin as T;
            if (cast == null) return false;

            field = cast;
            return true;
        }
    }
}
