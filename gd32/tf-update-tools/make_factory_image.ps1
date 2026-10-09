param(
    [string]$Output = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$bootPath = Join-Path $root 'bootloader\Output\GD_BOOT_DEV\GD_BOOT_DEV.bin'
$appPath = Join-Path $root 'AppOutput\IAP_A\IAP_A.bin'

if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $root 'Factory\AnimalRace_FACTORY_DEV.bin'
}

$boot = [IO.File]::ReadAllBytes($bootPath)
$app = [IO.File]::ReadAllBytes($appPath)
if ($boot.Length -gt 0xC000) { throw 'Bootloader exceeds the 48 KiB boot region.' }
if ($app.Length -gt 0x20000) { throw 'Application A exceeds its 128 KiB slot.' }

$image = New-Object byte[] (0x20000 + $app.Length)
for ($index = 0; $index -lt $image.Length; $index++) {
    $image[$index] = 0xFF
}
[Array]::Copy($boot, 0, $image, 0, $boot.Length)
[Array]::Copy($app, 0, $image, 0x20000, $app.Length)

$directory = Split-Path -Parent $Output
[IO.Directory]::CreateDirectory($directory) | Out-Null
[IO.File]::WriteAllBytes($Output, $image)
Write-Host "Factory image: $Output"
Write-Host ("Size: {0} bytes" -f $image.Length)
