using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;

#if !R2TB_IMPORT_EXTENSIONS
namespace RoR2.ThunderBurster.ImportExtensions
{
    /// <summary>
    /// Static class thats used to ensure the import extensions are installed properly, otherwise the package wont work fully.
    /// </summary>
    internal static class PromptInstallationIfMainImportExtensionsMissing
    {
        private static UnityWebRequest _webRequest;
        private static UnityWebRequestAsyncOperation _asyncOp;

        [InitializeOnLoadMethod]
        public static void Main()
        {
            if(!EditorUtility.DisplayDialog("Main Import Extensions Missing", "It seems like the main RoR2 Import Extensions package is missing from your project, this is required for RoR2ThunderBurster to function properly.", "Ok, install them", "Cancel"))
            {
                return;
            }

            string url = $"https://api.github.com/repos/risk-of-thunder/RoR2ImportExtensions/releases/latest";
            _webRequest = UnityWebRequest.Get(url);
            _asyncOp = _webRequest.SendWebRequest();
            _asyncOp.completed += AsyncOp_completed;
        }

        private static void AsyncOp_completed(AsyncOperation _)
        {
            var release = JsonUtility.FromJson<GitHubRelease>(_webRequest.downloadHandler.text);
            var request = Client.Add("https://github.com/risk-of-thunder/RoR2ImportExtensions.git" + "#" + release.tag_name);
            while (!request.IsCompleted)
            {
                Debug.Log("Waiting for package installation.");
            }

            _webRequest.Dispose();
            _webRequest = null;
            _asyncOp = null;
        }

        [Serializable]
        private class GitHubRelease
        {
            public string tag_name;
        }
    }
}
#endif