using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SAP.Creator.SmartAsset
{
    /// <summary>
    /// Contains the credentials to connect to the Smart Asset Cloud
    /// and all it's references.
    /// </summary>
    public class SmartAssetConnector : MonoBehaviour
    {

        [Serializable]
        public class SceneTypeList
        {
            public SceneType[] value;

            public static SceneTypeList CreateFromJSON(string jsonString)
            {
                return JsonUtility.FromJson<SceneTypeList>(jsonString);
            }
        }

        [Serializable]
        public class SceneType
        {
            public string Id;
            public string Name;

            public SmartAssetVersionUsage[] SmartAssetVersionUsages;

            public static SceneType CreateFromJSON(string jsonString)
            {
                return JsonUtility.FromJson<SceneType>(jsonString);
            }
        }

        [Serializable]
        public class SmartAssetVersionUsage
        {
            public string Id;
            public string InstanceName;
            public string SapId;
            public string Image_Id;
            public SmartAssetVersion SmartAssetVersion;

        }

        [Serializable]
        public class SmartAssetVersion
        {
            public string createdAt;
            public string Version;
            public SmartAssetVersionBinary[] Binaries;
        }

        [Serializable]
        public class SmartAssetVersionBinary
        {
            public string Id;
            public string BuildTarget;
            public string Binary_Id;
        }

        [Serializable]
        public struct TokenResponse
        {
            public string access_token;
            public string jti;
        }

        public string AccessTokenUrl = "";

        public string GrantType = "";

        public string ClientId = "";

        public string ClientSecret = "";

        public string AccessToken = "";

        public string Url = "";

        public List<SceneTypeReference> sceneTypes = new List<SceneTypeReference>();

        private void Start()
        {
            InitializeAccessToken(UpdateScene);
        }

        /// <summary>
        /// Iterares through all the references and triggers update of
        /// Smart Asset Instances
        /// objects.
        /// </summary>
        private void UpdateScene()
        {
            foreach (SceneTypeReference sceneType in sceneTypes)
            {
                DownloadJson(Url + "/vr-client/SceneType(" + sceneType.id + ")?$expand=SmartAssetVersionUsages($expand=SmartAssetVersion)", (json) =>
                {
                    SmartAssetVersionUsage[] smartAssetVersionUsages = SceneType.CreateFromJSON(json).SmartAssetVersionUsages;
                    UpdateSmartAssetInstance(sceneType.smartAssetInstances, smartAssetVersionUsages);
                });
            }
        }

        /// <summary>
        /// Updates the contents of Smart Asset Instances
        /// </summary>
        private void UpdateSmartAssetInstance(List<SmartAssetInstance> smartAssetInstances, SmartAssetVersionUsage[] smartAssetVersionUsages)
        {
            foreach (SmartAssetVersionUsage smartAssetVersionUsage in smartAssetVersionUsages)
            {
                foreach (SmartAssetInstance smartAssetInstance in smartAssetInstances)
                {
                    if (smartAssetInstance == null || smartAssetVersionUsage.Id != smartAssetInstance.Id)
                    {
                        continue;
                    }

                    if (smartAssetVersionUsage.Image_Id != null && smartAssetVersionUsage.Image_Id != "")
                    {
                        SpriteRenderer renderer = smartAssetInstance.gameObject.GetComponentInChildren<SpriteRenderer>();

                        DownloadImage(Url + "/v2/smart-assets/SmartAssetVersionImage(guid'" + smartAssetVersionUsage.Image_Id + "')/Data", (sprite) =>
                        {
                            renderer.sprite = sprite;
                        });
                        new WaitForSeconds(1);
                    }

                    if (smartAssetVersionUsage.SapId != "")
                    {
                        smartAssetInstance.UpdateMasterData(smartAssetVersionUsage.SapId);
                    }
                    break;

                }
            }
        }

        /// <summary>
        /// Updates the access token of the current configuration.
        /// </summary>
        public void UpdateAccessToken()
        {
            InitializeAccessToken(() => { });
        }

        /// <summary>
        /// Initializes an access token.
        /// </summary>
        public void InitializeAccessToken(Action onSuccess)
        {
            if(AccessToken != "")
            {
                onSuccess();
                return;
            }
            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData["grant_type"] = GrantType;
            formData["client_id"] = ClientId;
            formData["client_secret"] = ClientSecret;
            UnityWebRequest www = UnityWebRequest.Post(AccessTokenUrl, formData);

            www.SendWebRequest().completed += (AsyncOperation) =>
            {
                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.Log(www.downloadHandler.text);
                    Debug.Log(www.error);
                }
                else
                {
                    AccessToken = JsonUtility.FromJson<TokenResponse>(www.downloadHandler.text).access_token;
                    onSuccess();
                }
            };


        }

        /// <summary>
        /// Downloads json-response from the Smart Asset Cloud oData-Service.
        /// </summary>
        private void DownloadJson(string url, Action<string> onSuccess)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            Debug.Log(url);
            request.SetRequestHeader("Authorization", "Bearer " + AccessToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SendWebRequest().completed += (AsyncOperation) =>
            {
                if (!(request.isHttpError || request.isNetworkError))
                {
                    onSuccess(request.downloadHandler.text);
                }
                else
                {
                    Debug.Log(request.error);
                }
            };
        }

        /// <summary>
        /// Downloads sprites and images
        /// </summary>
        private void DownloadImage(string url, Action<Sprite> onSuccess)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);

            request.SetRequestHeader("Authorization", "Bearer " + AccessToken);
            //request.SetRequestHeader("Content-Type", "application/json");
            DownloadHandlerTexture downloadHandler = new DownloadHandlerTexture(true);
            request.downloadHandler = downloadHandler;

            request.SendWebRequest().completed += (AsyncOperation) =>
            {
                if (!(request.isHttpError || request.isNetworkError))
                {
                    Sprite sprite = Sprite.Create(downloadHandler.texture, new Rect(0, 0, downloadHandler.texture.width, downloadHandler.texture.height), new Vector2(0.5f, 0.5f), 100f);
                    onSuccess(sprite);
                }
                else
                {
                    Debug.Log(request.error);
                }
            };
        }
    }
}