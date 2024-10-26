#if UNITY_EDITOR && ENABLE_BURST_AOT && R2TB_BURST_INSTALLED && R2TB_THUNDERKIT_INSTALLED
using RoR2ThunderBurster.TK;
using RoR2ThunderBurster.TK.Datums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using ThunderKit.Core.Pipelines.Jobs;
using Unity.Burst;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;
using Assembly = System.Reflection.Assembly;

namespace RoR2ThunderBurster.BurstImpl
{
    internal static class Common
    {
        internal static BindingFlags all = (BindingFlags)~0;
    }
    public enum BurstTargetPlatform
    {
        Windows = 0,
        macOS = 1,
        Linux = 2,
        Android = 3,
        iOS = 4,
        PS4 = 5,
        XboxOne_Deprecated = 6,
        WASM = 7,
        UWP = 8,
        Lumin = 9,
        Switch = 10,
        Stadia_Deprecated = 11,
        tvOS = 12,
        EmbeddedLinux = 13,
        GameCoreXboxOne = 14,
        GameCoreXboxSeries = 15,
        PS5 = 16,
        QNX = 17,
        visionOS = 18,
        visionSimulator = 19,
    }
    public enum BurstTargetCpu
    {
        Auto = 0,
        X86_SSE2 = 1,
        X86_SSE4 = 2,
        X64_SSE2 = 3,
        X64_SSE4 = 4,
        AVX = 5,
        AVX2 = 6,
        WASM32 = 7,
        ARMV7A_NEON32 = 8,
        ARMV8A_AARCH64 = 9,
        THUMB2_NEON32 = 10,
        ARMV8A_AARCH64_HALFFP = 11,
        ARMV9A = 12,
    }
    public enum BurstDebugDataKind
    {
        LineOnly,
        Full
    }
    public struct R_BurstAotCompiler
    {
        private static Type _type;
        private static MethodInfo _isSupportedPlatform;
        private static MethodInfo _getTargetPlatformAndDefaultCpu;
        private static MethodInfo _collectCombinations;
        private static MethodInfo _isMonoReferenceAssemblyDirectory;
        private static MethodInfo _isDotNetStandardAssemblyDirectory;
        private static MethodInfo _postProcessCombinations;
        private static MethodInfo _collateMiscFiles;

        public static bool IsMonoReferenceAssemblyDirectory(string fullPath)
        {
            return (bool)_isMonoReferenceAssemblyDirectory.Invoke(null, new object[] {fullPath});
        }

        public static bool IsDotNetStandardAssemblyDirectory(string fullPath)
        {
            return (bool)_isDotNetStandardAssemblyDirectory.Invoke(null, new object[] { fullPath });
        }

        public static bool IsSupportedPlatform(BuildTarget platform, R_BurstPlatformAotSettings settings)
        {
            return (bool)_isSupportedPlatform.Invoke(null, new object[] { platform, settings.instance });
        }

        public static BurstTargetPlatform GetTargetPlatformAndDefaultCpu(BuildTarget target, out R_TargetCpus targetCpu, R_BurstPlatformAotSettings aotSettingsForTarget)
        {
            object[] parameters = new object[] { target, null, aotSettingsForTarget.instance };
            var result = (BurstTargetPlatform)(int)_getTargetPlatformAndDefaultCpu.Invoke(null, parameters);
            targetCpu = new R_TargetCpus(parameters[1]);
            return result;
        }

        public static List<R_BurstAotCompiler_BurstOutputCombination> CollectCombinations(BurstTargetPlatform targetPlatform, R_TargetCpus targetCpus, BuildSummary constructedSummary)
        {
            var combinations = _collectCombinations.Invoke(null, new object[]
            {
                (int)targetPlatform,
                targetCpus.instance,
                constructedSummary,
            });

            var returnVal = new List<R_BurstAotCompiler_BurstOutputCombination>();
            var combinationsAsIList = (IList)combinations;
            foreach(var combination in combinationsAsIList)
            {
                returnVal.Add(new R_BurstAotCompiler_BurstOutputCombination(combination));
            }
            return returnVal;
        }

        public static void PostProcessCombinations(BurstTargetPlatform targetPlatform, object combinations, BuildSummary buildSummary)
        {
            _postProcessCombinations.Invoke(null, new object[]
            {
                (int)targetPlatform,
                combinations,
                buildSummary
            });
        }

        public static IEnumerable<string> CollateMiscFiles(object combinations, string finalForder, bool retainPDBs)
        {
            return (IEnumerable<string>)_collateMiscFiles.Invoke(null, new object[]
            {
                combinations,
                finalForder,
                retainPDBs
            });
        }

        [InitializeOnLoadMethod]
        static void Reflect()
        {
            _type = Type.GetType(Assembly.CreateQualifiedName("Unity.Burst.Editor", "Unity.Burst.Editor.BurstAotCompiler"));
            _isSupportedPlatform = _type.GetMethod("IsSupportedPlatform", Common.all);
            _getTargetPlatformAndDefaultCpu = _type.GetMethod("GetTargetPlatformAndDefaultCpu", Common.all);
            _collectCombinations = _type.GetMethod("CollectCombinations", Common.all);
            _isMonoReferenceAssemblyDirectory = _type.GetMethod("IsMonoReferenceAssemblyDirectory", Common.all);
            _isDotNetStandardAssemblyDirectory = _type.GetMethod("IsDotNetStandardAssemblyDirectory", Common.all);
            _postProcessCombinations = _type.GetMethod("PostProcessCombinations", Common.all);
            _collateMiscFiles = _type.GetMethod("CollateMiscFiles", Common.all);
        }
    }
    public struct R_BurstAotCompiler_BurstAotSettings
    {
        private static Type _type;
        private static FieldInfo _summary;
        private static FieldInfo _productName;
        private static FieldInfo _aotSettingsForTarget;
        private static FieldInfo _isSupported;
        private static FieldInfo _targetCpus;
        private static FieldInfo _targetPlatform;
        private static FieldInfo _combinations;
        private static FieldInfo _scriptingBackend;
        private static FieldInfo _extraOptions;
        private static FieldInfo _symbolDefinesHash;
        private static MethodInfo _save;

        public BuildSummary summary
        {
            get
            {
                return (BuildSummary)_summary.GetValue(instance);
            }
            set
            {
                _summary.SetValue(instance, value);
            }
        }

        public string productName
        {
            get
            {
                return (string)_productName.GetValue(instance);
            }
            set
            {
                _productName.SetValue(instance, value);
            }
        }

        /// <summary>
        /// Gets or Sets <see cref="instance"/>'s aotSettingsForTarget
        /// </summary>
        public R_BurstPlatformAotSettings aotSettingsForTarget
        {
            get
            {
                return new R_BurstPlatformAotSettings(_aotSettingsForTarget.GetValue(instance));
            }
            set
            {
                _aotSettingsForTarget.SetValue(instance, value.instance);
            }
        }
        public bool isSupported
        {
            get
            {
                return (bool)_isSupported.GetValue(instance);
            }
            set
            {
                _isSupported.SetValue(instance, value);
            }
        }
        public BurstTargetPlatform targetPlatform
        {
            get
            {
                return (BurstTargetPlatform)(int)_targetPlatform.GetValue(instance);
            }
            set
            {
                _targetPlatform.SetValue(instance, (int)value);
            }
        }

        public R_TargetCpus targetCpus
        {
            get
            {
                return new R_TargetCpus(_targetCpus.GetValue(instance));
            }
            set
            {
                _targetCpus.SetValue(instance, value.instance);
            }
        }

        public ReadOnlyCollection<R_BurstAotCompiler_BurstOutputCombination> combinations
        {
            get
            {
                List<R_BurstAotCompiler_BurstOutputCombination> combinations = new List<R_BurstAotCompiler_BurstOutputCombination>();
                IList fromInstance = (IList)_combinations.GetValue(instance);
                foreach(object obj in fromInstance)
                {
                    combinations.Add(new R_BurstAotCompiler_BurstOutputCombination(obj));
                }
                return combinations.AsReadOnly();
            }
        }
        public void SetCombinations(IList<R_BurstAotCompiler_BurstOutputCombination> combinations)
        {
            var listType = typeof(List<>);
            var constructedListType = listType.MakeGenericType(R_BurstAotCompiler_BurstOutputCombination.type);
            var newValueForField = Activator.CreateInstance(constructedListType);
            IList newValueAsList = (IList)newValueForField;
            foreach(var combination in combinations)
            {
                newValueAsList.Add(combination.instance);
            }
            _combinations.SetValue(instance, newValueForField);
        }

        public object GetCombinationsAsObject()
        {
            var listType = typeof(List<>);
            var constructedListType = listType.MakeGenericType(R_BurstAotCompiler_BurstOutputCombination.type);
            var newValueForField = Activator.CreateInstance(constructedListType);
            IList newValueAsList = (IList)newValueForField;
            foreach (var combination in combinations)
            {
                newValueAsList.Add(combination.instance);
            }
            return newValueForField;
        }
        public ScriptingImplementation scriptingBackend
        {
            get
            {
                return (ScriptingImplementation)_scriptingBackend.GetValue(instance);
            }
            set
            {
                _scriptingBackend.SetValue(instance, value);
            }
        }
        public List<string> extraOptions
        {
            get
            {
                return (List<string>)_extraOptions.GetValue(instance);
            }
            set
            {
                _extraOptions.SetValue(instance, value);
            }
        }
        public Hash128 symbolDefinesHash
        {
            get
            {
                return (Hash128)_symbolDefinesHash.GetValue(instance);
            }
            set
            {
                _symbolDefinesHash.SetValue(instance, value);
            }
        }

        /// <summary>
        /// The boxed instance of BurstAotCompiler.BurstAotSettings
        /// </summary>
        public object instance { get; private set; }

        public UnityEditor.Compilation.Assembly assembly;


        public static R_BurstAotCompiler_BurstAotSettings DoSetup(StageAssemblies stageAssemblies, DeserializedAssemblyDefinition assemblyToBurst)
        {
            BuildTarget target = stageAssemblies.buildTarget;

            var _instance = FormatterServices.GetUninitializedObject(_type);
            var settings = new R_BurstAotCompiler_BurstAotSettings(_instance);
            settings.summary = BuildSummaryHelper.ConstructSummary(stageAssemblies.buildTarget, stageAssemblies.releaseBuild ? BuildOptions.None : BuildOptions.Development, Path.GetFullPath($"ThunderKit/Libraries/{assemblyToBurst.name}.dll"));
            settings.productName = assemblyToBurst.name;
            settings.aotSettingsForTarget = R_BurstPlatformAotSettings.GetOrCreateSettings(target);
            settings.isSupported = R_BurstAotCompiler.IsSupportedPlatform(target, settings.aotSettingsForTarget);
            if (!settings.isSupported)
            {
                return settings;
            }
            settings.targetPlatform = R_BurstAotCompiler.GetTargetPlatformAndDefaultCpu(target, out var targetCPUs, settings.aotSettingsForTarget);
            settings.targetCpus = targetCPUs;
            settings.SetCombinations(R_BurstAotCompiler.CollectCombinations(settings.targetPlatform, targetCPUs, settings.summary));
            settings.scriptingBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target)));

            if (settings.targetPlatform == BurstTargetPlatform.UWP)
            {
                List<string> extraOptions = new List<string>();
                settings.extraOptions = new List<string>();

                if (!string.IsNullOrEmpty(EditorUserBuildSettings.wsaUWPVisualStudioVersion))
                {
                    settings.extraOptions.Add(R_BurstCompilerOptions.GetOption("linker-options=", $"vs-version={EditorUserBuildSettings.wsaUWPVisualStudioVersion}"));
                }

                if (!string.IsNullOrEmpty(EditorUserBuildSettings.wsaUWPSDK))
                {
                    settings.extraOptions.Add(R_BurstCompilerOptions.GetOption("linker-options=", $"target-sdk-version={EditorUserBuildSettings.wsaUWPSDK}"));
                }

                settings.extraOptions.Add(R_BurstCompilerOptions.GetOption("platform-configuration=", $"{EditorUserBuildSettings.wsaUWPVisualStudioVersion}:{EditorUserBuildSettings.wsaUWPSDK}:{EditorUserBuildSettings.wsaMinUWPSDK}"));
                settings.extraOptions = extraOptions;
            }

            UnityEditor.Compilation.Assembly assembly = GetAssemblyFromDeserialized(assemblyToBurst);
            settings.assembly = assembly;

            Hash128 definesHash = default;
            definesHash.Append(assembly.name);
            definesHash.Append(assembly.defines.Length);
            foreach(var symbol in assembly.defines.OrderBy(x => x))
            {
                definesHash.Append(symbol);
            }
            settings.symbolDefinesHash = definesHash;

            settings.Save();
            return settings;
        }

        private static UnityEditor.Compilation.Assembly GetAssemblyFromDeserialized(DeserializedAssemblyDefinition deserializedAssemblyDefinition)
        {
            UnityEditor.Compilation.Assembly[] assembliesInCompilationPipeline = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
            foreach(var assembly in assembliesInCompilationPipeline)
            {
                if (deserializedAssemblyDefinition.name == assembly.name)
                    return assembly;
            }
            return null;
        }

        private void Save()
        {
            _save.Invoke(instance, null);
        }

        public R_BurstAotCompiler_BurstAotSettings(object instance)
        {
            this.instance = instance;
            assembly = null;
        }

        [InitializeOnLoadMethod]
        static void Reflect()
        {
            _type = Type.GetType("Unity.Burst.Editor.BurstAotCompiler+BurstAOTSettings, Unity.Burst.Editor");
            _summary = _type.GetField("summary", Common.all);
            _productName = _type.GetField("productName", Common.all);
            _aotSettingsForTarget = _type.GetField("aotSettingsForTarget", Common.all);
            _isSupported = _type.GetField("isSupported", Common.all);
            _targetPlatform = _type.GetField("targetPlatform", Common.all);
            _targetCpus = _type.GetField("targetCpus", Common.all);
            _combinations = _type.GetField("combinations", Common.all);
            _scriptingBackend = _type.GetField("scriptingBackend", Common.all);
            _extraOptions = _type.GetField("extraOptions", Common.all);
            _symbolDefinesHash = _type.GetField("symbolDefinesHash", Common.all);
            _save = _type.GetMethod("Save", Common.all);
        }
    }

    public struct R_BurstCompilerOptions
    {
        static Type _type;
        static MethodInfo _getOption;
        static FieldInfo _forceDisableBurstCompilation;

        public static bool forceDisableBurstCompilation
        {
            get
            {
                return (bool)_forceDisableBurstCompilation.GetValue(null);
            }
        }
        public static string GetOption(string optionName, object value = null)
        {
            return (string)_getOption.Invoke(null, new object[]
            {
                optionName,
                value
            });
        }

        [InitializeOnLoadMethod]
        static void Reflect()
        {
            _type = Type.GetType(Assembly.CreateQualifiedName("Unity.Burst", "Unity.Burst.BurstCompilerOptions"));
            _getOption = _type.GetMethod("GetOption", Common.all);
            _forceDisableBurstCompilation = _type.GetField("ForceDisableBurstCompilation", Common.all);
        }
    }

    public struct R_BurstPlatformAotSettings
    {
        private static Type _type;
        private static MethodInfo _getOrCreateSettings;
        private static MethodInfo _fetchOutputPath;
        private static MethodInfo _resolveTarget;
        private static FieldInfo _enableBurstCompilation;
        private static FieldInfo _disabledWarnings;
        private static FieldInfo _enableDebugInAllBuilds;
        private static FieldInfo _debugDataKind;
        private static FieldInfo _enableOptimisations;
        private static FieldInfo _optimizeFor;

        public bool enableBurstCompilation
        {
            get
            {
                return (bool)_enableBurstCompilation.GetValue(instance);
            }
        }

        public string disabledWarnings
        {
            get
            {
                return (string)_disabledWarnings.GetValue(instance);
            }
        }

        public bool enableDebugInAllBuilds
        {
            get
            {
                return (bool)_enableDebugInAllBuilds.GetValue(instance);
            }
        }

        public BurstDebugDataKind debugDataKind
        {
            get
            {
                return (BurstDebugDataKind)(int)_debugDataKind.GetValue(instance);
            }
        }

        public bool enableOptimisations
        {
            get
            {
                return (bool)_enableOptimisations.GetValue(instance);
            }
        }

        public OptimizeFor optimizeFor
        {
            get
            {
                return (OptimizeFor)_optimizeFor.GetValue(instance);
            }
        }



        /// <summary>
        /// The boxed instance of BurstPlatformAotSettings
        /// </summary>
        public object instance { get; private set; }
        public static R_BurstPlatformAotSettings GetOrCreateSettings(BuildTarget platform)
        {
            return new R_BurstPlatformAotSettings
            {
                instance = _getOrCreateSettings?.Invoke(null, new object[] { platform })
            };
        }

        public static string FetchOutputPath(BuildSummary summary, string productName)
        {
            return (string)_fetchOutputPath.Invoke(null, new object[]
            {
                summary,
                productName
            });
        }

        public static BuildTarget? ResolveTarget(BuildTarget target)
        {
            return (BuildTarget?)_resolveTarget.Invoke(null, new object[] { target });
        }

        public R_BurstPlatformAotSettings(object instance) => this.instance = instance;


        [InitializeOnLoadMethod]
        static void Reflect()
        {
            _type = Type.GetType(Assembly.CreateQualifiedName("Unity.Burst.Editor", "Unity.Burst.Editor.BurstPlatformAotSettings"));
            _getOrCreateSettings = _type.GetMethod("GetOrCreateSettings", Common.all);
            _fetchOutputPath = _type.GetMethod("FetchOutputPath", Common.all);
            _resolveTarget = _type.GetMethod("ResolveTarget", Common.all);
            _enableBurstCompilation = _type.GetField("EnableBurstCompilation", Common.all);
            _disabledWarnings = _type.GetField("DisabledWarnings", Common.all);
            _enableDebugInAllBuilds = _type.GetField("EnableDebugInAllBuilds", Common.all);
            _debugDataKind = _type.GetField("DebugDataKind", Common.all);
            _enableOptimisations = _type.GetField("EnableOptimisations", Common.all);
            _optimizeFor = _type.GetField("OptimizeFor", Common.all);
        }
    }

    public struct R_BurstAotCompiler_BurstOutputCombination
    {
        public static Type type { get; private set; }
        private static FieldInfo _outputPath;
        private static FieldInfo _libraryName;
        private static FieldInfo _collateDirectory;
        private static FieldInfo _targetCpus;
        private static FieldInfo _workaroundFullDebugInfo;
        private static FieldInfo _environmentVariables;

        public string outputPath
        {
            get
            {
                return (string)_outputPath.GetValue(instance);
            }
        }
        public string libraryName
        {
            get
            {
                return (string)_libraryName.GetValue(instance);
            }
        }
        public R_TargetCpus targetCpus
        {
            get
            {
                return new R_TargetCpus(_targetCpus.GetValue(instance));
            }
        }
        public bool workaroundFullDebugInfo
        {
            get
            {
                return (bool)_workaroundFullDebugInfo.GetValue(instance);
            }
        }

        public Dictionary<string, string> environmentVariables
        {
            get
            {
                return (Dictionary<string, string>)_environmentVariables.GetValue(instance);
            }
        }

        public object instance { get; private set; }
        public R_BurstAotCompiler_BurstOutputCombination(object instance) => this.instance = instance;

        [InitializeOnLoadMethod]
        static void Reflect()
        {
            type = Type.GetType(Assembly.CreateQualifiedName("Unity.Burst.Editor", "Unity.Burst.Editor.BurstAotCompiler+BurstOutputCombination"));
            _outputPath = type.GetField("OutputPath", Common.all);
            _libraryName = type.GetField("LibraryName", Common.all);
            _targetCpus = type.GetField("TargetCpus", Common.all);
            _workaroundFullDebugInfo = type.GetField("WorkaroundFullDebugInfo", Common.all);
            _environmentVariables = type.GetField("EnvironmentVariables", Common.all);
        }
    }

    public struct R_TargetCpus
    {
        public static Type type { get; private set; }
        private static FieldInfo _cpus;

        public List<BurstTargetCpu> cpus
        {
            get
            {
                var val = _cpus.GetValue(instance);
                var asList = (IList)val;
                List<BurstTargetCpu> result = new List<BurstTargetCpu>();
                foreach(var cpu in asList)
                {
                    result.Add((BurstTargetCpu)(int)cpu);
                }
                return result;
            }
        }
        public object instance { get; private set; }
        public R_TargetCpus(object instance) => this.instance = instance;

        [InitializeOnLoadMethod]
        static void Reflect()
        {
            type = Type.GetType(Assembly.CreateQualifiedName("Unity.Burst.Editor", "Unity.Burst.Editor.TargetCpus"));
            _cpus = type.GetField("Cpus", Common.all);
        }
    }

    public struct R_BurstLoader
    {
        private static Type _type;
        private static PropertyInfo _isDebugging;

        public static bool isDebugging
        {
            get
            {
                return (bool)_isDebugging.GetValue(null, null);
            }
        }
        
        public static bool isBclExecutableNative
        {
            get
            {
                var propertyInfo = _type.GetProperty("BclConfiguration", Common.all);
                var configAsObject = propertyInfo.GetValue(null);

                var configurationType = Type.GetType(Assembly.CreateQualifiedName("Unity.Burst", "Unity.Burst.Editor.BclConfiguration"));
                var isExecutableNativeProp = configurationType.GetProperty("IsExecutableNative", Common.all);
                return (bool)isExecutableNativeProp.GetValue(configAsObject);
            }
        }

        public static string bclPath
        {
            get
            {
                var propertyInfo = _type.GetProperty("BclConfiguration", Common.all);
                var configAsObject = propertyInfo.GetValue(null);

                var configurationType = Type.GetType(Assembly.CreateQualifiedName("Unity.Burst", "Unity.Burst.Editor.BclConfiguration"));
                var bclPath = configurationType.GetProperty("ExecutablePath", Common.all);
                return (string)bclPath.GetValue(configAsObject);
            }
        }

        [InitializeOnLoadMethod]
        static void Reflect()
        {
            _type = Type.GetType(Assembly.CreateQualifiedName("Unity.Burst", "Unity.Burst.Editor.BurstLoader"));
            _isDebugging = _type.GetProperty("IsDebugging", Common.all);
        }
    }

    public struct R_BurstAssemblyDisable
    {
        private static Type _type;
        private static MethodInfo _getDisabledAssemblies;

        public static string[] GetDisabledAssemblies(DisableType type, string platformIdentifier)
        {
            return (string[])_getDisabledAssemblies.Invoke(null, new object[] { (int)type, platformIdentifier });
        }
        [InitializeOnLoadMethod]
        static void Reflect()
        {
            _type = Type.GetType(Assembly.CreateQualifiedName("Unity.Burst", "Unity.Burst.Editor.BurstAssemblyDisable"));
            _getDisabledAssemblies = _type.GetMethod("GetDisabledAssemblies", Common.all);
        }
        public enum DisableType
        {
            Editor,
            Player
        }
    }

    public struct R_BclOutputErrorParser
    {
        private static Type _type;

        public object instance { get; private set; }

        public static R_BclOutputErrorParser Create()
        {
            return new R_BclOutputErrorParser(Activator.CreateInstance(_type));
        }

        public R_BclOutputErrorParser(object instance) => this.instance = instance;
        [InitializeOnLoadMethod]
        static void Reflect()
        {
            _type = Type.GetType(Assembly.CreateQualifiedName("Unity.Burst.Editor", "Unity.Burst.Editor.BurstAotCompiler+BclOutputErrorParser"));
        }
    }

    public struct R_BCLRunner
    {
        private static Type _type;
        private static MethodInfo _runManagedProgram;
        private static MethodInfo _runNativeProgram;
        private static MethodInfo _escapeForShell;

        public static void RunManagedProgram(string executablePath, string responseFile, R_BclOutputErrorParser errorParser ,Dictionary<string, string> environmentVariables)
        {
            _runManagedProgram.Invoke(null, new object[]
            {
                executablePath,
                responseFile,
                errorParser.instance,
                environmentVariables
            });
        }

        public static void RunNativeProgram(string executablePath, string responseFile, R_BclOutputErrorParser errorParser)
        {
            _runNativeProgram.Invoke(null, new object[]
            {
                executablePath,
                responseFile,
                errorParser.instance
            });
        }

        public static string EscapeForShell(string s, bool singleQuoteWrapped = false)
        {
            return (string) _escapeForShell.Invoke(null, new object[] {s, singleQuoteWrapped});
        }

        [InitializeOnLoadMethod]
        static void Reflect()
        {
            _type = Type.GetType(Assembly.CreateQualifiedName("Unity.Burst.Editor", "Unity.Burst.Editor.BurstAotCompiler+BclRunner"));
            _runManagedProgram = _type.GetMethod("RunManagedProgram", BindingFlags.Public | BindingFlags.Static);
            _runNativeProgram = _type.GetMethod("RunNativeProgram", BindingFlags.Public | BindingFlags.Static);
            _escapeForShell = _type.GetMethod("EscapeForShell", Common.all);
        }
    }

    //Some cursed shit going on here
    public struct BuildSummaryHelper
    {
        static Type _type;
        static FieldInfo _platform;
        static FieldInfo _output;
        static FieldInfo _options;

        public static BuildSummary ConstructSummary(BuildTarget buildTarget, BuildOptions options, string output)
        {
            var summaryInstance = Activator.CreateInstance(_type);
            _platform.SetValue(summaryInstance, buildTarget);
            _output.SetValue(summaryInstance, output);
            _options.SetValue(summaryInstance, options);
            return (BuildSummary)summaryInstance;
        }

        [InitializeOnLoadMethod]
        static void Reflect()
        {
            _type = Type.GetType("UnityEditor.Build.Reporting.BuildSummary, UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            _platform = _type.GetField("<platform>k__BackingField", Common.all);
            _output = _type.GetField("<outputPath>k__BackingField", Common.all);
            _options = _type.GetField("<options>k__BackingField", Common.all);
        }
    }
}
#endif