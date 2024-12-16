using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

namespace CCLBStudio.SmartConfig.Demo
{
    [CreateAssetMenu(fileName = "GoogleDriveTransferStrategy", menuName = "CCLB Studio/Remote Config/Transfer Strategies/Google Drive Strategy")]
    public class GoogleDriveTransferStrategy : SmartConfigTransferStrategy
    {
        [SerializeField] private string fileId;
        private const string BaseUrl = "https://drive.google.com/uc?export=download&id=";
        [NonSerialized] private GameObject _coroutineRunner;
    
        public override void UploadJson(string json, Action<float> onUploadProgressed, Action onUploadSucceeded, Action onUploadFailed)
        {
            Debug.LogError("Upload not implemented !");
            onUploadFailed?.Invoke();
        }

        public override void DownloadJson(Action<float> onDownloadProgressed, Action<string> onDownloadSucceeded, Action onDownloadFailed)
        {
            if (Application.isPlaying)
            {
                if (!_coroutineRunner)
                {
                    _coroutineRunner = new GameObject("CoroutineRunner");
                }
                _coroutineRunner.AddComponent<CoroutineRunner>().StartCoroutine(DownloadRoutine(onDownloadProgressed, 
                    json =>
                    {
                        Destroy(_coroutineRunner);
                        onDownloadSucceeded?.Invoke(json);
                    }, () =>
                    {
                        Destroy(_coroutineRunner);
                        onDownloadFailed?.Invoke();
                    }));
                return;
            }
        
#if  UNITY_EDITOR
            EditorCoroutineUtility.StartCoroutineOwnerless(DownloadRoutine(onDownloadProgressed, onDownloadSucceeded, onDownloadFailed));
#endif
        }

        private IEnumerator DownloadRoutine(Action<float> onDownloadProgressed, Action<string> onDownloadSucceeded, Action onDownloadFailed)
        {
            string url = BaseUrl + fileId;
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SendWebRequest();
                while (!webRequest.isDone)
                {
                    Debug.Log($"Download progress : " + webRequest.downloadProgress);
                    onDownloadProgressed?.Invoke(webRequest.downloadProgress);
                    yield return null;
                }

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Google Drive file downloaded succeeded !");
                    string downloadedJson = webRequest.downloadHandler.text;
                    onDownloadSucceeded?.Invoke(downloadedJson);
                }
                else
                {
                    Debug.LogError("Problem during download : " + webRequest.error);
                    onDownloadFailed?.Invoke();
                }
            }
        }
        
        private class CoroutineRunner : MonoBehaviour { }
    }
}