$packFolder = (Get-Item -Path "./" -Verbose).FullName
$rootFolder = Join-Path $packFolder "../"
$projects = (
	"Async.Analyzers"
)

$apiKey = $args[0]

# Get the version
[xml]$csprojXml = Get-Content (Join-Path $rootFolder "Async.Analyzers\Async.Analyzers\Async.Analyzers.csproj")
$version = $csprojXml.Project.PropertyGroup.Version

# Publish all packages
foreach($project in $projects) {
    $projectName = $project.Substring($project.LastIndexOf("/") + 1)
    & dotnet nuget push ($projectName + "." + $version + ".nupkg") -s https://api.nuget.org/v3/index.json --api-key "$apiKey"
}

# Go back to the pack folder
Set-Location $packFolder
