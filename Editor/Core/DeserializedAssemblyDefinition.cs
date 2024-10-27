#if UNITY_EDITOR
using System;
using UnityEditorInternal;
using UnityEngine;

namespace RoR2ThunderBurster
{
    /// <summary>
    /// Represents a Deserialized version of an AssemblyDefinition, taken partly from the AsmDef in TK's Stage Assemblies
    /// </summary>
    [Serializable]
    public struct DeserializedAssemblyDefinition
    {
        /// <summary>
        /// The name of the assembly itself
        /// </summary>
        public string name;

        /// <summary>
        /// Wether the assembly allows for unsafe code
        /// </summary>
        public bool allowUnsafeCode;

        /// <summary>
        /// Wether the assembly is auto-referenced
        /// </summary>
        public bool autoReferenced;

        /// <summary>
        /// Wether the assembly has no engine references
        /// </summary>
        public bool noEngineReferences;

        /// <summary>
        /// Wether precompiled references can be overriden
        /// </summary>
        public bool overrideReferences;

        /// <summary>
        /// The root namespace of the assembly
        /// </summary>
        public string rootNamespace;

        /// <summary>
        /// The Define constraints needed to exist for this assembly to load
        /// </summary>
        public string[] defineConstraints;

        /// <summary>
        /// The AssemblyDef references that this AssemblyDef has
        /// </summary>
        public string[] references;

        /// <summary>
        /// The precompiled references for this assembly
        /// </summary>
        public string[] precompiledReferences;

        /// <summary>
        /// The optional unity references for this assembly
        /// </summary>
        public string[] optionalUnityReferences;

        /// <summary>
        /// On which platforms can this assembly be accessed
        /// </summary>
        public string[] includePlatforms;

        /// <summary>
        /// On which platforms is this assembly ignored
        /// </summary>
        public string[] excludePlatforms;

        /// <summary>
        /// An array of Defines that get added to the assembly if a specific version matches
        /// </summary>
        public VersionDefine[] versionDefines;

        /// <summary>
        /// Deserializes an AssemblyDefinitionAsset
        /// </summary>
        /// <param name="asset">The asset to deserialized</param>
        /// <returns>The deserialized assembly definition</returns>
        public static DeserializedAssemblyDefinition FromJSON(AssemblyDefinitionAsset asset)
        {
            return FromJSON(asset.ToString());
        }

        /// <summary>
        /// Deserializes a JSON string into a DeserializedAssemblyDefinition
        /// </summary>
        /// <param name="json">The JSON string</param>
        /// <returns>The Deserialized Assembly Definition</returns>
        public static DeserializedAssemblyDefinition FromJSON(string json)
        {
            return JsonUtility.FromJson<DeserializedAssemblyDefinition>(json);
        }

        /// <summary>
        /// Creates a JSON representation of <paramref name="deserializedAssemblyDefinition"/>
        /// </summary>
        /// <param name="deserializedAssemblyDefinition">The assembly definition to turn into JSON</param>
        /// <returns>The JSON string</returns>
        public static string ToJSON(DeserializedAssemblyDefinition deserializedAssemblyDefinition)
        {
            return JsonUtility.ToJson(deserializedAssemblyDefinition, true);
        }

        /// <summary>
        /// Represents a Compile Define that only gets included into the assembly if a specific package with specific version ranges is installed
        /// </summary>
        [Serializable]
        public struct VersionDefine
        {
            /// <summary>
            /// The name of the package needed to be installed
            /// </summary>
            public string name;
            /// <summary>
            /// The version expression for the package, see https://docs.unity3d.com/2021.3/Documentation/Manual/class-AssemblyDefinitionImporter.html#version-defines for more information
            /// </summary>
            public string expression;
            /// <summary>
            /// The define that gets added if the <see cref="expression"/> returns true
            /// </summary>
            public string define;
        }
    }
}
#endif