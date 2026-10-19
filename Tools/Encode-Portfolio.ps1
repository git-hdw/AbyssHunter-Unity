param(
    [string]$RawDirectory = "$PSScriptRoot/../Recordings/Presentation/raw",
    [string]$OutputFile = "$PSScriptRoot/../Recordings/Presentation/AbyssHunter-Gameplay.mp4",
    [string]$FFmpeg = "ffmpeg"
)
$ErrorActionPreference = 'Stop'
$captureRoot = (Resolve-Path -LiteralPath $RawDirectory).Path
$captureReport = Get-Content -LiteralPath (Join-Path $captureRoot 'report.txt') -Raw
if ($captureReport -notmatch 'DONE:' -or $captureReport -match 'ERROR:') {
    throw 'Capture must finish without runtime errors before encoding.'
}
$frameFiles = @(Get-ChildItem -LiteralPath $captureRoot -Filter 'frame-*.jpg')
$duration = $frameFiles.Count / 30.0
$fadeStart = [Math]::Max(0, $duration - 0.7).ToString('0.###', [Globalization.CultureInfo]::InvariantCulture)
$outputPath = [IO.Path]::GetFullPath($OutputFile)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($outputPath)) | Out-Null
& $FFmpeg -hide_banner -y -framerate 30 -i (Join-Path $captureRoot 'frame-%06d.jpg') -i (Join-Path $captureRoot 'audio.wav') `
    -vf "scale=in_range=pc:out_range=tv:out_color_matrix=bt709,fade=t=in:st=0:d=0.4,fade=t=out:st=${fadeStart}:d=0.7" `
    -c:v libx264 -preset slow -crf 20 -pix_fmt yuv420p `
    -color_primaries bt709 -color_trc bt709 -colorspace bt709 -color_range tv `
    -c:a aac -b:a 160k -ar 48000 -af "loudnorm=I=-18:TP=-1.5:LRA=11,afade=t=out:st=${fadeStart}:d=0.7" `
    -metadata 'title=Abyss Hunter - Unity Gameplay Demo' -movflags +faststart -shortest $outputPath
if ($LASTEXITCODE -ne 0) { throw "FFmpeg failed with exit code $LASTEXITCODE" }
Write-Output $outputPath
