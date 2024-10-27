#if UNITY_EDITOR && ENABLE_BURST_AOT && R2TB_THUNDERKIT_INSTALLED
using ThunderKit.Core.Manifests.Datums;

namespace RoR2ThunderBurster.TK.Datums
{
    /// <summary>
    /// A Subclass of the AssemblyDefinitions datum, assembly definitions defined here will be passed thru the Burst Compiler IF a <see cref="PipelineJobs.BurstStagedAssemblies"/> runs.
    /// </summary>
    public class BurstAssemblyDefinitionsDatum : AssemblyDefinitions
    {

    }
}
#endif