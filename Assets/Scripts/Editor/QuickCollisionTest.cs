using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public static class QuickCollisionTest
{
    public static void RunTest()
    {
        var startTime = DateTime.Now;
        string log = $"[QUICK_TEST] Start: {startTime}\n";
        
        try
        {
            // Skip scene generation - just test the collision math directly
            var footholdService = new MapleClient.GameLogic.FootholdService();
            
            // Load test footholds (main platform from -5000 to 5000 at Y=200)
            var footholds = new System.Collections.Generic.List<MapleClient.GameLogic.Foothold>
            {
                new MapleClient.GameLogic.Foothold
                {
                    Id = 1,
                    X1 = -5000,
                    Y1 = 200,
                    X2 = 5000,
                    Y2 = 200
                }
            };
            
            footholdService.LoadFootholds(footholds);
            
            // Test ground detection at various X positions
            bool allPassed = true;
            
            // Test center
            float ground = footholdService.GetGroundBelow(0, 100);
            if (ground != 199) // Should return ground-1
            {
                log += $"[QUICK_TEST] FAIL: Ground at X=0 is {ground}, expected 199\n";
                allPassed = false;
            }
            
            // Test extended boundaries
            ground = footholdService.GetGroundBelow(4900, 100);
            if (ground != 199)
            {
                log += $"[QUICK_TEST] FAIL: Ground at X=4900 is {ground}, expected 199\n";
                allPassed = false;
            }
            
            ground = footholdService.GetGroundBelow(-4900, 100);
            if (ground != 199)
            {
                log += $"[QUICK_TEST] FAIL: Ground at X=-4900 is {ground}, expected 199\n";
                allPassed = false;
            }
            
            // Test outside boundaries
            ground = footholdService.GetGroundBelow(5100, 100);
            if (ground != float.MaxValue)
            {
                log += $"[QUICK_TEST] FAIL: Ground at X=5100 is {ground}, expected MaxValue\n";
                allPassed = false;
            }
            
            var duration = (DateTime.Now - startTime).TotalSeconds;
            log += $"[QUICK_TEST] Duration: {duration:F2}s\n";
            log += $"[QUICK_TEST] Result: {(allPassed ? "PASS" : "FAIL")}\n";
            
            File.WriteAllText(@"C:\Users\me\MapleUnity\debug-log.txt", log);
            
            EditorApplication.Exit(allPassed ? 0 : 1);
        }
        catch (Exception e)
        {
            log += $"[QUICK_TEST] ERROR: {e.Message}\n";
            File.WriteAllText(@"C:\Users\me\MapleUnity\debug-log.txt", log);
            EditorApplication.Exit(1);
        }
    }
}