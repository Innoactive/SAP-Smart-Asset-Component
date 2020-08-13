
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
        public string name;

        [SerializeField]
        public string id;

        [SerializeField]
        public List<SmartAssetInstance> smartAssetInstances;

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
            foreach(SmartAssetInstance instance in smartAssetInstances)
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
        /// <param name="id"></param>
        public void RemoveInstance(string id)
        {
            smartAssetInstances.Remove(GetInstance(id));
        }
    }

}