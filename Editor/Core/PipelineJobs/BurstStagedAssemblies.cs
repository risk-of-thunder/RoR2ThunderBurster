#if UNITY_EDITOR && ENABLE_BURST_AOT && R2TB_THUNDERKIT_INSTALLED && R2TB_BURST_INSTALLED
using System.Collections.Generic;
using RoR2ThunderBurster.BurstImpl;
using RoR2ThunderBurster.TK.Datums;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Manifests;
using ThunderKit.Core.Pipelines;
using ThunderKit.Core.Pipelines.Jobs;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.EditorTools;
using UnityEngine;
using Unity.Burst;
using UnityEditor.Build;
using ThunderKit.Core.Paths;

namespace RoR2ThunderBurster.TK.PipelineJobs
{
    [PipelineSupport(typeof(Pipeline)), ManifestProcessor, RequiresManifestDatumType(typeof(BurstAssemblyDefinitionsDatum))]
    public class BurstStagedAssemblies : PipelineJob
    {
        [Tooltip("The StageAssemblies job that ran prior to this one, this is used to obtain data related if the source assemblies where built on release or debug mode, and what platform its targetting.")]
        public StageAssemblies stageAssembliesJob;
        [Tooltip("If true, All the assembly definitions defined in each BurstAssemblyDefinitionsDatum will be outputted as a single Bursted assembly, otherwise, each assembly will have its bursted counterpart.")]
        public bool combineOutputIntoSingleAssembly;
        
        public override Task Execute(Pipeline pipeline)
        {
            R_BurstAotCompiler_BurstAotSettings aotSettings = default;
            BurstStagedAssembliesEntry[] entries = GetAssemblyDefinitionsFromDatum(pipeline);
            foreach (BurstStagedAssembliesEntry entry in entries)
            {
                try
                {
                    aotSettings = R_BurstAotCompiler_BurstAotSettings.DoSetup(stageAssembliesJob, entry.deserializedAssemblyDefinition);
                    if (!aotSettings.isSupported)
                    {
                        pipeline.Log(LogLevel.Information, $"Build Target {stageAssembliesJob.buildTarget} is not valid for burst compilation.");
                        return Task.CompletedTask;
                    }
                    DoGenerate(aotSettings).ToList(); //Force enumeration
                }
                catch(Exception e)
                {
                    pipeline.Log(LogLevel.Error, $"Failed to burst compile assembly {entry.deserializedAssemblyDefinition.name}, check console for more details");
                    Debug.LogException(e);
                }
            }
            StageBurstedAssemblies(pipeline, entries);
            return Task.CompletedTask;
        }

        private void StageBurstedAssemblies(Pipeline pipeline, BurstStagedAssembliesEntry[] entries)
        {
            string assemblyStagingPath = stageAssembliesJob.assemblyArtifactPath.Resolve(pipeline, this);
            foreach(var entry in entries)
            {
                var assemblyName = entry.deserializedAssemblyDefinition.name;
                string baseFileName = assemblyName + "_Burst.{0}";
                string basePath = Path.Combine(assemblyStagingPath, baseFileName);
                string burstedAssembly = string.Format(basePath, "dll");
                string debugDatabase = string.Format(basePath, "pdb");
                string textFile = string.Format(basePath, "txt");

                foreach(var destPath in entry.stagingPaths)
                {
                    string fileName = "";
                    string srcFileName = "";
                    string destFileName = "";

                    if(File.Exists(burstedAssembly))
                    {
                        srcFileName = burstedAssembly;
                        fileName = Path.GetFileName(srcFileName);
                        destFileName = Path.Combine(destPath, fileName);
                        File.Copy(srcFileName, destFileName, true);
                        pipeline.Log(LogLevel.Information, $"Staged Bursted Assembly for Assembly {assemblyName}");
                    }

                    if (File.Exists(debugDatabase))
                    {
                        srcFileName = debugDatabase;
                        fileName = Path.GetFileName(srcFileName);
                        destFileName = Path.Combine(destPath, fileName);
                        File.Copy(srcFileName, destFileName, true);
                        pipeline.Log(LogLevel.Information, $"Staged Debug Databases for Assembly {assemblyName}");
                    }

                    if (File.Exists(textFile))
                    {
                        srcFileName = textFile;
                        fileName = Path.GetFileName(srcFileName);
                        destFileName = Path.Combine(destPath, fileName);
                        File.Copy(srcFileName, destFileName, true);
                        pipeline.Log(LogLevel.Information, $"Staged Text File for Assembly {assemblyName}");
                    }
                }
            }
        }

        private BurstStagedAssembliesEntry[] GetAssemblyDefinitionsFromDatum(Pipeline pipeline)
        {
            var datums = pipeline.Manifest.Data.OfType<BurstAssemblyDefinitionsDatum>();
            List<BurstStagedAssembliesEntry> result = new List<BurstStagedAssembliesEntry>();
            foreach(var datum in datums)
            {
                var stagingPaths = datum.StagingPaths.Select(p => PathReference.ResolvePath(p, pipeline, this));
                foreach(var definition in datum.definitions)
                {
                    result.Add(new BurstStagedAssembliesEntry
                    {
                        deserializedAssemblyDefinition = DeserializedAssemblyDefinition.FromJSON(definition),
                        stagingPaths = stagingPaths.ToArray()
                    });
                }
            }
            return result.OrderBy(p => p.deserializedAssemblyDefinition.name).ToArray();
        }

        private IEnumerable<string> DoGenerate(R_BurstAotCompiler_BurstAotSettings settings)
        {
            string tkLibraryStagingFolder = Path.GetDirectoryName(FindPathForAssembly(settings.assembly));

            var buildTarget = settings.summary.platform;
            string burstMiscAlongsidePath = "";

            if ((settings.summary.options & BuildOptions.InstallInBuildFolder) == 0)
            {
                burstMiscAlongsidePath = R_BurstPlatformAotSettings.FetchOutputPath(settings.summary, settings.productName);
            }

            HashSet<string> assemblyDefines = new HashSet<string>();


            // Early exit if burst is not activated or the platform is not supported
            if (R_BurstCompilerOptions.forceDisableBurstCompilation || !settings.aotSettingsForTarget.enableBurstCompilation)
            {
                return Array.Empty<string>();
            }

            var isDevelopmentBuild = (settings.summary.options & BuildOptions.Development) != 0;

            var commonOptions = new List<string>();
            var stagingFolder = Path.GetFullPath(tkLibraryStagingFolder);

            // grab the location of the root of the player folder - for handling nda platforms that require keys
            var keyFolder = BuildPipeline.GetPlaybackEngineDirectory(buildTarget, BuildOptions.None);
            commonOptions.Add(R_BurstCompilerOptions.GetOption("key-folder=", keyFolder));
            commonOptions.Add(R_BurstCompilerOptions.GetOption("decode-folder=", Path.Combine(Environment.CurrentDirectory, "Library", "Burst")));

            // Extract the TargetPlatform and Cpus from the current build settings
            commonOptions.Add(R_BurstCompilerOptions.GetOption("platform=", settings.targetPlatform));

            // --------------------------------------------------------------------------------------------------------
            // 1) Calculate AssemblyFolders
            // These are the folders to look for assembly resolution
            // --------------------------------------------------------------------------------------------------------
            var assemblyFolders = new List<string> { stagingFolder };

            AddAssemblyFolder(FindPathForAssembly(settings.assembly), stagingFolder, buildTarget, assemblyFolders);

            //Adds all the references to the assembly, used for making sure the compilation works succesfully.
            foreach (var assemblyRef in settings.assembly.compiledAssemblyReferences)
            {
                AddAssemblyFolder(assemblyRef, stagingFolder, buildTarget, assemblyFolders);
            }

            assemblyFolders.Add("D:\\ProgramFiles\\r2modman\\DataFolder\\RiskOfRain2\\profiles\\MSU2Dev\\BepInEx\\plugins\\RiskofThunder-Bursts_of_Rain\\.RuntimeAssemblies");
            assemblyFolders.Add(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "Library", "ScriptAssemblies")));

            if (settings.extraOptions != null)
            {
                commonOptions.AddRange(settings.extraOptions);
            }

            if (R_BurstLoader.isDebugging)
            {
                try
                {
                    var copyAssemblyFolder = Path.Combine(Environment.CurrentDirectory, "Logs", "StagingAssemblies");
                    try
                    {
                        if (Directory.Exists(copyAssemblyFolder)) Directory.Delete(copyAssemblyFolder);
                    }
                    catch
                    {
                    }

                    if (!Directory.Exists(copyAssemblyFolder)) Directory.CreateDirectory(copyAssemblyFolder);
                    foreach (var file in Directory.EnumerateFiles(stagingFolder))
                    {
                        File.Copy(file, Path.Combine(copyAssemblyFolder, Path.GetFileName(file)));
                    }
                }
                catch
                {
                }
            }

            // --------------------------------------------------------------------------------------------------------
            // 2) Calculate root assemblies
            // These are the assemblies that the compiler will look for methods to compile
            // This list doesn't typically include .NET runtime assemblies but only assemblies compiled as part
            // of the current Unity project
            // --------------------------------------------------------------------------------------------------------
            var rootAssemblies = new List<string>();
            var playerAssemblyPath = Path.Combine(stagingFolder, Path.GetFileName(settings.assembly.outputPath));

            if (!File.Exists(playerAssemblyPath))
            {
                Debug.LogWarning($"Unable to find player assembly: {settings.assembly.outputPath}");
            }
            else
            {
                rootAssemblies.Add(playerAssemblyPath);
                commonOptions.Add(R_BurstCompilerOptions.GetOption("assembly-defines=", $"{settings.assembly.name};{string.Join(";", settings.assembly.defines)}"));
            }
            commonOptions.AddRange(rootAssemblies.Select(root => R_BurstCompilerOptions.GetOption("root-assembly=", root)));

            // --------------------------------------------------------------------------------------------------------
            // 4) Compile each combination
            //
            // Here bcl.exe is called for each target CPU combination
            // --------------------------------------------------------------------------------------------------------
            string debugLogFile = null;
            if (R_BurstLoader.isDebugging)
            {
                try
                {
                    var logDir = Path.Combine(Environment.CurrentDirectory, "Logs");
                    debugLogFile = Path.Combine(logDir, "burst_bcl_editor.log");
                    if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                    File.WriteAllText(debugLogFile, string.Empty);
                }
                catch
                {
                    debugLogFile = null;
                }
            }

            if ((settings.summary.options & BuildOptions.InstallInBuildFolder) == 0)
            {
                try
                {
                    if (Directory.Exists(burstMiscAlongsidePath)) Directory.Delete(burstMiscAlongsidePath, true);
                }
                catch
                {
                }
                Directory.CreateDirectory(burstMiscAlongsidePath);
            }

            foreach (var combination in settings.combinations)
            {
                var assemblyTKLocation = FindPathForAssembly(settings.assembly);
                var assemblyTKDirectory = Path.GetDirectoryName(assemblyTKLocation);
                var outputFilePrefix = Path.Combine(assemblyTKDirectory, settings.assembly.name + "_Burst");

                var options = new List<string>(commonOptions)
                {
                    R_BurstCompilerOptions.GetOption("output=", outputFilePrefix),
                    R_BurstCompilerOptions.GetOption("temp-folder=", Path.Combine(Environment.CurrentDirectory, "Temp", "Burst"))
                };

                foreach (var cpu in combination.targetCpus.cpus)
                {
                    options.Add(R_BurstCompilerOptions.GetOption("target=", cpu));
                }
                if (settings.targetPlatform == BurstTargetPlatform.iOS || settings.targetPlatform == BurstTargetPlatform.tvOS || settings.targetPlatform == BurstTargetPlatform.Switch || settings.targetPlatform == BurstTargetPlatform.visionOS)
                {
                    options.Add(R_BurstCompilerOptions.GetOption("generate-static-linkage-methods"));
                }

                if (settings.targetPlatform == BurstTargetPlatform.Windows)
                {
                    options.Add(R_BurstCompilerOptions.GetOption("linker-options=", $"PdbAltPath=\"{settings.productName}_{combination.outputPath}/{Path.GetFileNameWithoutExtension(combination.libraryName)}.pdb\""));
                }


                options.AddRange(assemblyFolders.Select(assemblyFolder => R_BurstCompilerOptions.GetOption("assembly-folder=", assemblyFolder)));

                // Log the targets generated by BurstReflection.FindExecuteMethods
                if (R_BurstLoader.isDebugging && debugLogFile != null)
                {
                    try
                    {
                        var writer = new StringWriter();
                        writer.WriteLine("-----------------------------------------------------------");
                        writer.WriteLine("Combination: " + combination);
                        writer.WriteLine("-----------------------------------------------------------");

                        foreach (var option in options)
                        {
                            writer.WriteLine(option);
                        }

                        writer.WriteLine("Assemblies in AssemblyFolders:");
                        foreach (var assemblyFolder in assemblyFolders)
                        {
                            writer.WriteLine("|- Folder: " + assemblyFolder);
                            foreach (var assemblyOrDll in Directory.EnumerateFiles(assemblyFolder, "*.dll"))
                            {
                                var fileInfo = new FileInfo(assemblyOrDll);
                                writer.WriteLine("   |- " + assemblyOrDll + " Size: " + fileInfo.Length + " Date: " + fileInfo.LastWriteTime);
                            }
                        }

                        File.AppendAllText(debugLogFile, writer.ToString());
                    }
                    catch
                    {
                        // ignored
                    }
                }

                options.Add(R_BurstCompilerOptions.GetOption("pdb-search-paths=", @"Temp/ManagedSymbols/"));

                if(isDevelopmentBuild && Environment.GetEnvironmentVariable("UNITY_BURST_ENABLE_SAFETY_CHECKS_IN_PLAYER_BUILD") != null)
                {
                    options.Add("--global-safety-checks-setting=ForceOn");
                }

                options.Add(R_BurstCompilerOptions.GetOption("generate-link-xml=", Path.Combine("Temp", "burst.link.xml")));

                if(!string.IsNullOrWhiteSpace(settings.aotSettingsForTarget.disabledWarnings))
                {
                    options.Add(R_BurstCompilerOptions.GetOption("disable-warnings=", settings.aotSettingsForTarget.disabledWarnings));
                }

                if (isDevelopmentBuild || settings.aotSettingsForTarget.enableDebugInAllBuilds)
                {
                    if (!isDevelopmentBuild)
                    {
                        Debug.LogWarning(
                            "Symbols are being generated for burst compiled code, please ensure you intended this - see Burst AOT settings.");
                    }


                    options.Add(R_BurstCompilerOptions.GetOption("debug=",
                        (settings.aotSettingsForTarget.debugDataKind == BurstDebugDataKind.Full) && (!combination.workaroundFullDebugInfo) ? "Full" : "LineOnly"));
                }

                if(!settings.aotSettingsForTarget.enableOptimisations)
                {
                    options.Add(R_BurstCompilerOptions.GetOption("disable-opt"));
                }
                else
                {
                    switch(settings.aotSettingsForTarget.optimizeFor)
                    {
                        case OptimizeFor.Default:
                        case OptimizeFor.Balanced:
                            options.Add(R_BurstCompilerOptions.GetOption("opt-level=", 2));
                            break;
                        case OptimizeFor.Performance:
                            options.Add(R_BurstCompilerOptions.GetOption("opt-level=", 3));
                            break;
                        case OptimizeFor.Size:
                            options.Add(R_BurstCompilerOptions.GetOption("opt-for-size"));
                            options.Add(R_BurstCompilerOptions.GetOption("opt-level=", 3));
                            break;
                        case OptimizeFor.FastCompilation:
                            options.Add(R_BurstCompilerOptions.GetOption("opt-level=", 1));
                            break;
                    }
                }

                if(R_BurstLoader.isDebugging)
                {
                    options.Add(R_BurstCompilerOptions.GetOption("debug-logging"));
                }

                var disabledAssemblies = R_BurstAssemblyDisable.GetDisabledAssemblies(R_BurstAssemblyDisable.DisableType.Player, R_BurstPlatformAotSettings.ResolveTarget(buildTarget).ToString());
                foreach (var discard in disabledAssemblies)
                {
                    options.Add(R_BurstCompilerOptions.GetOption("discard-assemblies=", discard));
                }

                // Write current options to the response file
                var responseFile = Path.GetTempFileName();
                Debug.Log(responseFile);
                File.WriteAllLines(responseFile, options);

                if (R_BurstLoader.isDebugging)
                {
                    Debug.Log($"bcl.exe {"+burstc"} @{responseFile}\n\nResponse File:\n" + string.Join("\n", options));
                }

                try
                {
                    var burstcSwitch = "+burstc";

                    if (!string.IsNullOrEmpty(
                            Environment.GetEnvironmentVariable("UNITY_BURST_DISABLE_INCREMENTAL_PLAYER_BUILDS")))
                    {
                        burstcSwitch = "";
                    }

                    var errorParser = R_BclOutputErrorParser.Create();
                    if(R_BurstLoader.isBclExecutableNative)
                    {
                        R_BCLRunner.RunNativeProgram(R_BurstLoader.bclPath, $"{burstcSwitch} {R_BCLRunner.EscapeForShell("@" + responseFile)}", errorParser);
                    }
                    else
                    {
                        R_BCLRunner.RunManagedProgram(R_BurstLoader.bclPath, $"{burstcSwitch} {R_BCLRunner.EscapeForShell("@" + responseFile)}", errorParser, combination.environmentVariables);
                    }

                    var stagingOutputFolder = Path.GetFullPath(Path.Combine(@"Temp/StagingArea/", combination.outputPath));

                    // Additionally copy the pdb to the root of the player build so run in editor also locates the symbols
                    var pdbPath = $"{Path.Combine(stagingOutputFolder, combination.libraryName)}.pdb";
                    if (File.Exists(pdbPath))
                    {
                        var dstPath = Path.Combine(@"Temp/StagingArea/", $"{combination.libraryName}.pdb");
                        File.Copy(pdbPath, dstPath, overwrite: true);
                    }
                }
                catch (BuildFailedException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new BuildFailedException(e);
                }
            }

            R_BurstAotCompiler.PostProcessCombinations(settings.targetPlatform, settings.GetCombinationsAsObject(), settings.summary);


            var pdbsRemainInBuild = isDevelopmentBuild || settings.aotSettingsForTarget.enableDebugInAllBuilds || settings.targetPlatform == BurstTargetPlatform.UWP;

            if((settings.summary.options & BuildOptions.InstallInBuildFolder) == 0)
            {
                return R_BurstAotCompiler.CollateMiscFiles(settings.GetCombinationsAsObject(), burstMiscAlongsidePath, pdbsRemainInBuild);
            }

            return Array.Empty<string>();
        }

        private string FindPathForAssembly(UnityEditor.Compilation.Assembly assembly)
        {
            var mainDirectory = Environment.CurrentDirectory;
            var thunderKit = Path.Combine(mainDirectory, "ThunderKit");
            var libraries = Path.Combine(thunderKit, "Libraries");
            return Path.Combine(libraries, assembly.name + ".dll");
        }

        private void AddAssemblyFolder(string assemblyRef, string stagingFolder, BuildTarget buildTarget, List<string> output)
        {
            // Exclude folders with assemblies already compiled in the `folder`
            var assemblyName = Path.GetFileName(assemblyRef);
            if (assemblyName != null && File.Exists(Path.Combine(stagingFolder, assemblyName)))
            {
                return;
            }

            var directory = Path.GetDirectoryName(assemblyRef);
            if (directory != null)
            {
                var fullPath = Path.GetFullPath(directory);
                if (R_BurstAotCompiler.IsMonoReferenceAssemblyDirectory(fullPath) || R_BurstAotCompiler.IsDotNetStandardAssemblyDirectory(fullPath))
                {
                    fullPath = Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/lib/mono");
                    fullPath = Path.Combine(fullPath, "unityaot-" + GetPlatformProfileSuffix(buildTarget));
                    fullPath = Path.GetFullPath(fullPath);
                    if(!output.Contains(fullPath))
                    {
                        output.Add(fullPath);
                    }

                    fullPath = Path.Combine(fullPath, "Facades");
                    if(!output.Contains(fullPath))
                    {
                        output.Add(fullPath);
                    }
                }
                else if(!output.Contains(fullPath))
                {
                    output.Add(fullPath);
                }
            }
        }

        private string GetPlatformProfileSuffix(BuildTarget buildTarget)
        {
            Type t = Type.GetType("UnityEditor.BuildTargetDiscovery, UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            var method = t.GetMethod("GetPlatformProfileSuffix", Common.all);
            return (string)method.Invoke(null, new object[] {buildTarget});
        }

        private struct BurstStagedAssembliesEntry
        {
            public DeserializedAssemblyDefinition deserializedAssemblyDefinition;
            public string[] stagingPaths;
        }
    }
}
#endif