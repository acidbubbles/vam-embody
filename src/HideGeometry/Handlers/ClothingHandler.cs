using System.Collections.Generic;
using UnityEngine;

namespace Handlers
{
    public class ClothingHandler : IHandler
    {
        private readonly DAZClothingItem _clothing;
        private readonly List<MaterialAlphaSnapshot> _materials = new List<MaterialAlphaSnapshot>();

        public ClothingHandler(DAZClothingItem clothing)
        {
            _clothing = clothing;
        }

        public bool Prepare()
        {
            var wrap = _clothing.GetComponentInChildren<DAZSkinWrap>();
            if (wrap.GPUuseSimpleMaterial)
            {
                AddMaterial(wrap.GPUsimpleMaterial);
            }
            else
            {
                foreach (var mat in wrap.GPUmaterials)
                {
                    AddMaterial(mat);
                }
            }
            return _materials.Count != 0;
        }

        private void AddMaterial(Material mat)
        {
            if (mat == null) return;
            if (!mat.HasProperty("_AlphaAdjust")) return;
            _materials.Add(new MaterialAlphaSnapshot
            {
                material = mat,
                originalAlphaAdjust = mat.GetFloat("_AlphaAdjust"),
                renderQueue = mat.renderQueue
            });
            mat.renderQueue = 2500;
        }

        public void Dispose()
        {
            foreach (var snapshot in _materials)
            {
                snapshot.material.renderQueue = snapshot.renderQueue;
            }
        }

        public void BeforeRender()
        {
            for (var i = 0; i < _materials.Count; i++)
            {
                var material = _materials[i].material;
                material.SetFloat("_AlphaAdjust", -1f);
            }
        }

        public void AfterRender()
        {
            for (var i = 0; i < _materials.Count; i++)
            {
                var snapshot = _materials[i];
                var material = snapshot.material;
                material.SetFloat("_AlphaAdjust", snapshot.originalAlphaAdjust);
            }
        }
    }
}
