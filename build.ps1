param([switch]$Publish)
$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try
{
	dotnet run --project tests/Scope.Tests -c Release
	if ($LASTEXITCODE -ne 0) { throw 'Testy nie powiodły się.' }
	dotnet build src/Scope.App -c Release
	if ($LASTEXITCODE -ne 0) { throw 'Kompilacja nie powiodła się.' }
	if ($Publish)
	{
		dotnet publish src/Scope.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o artifacts/SDS1102CML-Viewer-0.1.0-win-x64
		if ($LASTEXITCODE -ne 0) { throw 'Publikowanie nie powiodło się.' }
		Copy-Item -LiteralPath LICENSE,README.md -Destination artifacts/SDS1102CML-Viewer-0.1.0-win-x64
		$dotnetRoot=Split-Path (Get-Command dotnet).Source
		Copy-Item -LiteralPath (Join-Path $dotnetRoot 'LICENSE.txt') -Destination artifacts/SDS1102CML-Viewer-0.1.0-win-x64/DOTNET-LICENSE.txt
		Copy-Item -LiteralPath (Join-Path $dotnetRoot 'ThirdPartyNotices.txt') -Destination artifacts/SDS1102CML-Viewer-0.1.0-win-x64/DOTNET-ThirdPartyNotices.txt
		Compress-Archive -Path artifacts/SDS1102CML-Viewer-0.1.0-win-x64 -DestinationPath artifacts/SDS1102CML-Viewer-0.1.0-win-x64.zip -Force
	}
}
finally { Pop-Location }
