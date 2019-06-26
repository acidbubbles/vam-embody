using System;
using System.Collections.Generic;
using System.Linq;

namespace Acidbubbles.ImprovedPoV
{
    public class State
    {
        public static MemoizedPerson current;

        public static void Register(MemoizedPerson person)
        {
            if (current != null) throw new InvalidOperationException("Only one person at a time can have the ImprovedPoV plugin");

            current = person;
            var temp = person.Select(m => new List<object> { m.originalSpecColor });
            UpdateMirrors(person);
        }

        public static void Unregister(MemoizedPerson person)
        {
            if (current == null) return;

            current = null;
            UpdateMirrors(null);
        }

        private static void UpdateMirrors(MemoizedPerson person)
        {
            var broadcastable = person == null ? MemoizedPerson.EmptyBroadcastable() : person.ToBroadcastable();
            // TODO: There may be a better method for this?
            foreach (var atom in SuperController.singleton.GetAtoms())
                atom.gameObject.BroadcastMessage("ImprovedPoVSkinUpdated", broadcastable);
        }
    }
}