param([Parameter(Mandatory=$true)][string]$ReferenceDirectory)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $MyInvocation.MyCommand.Path
$taskRefs=(Resolve-Path -LiteralPath $ReferenceDirectory).Path
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$taskReferences=@()
foreach($taskLibrary in @('mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll')){
 $taskPath=Join-Path $taskRefs $taskLibrary
 if(!(Test-Path -LiteralPath $taskPath)){throw ('Missing .NET3.5 reference: '+$taskLibrary)}
 $taskReferences+=('/r:'+$taskPath)
}
function Build-Component($taskOutput,$taskKind,$taskFiles,$taskIcon,$taskManifest){
 $taskArgs=@('/nologo','/noconfig','/nostdlib+','/langversion:3','/platform:x86','/optimize+','/codepage:65001',('/target:'+$taskKind),('/out:'+(Join-Path $taskRoot $taskOutput)))+$taskReferences
 if($taskIcon){$taskArgs+=('/win32icon:'+(Join-Path $taskRoot $taskIcon))}
 if($taskManifest){$taskArgs+=('/win32manifest:'+(Join-Path $taskRoot $taskManifest))}
 & $taskCompiler @taskArgs @taskFiles
 if($LASTEXITCODE -ne 0){throw ('Compilation failed: '+$taskOutput)}
 Write-Output ('Built x86 CLR2: '+$taskOutput)
}
$taskShared=Get-ChildItem -LiteralPath (Join-Path $taskRoot 'Shared') -Filter '*.cs' | ForEach-Object {$_.FullName}
$taskNative=Get-ChildItem -LiteralPath (Join-Path $taskRoot 'SOURCE') -Filter '*.cs' | ForEach-Object {$_.FullName}
Build-Component 'YTWin7SabrHost.exe' 'exe' @($taskNative+$taskShared) $null $null
$taskPeak=Get-ChildItem -LiteralPath (Join-Path $taskRoot 'PeakMP3\SOURCE') -Filter '*.cs' | ForEach-Object {$_.FullName}
Build-Component 'PeakMP3\PeakMP3.exe' 'winexe' @($taskPeak+$taskShared+(Join-Path $taskRoot 'SOURCE\MiniJson.cs')) 'PeakMP3\ASSETS\PeakMP3.ico' 'PeakMP3\SOURCE\app.manifest'
if(Test-Path -LiteralPath (Join-Path $taskRoot 'installer\ConnectionWizard.cs')){
 Build-Component 'BrowserSetup.exe' 'winexe' @((Join-Path $taskRoot 'installer\ConnectionWizard.cs')) 'installer\download.ico' 'PeakMP3\SOURCE\app.manifest'
}

Build-Component 'InstallerEngine.exe' 'exe' @((Join-Path $taskRoot 'installer\InstallEngine.cs'),(Join-Path $taskRoot 'SOURCE\IntegrationRegistration.cs'),(Join-Path $taskRoot 'SOURCE\MiniJson.cs')) $null $null
