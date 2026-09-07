using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameView;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class FaceAnimationSceneSmokeTests
    {
        private readonly List<string> diagnostics = new List<string>();
        [SetUp] public void WatchLogs() { diagnostics.Clear(); Application.logMessageReceived += OnLog; }
        private void OnLog(string message, string stack, LogType type) { if (type != LogType.Log) diagnostics.Add(type + ": " + message); }
        [TearDown] public void CheckLogs() { Application.logMessageReceived -= OnLog; Assert.That(diagnostics, Is.Empty); }
        private sealed class Input : IInputProvider, IExpressionInputProvider
        {
            public bool IsLeftPressed { get; set; } public bool IsRightPressed { get; set; }
            public bool IsUpPressed { get; set; } public bool IsDownPressed { get; set; }
            public bool IsJumpPressed { get; set; } public bool IsAttackPressed { get; set; }
            public CharacterExpression? ExpressionPressed { get; set; }
        }
        private static void Step(GameWorld w, int count = 1) { for (int i = 0; i < count; i++) { w.ProcessInput(); w.UpdatePhysics(.008f); } }
        private static Vector3 Brow(SpriteRenderer layer, INxNode node)
        {
            Vector2 pixel = node["origin"].GetValue<Vector2>() + (node["map"]?["brow"]?.GetValue<Vector2>() ?? Vector2.zero);
            var sprite = layer.sprite;
            return layer.transform.TransformPoint(new Vector3((pixel.x-sprite.pivot.x)/100,
                (sprite.rect.height-pixel.y-sprite.pivot.y)/100, 0));
        }
        private static void AssertFace(GameWorld w, SpriteRenderer face, SpriteRenderer head)
        {
            var p = w.Player; float alpha = w.GetPhysicsInterpolationFactor();
            string expression = CharacterExpressions.Name(p.FaceAnimation.SampleExpression(alpha));
            int frame = p.FaceAnimation.SampleFrame(alpha);
            Assert.That(face.sprite, Is.Not.Null); Assert.That(face.sprite.name, Does.Contain($"face/20000/{expression}/{frame}"));
            string stance = CharacterStances.Name(p.BodyStance); int bodyFrame = p.IsBasicAttacking ? p.BasicAttack.Frame : p.StanceAnimation.Sample(alpha);
            var file = NXAssetLoader.Instance.GetNxFile("character");
            var faceNode = file.GetNode($"Face/00020000.img/{expression}" + (expression == "default" ? "" : "/"+frame) + "/face");
            var headNode = file.GetNode($"00012000.img/{stance}/{bodyFrame}/head");
            Assert.That(Vector3.Distance(Brow(face, faceNode), Brow(head, headNode)), Is.LessThan(.0001f), "Animated face brow must meet the current head brow.");
        }

        [UnityTest]
        public IEnumerator RealBlinksAndSevenExpressionsStayAlignedPauseAndSurviveMovementAndAttacks()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var game = Object.FindFirstObjectByType<GameManager>(); game.enabled = false;
            var w = game.World; var p = w.Player; var input = new Input();
            typeof(GameWorld).GetField("inputProvider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(w, input);
            Step(w, 100); Assert.That(w.RequestPracticeSupplies(out _), Is.True);
            foreach (int id in new[] {1040002,1060002,1072001,1302000}) Assert.That(w.UseInventoryItem(id, out _), Is.True);
            var visual = GameObject.Find("Player").transform.Find("VisualRoot");
            var face = visual.Find("Face").GetComponent<SpriteRenderer>(); var head = visual.Find("Head").GetComponent<SpriteRenderer>();
            var body = visual.Find("Body").GetComponent<SpriteRenderer>(); var previousFace = face.sprite; var previousBody = body.sprite;
            var profile = new CharacterDataProvider().GetFaceAnimation(20000); var blinkFrames = new HashSet<int>(); bool independentFrame = false;
            for (int i = 0; i < 700 && blinkFrames.Count < profile.FrameCount(CharacterExpression.Blink); i++)
            {
                Step(w); yield return null; AssertFace(w, face, head);
                if (p.FaceAnimation.SampleExpression(w.GetPhysicsInterpolationFactor()) == CharacterExpression.Blink)
                {
                    blinkFrames.Add(p.FaceAnimation.SampleFrame(w.GetPhysicsInterpolationFactor()));
                    independentFrame |= face.sprite != previousFace && body.sprite == previousBody;
                }
                previousFace = face.sprite; previousBody = body.sprite;
            }
            Assert.That(blinkFrames.Count, Is.EqualTo(profile.FrameCount(CharacterExpression.Blink)));
            Assert.That(independentFrame, Is.True, "The face must animate even when the body frame has not changed.");
            InventorySceneSmokeTests.Capture(p, "-face-blink");
            for (int key = 1; key <= 7; key++)
            {
                Step(w, (p.FaceAnimation.CooldownMilliseconds + 7) / 8 + 1);
                var expression = CharacterExpressions.ForFunctionKey(key).Value;
                input.ExpressionPressed = expression; w.ProcessInput(); input.ExpressionPressed = null; w.ProcessInput();
                Step(w); yield return null;
                Assert.That(p.FaceAnimation.Expression, Is.EqualTo(expression)); AssertFace(w, face, head);
                InventorySceneSmokeTests.Capture(p, "-face-" + CharacterExpressions.Name(expression));
                var sprite = face.sprite; int elapsed = p.FaceAnimation.ElapsedMilliseconds, cooldown = p.FaceAnimation.CooldownMilliseconds;
                yield return new WaitForSeconds(.12f);
                Assert.That(face.sprite, Is.SameAs(sprite)); Assert.That(p.FaceAnimation.ElapsedMilliseconds, Is.EqualTo(elapsed));
                Assert.That(p.FaceAnimation.CooldownMilliseconds, Is.EqualTo(cooldown));
                var seen = new HashSet<int> { p.FaceAnimation.SampleFrame(w.GetPhysicsInterpolationFactor()) };
                for (int i = 0; i < 1000 && p.FaceAnimation.Expression == expression; i++)
                {
                    Step(w); yield return null; AssertFace(w, face, head);
                    if (p.FaceAnimation.SampleExpression(w.GetPhysicsInterpolationFactor()) == expression)
                        seen.Add(p.FaceAnimation.SampleFrame(w.GetPhysicsInterpolationFactor()));
                }
                Assert.That(seen.Count, Is.EqualTo(profile.FrameCount(expression)), expression.ToString());
                Assert.That(p.FaceAnimation.Expression, Is.EqualTo(CharacterExpression.Default));
            }
            Step(w, 626); p.RequestExpression(CharacterExpression.Smile); Step(w); input.IsLeftPressed = true; Step(w, 8); yield return null;
            Assert.That(p.FacingRight, Is.False); AssertFace(w, face, head);
            InventorySceneSmokeTests.Capture(p, "-face-left-walk");
            input.IsLeftPressed = false; input.IsAttackPressed = true; Step(w); input.IsAttackPressed = false;
            Assert.That(p.IsBasicAttacking, Is.True); int before = p.FaceAnimation.ElapsedMilliseconds;
            Step(w, 5); yield return null; AssertFace(w, face, head);
            Assert.That(p.FaceAnimation.ElapsedMilliseconds, Is.Not.EqualTo(before)); InventorySceneSmokeTests.Capture(p, "-face-attack");

            Step(w, 626);
            var ladder = w.CurrentMap.Ladders.First(); p.ResetMovementForMap();
            p.Position = new MapleClient.GameLogic.Vector2(ladder.X, (ladder.Y1+ladder.Y2)/2 + Player.Height/2);
            p.Velocity = MapleClient.GameLogic.Vector2.Zero; p.IsGrounded = false; input.IsUpPressed = true; Step(w); input.IsUpPressed = false;
            p.RequestExpression(CharacterExpression.Cry); Step(w); yield return null;
            Assert.That(p.State, Is.EqualTo(PlayerState.Climbing)); Assert.That(face.sprite, Is.Null);
            int stoppedCooldown = p.FaceAnimation.CooldownMilliseconds; Step(w, 100); yield return null;
            Assert.That(face.sprite, Is.Null); Assert.That(p.FaceAnimation.CooldownMilliseconds, Is.EqualTo(stoppedCooldown));
            w.LoadMap(100000001); yield return null; yield return null;
            Assert.That(p.FaceAnimation.Expression, Is.EqualTo(CharacterExpression.Cry));
            Assert.That(p.FaceAnimation.CooldownMilliseconds, Is.EqualTo(stoppedCooldown), "Respawn preserves the face clock in HeavenClient.");
            Assert.That(p.GetEquippedItems().Values, Does.Contain(1302000)); AssertFace(w, face, head);
        }
    }
}
