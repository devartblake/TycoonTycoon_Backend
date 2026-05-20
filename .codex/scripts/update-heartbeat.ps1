param(
  [Parameter(Mandatory=$true)]
  [string]$TaskId,

  [Parameter(Mandatory=$true)]
  [string]$Command,

  [Parameter(Mandatory=$true)]
  [ValidateSet("pass", "fail", "skipped", "not-run")]
  [string]$Result,

  [Parameter(Mandatory=$false)]
  [string]$Notes = ""
)

$LogPath = ".codex/heartbeat/verification-log.md"
$Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm zzz"
$EscapedCommand = $Command.Replace("|", "\|")
$EscapedNotes = $Notes.Replace("|", "\|")
$Line = "| $Timestamp | $TaskId | `$EscapedCommand` | $Result | $EscapedNotes |"

if (!(Test-Path $LogPath)) {
  New-Item -ItemType File -Path $LogPath -Force | Out-Null
  Add-Content -Path $LogPath -Value "# Verification Log`n"
}

Add-Content -Path $LogPath -Value $Line
Write-Host "Heartbeat verification entry added to $LogPath"
