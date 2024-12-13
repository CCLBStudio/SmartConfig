using System;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    /// <summary>
    /// Abstract script holding the methods to override for custom upload/download logic. Create a script inheriting from this one, override the methods and link an instance of your custom script in the RemoteConfigService.
    /// </summary>
    public abstract class RemoteConfigTransferStrategy : ScriptableObject
    {
        /// <summary>
        /// The method handling the upload logic.
        /// </summary>
        /// <param name="json">The json string to upload.</param>
        /// <param name="onUploadProgressed">Event to call on upload update. The float parameter should be a value ranged between 0 and 1 representing the normalized progress.</param>
        /// <param name="onUploadSucceeded">Event to call when the upload succeeded.</param>
        /// <param name="onUploadFailed">Event to call when the upload failed.</param>
        public abstract void UploadJson(string json, Action<float> onUploadProgressed, Action onUploadSucceeded, Action onUploadFailed);
        
        /// <summary>
        /// The method handling the download logic.
        /// </summary>
        /// <param name="onDownloadProgressed">Event to call on download update. The float parameter should be a value ranged between 0 and 1 representing the normalized progress.</param>
        /// <param name="onDownloadSucceeded">Event to call when the download succeeded. The string parameter should be the downloaded json.</param>
        /// <param name="onDownloadFailed">Event to call when the download failed.</param>
        public abstract void DownloadJson(Action<float> onDownloadProgressed, Action<string> onDownloadSucceeded, Action onDownloadFailed);
    }
}