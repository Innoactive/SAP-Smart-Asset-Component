using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using static SAP.Creator.SmartAsset.SmartAssetConnector;

namespace SAP.Creator.SmartAsset.Editor {
    /// <summary>
    /// Allows configurating the credentials for integrating the contents from
    /// XR Cloud. 
    /// </summary>
    public class ConfigureTraining : EditorWindow {

        private string accessTokenUrl = "";
        private string grantType = "";
        private string clientId = "";
        private string clientSecret = "";
        private string url = "";
        private string accessToken = "";

        private bool connectionTested = false;

        private int sceneTypeIndex = 0;
        private string[] sceneTypeOptions;
        private SceneType[] sceneTypes;
        private string sceneType;

        private static string basePath = "/SAP/Creator/SmartAsset/Resources";

        private SmartAssetConnector configuration;

        private AssetDatabase.ImportPackageCallback action;
        [MenuItem("SAP/Creator/Configure Training")]
        private static void ConfigureTrainingWindow()
        {
            ConfigureTraining window = (ConfigureTraining)GetWindow(typeof(ConfigureTraining));

            SmartAssetConnector configuration = FindObjectOfType<SmartAssetConnector>();

            if(configuration != null)
            {
                window.accessTokenUrl = configuration.AccessTokenUrl;
                window.grantType = configuration.GrantType;
                window.clientId = configuration.ClientId;
                window.clientSecret = configuration.ClientSecret;
                window.url = configuration.Url;
            }

            window.titleContent = new GUIContent("SAP Configuration");
            window.Show();
        }

        private void OnGUI()
        {
            accessTokenUrl = EditorGUILayout.TextField("Access Token Url", accessTokenUrl);
            grantType = EditorGUILayout.TextField("Grant Type", grantType);
            clientId = EditorGUILayout.TextField("Client ID", clientId);
            clientSecret = EditorGUILayout.TextField("Client Secret", clientSecret);
            url = EditorGUILayout.TextField("API Url", url);

            if (GUILayout.Button("Apply Configuration"))
            {
                configuration = FindObjectOfType<SmartAssetConnector>();

                if (configuration == null)
                {
                    GameObject configurationGameObject = new GameObject();
                    configurationGameObject.name = "[SAP_CONFIGURATION]";
                    configuration = configurationGameObject.AddComponent<SmartAssetConnector>();
                }

                configuration.AccessTokenUrl = accessTokenUrl;
                configuration.GrantType = grantType;
                configuration.ClientId = clientId;
                configuration.ClientSecret = clientSecret;
                configuration.Url = url;

                configuration.AccessToken = "";

                configuration.InitializeAccessToken(() =>
                {
                    accessToken = configuration.AccessToken;
                    configuration.AccessToken = "";

                    UpdateSceneTypes();
                    connectionTested = true;
                });
            }

            if(connectionTested)
            {

                EditorGUI.BeginChangeCheck();
                sceneTypeIndex = EditorGUILayout.Popup("Scene Type", sceneTypeIndex, sceneTypeOptions);

                if (EditorGUI.EndChangeCheck())
                {
                    Debug.Log(sceneTypeOptions[sceneTypeIndex]);
                }

                configuration.UpdateAccessToken();

                if (GUILayout.Button("Import Smart Assets into Project"))
                {
                    Debug.Log("Add GameObjects into Scene");
                    ImportSceneType(sceneTypes[sceneTypeIndex]);
                }
            }
        }

        /// <summary>
        /// Downloads json-response from the Smart Asset Cloud oData-Service.
        /// </summary>
        private void DownloadJson(string url, string accessToken, Action<string> onSuccess)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
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
        /// Updates the list of existing Scenetypes.
        /// </summary>
        private void UpdateSceneTypes()
        {
            DownloadJson(configuration.Url + "/vr-client/SceneType", accessToken, (json) =>
            {
                sceneTypes = SceneTypeList.CreateFromJSON(json).value;
                List<string> sceneTypeList = new List<string>();
                foreach (SceneType sceneType in sceneTypes)
                {
                    sceneTypeList.Add(sceneType.Name);
                }
                sceneTypeOptions = sceneTypeList.ToArray();
            });
        }

        /// <summary>
        /// Triggers the import process for the selected SceneType.
        /// </summary>
        private void ImportSceneType(SceneType sceneType)
        {
            SceneTypeReference sceneTypeReference = null;

            for (int i = 0; i < configuration.sceneTypes.Count; i++)
            {
                if (configuration.sceneTypes[i].id == sceneType.Id)
                {
                    sceneTypeReference = configuration.sceneTypes[i];
                    break;
                }
            }

            if (sceneTypeReference == null)
            {
                sceneTypeReference = new SceneTypeReference();
                sceneTypeReference.id = sceneType.Id;
                sceneTypeReference.name = sceneType.Name;
                sceneTypeReference.smartAssetInstances = new List<SmartAssetInstance>();
                configuration.sceneTypes.Add(sceneTypeReference);
            }

            DownloadJson(configuration.Url + "/vr-client/SceneType(" + sceneType.Id + ")?$expand=SmartAssetVersionUsages($expand=SmartAssetVersion($expand=Binaries))", accessToken, (json) =>
            {
                Debug.Log("Update SceneTypeInstances");
                SmartAssetVersionUsage[] smartAssetVersionUsages = SceneType.CreateFromJSON(json).SmartAssetVersionUsages;
                ImportSmartAssetUsage(sceneTypeReference, smartAssetVersionUsages, 0);
            });
        }

        /// <summary>
        /// Imports all the needed Smart Assets into the scene. Since 
        /// Smart Assets for design time are just simple unity packages,
        /// the import process needs to be step by step. That's why the index 
        /// variable is needed for
        /// </summary>
        /// <param name="sceneTypeReference"></param>
        /// <param name="smartAssetVersionUsages"></param>
        /// <param name="index"></param>
        private void ImportSmartAssetUsage(SceneTypeReference sceneTypeReference, SmartAssetVersionUsage[] smartAssetVersionUsages, int index)
        {
            if (index == smartAssetVersionUsages.Length)
            {
                Debug.Log("Import Finished");
                return;
            }
            SmartAssetVersionUsage smartAssetUsage = smartAssetVersionUsages[index];
            if (sceneTypeReference.HasInstance(smartAssetUsage.Id))
            {
                Debug.Log("Instance already existing, so skipping");
                ImportSmartAssetUsage(sceneTypeReference, smartAssetVersionUsages, index + 1);
                return;
            }

            Debug.LogFormat("Download {0}", smartAssetUsage.Id);
            DownloadPackage(smartAssetUsage, (packageName) =>
            {
                Debug.LogFormat("Import {0}", packageName);
                ImportPackage(packageName, () =>
                {
                    Debug.LogFormat("Instantiate {0}", packageName);
                    InstantiatePrefab(packageName, (gameObject) =>
                    {
                        Debug.Log("Instantiate GameObject successfull");

                        gameObject.name = smartAssetUsage.InstanceName;
                        SmartAssetInstance smartAssetInstance = gameObject.GetComponent<SmartAssetInstance>();
                        if (smartAssetInstance == null)
                        {
                            smartAssetInstance = gameObject.AddComponent<SmartAssetInstance>();
                        }
                        smartAssetInstance.Id = smartAssetUsage.Id;
                        smartAssetInstance.name = smartAssetUsage.InstanceName;
                        sceneTypeReference.smartAssetInstances.Add(smartAssetInstance);

                        ImportSmartAssetUsage(sceneTypeReference, smartAssetVersionUsages, index + 1);
                    });
                });


            });
        }

        /// <summary>
        /// Downloads the referenced package in the "SmartAssetVersionUsage"-Object.
        /// </summary>
        private void DownloadPackage(SmartAssetVersionUsage smartAssetUsage, Action<string> onSuccess)
        {

            string packageName = smartAssetUsage.SmartAssetVersion.Binaries[0].Binary_Id + ".unitypackage";
            string packagePath = Application.dataPath+basePath + "/Packages/" + packageName;

            Debug.LogFormat("{0}", packagePath);

            if (smartAssetUsage.SmartAssetVersion.Binaries[0].Binary_Id != "" && !File.Exists(packagePath))
            {
                string assetDownloadurl = $"{configuration.Url}/v2/smart-assets/SmartAssetVersionBinary(guid'{smartAssetUsage.SmartAssetVersion.Binaries[0].Binary_Id}')/Data";
                Debug.LogFormat("Download from {0}", assetDownloadurl);

                WebClient client = new WebClient();
                client.Headers.Set("Authorization", "Bearer " + accessToken);
                client.DownloadFile(new Uri(assetDownloadurl), packagePath);
                
            }
            onSuccess(packageName);
        }


        /// <summary>
        /// Imports a specified package.
        /// </summary>
        private void ImportPackage(string packageName, Action onSuccess)
        {
            string packagePath = Application.dataPath + basePath + "/Packages/" + packageName;

            bool isImported = true;
            string[] prefabNames = GetPrefabNames(packageName);
            foreach(string prefabName in prefabNames)
            {
                if (!File.Exists(Application.dataPath + basePath + "/Prefabs/" + prefabName + ".prefab"))
                {
                    isImported = false;
                }
            }
            if(isImported)
            {
                onSuccess();
                return;
            }

            
            // Strange workout for some synchronizing issues.
            action = (packageImported) =>
            {
                AssetDatabase.importPackageCompleted -= action;
                onSuccess();
            };
            AssetDatabase.importPackageCompleted += action;
            AssetDatabase.ImportPackage(packagePath, true);
            //AssetDatabase.Refresh();

        }

        /// <summary>
        /// Instantiates the prefabs within a unitypackage.
        /// </summary>
        private void InstantiatePrefab(string packageName, Action<GameObject> onSuccess)
        {
            String[] prefabNames = GetPrefabNames(packageName);
            if (prefabNames.Length == 0)
            {
                Debug.Log("No prefabs found in package. Abort import.");
                return;
            }

            if (prefabNames.Length > 1)
            {
                Debug.Log("More than 1 prefab per Package is currently not supported, so skipping.");
                return;
            }

            foreach (String prefabName in prefabNames)
            {
                Debug.LogFormat("Instantiate GameObject {0}", prefabName);
                GameObject gameObject = PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/" + prefabName)) as GameObject;
                onSuccess(gameObject);
                break;
            }
        }

        /// <summary>
        /// Searches a Unitypackage whether it contains prefabs.
        /// </summary>
        private string[] GetPrefabNames(string packageFileName)
        {
            List<string> prefabNames = new List<string>();
            string filePath = Application.dataPath + basePath + "/Packages/" + packageFileName;

            Stream stream = File.OpenRead(filePath);
            GZipInputStream gzipStream = new GZipInputStream(stream);

            TarInputStream tarStream = new TarInputStream(gzipStream, Encoding.UTF8);
            TarEntry tarEntry;

            while((tarEntry = tarStream.GetNextEntry()) != null)
            {

                if (!tarEntry.Name.EndsWith("pathname"))
                {
                    continue;
                }

                StreamReader reader = new StreamReader(tarStream);
                String path = reader.ReadLine();

                if (!path.EndsWith(".prefab"))
                {
                    continue;
                }

                String[] pathParts = path.Split('/');
                String[] nameParts = pathParts[pathParts.Length - 1].Split('.');
                prefabNames.Add(nameParts[0]);
            }

            return prefabNames.ToArray();
        }
    }
}
