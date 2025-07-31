using NUnit.Framework;

namespace MapleClient.GameLogic.Tests.Data
{
    [TestFixture]
    public class FootholdTests
    {
        [Test]
        public void GetYAtX_ReturnsCorrectY_ForHorizontalFoothold()
        {
            // Arrange - horizontal foothold at Y=100
            var foothold = new Foothold(1, 0, 100, 200, 100);
            
            // Act & Assert
            Assert.AreEqual(100, foothold.GetYAtX(0), 0.001f);
            Assert.AreEqual(100, foothold.GetYAtX(100), 0.001f);
            Assert.AreEqual(100, foothold.GetYAtX(200), 0.001f);
        }
        
        [Test]
        public void GetYAtX_ReturnsCorrectY_ForSlopedFoothold()
        {
            // Arrange - sloped foothold from (0,100) to (200,200)
            var foothold = new Foothold(1, 0, 100, 200, 200);
            
            // Act & Assert
            Assert.AreEqual(100, foothold.GetYAtX(0), 0.001f);
            Assert.AreEqual(150, foothold.GetYAtX(100), 0.001f);
            Assert.AreEqual(200, foothold.GetYAtX(200), 0.001f);
        }
        
        [Test]
        public void GetYAtX_ReturnsNaN_ForOutOfBounds()
        {
            // Arrange
            var foothold = new Foothold(1, 100, 200, 300, 250);
            
            // Act & Assert
            Assert.IsTrue(float.IsNaN(foothold.GetYAtX(50)));  // Before start
            Assert.IsTrue(float.IsNaN(foothold.GetYAtX(350))); // After end
        }
        
        [Test]
        public void ContainsPoint_ReturnsTrueForPointOnFoothold()
        {
            // Arrange
            var foothold = new Foothold(1, 0, 100, 200, 100);
            
            // Act & Assert
            Assert.IsTrue(foothold.ContainsPoint(100, 100, 1f));
            Assert.IsTrue(foothold.ContainsPoint(100, 100.5f, 1f)); // Within tolerance
            Assert.IsTrue(foothold.ContainsPoint(100, 99.5f, 1f));  // Within tolerance
        }
        
        [Test]
        public void ContainsPoint_ReturnsFalseForPointOffFoothold()
        {
            // Arrange
            var foothold = new Foothold(1, 0, 100, 200, 100);
            
            // Act & Assert
            Assert.IsFalse(foothold.ContainsPoint(100, 102, 1f));  // Too far below
            Assert.IsFalse(foothold.ContainsPoint(100, 98, 1f));   // Too far above
            Assert.IsFalse(foothold.ContainsPoint(-10, 100, 1f));  // Out of X bounds
        }
        
        [Test]
        public void GetSlope_ReturnsCorrectAngle()
        {
            // Arrange
            var horizontal = new Foothold(1, 0, 100, 200, 100);
            var upwardSlope = new Foothold(2, 0, 200, 200, 100); // Going up (Y decreases)
            var downwardSlope = new Foothold(3, 0, 100, 200, 200); // Going down (Y increases)
            
            // Act & Assert
            Assert.AreEqual(0, horizontal.GetSlope(), 0.001f);
            Assert.Less(upwardSlope.GetSlope(), 0); // Negative angle for upward slope
            Assert.Greater(downwardSlope.GetSlope(), 0); // Positive angle for downward slope
        }
        
        [Test]
        public void VerticalFoothold_HandledCorrectly()
        {
            // Arrange - vertical wall
            var wall = new Foothold(1, 100, 50, 100, 200) { IsWall = true };
            
            // Act & Assert
            Assert.AreEqual(50, wall.GetYAtX(100), 0.001f); // Returns top Y
            Assert.IsTrue(float.IsNaN(wall.GetYAtX(99)));   // Not on the wall
            Assert.AreEqual(0, wall.GetSlope(), 0.001f);    // Treated as flat
        }
        
        [Test]
        public void MapleStoryCoordinateSystem_YIncreasesDownward()
        {
            // This test documents the coordinate system assumption
            // In MapleStory, Y=0 is at the top, and Y increases downward
            
            // A character at Y=100 is higher (closer to top) than a character at Y=200
            float higherPosition = 100;
            float lowerPosition = 200;
            
            Assert.Less(higherPosition, lowerPosition);
            
            // A foothold at Y=300 is below (supports) a character at Y=299
            var foothold = new Foothold(1, 0, 300, 200, 300);
            float characterY = 299; // Character is above the foothold
            
            Assert.Less(characterY, foothold.Y1); // Character Y is less than foothold Y
        }
    }
}