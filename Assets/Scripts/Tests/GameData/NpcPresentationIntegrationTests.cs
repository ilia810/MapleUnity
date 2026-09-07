using GameData;
using MapleClient.GameData;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class NpcPresentationIntegrationTests
    {
        [TestCase(1012111,"I am a scholar who studies animals and monsters.")]
        [TestCase(1012004,"Pet food for your precious pet!! On Sale!!")]
        [TestCase(2005,"Head to Amherst! Lots going on there...")]
        public void AmbientLinesResolveInfoStancesAndLinkedArtWithTheOriginalIdentity(int id,string expected)
        {
            var manager=NXDataManagerSingleton.Instance.DataManager;
            Assert.That(NxNpcSpeech.Read(manager.GetFile("npc"),manager.GetFile("string"),id),Does.Contain(expected));
        }
        [Test] public void AnimationStancesResolveLinksAndExcludeNonAnimationData()
        {
            var file=NXDataManagerSingleton.Instance.DataManager.GetFile("npc");
            string linked=NxNpcAnimations.ResolvePath(file,2005);
            Assert.That(linked,Is.EqualTo("0002003.img"));
            Assert.That(NxNpcAnimations.Stances(file,linked),Is.EquivalentTo(new[]{"stand","finger","heart","wink"}));
            Assert.That(NxNpcAnimations.Stances(file,NxNpcAnimations.ResolvePath(file,1012111)),Is.EquivalentTo(new[]{"stand","say"}));
            Assert.That(NxNpcAnimations.Stances(file,NxNpcAnimations.ResolvePath(file,1012108)),Is.EquivalentTo(new[]{"stand","say"}),"Numbered quest conditions are not animation frames.");
            Assert.That(NxNpcAnimations.ResolvePath(file,9999999),Is.Null);
            Assert.That(NxNpcAnimations.Stances(null,null),Is.Empty);
        }
        [Test] public void CyclicAndBrokenNpcArtLinksDoNotHangOrUseTheWrongIdentity()
        {
            var file=new MockNxFile("npc");var root=(NxNode)file.Root;
            var a=new NxNode("9000000.img");var ai=new NxNode("info");ai.AddChild(new NxNode("link","9000001"));a.AddChild(ai);root.AddChild(a);
            var b=new NxNode("9000001.img");var bi=new NxNode("info");bi.AddChild(new NxNode("link","9000000"));b.AddChild(bi);root.AddChild(b);
            Assert.That(NxNpcAnimations.ResolvePath(file,9000000),Is.Null);
            Assert.That(NxNpcSpeech.Read(file,file,9000000),Is.Empty);
        }
        [Test] public void UnreferencedDialogueAndMissingDataDoNotBecomeAmbientSpeech()
        {
            var manager=NXDataManagerSingleton.Instance.DataManager;
            Assert.That(NxNpcSpeech.Read(manager.GetFile("npc"),manager.GetFile("string"),1012119),Is.Empty);
            Assert.That(NxNpcSpeech.Read(null,null,1012111),Is.Empty);
        }
    }
}
