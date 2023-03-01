///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = ConsoleOrBuildSystemArgument("Target", "Build");
var buildNumber = ConsoleOrBuildSystemArgument("BuildNumber", 0);
var configuration = ConsoleOrBuildSystemArgument("Configuration", "Release");
var sourcesDir = ConsoleOrBuildSystemArgument("SourcesDir", "../..");
var buildDir = ConsoleOrBuildSystemArgument("BuildDir", "./");
var artifactsDirName = ConsoleOrBuildSystemArgument("ArtifactsDirName", "build.output");
var details = ConsoleOrBuildSystemArgument<DotNetVerbosity>("Details", DotNetVerbosity.Detailed);

var buildMetainfo = ConsoleOrBuildSystemArgument<bool>("BuildMetadata", false);
var repositoryUri = ConsoleOrBuildSystemArgument("Repository", "");
var author = ConsoleOrBuildSystemArgument("Author", "");

var sourcesPath = DirectoryPath.FromString(sourcesDir);
var projectPath = sourcesPath.Combine("Repository.Public.sln");

var buildPath = DirectoryPath.FromString(buildDir);
var artifactsPath = buildPath.Combine(artifactsDirName);

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    var exitCode = StartProcess("dotnet", new ProcessSettings {
        WorkingDirectory = sourcesPath,
        Arguments = "--info"
    });

    if (exitCode != 0)
        throw new Exception($"Failed to get .NET SDK info: {exitCode}");
});

// Teardown(ctx =>
// {
// });

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    var block = BuildSystem.IsRunningOnTeamCity ? TeamCity.Block("Clean") : null;

    try
    {
        DotNetClean(projectPath.ToString(), new DotNetCleanSettings {
            Configuration = configuration,
            Verbosity = details,
        });
        CleanDirectory(artifactsPath);
    }
    finally
    {
        block?.Dispose();
    }
});

Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var block = BuildSystem.IsRunningOnTeamCity ? TeamCity.Block("Build") : null;

    try
    {
        var msBuildSettings = new DotNetMSBuildSettings();
        msBuildSettings.WithProperty("CIBuild", "true");
        msBuildSettings.WithProperty("AlgoPackage_OutputPath", artifactsPath.MakeAbsolute(Context.Environment).ToString());

        msBuildSettings.WithProperty("AlgoPackage_BuildMetadata", buildMetainfo.ToString());
        msBuildSettings.WithProperty("AlgoPackage_Repository", repositoryUri);
        msBuildSettings.WithProperty("AlgoPackage_Author", author);

        BuildProject(msBuildSettings);
    }
    finally
    {
        block?.Dispose();
    }
});


PrintArguments();
RunTarget(target);

public void PrintArguments()
{
    Information("Target: {0}", target);
    Information("BuildNumber: {0}", buildNumber);
    Information("Configuration: {0}", configuration);
    Information("SourcesDir: {0}", sourcesDir);
    Information("BuildDir: {0}", buildDir);
    Information("ArtifactsDirName: {0}", artifactsDirName);
    Information("Details: {0}", details);

    Information("BuildMetadata: {0}", buildMetainfo);
    Information("Repository: {0}", repositoryUri);
    Information("Author: {0}", author);
}

public string ConsoleOrBuildSystemArgument(string name, string defautValue) => ConsoleOrBuildSystemArgument<string>(name, defautValue);

public T ConsoleOrBuildSystemArgument<T>(string name, T defautValue)
{
    if (HasArgument(name))
        return Argument<T>(name);

    if (BuildSystem.IsRunningOnTeamCity
        && TeamCity.Environment.Build.BuildProperties.TryGetValue(name, out var teamCityProperty))
    {
        Information("Found Teamcity property: {0}", name);

        const string envVarName = "env_TempTeamCityProperty";
        Environment.SetEnvironmentVariable(envVarName, teamCityProperty, EnvironmentVariableTarget.Process);
        return EnvironmentVariable<T>(envVarName, defautValue);
    }

    return defautValue;
}

public void BuildProject(DotNetMSBuildSettings msBuildSettings)
{
    DotNetBuild(projectPath.ToString(), new DotNetBuildSettings {
        Configuration = configuration,
        Verbosity = details,
        MSBuildSettings = msBuildSettings,
    });
}