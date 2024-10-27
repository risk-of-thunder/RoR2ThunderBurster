//These defines basically ensures this dependency installer enables if any of our dependencies are missing.
#if UNITY_EDITOR && (!R2TB_THUNDERKIT_INSTALLED || !R2TB_BURST_INSTALLED || !R2TB_COLLECTIONS_INSTALLED || !R2TB_MATHEMATICS_INSTALLED)
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using System.Collections;
using UnityEngine.Networking;
using static UnityEditor.PlayerSettings;

namespace RoR2ThunderBurster
{
    /// <summary>
    /// Static class that handles installation of multiple dependencies for the package.
    /// </summary>
    internal static class DependencyInstaller
    {
        private const string TK_API_URL = "https://api.github.com/repos/PassivePicasso/ThunderKit/releases/latest";
        private const string TK_GIT_URL = "https://github.com/PassivePicasso/ThunderKit.git";
        private const string BURST_DEPENDENCY_ID = "com.unity.burst";
        private const string COLLECTIONS_DEPENDENCY_ID = "com.unity.collections";
        private const string MATHEMATICS_DEPENDENCY_ID = "com.unity.mathematics";

        private static IEnumerator coroutine = null;
        [InitializeOnLoadMethod]
        private static void Check()
        {
            List<(string dependencyID, string packageName, string version)> toInstall = new List<(string dependencyID, string packageName, string version)>();

            //These defines are on the assemblydef themselves, they enable ONLY if a specific unity package is in the project, which is how we make sure we only install the needed packages.
#if !R2TB_THUNDERKIT_INSTALLED
            toInstall.Add((TK_API_URL + ";" + TK_GIT_URL, "ThunderKit", "Latest"));
#endif

#if !R2TB_BURST_INSTALLED
            toInstall.Add((BURST_DEPENDENCY_ID, "Burst", "1.8.18"));
#endif

#if !R2TB_COLLECTIONS_INSTALLED
            toInstall.Add((COLLECTIONS_DEPENDENCY_ID, "Collections", "1.5.1"));
#endif

#if !R2TB_MATHEMATICS_INSTALLED
            toInstall.Add((MATHEMATICS_DEPENDENCY_ID, "Mathematics", "1.2.6"));
#endif

            if(toInstall.Count > 0)
            {
                string msgConcat = string.Join('\n', toInstall.Select(x => $"{x.packageName} version {x.version}"));
                if(!EditorUtility.DisplayDialog("Missing Dependencies", "It seems like your project is missing key dependencies for RoR2ThunderBurster to work properly, clicking \"Install All\" will install the following dependencies required for the package to work properly.\r\n" + msgConcat, "Install All", "Cancel"))
                {
                    return;
                }

                coroutine = InstallPackages(toInstall);
                EditorApplication.update += RunCoroutine;
            }
        }

        //Installation in an async fashion using a coroutine
        private static void RunCoroutine()
        {
            if(!(coroutine?.MoveNext() ?? false))
            {
                EditorApplication.update -= RunCoroutine;
                coroutine = null;
            }
        }

        private static IEnumerator InstallPackages(List<(string, string, string)> values)
        {
            int id = Progress.Start("Installing Packages");
            List<string> addValues = new List<string>();
            for(int i = 0; i < values.Count; i++)
            {
                (string dep, string pckName, string version) = values[i];
                if (pckName == "ThunderKit")
                {
                    //Since thunderkit is not part of the UPM, we need to use the github api to obtain the latest release, ergo this special handling
                    var subroutine = HandleThunderkit(dep, addValues, id);
                    while (subroutine.MoveNext())
                    {
                        Progress.Report(id, remap(i, 0, values.Count - 1, 0, 0.5f), "Awaiting for GIT-API response for installing ThunderKit");
                        yield return null;
                    }
                    continue;
                }
                Progress.Report(id, remap(i, 0, values.Count - 1, 0, 0.5f), "Adding " + pckName + " Version " + version);
                yield return null;
                addValues.Add($"{dep}@{version}");
            }

            //Client.AddAndRemove doesnt work for whatever reason
            for(int i = 0; i < addValues.Count; i++)
            {
                var packageID = addValues[i];
                var op = Client.Add(packageID);
                while(!op.IsCompleted)
                {
                    Progress.Report(id, remap(i, 0, addValues.Count - 1, 0.5f, 1f), "Installing " + packageID);
                }
            }
            Progress.Finish(id);
        }

        private static IEnumerator HandleThunderkit(string apiAndGitURL, List<string> output, int progressID)
        {
            string[] split = apiAndGitURL.Split(';');
            var api = split[0];
            var gitURL = split[1];

            var webRequest = UnityWebRequest.Get(api);
            var asyncOp = webRequest.SendWebRequest();
            while (!asyncOp.isDone)
            {
                yield return null;
            }

            var release = JsonUtility.FromJson<GitHubRelease>(webRequest.downloadHandler.text);
            output.Add($"{gitURL}#{release.tag_name}");
            yield break;
        }

        private static float remap(float val, float inMin, float inMax, float outMin, float outMax)
        {
            return outMin + (val - inMin) / (inMax - inMin) * (outMax - outMin);
        }


        [Serializable]
        private class GitHubRelease
        {
            public string tag_name;
        }
    }
}
#endif