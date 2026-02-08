using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using System.ComponentModel;
using System.Linq;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
// ReSharper disable UnusedMember.Local
// ReSharper disable AllUnderscoreLocalParameterName

[GitHubActions(
    "compile", GitHubActionsImage.UbuntuLatest,
    OnPushBranches = ["**"], InvokedTargets = [nameof(Compile)], ImportSecrets = [],
    Progress = true, FetchDepth = 0
)]
[GitHubActions(
    "publish_nuget", GitHubActionsImage.UbuntuLatest,
    OnPushTags = ["v*"], InvokedTargets = [nameof(PublishNuget)],
    ImportSecrets = [],
    EnableGitHubToken = true,
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

    [Parameter("NuGet API Key for authentication")]
    [Description("NuGet API Key (use GitHub Token for GitHub Packages)")]
    readonly string NuGetApiKey;

    [Parameter("GitHub Token (automatically provided in GitHub Actions)")]
    [Description("GitHub Token for GitHub Packages authentication")]
    readonly string GitHubToken;

    [Parameter("NuGet Source URL")]
    [Description("NuGet Source URL. Default: GitHub Packages")]
    readonly string NuGetSource = "https://nuget.pkg.github.com/paralaxsd/index.json";

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

    [Description("Packs the AgentOrange.Core library as a NuGet package.")]
    Target PackNuget => _ => _
        .Description("Packs the AgentOrange.Core library as a NuGet package.")
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetProject(SourceDirectory / "AgentOrange.Core" / "AgentOrange.Core.csproj")
                .SetConfiguration(Configuration.Release)
                .SetOutputDirectory(ArtifactsDirectory)
                .EnableNoRestore());
        });

    [Description("Publishes the NuGet package to the configured NuGet source.")]
    Target PublishNuget => _ => _
        .Description("Publishes the NuGet package to the configured NuGet source.")
        .DependsOn(PackNuget)
        .Executes(() =>
        {
            var apiKey = GitHubToken ?? NuGetApiKey;
            Assert.True(!string.IsNullOrEmpty(apiKey), "Either GitHubToken or NuGetApiKey must be provided");

            DotNetNuGetPush(s => s
                .SetTargetPath(ArtifactsDirectory / "*.nupkg")
                .SetSource(NuGetSource)
                .SetApiKey(apiKey)
                .EnableSkipDuplicate());
        });

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public static int Main() => Execute<Build>(x => x.Compile);
}
