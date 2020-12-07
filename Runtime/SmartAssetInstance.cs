using System;
using UnityEngine;

namespace SAP.Creator.SmartAsset
{
    [Serializable]
    public class SmartAssetInstance : MonoBehaviour
    {

        public class UpdateEventArgs : System.EventArgs
        {
            public readonly string MasterData;
            public UpdateEventArgs(string masterData)
            {
                this.MasterData = masterData;
            }
        }

        public event EventHandler<UpdateEventArgs> Updated;

        [SerializeField]
        public string Id = "";

        [SerializeField]
        public string Name = "";

        [SerializeField]
        public string Version = "";

        [NonSerialized]
        public string Title = "";

        [NonSerialized]
        public string Image = "";

        private string masterData;

        public void UpdateMasterData(string masterData)
        {
            this.masterData = masterData;
            if (Updated != null)
            {
                Updated.Invoke(this, new UpdateEventArgs(masterData));

            }
        }

        public string GetMasterData()
        {
            return this.masterData;
        }
    }
}
