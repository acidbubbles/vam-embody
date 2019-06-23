#define POV_DIAGNOSTICS
using System;

namespace Acidbubbles.VaM.Plugins
{
    public class PossessOnly : MVRScript
    {
        private Atom _target;
        private FreeControllerV3 _headControl;
        private JSONStorableBool _whenPossessed;
        private bool _enabled;
        private bool _hidden;

        public override void Init()
        {
            try
            {
                _target = containingAtom;
                _headControl = (FreeControllerV3)_target.GetStorableByID("headControl");
                InitControls();
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to initialize Possess Only: " + e);
            }
        }

        public void OnEnable()
        {
            try
            {
                _enabled = true;
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to enable Possess Only: " + e);
            }
        }

        public void OnDisable()
        {
            try
            {
                if (!_enabled) return;

                _enabled = false;
                Update();
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to disable Possess Only: " + e);
            }
        }

        public void OnDestroy()
        {
            OnDisable();
        }

        public void Update()
        {
            if (!_enabled)
            {
                if (_hidden)
                {
                    _target.hidden = false;
                    _hidden = false;
                }
                return;
            }

            var shouldHide = _headControl.possessed == _whenPossessed.val;

            if (shouldHide && !_hidden)
            {
                _target.hidden = true;
                _hidden = true;
            }
            else if (!shouldHide && _hidden)
            {
                _target.hidden = false;
                _hidden = false;
            }
        }

        private void InitControls()
        {
            try
            {
                _whenPossessed = new JSONStorableBool("Hidden when possession is active", true);
                RegisterBool(_whenPossessed);
                CreateToggle(_whenPossessed, true);
            }
            catch (Exception e)
            {
                SuperController.LogError("Failed to register controls: " + e);
            }
        }
    }
}