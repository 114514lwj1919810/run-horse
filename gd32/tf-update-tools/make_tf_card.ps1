param(
    [string]$Output = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$python = (Get-Command python -ErrorAction Stop).Source
$packager = Join-Path $root 'tools\universal_iap_packager.py'
$appA = Join-Path $root 'AppOutput\IAP_A\IAP_A.bin'
$appB = Join-Path $root 'AppOutput\IAP_B\IAP_B.bin'
$registry = Join-Path $root '产品表\devices.csv'
$privateKey = Join-Path $root '产品表\签名密钥\manufacturer_ed25519_private.pem'

if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $root 'TF_CARD'
}

$arguments = @(
    $packager,
    '--app-a', $appA,
    '--app-b', $appB,
    '--registry', $registry,
    '--private-key', $privateKey,
    '--output', $Output
)

& $python @arguments
if ($LASTEXITCODE -ne 0) {
    throw "TF card packaging failed with exit code $LASTEXITCODE"
}

Write-Host "TF card output: $Output"
