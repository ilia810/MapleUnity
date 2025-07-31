$unityPath = "C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe"
$projectPath = "C:\Users\me\MapleUnity"
$logFile = "$projectPath\character-direct-test.log"

Write-Host "Running Unity character test..."

# Create a simple test script
$testScript = @'
using UnityEngine;
using UnityEditor;
using System.IO;

public static class DirectTest
{
    public static void Run()
    {
        Debug.Log("=== DIRECT CHARACTER TEST ===");
        
        try
        {
            // Create character
            GameObject character = new GameObject("TestCharacter");
            
            // Try to find and add MapleCharacterRenderer
            var rendererType = System.Type.GetType("MapleClient.GameView.MapleCharacterRenderer");
            if (rendererType != null)
            {
                var renderer = character.AddComponent(rendererType);
                Debug.Log("Added MapleCharacterRenderer");
                
                // Force initialization
                var startMethod = rendererType.GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(renderer, null);
                }
                
                // Analyze children
                var children = character.GetComponentsInChildren<Transform>();
                Debug.Log($"Total children: {children.Length}");
                
                foreach (var child in children)
                {
                    if (child != character.transform)
                    {
                        Debug.Log($"  {child.name}: Y={child.localPosition.y}");
                        
                        var sr = child.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            Debug.Log($"    flipX={sr.flipX}, order={sr.sortingOrder}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("MapleCharacterRenderer type not found!");
            }
            
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
}
'@

# Save test script
$testScriptPath = "$projectPath\Assets\Scripts\Editor\DirectTest.cs"
$testScript | Out-File -FilePath $testScriptPath -Encoding UTF8

Write-Host "Created test script at: $testScriptPath"

# Run Unity
$arguments = @(
    "-batchmode",
    "-nographics", 
    "-noHub",
    "-ignoreCompilerErrors",
    "-silent-crashes",
    "-projectPath", "`"$projectPath`"",
    "-executeMethod", "DirectTest.Run",
    "-quit",
    "-logFile", "`"$logFile`""
)

Write-Host "Running Unity with arguments:"
Write-Host ($arguments -join " ")

$process = Start-Process -FilePath $unityPath -ArgumentList $arguments -PassThru -NoNewWindow -Wait

Write-Host "Unity exit code: $($process.ExitCode)"

# Show log file
if (Test-Path $logFile) {
    Write-Host "`nLog file contents:"
    Get-Content $logFile | Select-Object -Last 100
} else {
    Write-Host "Log file not created!"
}

# Clean up test script
Remove-Item $testScriptPath -ErrorAction SilentlyContinue