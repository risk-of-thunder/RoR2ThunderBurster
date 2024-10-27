# Risk of Thunder - RoR2 Thunder Burster

For the runtime portion of the Burst implementation in Risk of Rain, see [Bursts of Rain](https://github.com/risk-of-thunder/BurstsOfRain)

RoR2 Thunder Burster is a ThunderKit extension used to develop mods that contain Burst-Compilable jobs in their assemblies. This is done via 2 new CompossableObject Elements, The ``BurstAssemblyDefinitions`` ``ManifestDatum``, and the ``BurstStagedAssemblies`` ``PipelineJob``.

## Features

* 1 new import extension used to install [Bursts of Rain](https://github.com/risk-of-thunder/BurstsOfRain). The runtime counterpart of the Burst Implementation.
* Automated process of installing dependencies, including but not limited to:
    * [RoR2 Import Extensions](https://github.com/risk-of-thunder/RoR2ImportExtensions)
    * [ThunderKit](https://github.com/risk-of-thunder/RoR2ImportExtensions)
    * [com.unity.burst version 1.8.18](https://docs.unity3d.com/2021.3/Documentation/Manual/com.unity.burst.html)
    * [com.unity.collections version 1.5.1](https://docs.unity3d.com/2021.3/Documentation/Manual/com.unity.collections.html)
    * [com.unity.mathematics version 1.2.6](https://docs.unity3d.com/2021.3/Documentation/Manual/com.unity.mathematics.html)
* Contains an automatic embedder for com.unity.collections, which is used to embed the package into the project and remove it's Mono.Cecil explicit dependency, allowing for Collections and BepInEx to co-exist in the same project.
* A Subclass of ``AssemblyDefinitions`` ``ManifestDatum``, the ``BurstAssemblyDefinitionsDatum`` which is used to mark that specific assemblies should be passed thru the Burst Compiler.
* A custom ``PipelineJob``, the ``BurstStagedAssemblies`` job, which should be executed after a ``StageAssemblies`` job. It passes the assembly definitions defined in a ``BurstAssemblyDefinitionsDatum`` thru the Burst Compiler, and Stages them.

## Use Case

To burst an assembly, first you need to replace your Manifest's ``AssemblyDefinitions`` datum with a ``BurstAssemblyDefinitionsDatum``.

You can do this by right clicking the cog to the right of the compossable element and clicking remove. then adding the new datum.

![ManifestWithDatum]()

Secondly, on any pipeline of your choice, add the ``BurstStagedAssemblies`` job.

You may notice that it has a single field, this field should point to the Stage Assemblies pipeline job that staged the normal assemblies, this field is REQUIRED for the pipeline to work properly, as its used to obtain certain data such as the Build Target and wether the assembly was built in debug or release mode.

A Burst Staged Assemblies job should run AFTER it's Stage Assemblies counterpart.

![PipelineWithJob]()