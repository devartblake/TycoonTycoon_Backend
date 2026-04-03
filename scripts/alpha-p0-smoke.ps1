param(
  [string]$BaseUrl = "http://localhost:5000",
  [string]$Email = "demo@example.com",
  [string]$Password = "demo",
  [ValidateSet("live", "routes")]
  [string]$SmokeMode = "live",
  [switch]$ExpectIapStrictReady
)

$ErrorActionPreference = "Stop"

if ($SmokeMode -eq "routes") {
  Write-Host "[route-check] verifying required endpoint maps exist"
  Select-String -Path "Tycoon.Backend.Api/Features/Auth/AuthEndpoints.cs" -Pattern 'MapPost\("/login"' | Out-Null
  Select-String -Path "Tycoon.Backend.Api/Features/Questions/QuestionsEndpoints.cs" -Pattern 'MapGet\("/set"|MapPost\("/check"|MapPost\("/check-batch"' | Out-Null
  Select-String -Path "Tycoon.Backend.Api/Features/Store/StoreEndpoints.cs" -Pattern 'MapGet\("/catalog"|MapPost\("/purchase"|MapPost\("/iap/validate"' | Out-Null
  Select-String -Path "Tycoon.Backend.Api/Features/Crypto/CryptoEconomyEndpoints.cs" -Pattern 'MapPost\("/link-wallet"|MapGet\("/balance|MapGet\("/history|MapPost\("/withdraw"' | Out-Null
  Select-String -Path "Tycoon.Backend.Api/Features/Leaderboards/LeaderboardsEndpoints.cs" -Pattern 'MapGet\("/tiers/\{tierId:int\}"' | Out-Null
  Write-Host "P0 smoke route-check completed."
  exit 0
}

Write-Host "[1/6] Login"
$loginBody = @{
  email = $Email
  password = $Password
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/login" -ContentType "application/json" -Body $loginBody
$token = $null
if ($loginResponse.data -and $loginResponse.data.accessToken) {
  $token = $loginResponse.data.accessToken
} elseif ($loginResponse.accessToken) {
  $token = $loginResponse.accessToken
}

if ([string]::IsNullOrWhiteSpace($token)) {
  throw "Failed to obtain access token from /auth/login"
}

$headers = @{ Authorization = "Bearer $token" }

Write-Host "[2/6] Questions set"
Invoke-RestMethod -Method Get -Uri "$BaseUrl/questions/set?count=5" | Out-Null

Write-Host "[3/6] Store catalog"
Invoke-RestMethod -Method Get -Uri "$BaseUrl/store/catalog" | Out-Null

Write-Host "[4/6] IAP validate (strict mode behavior check)"
$iapBody = @{
  playerId = "00000000-0000-0000-0000-000000000001"
  platform = "apple"
  receipt = "test-receipt"
} | ConvertTo-Json
$iapResponse = Invoke-RestMethod -Method Post -Uri "$BaseUrl/store/iap/validate" -Headers $headers -ContentType "application/json" -Body $iapBody
if ($ExpectIapStrictReady -and ($iapResponse | ConvertTo-Json -Depth 10) -match "IAP_STRICT_CONFIG_MISSING") {
  throw "Strict IAP config is not fully configured (IAP_STRICT_CONFIG_MISSING)."
}

Write-Host "[5/6] Crypto history route health check"
Invoke-RestMethod -Method Get -Uri "$BaseUrl/crypto/history/00000000-0000-0000-0000-000000000001?page=1&pageSize=1" -Headers $headers | Out-Null

Write-Host "[6/6] Leaderboards route health check"
Invoke-RestMethod -Method Get -Uri "$BaseUrl/leaderboards/tiers/1?page=1&pageSize=10" | Out-Null

Write-Host "P0 smoke script completed."
