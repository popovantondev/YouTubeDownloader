param([Parameter(Mandatory=$true)][string]$ReferenceDirectory)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Push-Location $taskRoot
try {
 & .\BUILD_ALL.ps1 -ReferenceDirectory $ReferenceDirectory
 $taskArgs=@('/nologo','/noconfig','/nostdlib+','/langversion:3','/platform:x86','/codepage:65001','/target:exe','/define:TESTING','/main:PeakMp3Win7.IntegrationQa','/out:PeakMP3\IntegrationQa.exe')
 foreach($taskLib in @('mscorlib','System','System.Core','System.Drawing','System.Windows.Forms')){$taskArgs+=('/r:'+(Join-Path $ReferenceDirectory ($taskLib+'.dll')))}
 $taskSources=@(Get-ChildItem PeakMP3\SOURCE\*.cs,Shared\*.cs | ForEach-Object {$_.FullName})+@('SOURCE\MiniJson.cs','tests\IntegrationQa.cs')
 & "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe" @taskArgs @taskSources
 if($LASTEXITCODE -ne 0){throw 'Test compilation failed'}
 Copy-Item PeakMP3\PeakMP3.exe.config PeakMP3\IntegrationQa.exe.config
 & .\PeakMP3\IntegrationQa.exe .test-artifacts\native\input.m4a ('.test-artifacts\desktop-'+[Guid]::NewGuid().ToString('N'))
 if($LASTEXITCODE -ne 0){throw 'Desktop checks failed'}
 $taskBridgeArgs=@($taskArgs | Where-Object {!$_.StartsWith('/main:') -and !$_.StartsWith('/out:')})+@('/main:PeakMp3Win7.BridgeHarness','/out:PeakMP3\BridgeHarness.exe')
 & "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe" @taskBridgeArgs @taskSources tests\BridgeHarness.cs
 if($LASTEXITCODE -ne 0){throw 'Bridge harness compilation failed'}
 python tests\test_handoff260.py
 if($LASTEXITCODE -ne 0){throw 'Native-to-desktop handoff failed'}
 $taskExportArgs=@($taskArgs | Where-Object {!$_.StartsWith('/main:') -and !$_.StartsWith('/out:')})+@('/main:PeakMp3Win7.ExportBaseline','/out:PeakMP3\ExportQa.exe')
 & "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe" @taskExportArgs @taskSources PeakMP3\TESTS\ExportBaseline.cs
 if($LASTEXITCODE -ne 0){throw 'Export harness compilation failed'}
 Copy-Item PeakMP3\PeakMP3.exe.config PeakMP3\ExportQa.exe.config
 $taskExport=Join-Path $taskRoot ('.test-artifacts\export-'+[Guid]::NewGuid().ToString('N'))
 foreach($taskLevel in @(0,5,10)){
  & .\PeakMP3\ExportQa.exe (Join-Path $taskRoot '.test-artifacts\native\input.m4a') $taskLevel (Join-Path $taskExport ('level'+$taskLevel)) (Join-Path $taskRoot 'PeakMP3\ffmpeg.exe')
  if($LASTEXITCODE -ne 0){throw ('Peak export failed at density '+$taskLevel)}
 }
} finally {Pop-Location}
