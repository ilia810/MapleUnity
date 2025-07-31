using System.Collections.Generic;

namespace MapleClient.GameData
{
    public class MockNxFile : INxFile
    {
        private readonly NxNode rootNode;

        public bool IsLoaded => true;
        public INxNode Root => rootNode;

        public MockNxFile()
        {
            rootNode = new NxNode("root");
            if (this is MockNxFile)
            {
                CreateMockMapData();
            }
        }
        
        public MockNxFile(string fileName)
        {
            rootNode = new NxNode("root");
            // Create mock data based on file name
            CreateMockDataForFile(fileName);
        }

        public INxNode GetNode(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Root;

            var parts = path.Split('/');
            INxNode current = Root;

            foreach (var part in parts)
            {
                if (current == null || !current.HasChild(part))
                    return null;

                current = current[part];
            }

            return current;
        }

        private void CreateMockMapData()
        {
            // Create Map node structure
            var mapNode = new NxNode("Map");
            (rootNode as NxNode).AddChild(mapNode);

            var map0Node = new NxNode("Map0");
            (mapNode as NxNode).AddChild(map0Node);

            // Add Henesys map (100000000)
            var henesysNode = new NxNode("100000000.img");
            (map0Node as NxNode).AddChild(henesysNode);

            // Add info
            var infoNode = new NxNode("info");
            infoNode.AddChild(new NxNode("bgm", "Bgm00/GoPicnic"));
            (henesysNode as NxNode).AddChild(infoNode);

            // Add footholds - need actual foothold data for testing
            var footholdNode = new NxNode("foothold");
            var layer0 = new NxNode("0");
            var group0 = new NxNode("0");
            
            // Add actual footholds that match the visible platforms in Henesys
            // In MapleStory, Y coordinates are inverted (negative Y is up)
            // Main ground platform - extend to cover entire playable area
            var fh1 = new NxNode("1");
            fh1.AddChild(new NxNode("x1", -5000));  // Much wider platform
            fh1.AddChild(new NxNode("y1", 20));  // Positive Y for ground level
            fh1.AddChild(new NxNode("x2", 5000));   // Much wider platform
            fh1.AddChild(new NxNode("y2", 20));
            group0.AddChild(fh1);
            
            // Left platform (higher up = smaller Y value)
            var fh2 = new NxNode("2");
            fh2.AddChild(new NxNode("x1", -400));
            fh2.AddChild(new NxNode("y1", -150)); // Negative Y for elevated platform
            fh2.AddChild(new NxNode("x2", -200));
            fh2.AddChild(new NxNode("y2", -150));
            group0.AddChild(fh2);
            
            // Right platform  
            var fh3 = new NxNode("3");
            fh3.AddChild(new NxNode("x1", 200));
            fh3.AddChild(new NxNode("y1", -150)); // Negative Y for elevated platform
            fh3.AddChild(new NxNode("x2", 400));
            fh3.AddChild(new NxNode("y2", -150));
            group0.AddChild(fh3);
            
            // Higher center platform
            var fh4 = new NxNode("4");
            fh4.AddChild(new NxNode("x1", -100));
            fh4.AddChild(new NxNode("y1", -300)); // More negative Y for higher platform
            fh4.AddChild(new NxNode("x2", 100));
            fh4.AddChild(new NxNode("y2", -300));
            group0.AddChild(fh4);

            layer0.AddChild(group0);
            footholdNode.AddChild(layer0);
            (henesysNode as NxNode).AddChild(footholdNode);

            // Add portal
            var portalNode = new NxNode("portal");
            var portal0 = new NxNode("0");
            portal0.AddChild(new NxNode("id", 0));
            portal0.AddChild(new NxNode("pn", "sp"));
            portal0.AddChild(new NxNode("x", 0));
            portal0.AddChild(new NxNode("y", 50));
            portal0.AddChild(new NxNode("pt", 0));
            portalNode.AddChild(portal0);
            (henesysNode as NxNode).AddChild(portalNode);

            // Add life (monsters)
            var lifeNode = new NxNode("life");
            
            // Add a snail
            var life0 = new NxNode("0");
            life0.AddChild(new NxNode("type", "m"));
            life0.AddChild(new NxNode("id", "100100"));
            life0.AddChild(new NxNode("x", 200));
            life0.AddChild(new NxNode("y", 50));
            life0.AddChild(new NxNode("mobTime", 30));
            lifeNode.AddChild(life0);
            
            // Add another snail
            var life1 = new NxNode("1");
            life1.AddChild(new NxNode("type", "m"));
            life1.AddChild(new NxNode("id", "100100"));
            life1.AddChild(new NxNode("x", -200));
            life1.AddChild(new NxNode("y", 50));
            life1.AddChild(new NxNode("mobTime", 30));
            lifeNode.AddChild(life1);
            
            (henesysNode as NxNode).AddChild(lifeNode);

            // Add String.img structure
            var stringImgNode = new NxNode("Map.img");
            var streetNameNode = new NxNode("streetName");
            var mapNameNode = new NxNode("mapName");
            
            var henesysStreet = new NxNode("100000000");
            henesysStreet.AddChild(new NxNode("streetName", "Henesys"));
            henesysStreet.AddChild(new NxNode("mapName", "Henesys"));
            streetNameNode.AddChild(henesysStreet);
            mapNameNode.AddChild(henesysStreet);
            
            stringImgNode.AddChild(streetNameNode);
            stringImgNode.AddChild(mapNameNode);
            (rootNode as NxNode).AddChild(stringImgNode);
        }
        
        private void CreateMockDataForFile(string fileName)
        {
            // Create appropriate mock data based on file name
            switch (fileName.ToLower())
            {
                case "map.nx":
                    CreateMockMapData();
                    break;
                case "string.nx":
                    // Create string data if needed
                    break;
                case "item.nx":
                    // Create item data if needed
                    break;
                default:
                    // Create minimal structure
                    break;
            }
        }
    }
}