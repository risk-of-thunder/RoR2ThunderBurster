#if UNITY_EDITOR && R2TB_COLLECTIONS_INSTALLED
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
    // Class that embeds com.unity.collections into the project, this is done to remove an explicit dependency on Mono.Cecil, to allow Bepinex to exist within the project as well
    internal static class CollectionsEmbedder
    {
        private const string BEPINEX_NAME = "bbepis-bepinexpack";
        private const string CONSTRAINT = "R2TB_BEPINEX_INSTALLED";

        [InitializeOnLoadMethod]
        private static void Check()
        {
            if (IsEmbedded())
            {
                EnsureConstraint();
                EnsureRemoveDependency();
                return;
            }

            if (!EditorUtility.DisplayDialog("Unity.Collections not Embedded", "Looks like com.unity.collections is in the project but not embedded into the project itself, this means that the Unity Package Manager's version of Mono Cecil is installed, which will cause conflicts with Bepinex, please hit \"Ok\" to embed the package and remove this dependency to ensure no issues with BepInEx", "Ok", "Cancel"))
            {
                return;
            }

            EmbedPackage();
            //Trying to write immediatly after embedding doesnt work, wait for the next opportunity
            EditorApplication.update += UnhookAndEnsure;


            void UnhookAndEnsure()
            {
                EditorApplication.update -= UnhookAndEnsure;
                EnsureConstraint();
                EnsureRemoveDependency();
            }
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
            //Directory.Exists is returning true despite the fact it doesnt exist? weird fucking thing tbh.
            //Just use a random asset from the collections package and check if the physical path lies in the package cache.
            string path = AssetDatabase.GUIDToAssetPath("bd605698a338c4de88bb5b3c4632af68");
            string physicalPath = FileUtil.GetPhysicalPath(path);
            return !physicalPath.Contains("Library/PackageCache");
        }

        private static void EnsureConstraint()
        {
            string collectionsPackageInPackagesPath = Path.Combine(Environment.CurrentDirectory, "Packages", "com.unity.collections");
            string codeGenFolder = Path.Combine(collectionsPackageInPackagesPath, "Unity.Collections.CodeGen");
            var filePathToAssemblyDefinition = Path.Combine(codeGenFolder, "Unity.Collections.CodeGen.asmdef");
            var deserializedAssemblyDefinition = DeserializedAssemblyDefinition.FromJSON(File.ReadAllText(filePathToAssemblyDefinition));
            //These arrays may be null, ensure theyre not to avoid exceptions
            deserializedAssemblyDefinition.versionDefines ??= new DeserializedAssemblyDefinition.VersionDefine[0];
            deserializedAssemblyDefinition.defineConstraints ??= new string[0];

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
                WriteBepinexDefine(ref deserializedAssemblyDefinition);
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
                WriteConstraint(ref deserializedAssemblyDefinition);
                anyChangesMade = true;
            }

            //only write changes if needed
            if(anyChangesMade)
            {
                WriteAssemblyDefinition(ref deserializedAssemblyDefinition, filePathToAssemblyDefinition);
            }
        }

        //Removes the explicit dependency on mono.cecil
        private static void EnsureRemoveDependency()
        {
            string collectionsPackageInPackagesPath = Path.Combine(Environment.CurrentDirectory, "Packages", "com.unity.collections");
            string packageJSONPath = Path.Combine(collectionsPackageInPackagesPath, "package.json");
            string json = File.ReadAllText(packageJSONPath);

            //No easy way to get an actual C# object from json since the dependency mappings cant be obtained from json utility, and we cant just depend on newtonsoft, so we'll just do it the dirty way.
            string modified = json.Replace("\"com.unity.nuget.mono-cecil\": \"1.11.4\",", "");
            File.WriteAllText(packageJSONPath, modified);
        }

        //Adds a version define for bepinex, this way we can constraint the assembly to load only if bepinex is installed.
        private static void WriteBepinexDefine(ref DeserializedAssemblyDefinition deserializedAssemblyDefinition)
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

        // Makes sure the assembly only loads if bepinex is installed
        private static void WriteConstraint(ref DeserializedAssemblyDefinition deserializedAssemblyDefinition)
        {
            deserializedAssemblyDefinition.defineConstraints ??= new string[0];
            ArrayUtility.Add(ref deserializedAssemblyDefinition.defineConstraints, CONSTRAINT);
        }

        private static void WriteAssemblyDefinition(ref DeserializedAssemblyDefinition deserializedAssemblyDefinition, string filePath)
        {
            File.WriteAllText(filePath, DeserializedAssemblyDefinition.ToJSON(deserializedAssemblyDefinition));
        }
    }
}
#endif