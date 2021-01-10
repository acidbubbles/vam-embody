using System.Linq;

namespace Handlers
{
    public class MaterialOptionsSnapshot
    {
        public MaterialOptions options;
        private float _alpha;

        public static bool Supports(MaterialOptions arg)
        {
            // TODO: Delete this once I re-created the slider7 behavior...
            return arg.param7Slider != null;
        }

        public static MaterialOptionsSnapshot Snap(MaterialOptions options)
        {
            return new MaterialOptionsSnapshot
            {
                options = options,
                _alpha = options.param7Slider.value
            };
        }

        public void Restore()
        {
            options.param7Slider.value = _alpha;
        }
    }

    public class ClothingHandler : IHandler
    {
        private readonly DAZClothingItem _clothing;
        private MaterialOptionsSnapshot[] _materials;

        public ClothingHandler(DAZClothingItem clothing)
        {
            _clothing = clothing;
        }

        public bool Prepare()
        {
            var materialOptions = _clothing.GetComponentsInChildren<MaterialOptions>();
            if (materialOptions.Length == 0) return false;
            _materials = materialOptions.Where(MaterialOptionsSnapshot.Supports).Select(MaterialOptionsSnapshot.Snap).ToArray();
            return true;
        }

        public void Dispose()
        {
        }

        public void BeforeRender()
        {
            // TODO: for instead of foreach
            foreach (var material in _materials)
            {
                // TODO: This is WAY too expensive for something to run on every camera on every frame!
                material.options.param7Slider.value = -1f;
            }
        }

        public void AfterRender()
        {
            foreach (var material in _materials)
            {
                material.Restore();
            }
        }
    }
}
