#define POV_DIAGNOSTICS
using System.Collections.Generic;
using UnityEngine;

namespace Acidbubbles.ImprovedPoV
{
    public class SceneWatcher
    {
        private readonly MemoizedPerson _person;
        private int _lastUpdatedFrame = 0;

        public SceneWatcher(MemoizedPerson person)
        {
            _person = person;
        }

        public void Start()
        {
            SuperController.singleton.onAtomUIDsChangedHandlers += OnAtomUIDsChanged;
            SuperController.singleton.onAtomUIDRenameHandlers += OnAtomUIDRename;
            UpdateMirrors(_person, true);
        }

        public void Stop()
        {
            SuperController.singleton.onAtomUIDsChangedHandlers -= OnAtomUIDsChanged;
            SuperController.singleton.onAtomUIDRenameHandlers -= OnAtomUIDRename;
            UpdateMirrors(null, true);
        }

        private void OnAtomUIDsChanged(List<string> atomUIDs)
        {
            SuperController.LogMessage("Changed " + atomUIDs.Count);
            // This may mean a new mirror was added; we usually don't care about other atoms.
            UpdateMirrors(_person, false);
        }

        private void OnAtomUIDRename(string oldName, string newName)
        {
            // TODO: This doesn't seem to work for load look / skin
            SuperController.LogMessage("Rename " + oldName + " " + newName);
            // This may mean a new look was applied
            UpdateMirrors(_person, false);
        }

        private void UpdateMirrors(MemoizedPerson person, bool force)
        {
            if (_lastUpdatedFrame == Time.frameCount && !force) return;

            _lastUpdatedFrame = Time.frameCount;
            var broadcastable = person == null ? MemoizedPerson.EmptyBroadcastable() : person.ToBroadcastable();
            // TODO: There may be a better method for this?
            foreach (var atom in SuperController.singleton.GetAtoms())
                atom.gameObject.BroadcastMessage("ImprovedPoVSkinUpdated", broadcastable);
        }
    }
}