using System.Linq;

namespace Interop
{
    public class InteropProxy
    {
        private readonly Atom _containingAtom;

        public IImprovedPoV improvedPoV;

        public InteropProxy(Atom containingAtom)
        {
            _containingAtom = containingAtom;
        }

        public void Connect()
        {
            if (_containingAtom == null) return;
            foreach (var plugin in _containingAtom.GetStorableIDs().Select(s => _containingAtom.GetStorableByID(s)).OfType<IEmbodyPlugin>())
            {
                if (TryAssign(plugin, ref improvedPoV)) continue;
            }
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
