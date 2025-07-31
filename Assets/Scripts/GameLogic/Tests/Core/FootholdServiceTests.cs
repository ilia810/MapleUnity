using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic;

namespace MapleClient.GameLogic.Tests
{
    [TestFixture]
    public class FootholdServiceTests
    {
        private FootholdService service;
        
        [SetUp]
        public void SetUp()
        {
            service = new FootholdService();
        }
        
        [Test]
        public void GetGroundBelow_NoFootholds_ReturnsMaxValue()
        {
            // Arrange
            service.LoadFootholds(new List<Foothold>());
            
            // Act
            float result = service.GetGroundBelow(100, 100);
            
            // Assert
            Assert.AreEqual(float.MaxValue, result);
        }
        
        [Test]
        public void GetGroundBelow_WithSingleFlatFoothold_ReturnsGroundY()
        {
            // Arrange
            var footholds = new List<Foothold>
            {
                new Foothold(1, 0, 200, 300, 200) // Flat foothold at Y=200
            };
            service.LoadFootholds(footholds);
            
            // Act
            float result = service.GetGroundBelow(150, 100); // Search from above the foothold
            
            // Assert
            Assert.AreEqual(199, result); // Should return ground - 1 as per C++ client
        }
        
        [Test]
        public void GetGroundBelow_WithSlopedFoothold_ReturnsInterpolatedY()
        {
            // Arrange
            var footholds = new List<Foothold>
            {
                new Foothold(1, 0, 100, 200, 200) // Sloped foothold
            };
            service.LoadFootholds(footholds);
            
            // Act
            float result = service.GetGroundBelow(100, 50); // X=100, halfway between X1 and X2
            
            // Assert
            float expectedY = 150 - 1; // Interpolated Y minus 1
            Assert.AreEqual(expectedY, result);
        }
        
        [Test]
        public void GetGroundBelow_MultipleFootholds_ReturnsClosestBelow()
        {
            // Arrange
            var footholds = new List<Foothold>
            {
                new Foothold(1, 0, 300, 300, 300), // Lower foothold
                new Foothold(2, 0, 200, 300, 200)  // Upper foothold
            };
            service.LoadFootholds(footholds);
            
            // Act
            float result = service.GetGroundBelow(150, 150); // Between both footholds
            
            // Assert
            Assert.AreEqual(199, result); // Should return the upper foothold (closest below)
        }
        
        [Test]
        public void GetGroundBelow_XOutsideFootholdRange_ReturnsMaxValue()
        {
            // Arrange
            var footholds = new List<Foothold>
            {
                new Foothold(1, 100, 200, 200, 200)
            };
            service.LoadFootholds(footholds);
            
            // Act
            float result = service.GetGroundBelow(50, 100); // X is outside foothold range
            
            // Assert
            Assert.AreEqual(float.MaxValue, result);
        }
        
        [Test]
        public void IsOnGround_OnFoothold_ReturnsTrue()
        {
            // Arrange
            var footholds = new List<Foothold>
            {
                new Foothold(1, 0, 200, 300, 200)
            };
            service.LoadFootholds(footholds);
            
            // Act
            bool result = service.IsOnGround(150, 200, 1f);
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [Test]
        public void IsOnGround_AboveFoothold_ReturnsFalse()
        {
            // Arrange
            var footholds = new List<Foothold>
            {
                new Foothold(1, 0, 200, 300, 200)
            };
            service.LoadFootholds(footholds);
            
            // Act
            bool result = service.IsOnGround(150, 150, 1f); // Y=150 is above Y=200
            
            // Assert
            Assert.IsFalse(result);
        }
        
        [Test]
        public void GetFootholdAt_ExactPosition_ReturnsFoothold()
        {
            // Arrange
            var foothold = new Foothold(1, 0, 200, 300, 200);
            var footholds = new List<Foothold> { foothold };
            service.LoadFootholds(footholds);
            
            // Act
            var result = service.GetFootholdAt(150, 200);
            
            // Assert
            Assert.AreEqual(foothold, result);
        }
        
        [Test]
        public void GetFootholdBelow_SearchesDownward_ReturnsNearestBelow()
        {
            // Arrange
            var targetFoothold = new Foothold(1, 0, 300, 300, 300);
            var footholds = new List<Foothold>
            {
                new Foothold(2, 0, 100, 300, 100), // Above search point
                targetFoothold,
                new Foothold(3, 0, 400, 300, 400)  // Even lower
            };
            service.LoadFootholds(footholds);
            
            // Act
            var result = service.GetFootholdBelow(150, 250);
            
            // Assert
            Assert.AreEqual(targetFoothold, result);
        }
        
        [Test]
        public void GetFootholdsInArea_ReturnsFootholdsInBounds()
        {
            // Arrange
            var footholds = new List<Foothold>
            {
                new Foothold(1, 0, 100, 100, 100),    // Inside
                new Foothold(2, 200, 200, 300, 200),  // Inside
                new Foothold(3, 400, 100, 500, 100)   // Outside
            };
            service.LoadFootholds(footholds);
            
            // Act
            var result = service.GetFootholdsInArea(0, 0, 350, 300).ToList();
            
            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.Contains(footholds[0], result);
            Assert.Contains(footholds[1], result);
        }
        
        [Test]
        public void GetConnectedFoothold_WithNextId_ReturnsNextFoothold()
        {
            // Arrange
            var fh1 = new Foothold(1, 0, 200, 100, 200) { NextId = 2 };
            var fh2 = new Foothold(2, 100, 200, 200, 200) { PreviousId = 1 };
            service.LoadFootholds(new List<Foothold> { fh1, fh2 });
            
            // Act
            var result = service.GetConnectedFoothold(fh1, true); // Moving right
            
            // Assert
            Assert.AreEqual(fh2, result);
        }
        
        [Test]
        public void IsWall_VerticalFoothold_ReturnsTrue()
        {
            // Arrange
            var foothold = new Foothold(1, 100, 100, 100, 300) { IsWall = true };
            
            // Act
            bool result = service.IsWall(foothold);
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [Test]
        public void GetSlopeAt_ReturnsFootholdSlope()
        {
            // Arrange
            var foothold = new Foothold(1, 0, 100, 100, 200); // 45 degree downward slope
            
            // Act
            float slope = service.GetSlopeAt(foothold, 50);
            
            // Assert
            float expectedSlope = System.Math.Atan2(100, 100); // Rise over run
            Assert.AreEqual(expectedSlope, slope, 0.001f);
        }
        
        [Test]
        public void FindNearestFoothold_ReturnsClosestFoothold()
        {
            // Arrange
            var nearFoothold = new Foothold(1, 90, 190, 110, 210);
            var farFoothold = new Foothold(2, 300, 300, 400, 300);
            service.LoadFootholds(new List<Foothold> { nearFoothold, farFoothold });
            
            // Act
            var result = service.FindNearestFoothold(100, 200, 1000f);
            
            // Assert
            Assert.AreEqual(nearFoothold, result);
        }
    }
}