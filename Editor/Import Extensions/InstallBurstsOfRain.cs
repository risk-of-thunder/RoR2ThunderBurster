#if R2TB_IMPORT_EXTENSIONS && R2TB_THUNDERKIT_INSTALLED
using ThunderKit.Integrations.Thunderstore;
using RiskOfThunder.RoR2Importer;

namespace RiskOfThunder.ThunderBurster.ImportExtensions
{
    /// <summary>
    /// Import extension that installs Bursts of Rain
    /// </summary>
    public class InstallBurstsOfRain : ThunderstorePackageInstaller
    {
        public override string DependencyId => "RiskofThunder-BurstsOfRain";

        public override string ThunderstoreAddress => "https://thunderstore.io";

        public override int Priority => Constants.Priority.InstallBepInEx - 1;

        public override bool ForceLatestDependencies => true;

        public override string Description => "Installs Bursts of Rain, the Burst assembly loader";
    }
}
#endif