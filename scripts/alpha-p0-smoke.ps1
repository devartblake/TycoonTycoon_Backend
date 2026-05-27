param(
  [string]$BaseUrl = "http://localhost:5000",
  [string]$Email = "demo@example.com",
  [string]$LoginPassword = "demo",
  [string]$SignupPassword = "Passw0rd!",
  [ValidateSet("live", "routes")]
  [string]$SmokeMode = "live",
  [switch]$AutoSignup = $true,
  [switch]$ExpectIapStrictReady
)

$ErrorActionPreference = "Stop"

if ($SmokeMode -eq "routes") {
  Write-Host "[route-check] verifying required endpoint maps exist"
  Select-String -Path "Synaptix.Backend.Api/Features/Auth/AuthEndpoints.cs" -Pattern 'MapPost\("/login"' | Out-Null
  Select-String -Path "Synaptix.Backend.Api/Features/Questions/QuestionsEndpoints.cs" -Pattern 'MapGet\("/set"|MapPost\("/check"|MapPost\("/check-batch"' | Out-Null
  Select-String -Path "Synaptix.Backend.Api/Features/Store/StoreEndpoints.cs" -Pattern 'MapGet\("/catalog"|MapPost\("/purchase"|MapPost\("/iap/validate"' | Out-Null
  Select-String -Path "Synaptix.Backend.Api/Features/Crypto/CryptoEconomyEndpoints.cs" -Pattern 'MapPost\("/link-wallet"|MapGet\("/balance|MapGet\("/history|MapPost\("/withdraw"' | Out-Null
  Select-String -Path "Synaptix.Backend.Api/Features/Leaderboards/LeaderboardsEndpoints.cs" -Pattern 'MapGet\("/tiers/\{tierId:int\}"' | Out-Null
  Write-Host "P0 smoke route-check completed."
  exit 0
}

if ($AutoSignup) {
  Write-Host "[1/8] Signup"
  $Email = "smoke-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())-$([System.Guid]::NewGuid().ToString('N').Substring(0,6))@example.com"
  $signupBody = @{
    email = $Email
    password = $SignupPassword
    deviceId = "smoke-device-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
    username = "smoke_user_$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
  } | ConvertTo-Json
  $authResponse = Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/signup" -ContentType "application/json" -Body $signupBody
  $playerId = $authResponse.userId
} else {
  Write-Host "[1/8] Login"
  $loginBody = @{
    email = $Email
    password = $LoginPassword
  } | ConvertTo-Json
  $authResponse = Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/login" -ContentType "application/json" -Body $loginBody
  $playerId = "00000000-0000-0000-0000-000000000001"
}

$token = $null
if ($authResponse.data -and $authResponse.data.accessToken) {
  $token = $authResponse.data.accessToken
} elseif ($authResponse.accessToken) {
  $token = $authResponse.accessToken
}

if ([string]::IsNullOrWhiteSpace($token)) {
  throw "Failed to obtain access token from /auth/login"
}

$headers = @{ Authorization = "Bearer $token" }
if ([string]::IsNullOrWhiteSpace($playerId)) {
  $playerId = "00000000-0000-0000-0000-000000000001"
}

Write-Host "[2/8] Questions set"
$setResponse = Invoke-RestMethod -Method Get -Uri "$BaseUrl/questions/set?count=1"
$questionId = $null
if ($setResponse.questions -and $setResponse.questions.Count -gt 0) {
  $questionId = $setResponse.questions[0].id
}

if (-not [string]::IsNullOrWhiteSpace($questionId)) {
  Write-Host "[3/8] Questions check"
  $checkBody = @{
    questionId = $questionId
    selectedIndex = 0
  } | ConvertTo-Json
  Invoke-RestMethod -Method Post -Uri "$BaseUrl/questions/check" -ContentType "application/json" -Body $checkBody | Out-Null
} else {
  Write-Host "[3/8] Questions check skipped (no question returned)"
}

Write-Host "[4/8] Store catalog"
Invoke-RestMethod -Method Get -Uri "$BaseUrl/store/catalog" | Out-Null

Write-Host "[5/8] IAP validate (strict mode behavior check)"
$iapBody = @{
  playerId = $playerId
  platform = "apple"
  receipt = "test-receipt"
} | ConvertTo-Json
$iapResponse = Invoke-RestMethod -Method Post -Uri "$BaseUrl/store/iap/validate" -Headers $headers -ContentType "application/json" -Body $iapBody
if ($ExpectIapStrictReady -and ($iapResponse | ConvertTo-Json -Depth 10) -match "IAP_STRICT_CONFIG_MISSING") {
  throw "Strict IAP config is not fully configured (IAP_STRICT_CONFIG_MISSING)."
}

Write-Host "[6/8] Store purchase contract check"
$purchaseBody = @{
  playerId = $playerId
  sku = "coins_pack_small"
  quantity = 1
  currency = "coins"
} | ConvertTo-Json
try {
  Invoke-RestMethod -Method Post -Uri "$BaseUrl/store/purchase" -Headers $headers -ContentType "application/json" -Body $purchaseBody | Out-Null
} catch {
  $statusCode = $_.Exception.Response.StatusCode.value__
  if ($statusCode -notin @(400,404,409)) {
    throw
  }
}

Write-Host "[7/8] Crypto history route health check"
Invoke-RestMethod -Method Get -Uri "$BaseUrl/crypto/history/${playerId}?page=1&pageSize=1" -Headers $headers | Out-Null

Write-Host "[8/8] Leaderboards route health check"
Invoke-RestMethod -Method Get -Uri "$BaseUrl/leaderboards/tiers/1?page=1&pageSize=10" | Out-Null

Write-Host "P0 smoke script completed."
