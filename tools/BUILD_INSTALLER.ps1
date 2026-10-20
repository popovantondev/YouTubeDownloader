param([Parameter(Mandatory=$true)][string]$Compiler)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$taskRuntime=Split-Path -Parent $taskRoot
$taskStaging=Join-Path $taskRoot '.payload'
foreach($taskName in Get-Content -LiteralPath (Join-Path $taskRuntime 'install-files.txt')){
 if($taskName -match '(^|[/\\])\.\.([/\\]|$)' -or [IO.Path]::IsPathRooted($taskName)){throw 'Invalid runtime manifest'}
 $taskSource=Join-Path $taskRuntime $taskName
 $taskCopy=Join-Path $taskRoot $taskName
 if(!(Test-Path -LiteralPath $taskCopy)){
  New-Item -ItemType Directory -Force -Path (Split-Path -Parent $taskCopy) | Out-Null
  Copy-Item -LiteralPath $taskSource -Destination $taskCopy
 }
 $taskDest=Join-Path $taskStaging $taskName
 New-Item -ItemType Directory -Force -Path (Split-Path -Parent $taskDest) | Out-Null
 Copy-Item -LiteralPath $taskCopy -Destination $taskDest
}
$taskSha=[Security.Cryptography.SHA256]::Create()
try {
 $taskLines=@()
 foreach($taskName in Get-Content -LiteralPath (Join-Path $taskStaging 'install-files.txt')){
  if($taskName -eq 'runtime-SHA256SUMS.txt'){continue}
  $taskStream=[IO.File]::OpenRead((Join-Path $taskStaging $taskName))
  try {$taskHash=[BitConverter]::ToString($taskSha.ComputeHash($taskStream)).Replace('-','').ToLowerInvariant()}finally{$taskStream.Dispose()}
  $taskLines+=($taskHash+'  '+$taskName)
 }
 [IO.File]::WriteAllLines((Join-Path $taskStaging 'runtime-SHA256SUMS.txt'),[string[]]$taskLines,[Text.Encoding]::ASCII)
}finally{$taskSha.Dispose()}
& $Compiler (Join-Path $taskRoot 'installer\YouTubeDownloader.iss')
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed'}
