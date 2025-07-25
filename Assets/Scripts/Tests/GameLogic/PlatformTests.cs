using NUnit.Framework;
using MapleClient.GameLogic;

namespace MapleClient.GameLogic.Tests
{
    [TestFixture]
    public class PlatformTests
    {
        [Test]
        public void GetYAtX_WithHorizontalPlatform_ReturnsConstantY()
        {
            // Arrange
            var platform = new Platform
            {
                X1 = 0,
                Y1 = 100,
                X2 = 200,
                Y2 = 100
            };

            // Act & Assert
            Assert.That(platform.GetYAtX(0), Is.EqualTo(100));
            Assert.That(platform.GetYAtX(100), Is.EqualTo(100));
            Assert.That(platform.GetYAtX(200), Is.EqualTo(100));
        }

        [Test]
        public void GetYAtX_WithSlopedPlatform_ReturnsInterpolatedY()
        {
            // Arrange
            var platform = new Platform
            {
                X1 = 0,
                Y1 = 100,
                X2 = 100,
                Y2 = 200
            };

            // Act & Assert
            Assert.That(platform.GetYAtX(0), Is.EqualTo(100));
            Assert.That(platform.GetYAtX(50), Is.EqualTo(150));
            Assert.That(platform.GetYAtX(100), Is.EqualTo(200));
        }

        [Test]
        public void GetYAtX_OutsidePlatformBounds_ReturnsNaN()
        {
            // Arrange
            var platform = new Platform
            {
                X1 = 100,
                Y1 = 100,
                X2 = 200,
                Y2 = 100
            };

            // Act & Assert
            Assert.That(platform.GetYAtX(50), Is.NaN);
            Assert.That(platform.GetYAtX(250), Is.NaN);
        }
    }
}