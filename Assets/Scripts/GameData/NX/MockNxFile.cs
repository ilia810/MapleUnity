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

            // Add footholds
            var footholdNode = new NxNode("foothold");
            var layer0 = new NxNode("0");
            var group0 = new NxNode("0");
            
            // Main ground platform
            var fh1 = new NxNode("1");
            fh1.AddChild(new NxNode("x1", -1500));
            fh1.AddChild(new NxNode("y1", 0));
            fh1.AddChild(new NxNode("x2", 1500));
            fh1.AddChild(new NxNode("y2", 0));
            group0.AddChild(fh1);

            // Elevated platform
            var fh2 = new NxNode("2");
            fh2.AddChild(new NxNode("x1", -300));
            fh2.AddChild(new NxNode("y1", 150));
            fh2.AddChild(new NxNode("x2", 300));
            fh2.AddChild(new NxNode("y2", 150));
            group0.AddChild(fh2);

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
    }
}