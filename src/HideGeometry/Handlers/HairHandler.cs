using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Handlers
{
    public class HairHandler : IHandler
    {
        private readonly DAZHairGroup _hair;
        private Material _hairMaterial;
        private string _hairShaderProperty;
        private float _hairShaderHiddenValue;
        private float _hairShaderOriginalValue;
        private List<MaterialAlphaSnapshot> _materialRefs;

        public HairHandler(DAZHairGroup hair)
        {
            _hair = hair;
        }

        public static bool Supports(DAZHairGroup hair)
        {
            if (hair == null || hair.name == "NoHair")
                return false;

            if (hair.name == "SimHairGroup" || hair.name == "SimHairGroup2")
                return false;

            return true;
        }

        public bool Prepare()
        {
            if (_hair.name == "Sim2Hair" || _hair.name == "Sim2HairMale" || _hair.name == "CustomHairItem")
                return ConfigureSimV2Hair();

            return ConfigureSimpleHair();
        }

        private bool ConfigureSimV2Hair()
        {
            var materialRefs = new List<MaterialAlphaSnapshot>(GetScalpMaterialReferences(_hair));
            if (materialRefs.Count != 0) _materialRefs = materialRefs;

            var hairMaterial = _hair.GetComponentInChildren<MeshRenderer>()?.material;
            if (hairMaterial == null)
                return false;

            _hairMaterial = hairMaterial;
            _hairShaderProperty = "_StandWidth";
            if(!_hairMaterial.HasProperty(_hairShaderProperty))
                SuperController.LogError($"Hair {_hair.displayName} does not have shader property {_hairShaderProperty}");
            _hairShaderHiddenValue = 0f;
            _hairShaderOriginalValue = _hairMaterial.GetFloat(_hairShaderProperty);
            return true;
        }

        private bool ConfigureSimpleHair()
        {
            var materialRefs = _hair.GetComponentsInChildren<DAZMesh>()
                .SelectMany(m => m.materials)
                .Distinct()
                .Select(m => new MaterialAlphaSnapshot
                {
                    material = m,
                    originalAlphaAdjust = m.GetFloat("_AlphaAdjust")
                })
                .ToList();

            if (materialRefs.Count == 0)
                return false;

            materialRefs.AddRange(GetScalpMaterialReferences(_hair));

            _materialRefs = materialRefs;

            return true;
        }

        private static IEnumerable<MaterialAlphaSnapshot> GetScalpMaterialReferences(DAZHairGroup hair)
        {
            return hair.GetComponentsInChildren<DAZSkinWrap>()
                .SelectMany(m => m.GPUmaterials)
                .Distinct()
                .Select(m => new MaterialAlphaSnapshot
                {
                    material = m,
                    originalAlphaAdjust = m.GetFloat("_AlphaAdjust")
                });
        }

        public void Dispose()
        {
        }

        public void BeforeRender()
        {
            // ReSharper disable once Unity.NoNullPropagation
            _hairMaterial?.SetFloat(_hairShaderProperty, _hairShaderHiddenValue);
            for (var i = 0; i < _materialRefs.Count; i++)
            {
                var materialRef = _materialRefs[i];
                materialRef.material.SetFloat("_AlphaAdjust", -1f);
            }
        }

        public void AfterRender()
        {
            // ReSharper disable once Unity.NoNullPropagation
            _hairMaterial?.SetFloat(_hairShaderProperty, _hairShaderOriginalValue);
            for (var i = 0; i < _materialRefs.Count; i++)
            {
                var materialRef = _materialRefs[i];
                materialRef.material.SetFloat("_AlphaAdjust", materialRef.originalAlphaAdjust);
            }
        }
    }
}
