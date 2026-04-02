param(
  [string]$BaseUrl = "http://localhost:5000",
  [string]$Email = "demo@example.com",
  [SecureString]$Password = "demo"
)

$ErrorActionPreference = "Stop"

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
Invoke-RestMethod -Method Post -Uri "$BaseUrl/store/iap/validate" -Headers $headers -ContentType "application/json" -Body $iapBody | Out-Null

Write-Host "[5/6] Crypto history route health check"
Invoke-RestMethod -Method Get -Uri "$BaseUrl/crypto/history/00000000-0000-0000-0000-000000000001?page=1&pageSize=1" -Headers $headers | Out-Null

Write-Host "[6/6] Leaderboards route health check"
Invoke-RestMethod -Method Get -Uri "$BaseUrl/leaderboards/tiers/1?page=1&pageSize=10" | Out-Null

Write-Host "P0 smoke script completed."