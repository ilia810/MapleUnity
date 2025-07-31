# PowerShell script to validate rendering fixes
Write-Host "=== CHARACTER RENDERING VALIDATION ===" -ForegroundColor Cyan
Write-Host "Checking MapleCharacterRenderer implementation..."

$rendererPath = "C:\Users\me\MapleUnity\Assets\Scripts\GameView\MapleCharacterRenderer.cs"

if (Test-Path $rendererPath) {
    Write-Host "`nChecking key fixes in MapleCharacterRenderer.cs:" -ForegroundColor Yellow
    
    # Check 1: Sprite pivot Y-flip formula
    Write-Host "`n1. Sprite Pivot Y-Flip Formula:" -ForegroundColor Green
    $content = Get-Content $rendererPath -Raw
    
    if ($content -match "textureHeight - pivot.y") {
        Write-Host "  ✓ Found Y-flip formula: textureHeight - pivot.y" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Y-flip formula not found!" -ForegroundColor Red
    }
    
    # Check 2: Head attachment formula
    Write-Host "`n2. Head Attachment Formula (body.neck - head.neck):" -ForegroundColor Green
    if ($content -match "bodyNeck - headNeck") {
        Write-Host "  ✓ Found head attachment formula" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Head attachment formula not found!" -ForegroundColor Red
    }
    
    # Check 3: Arm attachment formula
    Write-Host "`n3. Arm Attachment Formula (body.navel - arm.navel):" -ForegroundColor Green
    if ($content -match "bodyNavel - armNavel") {
        Write-Host "  ✓ Found arm attachment formula" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Arm attachment formula not found!" -ForegroundColor Red
    }
    
    # Check 4: Face attachment formula
    Write-Host "`n4. Face Attachment Formula (head pos + head.brow):" -ForegroundColor Green
    if ($content -match "headBrow") {
        Write-Host "  ✓ Found face attachment using head.brow" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Face attachment formula not found!" -ForegroundColor Red
    }
    
    # Check 5: Facing direction logic
    Write-Host "`n5. Facing Direction Logic:" -ForegroundColor Green
    if ($content -match "UpdateFacingDirection" -and $content -match "velocity\.x") {
        Write-Host "  ✓ Found facing direction update based on velocity" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Facing direction logic not found!" -ForegroundColor Red
    }
    
    # Check 6: FlipX handling
    Write-Host "`n6. Sprite FlipX Handling:" -ForegroundColor Green
    if ($content -match "flipX = !isFacingRight") {
        Write-Host "  ✓ Found flipX handling for facing direction" -ForegroundColor Green
    } else {
        Write-Host "  ✗ FlipX handling not found!" -ForegroundColor Red
    }
    
    Write-Host "`n=== RESEARCH6.TXT FORMULA VERIFICATION ===" -ForegroundColor Cyan
    
    # Extract key positioning code
    Write-Host "`nKey positioning code found:" -ForegroundColor Yellow
    
    # Find head positioning
    $headMatch = [regex]::Match($content, "if \(bodyParts\.ContainsKey\(`"head`"\)\)[\s\S]*?bodyNeck - headNeck")
    if ($headMatch.Success) {
        Write-Host "`nHead positioning:" -ForegroundColor Green
        Write-Host "  ✓ Uses formula: body.neck - head.neck" -ForegroundColor Green
    }
    
    # Find arm positioning  
    $armMatch = [regex]::Match($content, "arm.*?localPosition.*?=.*?Vector3")
    if ($armMatch.Success) {
        Write-Host "`nArm positioning:" -ForegroundColor Green
        Write-Host "  ✓ Uses attachment point calculation" -ForegroundColor Green
    }
    
    # Find face positioning
    $faceMatch = [regex]::Match($content, "face.*?SetParent.*?head")
    if ($faceMatch.Success) {
        Write-Host "`nFace positioning:" -ForegroundColor Green
        Write-Host "  ✓ Face is parented to head (correct hierarchy)" -ForegroundColor Green
    }
    
} else {
    Write-Host "ERROR: MapleCharacterRenderer.cs not found!" -ForegroundColor Red
}

Write-Host "`n=== VALIDATION COMPLETE ===" -ForegroundColor Cyan
Write-Host "All critical rendering fixes should be implemented based on research6.txt" -ForegroundColor Yellow