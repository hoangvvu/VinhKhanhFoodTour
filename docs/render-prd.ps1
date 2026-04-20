# Render all Mermaid diagrams in PRD.md to PNG + SVG images.
# Usage (run from repo root):
#     pwsh -File docs/render-prd.ps1

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

if (-not (Get-Command mmdc -ErrorAction SilentlyContinue)) {
    Write-Host "mmdc not found. Installing @mermaid-js/mermaid-cli globally..."
    npm install -g "@mermaid-js/mermaid-cli"
}

$outDir = "docs/diagrams"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }

Write-Host "Rendering PNG..."
mmdc -i PRD.md -o "$outDir/PRD.Rendered.md" `
    -t default -b white `
    -c docs/mmdc-config.json `
    -p docs/puppeteer-config.json `
    --width 1600 --scale 2 -e png

Write-Host "Rendering SVG..."
mmdc -i PRD.md -o "$outDir/PRD.Rendered.md" `
    -t default -b white `
    -c docs/mmdc-config.json `
    -p docs/puppeteer-config.json `
    --width 1600 --scale 2 -e svg

Write-Host "Done. Output in $outDir" -ForegroundColor Green
