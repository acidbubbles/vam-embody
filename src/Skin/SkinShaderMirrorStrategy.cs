#define POV_DIAGNOSTICS
using System.Collections.Generic;
using System.Linq;

namespace Acidbubbles.ImprovedPoV.Skin
{
    public class SkinShaderMirrorStrategy : IMirrorStrategy
    {
        private string _ownerStrategyName;
        string IMirrorStrategy.OwnerStrategyName => _ownerStrategyName;

        public List<SkinShaderMaterialReference> materials;

        public SkinShaderMirrorStrategy(string ownerStrategyName, List<SkinShaderMaterialReference> materials)
        {
            this._ownerStrategyName = ownerStrategyName;
            this.materials = materials;
        }

        public static SkinShaderMirrorStrategy FromBroadcastable(string ownerStrategyName, object data)
        {
            return new SkinShaderMirrorStrategy(
                ownerStrategyName,
                ((List<object>)data).Select(o => SkinShaderMaterialReference.FromBroadcastable(o)).ToList()
                );
        }

        public object ToBroadcastable()
        {
            return materials.Select(m => m.ToBroadcastable()).ToList();
        }

        public bool BeforeMirrorRender()
        {
            if (materials == null) return false;
            foreach (var material in materials)
            {
                material.MakeVisible();
            }
            return true;
        }

        public void AfterMirrorRender()
        {
            if (materials == null) return;
            foreach (var material in materials)
            {
                material.MakeInvisible();
            }
        }
    }
}