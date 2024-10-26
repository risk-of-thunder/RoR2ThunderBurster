#if R2TB_COLLECTIONS_INSTALLED
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace RoR2ThunderBurster
{
    internal static class CollectionsEmbedder
    {
        private const string BEPINEX_NAME = "bbepis-bepinexpack";
        private const string CONSTRAINT = "R2TB_BEPINEX_INSTALLED";

        [InitializeOnLoadMethod]
        private static void Check()
        {
            if (IsEmbedded())
            {
                EnsureConstrain();
                EnsureRemoveDependency();
                return;
            }

            if (!EditorUtility.DisplayDialog("Unity.Collections not Embedded", "Looks like com.unity.collections is in the project but not embedded into the project itself, this means that the Unity Package Manager's version of Mono Cecil is installed, which will cause conflicts with Bepinex, please hit \"Ok\" to embed the package and remove this dependency to ensure no issues with BepInEx", "Ok", "Cancel"))
            {
                return;
            }

            EmbedPackage();
        }

        private static void EmbedPackage()
        {
            EditorUtility.DisplayProgressBar("Embedding Collections", "", 0);
            try
            {
                var operation = Client.Embed("com.unity.collections");
                while(!operation.IsCompleted)
                {
                    EditorUtility.DisplayProgressBar("Embedding Collections", "Waiting for Embedding procedure to complete", 0.5f);
                }

                EditorUtility.DisplayProgressBar("Embedding Collections", "Completed procedure", 1);
            }
            catch(Exception e)
            {
                Debug.LogError($"Exception during Unity.Collections embedding: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static bool IsEmbedded()
        {
            string collectionsPackageInPackagesPath = Path.Combine(Environment.CurrentDirectory, "Packages", "com.unity.collections");
            if(!Directory.Exists(collectionsPackageInPackagesPath))
            {
                //Has not been embedded, prompt embedding.
                return false;
            }
            return true;
        }

        private static void EnsureConstrain()
        {
            string collectionsPackageInPackagesPath = Path.Combine(Environment.CurrentDirectory, "Packages", "com.unity.collections");
            string codeGenFolder = Path.Combine(collectionsPackageInPackagesPath, "Unity.Collections.CodeGen");
            var filePathToAssemblyDefinition = Path.Combine(codeGenFolder, "Unity.Collections.CodeGen.asmdef");
            var deserializedAssemblyDefinition = DeserializedAssemblyDefinition.FromJSON(File.ReadAllText(filePathToAssemblyDefinition));

            bool anyChangesMade = false;

            bool bepinexDefineExists = false;
            foreach(var versionDefine in deserializedAssemblyDefinition.versionDefines)
            {
                if(versionDefine.name == BEPINEX_NAME)
                {
                    bepinexDefineExists = true;
                    break;
                }
            }
            if(!bepinexDefineExists)
            {
                WriteBepinexDefine(deserializedAssemblyDefinition);
                anyChangesMade = true;
            }

            bool constraintExists = false;
            foreach(var constraint in deserializedAssemblyDefinition.defineConstraints)
            {
                if(constraint == CONSTRAINT)
                {
                    constraintExists = true;
                    break;
                }
            }
            if(!constraintExists)
            {
                WriteConstraint(deserializedAssemblyDefinition);
                anyChangesMade = true;
            }

            if(anyChangesMade)
            {
                WriteAssemblyDefinition(deserializedAssemblyDefinition, filePathToAssemblyDefinition);
            }
        }

        private static void EnsureRemoveDependency()
        {
            string collectionsPackageInPackagesPath = Path.Combine(Environment.CurrentDirectory, "Packages", "com.unity.collections");
            string packageJSONPath = Path.Combine(collectionsPackageInPackagesPath, "package.json");

            string json = File.ReadAllText(packageJSONPath);
            //this hurts
            string modified = json.Replace("\"com.unity.nuget.mono-cecil\": \"1.11.4\",", "");
            File.WriteAllText(packageJSONPath, modified);
        }

        private static void WriteBepinexDefine(DeserializedAssemblyDefinition deserializedAssemblyDefinition)
        {
            DeserializedAssemblyDefinition.VersionDefine define = new DeserializedAssemblyDefinition.VersionDefine
            {
                name = BEPINEX_NAME,
                define = CONSTRAINT,
                expression = "5.4"
            };
            deserializedAssemblyDefinition.versionDefines ??= new DeserializedAssemblyDefinition.VersionDefine[0];
            ArrayUtility.Add(ref deserializedAssemblyDefinition.versionDefines, define);
        }

        private static void WriteConstraint(DeserializedAssemblyDefinition deserializedAssemblyDefinition)
        {
            deserializedAssemblyDefinition.defineConstraints ??= new string[0];
            ArrayUtility.Add(ref deserializedAssemblyDefinition.defineConstraints, CONSTRAINT);
        }

        private static void WriteAssemblyDefinition(DeserializedAssemblyDefinition deserializedAssemblyDefinition, string filePath)
        {
            File.WriteAllText(filePath, DeserializedAssemblyDefinition.ToJSON(deserializedAssemblyDefinition));
        }
    }
}
#endif