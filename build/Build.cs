using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using System.ComponentModel;
using System.Linq;
// ReSharper disable UnusedMember.Local
// ReSharper disable AllUnderscoreLocalParameterName

[GitHubActions(
    "compile", GitHubActionsImage.UbuntuLatest,
    OnPushBranches = ["**"], InvokedTargets = [nameof(Compile)], ImportSecrets = [],
    Progress = true, FetchDepth = 0
)]
sealed class Build : NukeBuild
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    [Description("Configuration to build. Default: Debug (local) or Release (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Solution file to build")]
    [Description("Solution file to build")]
    readonly Solution Solution = RootDirectory.GlobFiles("*.slnx").First().ReadSolution();

    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    [Description("Cleans all output and intermediate directories.")]
    Target Clean => _ => _
        .Description("Cleans all output and intermediate directories.")
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(p => p.DeleteDirectory());
        });

    [Description("Restores all NuGet packages for the solution.")]
    Target Restore => _ => _
        .Description("Restores all NuGet packages for the solution.")
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    [Description("Compiles the solution in the specified configuration.")]
    Target Compile => _ => _
        .Description("Compiles the solution in the specified configuration.")
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    AbsolutePath SourceDirectory => RootDirectory / "src";

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public static int Main() => Execute<Build>(x => x.Compile);
}
