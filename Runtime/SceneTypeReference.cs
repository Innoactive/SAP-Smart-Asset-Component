using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAP.Creator.SmartAsset
{
    /// <summary>
    /// The reference to a specific Scene-Type.
    /// </summary>
    [Serializable]
    public class SceneTypeReference
    {
        [SerializeField]
        public string Name;

        [SerializeField]
        public string Id;

        [SerializeField]
        public List<SmartAssetInstance> SmartAssetInstances;

        /// <summary>
        /// Check wether a Smart Asset Instance exists in this context.
        /// </summary>
        public bool HasInstance(string id)
        {
            return GetInstance(id) != null;
        }

        /// <summary>
        /// Return the instance with a specific id.
        /// </summary>
        public SmartAssetInstance GetInstance(string id)
        {
            foreach(SmartAssetInstance instance in SmartAssetInstances)
            {
                if (instance != null && instance.Id == id)
                {
                    return instance;
                }
            }

            return null;
        }

        /// <summary>
        /// Remove a Smart Asset Instance from this scene.
        /// </summary>
        public void RemoveInstance(string id)
        {
            SmartAssetInstances.Remove(GetInstance(id));
        }
    }
}