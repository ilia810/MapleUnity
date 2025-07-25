using NUnit.Framework;
using MapleClient.GameData;
using System.IO;

namespace MapleClient.GameData.Tests
{
    [TestFixture]
    public class NxFileTests
    {
        [Test]
        public void NxFile_Constructor_ThrowsWhenFileNotFound()
        {
            Assert.Throws<FileNotFoundException>(() => new NxFile("nonexistent.nx"));
        }

        [Test]
        [Ignore("Requires actual NX file")]
        public void NxFile_LoadsValidFile()
        {
            // This test would require an actual NX file
            // For now we'll use mock data in our implementation
            var nxFile = new NxFile("Map.nx");
            Assert.That(nxFile.IsLoaded, Is.True);
        }
    }
}