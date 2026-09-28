param([switch]$Publish)
$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try
{
	[xml]$buildProperties=Get-Content -Raw Directory.Build.props
	$version=[string]$buildProperties.Project.PropertyGroup.Version
	$releaseName="SDS1102CML-Viewer-$version-win-x64"
	dotnet run --project tests/Scope.Tests -c Release
	if ($LASTEXITCODE -ne 0) { throw 'Testy nie powiodły się.' }
	dotnet build src/Scope.App -c Release
	if ($LASTEXITCODE -ne 0) { throw 'Kompilacja nie powiodła się.' }
	if ($Publish)
	{
		dotnet publish src/Scope.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o "artifacts/$releaseName"
		if ($LASTEXITCODE -ne 0) { throw 'Publikowanie nie powiodło się.' }
		Copy-Item -LiteralPath LICENSE,README.md -Destination "artifacts/$releaseName"
		$dotnetRoot=Split-Path (Get-Command dotnet).Source
		Copy-Item -LiteralPath (Join-Path $dotnetRoot 'LICENSE.txt') -Destination "artifacts/$releaseName/DOTNET-LICENSE.txt"
		Copy-Item -LiteralPath (Join-Path $dotnetRoot 'ThirdPartyNotices.txt') -Destination "artifacts/$releaseName/DOTNET-ThirdPartyNotices.txt"
		Compress-Archive -Path "artifacts/$releaseName" -DestinationPath "artifacts/$releaseName.zip" -Force
	}
}
finally { Pop-Location }
