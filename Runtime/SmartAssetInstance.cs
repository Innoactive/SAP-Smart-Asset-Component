using System;
using UnityEngine;

namespace SAP.Creator.SmartAsset
{
    [Serializable]
    public class SmartAssetInstance : MonoBehaviour
    {

        public class UpdateEventArgs : System.EventArgs
        {
            public readonly string masterData;
            public UpdateEventArgs(string masterData)
            {
                this.masterData = masterData;
            }
        }

        [SerializeField]
        public string Id = "";

        [NonSerialized]
        public string Title = "";

        [NonSerialized]
        public string Image = "";

        private string masterData;

        public event EventHandler<UpdateEventArgs> Updated;
        public void UpdateMasterData(string masterData) {
            this.masterData = masterData;
            Updated.Invoke(this, new UpdateEventArgs(masterData));
        }

        public string GetMasterData()
        {
            return this.masterData;
        }
    }
}
