#define POV_DIAGNOSTICS
using System.Collections.Generic;
using System.Linq;

namespace Acidbubbles.ImprovedPoV
{
    public class MemoizedPerson : List<MemoizedMaterial>
    {
        public MemoizedPerson()
        {
        }

        public MemoizedPerson(IEnumerable<MemoizedMaterial> materials)
        : base(materials)
        {
        }

        public static MemoizedPerson FromBroadcastable(List<List<object>> value)
        {
            if (value == null) return null;
            if (value.Count == 0) return null;
            return new MemoizedPerson(value.Select(m => MemoizedMaterial.FromBroadcastable(m)));
        }

        public List<List<object>> ToBroadcastable()
        {
            return this.Select(m => m.ToBroadcastable()).ToList();
        }

        internal static List<List<object>> EmptyBroadcastable()
        {
            // NOTE: Unity does not allow broadcasting null
            return new List<List<object>>();
        }
    }
}