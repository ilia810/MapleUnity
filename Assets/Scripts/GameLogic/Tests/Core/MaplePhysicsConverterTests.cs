using NUnit.Framework;
using MapleClient.GameLogic;

namespace MapleClient.GameLogic.Tests
{
    [TestFixture]
    public class MaplePhysicsConverterTests
    {
        [Test]
        public void UnityToMaple_ConvertsCorrectly()
        {
            // Arrange
            var unityPos = new Vector2(5f, 3f); // Unity units
            
            // Act
            var maplePos = MaplePhysicsConverter.UnityToMaple(unityPos);
            
            // Assert
            Assert.AreEqual(500f, maplePos.X); // 5 * 100
            Assert.AreEqual(-300f, maplePos.Y); // -3 * 100 (inverted Y)
        }
        
        [Test]
        public void MapleToUnity_ConvertsCorrectly()
        {
            // Arrange
            var maplePos = new Vector2(500f, 300f); // MapleStory pixels
            
            // Act
            var unityPos = MaplePhysicsConverter.MapleToUnity(maplePos);
            
            // Assert
            Assert.AreEqual(5f, unityPos.X); // 500 / 100
            Assert.AreEqual(-3f, unityPos.Y); // -300 / 100 (inverted Y)
        }
        
        [Test]
        public void UnityToMaple_ZeroPosition_ReturnsZero()
        {
            // Arrange
            var unityPos = new Vector2(0f, 0f);
            
            // Act
            var maplePos = MaplePhysicsConverter.UnityToMaple(unityPos);
            
            // Assert
            Assert.AreEqual(0f, maplePos.X);
            Assert.AreEqual(0f, maplePos.Y);
        }
        
        [Test]
        public void MapleToUnity_ZeroPosition_ReturnsZero()
        {
            // Arrange
            var maplePos = new Vector2(0f, 0f);
            
            // Act
            var unityPos = MaplePhysicsConverter.MapleToUnity(maplePos);
            
            // Assert
            Assert.AreEqual(0f, unityPos.X);
            Assert.AreEqual(0f, unityPos.Y);
        }
        
        [Test]
        public void RoundTripConversion_PreservesValues()
        {
            // Arrange
            var originalUnity = new Vector2(12.34f, -56.78f);
            
            // Act
            var maple = MaplePhysicsConverter.UnityToMaple(originalUnity);
            var backToUnity = MaplePhysicsConverter.MapleToUnity(maple);
            
            // Assert
            Assert.AreEqual(originalUnity.X, backToUnity.X, 0.001f);
            Assert.AreEqual(originalUnity.Y, backToUnity.Y, 0.001f);
        }
        
        [Test]
        public void UnityToMaple_NegativeUnityY_ReturnsPositiveMapleY()
        {
            // Arrange
            var unityPos = new Vector2(0f, -5f); // 5 units below Unity origin
            
            // Act
            var maplePos = MaplePhysicsConverter.UnityToMaple(unityPos);
            
            // Assert
            Assert.AreEqual(500f, maplePos.Y); // Positive in MapleStory coords
        }
        
        [Test]
        public void MapleToUnity_PositiveMapleY_ReturnsNegativeUnityY()
        {
            // Arrange
            var maplePos = new Vector2(0f, 500f); // 500 pixels down in MapleStory
            
            // Act
            var unityPos = MaplePhysicsConverter.MapleToUnity(maplePos);
            
            // Assert
            Assert.AreEqual(-5f, unityPos.Y); // Negative in Unity coords
        }
    }
}