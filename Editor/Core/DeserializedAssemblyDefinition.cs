#if UNITY_EDITOR
using System;
using UnityEditorInternal;
using UnityEngine;

namespace RoR2ThunderBurster
{
    [Serializable]
    public struct DeserializedAssemblyDefinition
    {
        public string name;
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public bool autoReferenced;
        public string[] optionalUnityReferences;
        public string[] includePlatforms;
        public string[] excludePlatforms;
        public string[] precompiledReferences;
        public string[] defineConstraints;
        public VersionDefine[] versionDefines;

        public static DeserializedAssemblyDefinition FromJSON(AssemblyDefinitionAsset asset)
        {
            return FromJSON(asset.ToString());
        }
        public static DeserializedAssemblyDefinition FromJSON(string json)
        {
            return JsonUtility.FromJson<DeserializedAssemblyDefinition>(json);
        }
        public static string ToJSON(DeserializedAssemblyDefinition deserializedAssemblyDefinition)
        {
            return JsonUtility.ToJson(deserializedAssemblyDefinition, true);
        }

        [Serializable]
        public struct VersionDefine
        {
            public string name;
            public string expression;
            public string define;
        }
    }
}
#endif