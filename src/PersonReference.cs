#define POV_DIAGNOSTICS
using System;
using System.Collections.Generic;
using Acidbubbles.ImprovedPoV.Hair;
using Acidbubbles.ImprovedPoV.Skin;

namespace Acidbubbles.ImprovedPoV
{
    public class PersonReference
    {
        public DAZSkinV2 skin;
        public IMirrorStrategy skinStrategy;
        public DAZHairGroup hair;
        public IMirrorStrategy hairStrategy;
        private bool _failedOnce;

        public PersonReference()
        {
        }

        public PersonReference(DAZSkinV2 skin, DAZHairGroup hair)
        {
            this.skin = skin;
            this.hair = hair;
        }

        public static PersonReference FromBroadcastable(Dictionary<string, object> value)
        {
            if (value == null) return null;
            if (value.Keys.Count == 0) return null;
            var reference = new PersonReference((DAZSkinV2)value["skin"], (DAZHairGroup)value["hair"]);
            object skinStrategyName;
            if (value.TryGetValue("skin_strategy", out skinStrategyName))
            {
                var strategy = new SkinStrategyFactory().Create((string)skinStrategyName);
                reference.skinStrategy = strategy.GetMirrorStrategy(value["skin_data"]);
            }
            object hairStrategyName;
            if (value.TryGetValue("hair_strategy", out hairStrategyName))
            {
                var strategy = new HairStrategyFactory().Create((string)hairStrategyName);
                reference.hairStrategy = strategy.GetMirrorStrategy(value["hair_data"]);
            }
            return reference;
        }

        public Dictionary<string, object> ToBroadcastable()
        {
            var serialized = new Dictionary<string, object>();
            serialized["skin"] = skin;
            if (skinStrategy != null)
            {
                serialized["skin_strategy"] = skinStrategy.OwnerStrategyName;
                serialized["skin_data"] = skinStrategy.ToBroadcastable();
            }
            serialized["hair"] = hair;
            if (hairStrategy != null)
            {
                serialized["hair_strategy"] = hairStrategy.OwnerStrategyName;
                serialized["hair_data"] = hairStrategy.ToBroadcastable();
            }
            return serialized;
        }

        public static Dictionary<string, object> EmptyBroadcastable()
        {
            // NOTE: Unity does not allow broadcasting null
            return new Dictionary<string, object>();
        }

        public bool BeforeMirrorRender()
        {
            try
            {
                var ok = true;
                if (skinStrategy != null)
                    ok |= skinStrategy.BeforeMirrorRender();
                if (hairStrategy != null)
                    ok |= hairStrategy.BeforeMirrorRender();
                return ok;
            }
            catch (Exception e)
            {
                if (_failedOnce) return false;
                _failedOnce = true;
                SuperController.LogError("Failed to show PoV materials: " + e);
                return false;
            }
        }

        public void AfterMirrorRender()
        {
            try
            {
                if (skinStrategy != null)
                    skinStrategy.AfterMirrorRender();
                if (hairStrategy != null)
                    hairStrategy.AfterMirrorRender();
            }
            catch (Exception e)
            {
                if (_failedOnce) return;
                _failedOnce = true;
                SuperController.LogError("Failed to hide PoV materials: " + e);
            }
        }
    }
}