$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$taskSha = [Security.Cryptography.SHA256]::Create()
try {
 foreach ($taskLine in Get-Content -LiteralPath (Join-Path $taskRoot 'SHA256SUMS.txt')) {
  if ($taskLine -notmatch '^([a-f0-9]{64})  (.+)$') { throw 'Invalid checksum entry' }
  $taskExpected = $Matches[1]
  $taskFile = [IO.Path]::GetFullPath((Join-Path $taskRoot $Matches[2].Replace('/','\')))
  $taskBoundary = [IO.Path]::GetFullPath($taskRoot).TrimEnd('\') + '\'
  if (-not $taskFile.StartsWith($taskBoundary,[StringComparison]::OrdinalIgnoreCase)) { throw 'Path outside package' }
  $taskStream = [IO.File]::OpenRead($taskFile)
  try { $taskActual = [BitConverter]::ToString($taskSha.ComputeHash($taskStream)).Replace('-','').ToLowerInvariant() } finally { $taskStream.Dispose() }
  if ($taskActual -ne $taskExpected) { throw ('Checksum mismatch: ' + $taskFile) }
 }
 Write-Host 'SUCCESS: All included files match SHA256SUMS.'
} finally { $taskSha.Dispose() }
