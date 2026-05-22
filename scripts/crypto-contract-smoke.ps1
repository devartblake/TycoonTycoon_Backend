param(
  [string]$BaseUrl = "http://localhost:5000",
  [string]$Email = "",
  [string]$Password = "",
  [switch]$AutoSignup = $true
)

$ErrorActionPreference = "Stop"

function Get-ErrorBody($ErrorRecord) {
  $stream = $ErrorRecord.Exception.Response.GetResponseStream()
  if (-not $stream) { return "" }
  $reader = New-Object System.IO.StreamReader($stream)
  return $reader.ReadToEnd()
}

$BaseUrl = $BaseUrl.TrimEnd("/")

if ($AutoSignup) {
  $stamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
  $Email = "crypto-smoke-$stamp@synaptix.local"
  $Password = "CryptoSmoke123!"
  $authBody = @{
    email = $Email
    password = $Password
    deviceId = "crypto-smoke-$stamp"
    username = "cryptosmoke$stamp"
  } | ConvertTo-Json -Compress

  Write-Host "[1/7] Signup disposable crypto smoke user"
  $authResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/signup" -Method Post -ContentType "application/json" -Body $authBody -TimeoutSec 20
} else {
  if ([string]::IsNullOrWhiteSpace($Email) -or [string]::IsNullOrWhiteSpace($Password)) {
    throw "Email and Password are required when AutoSignup is disabled."
  }

  Write-Host "[1/7] Login crypto smoke user"
  $authBody = @{
    email = $Email
    password = $Password
    deviceId = "crypto-smoke-login"
  } | ConvertTo-Json -Compress
  $authResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -ContentType "application/json" -Body $authBody -TimeoutSec 20
}

$token = $authResponse.accessToken
if (-not $token -and $authResponse.data) { $token = $authResponse.data.accessToken }
if ([string]::IsNullOrWhiteSpace($token)) {
  throw "Auth response did not include an access token."
}

$playerId = [string]$authResponse.userId
if ([string]::IsNullOrWhiteSpace($playerId) -and $authResponse.user) {
  $playerId = [string]$authResponse.user.id
}
if ([string]::IsNullOrWhiteSpace($playerId)) {
  throw "Auth response did not include a player/user id."
}

$headers = @{ Authorization = "Bearer $token" }

Write-Host "[2/7] GET /crypto/balance/$playerId"
$balance = Invoke-RestMethod -Uri "$BaseUrl/crypto/balance/${playerId}" -Headers $headers -TimeoutSec 20
if ($balance.playerId -ne $playerId -or $balance.unitType -ne "CRYPTO_UNITS") {
  throw "Crypto balance response contract mismatch."
}

Write-Host "[3/7] GET /crypto/history/$playerId"
$history = Invoke-RestMethod -Uri "$BaseUrl/crypto/history/${playerId}?page=1&pageSize=5" -Headers $headers -TimeoutSec 20
if ($history.page -ne 1 -or $history.pageSize -ne 5 -or $null -eq $history.items) {
  throw "Crypto history response contract mismatch."
}

Write-Host "[4/7] GET /crypto/staking/$playerId"
$staking = Invoke-RestMethod -Uri "$BaseUrl/crypto/staking/${playerId}" -Headers $headers -TimeoutSec 20
if ($staking.playerId -ne $playerId -or $staking.unitType -ne "CRYPTO_UNITS") {
  throw "Crypto staking response contract mismatch."
}

Write-Host "[5/7] GET /crypto/prize-pool/global"
$pool = Invoke-RestMethod -Uri "$BaseUrl/crypto/prize-pool/global" -Headers $headers -TimeoutSec 20
if ($pool.poolId -ne "global" -or $pool.unitType -ne "CRYPTO_UNITS") {
  throw "Crypto prize-pool response contract mismatch."
}

Write-Host "[6/7] POST /crypto/link-wallet secure-channel guard"
$linkBody = @{
  playerId = $playerId
  walletAddress = "7EcDhSYGxXyscszYEp35KHN8vvw3svAuLKTzXwCFLtV"
  network = "solana"
} | ConvertTo-Json -Compress
try {
  Invoke-RestMethod -Uri "$BaseUrl/crypto/link-wallet" -Method Post -Headers $headers -ContentType "application/json" -Body $linkBody -TimeoutSec 20 | Out-Null
  throw "Expected /crypto/link-wallet to require secure-channel headers."
} catch {
  $statusCode = [int]$_.Exception.Response.StatusCode
  $body = Get-ErrorBody $_
  if ($statusCode -ne 400 -or $body -notmatch "secure_session_required") {
    throw "Expected secure_session_required from /crypto/link-wallet, got status $statusCode body $body"
  }
}

Write-Host "[7/7] Crypto contract smoke complete"
[ordered]@{
  baseUrl = $BaseUrl
  email = $Email
  playerId = $playerId
  balanceUnits = $balance.units
  historyTotal = $history.total
  stakingAvailableUnits = $staking.availableUnits
  stakingStakedUnits = $staking.stakedUnits
  prizePoolUnits = $pool.units
  secureWriteGuard = "secure_session_required"
} | ConvertTo-Json -Depth 4
