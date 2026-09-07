using System;
using MapleClient.GameData;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using UnityEditor;
using UnityEngine;

public static class CustomSkillExamples
{
    [MenuItem("MapleUnity/Skills/Create Example Assets")]
    public static void Create()
    {
        const string folder = "Assets/SkillExamples";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "SkillExamples");
        if (AssetDatabase.LoadAssetAtPath<CustomSkillAsset>(folder + "/ArcBolt.asset") == null)
        {
            var a = ScriptableObject.CreateInstance<CustomSkillAsset>();
            a.Id = 100000001; a.DisplayName = "Arc Bolt (Example)";
            a.Description = "Two magic projectiles. Uses an explicit job and fixed base damage, with original Magic Bolt presentation.";
            a.JobId = 200; a.Execution = SkillExecution.Attack; a.AttackFamily = SkillAttackFamily.Magic;
            a.DamagePolicy = SkillDamagePolicy.FixedMagic; a.CastEffects = Array.Empty<string>();
            a.SourcePresentationSkillId = 2001004; a.Ranks[0].Damage = 18; a.Ranks[0].AttackCount = 2;
            a.Ranks[0].MpCost = 8; a.Ranks[0].CooldownMilliseconds = 1000;
            AssetDatabase.CreateAsset(a, folder + "/ArcBolt.asset");
        }
        if (AssetDatabase.LoadAssetAtPath<CustomSkillAsset>(folder + "/FieldFocus.asset") == null)
        {
            var a = ScriptableObject.CreateInstance<CustomSkillAsset>();
            a.Id = 100000002; a.DisplayName = "Field Focus (Example)";
            a.Description = "Restores 12 HP and grants 7 magic attack for 10 seconds. Composes recovery and a timed buff.";
            a.SourcePresentationSkillId = 2001002; a.CastEffects = new[] { SkillCastEffects.StatBuff, SkillCastEffects.Recovery };
            a.Ranks[0].MpCost = 4; a.Ranks[0].HealHp = 12; a.Ranks[0].DurationMilliseconds = 10000;
            a.Ranks[0].CooldownMilliseconds = 2000;
            a.Ranks[0].Buffs = new[] { new CustomSkillAsset.Buff { Type = BuffType.MagicAttack, Value = 7 } };
            AssetDatabase.CreateAsset(a, folder + "/FieldFocus.asset");
        }
        AssetDatabase.SaveAssets(); Debug.Log("CUSTOM_SKILL_EXAMPLES_CREATED");
    }
}
