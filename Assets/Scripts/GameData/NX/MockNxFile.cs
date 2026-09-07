using System.Collections.Generic;
using UnityEngine;

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
        
        private void CreateMockCharacterData()
        {
            Debug.Log("Creating mock character data for tests");
            
            // Create character body structure for skin ID 0
            var characterNode = new NxNode("00002000.img"); // Character body
            
            // Create stand1 animation
            var stand1Node = new NxNode("stand1");
            
            // Create frame 0
            var frame0Node = new NxNode("0");
            
            // Add body parts for standing frame 0
            var bodyNode = new NxNode("body");
            bodyNode.AddChild(new NxNode("_inlink", "00002000.img/front/body"));
            frame0Node.AddChild(bodyNode);
            
            var armNode = new NxNode("arm");
            armNode.AddChild(new NxNode("_inlink", "00002000.img/front/arm"));
            frame0Node.AddChild(armNode);
            
            var headNode = new NxNode("head");
            headNode.AddChild(new NxNode("_inlink", "00002000.img/front/head"));
            frame0Node.AddChild(headNode);
            
            // Add attachment points that match C++ runtime values
            // Body attachment points need a map node
            var bodyMapNode = new NxNode("map");
            bodyMapNode.AddChild(new NxNode("navel", new Dictionary<string, object> { { "x", 0 }, { "y", 0 } })); // Body origin
            bodyMapNode.AddChild(new NxNode("neck", new Dictionary<string, object> { { "x", -7 }, { "y", -31 } })); // Body neck position
            bodyNode.AddChild(bodyMapNode);
            
            // Head attachment points need a map node
            var headMapNode = new NxNode("map");
            headMapNode.AddChild(new NxNode("neck", new Dictionary<string, object> { { "x", 14 }, { "y", 19 } })); // Head neck position
            headMapNode.AddChild(new NxNode("brow", new Dictionary<string, object> { { "x", 13 }, { "y", 9 } })); // Head brow position
            headNode.AddChild(headMapNode);
            
            // Add delay for frame
            frame0Node.AddChild(new NxNode("delay", 500));
            
            stand1Node.AddChild(frame0Node);
            characterNode.AddChild(stand1Node);
            
            // Create front folder with actual sprite references
            var frontNode = new NxNode("front");
            
            var frontBodyNode = new NxNode("body");
            frontBodyNode.AddChild(new NxNode("_outlink", "Character/00002000.img/front/body.png"));
            frontNode.AddChild(frontBodyNode);
            
            var frontArmNode = new NxNode("arm");
            frontArmNode.AddChild(new NxNode("_outlink", "Character/00002000.img/front/arm.png"));
            frontNode.AddChild(frontArmNode);
            
            var frontHeadNode = new NxNode("head");
            frontHeadNode.AddChild(new NxNode("_outlink", "Character/00002000.img/front/head.png"));
            frontHeadNode.AddChild(new NxNode("origin", new Dictionary<string, object> { { "x", 12 }, { "y", 20 } }));
            frontNode.AddChild(frontHeadNode);
            
            characterNode.AddChild(frontNode);
            
            // Add character node to root
            (rootNode as NxNode).AddChild(characterNode);
            
            // Create head structure (00012000.img) with proper attachment points
            var headImgNode = new NxNode("00012000.img"); // Character head
            
            // Create stand1 animation for head
            var headStand1Node = new NxNode("stand1");
            
            // Create frame 0 for head
            var headFrame0Node = new NxNode("0");
            
            // Add head part
            var headPartNode = new NxNode("head");
            headPartNode.AddChild(new NxNode("_outlink", "Character/00012000.img/front/head.png"));
            headPartNode.AddChild(new NxNode("origin", new Dictionary<string, object> { { "x", 14 }, { "y", 19 } }));
            
            // Add head attachment points (these values should produce face offset of -8,-52)
            var headPartMapNode = new NxNode("map");
            headPartMapNode.AddChild(new NxNode("neck", new Dictionary<string, object> { { "x", 14 }, { "y", 19 } }));
            headPartMapNode.AddChild(new NxNode("brow", new Dictionary<string, object> { { "x", 13 }, { "y", -2 } })); // Adjusted to get -52 Y offset
            headPartNode.AddChild(headPartMapNode);
            
            headFrame0Node.AddChild(headPartNode);
            headStand1Node.AddChild(headFrame0Node);
            headImgNode.AddChild(headStand1Node);
            
            // Add head img node to root
            (rootNode as NxNode).AddChild(headImgNode);
            
            // Create face structure
            var faceNode = new NxNode("Face");
            var face20000Node = new NxNode("00020000.img");
            
            // Create default face expression
            var defaultNode = new NxNode("default");
            var faceFrameNode = new NxNode("face");
            faceFrameNode.AddChild(new NxNode("_outlink", "Character/Face/00020000.img/default/face.png"));
            faceFrameNode.AddChild(new NxNode("origin", new Dictionary<string, object> { { "x", 15 }, { "y", 15 } }));
            defaultNode.AddChild(faceFrameNode);
            
            face20000Node.AddChild(defaultNode);
            faceNode.AddChild(face20000Node);
            (rootNode as NxNode).AddChild(faceNode);
            
            // Create hair structure
            var hairNode = new NxNode("Hair");
            var hair30000Node = new NxNode("00030000.img");
            
            // Create default hair
            var hairDefaultNode = new NxNode("default");
            var hairBackNode = new NxNode("hairBelowBody");
            hairBackNode.AddChild(new NxNode("_outlink", "Character/Hair/00030000.img/default/hairBelowBody.png"));
            hairBackNode.AddChild(new NxNode("origin", new Dictionary<string, object> { { "x", 17 }, { "y", 17 } }));
            hairDefaultNode.AddChild(hairBackNode);
            
            var hairFrontNode = new NxNode("hair");
            hairFrontNode.AddChild(new NxNode("_outlink", "Character/Hair/00030000.img/default/hair.png"));
            hairFrontNode.AddChild(new NxNode("origin", new Dictionary<string, object> { { "x", 17 }, { "y", 17 } }));
            hairDefaultNode.AddChild(hairFrontNode);
            
            hair30000Node.AddChild(hairDefaultNode);
            hairNode.AddChild(hair30000Node);
            (rootNode as NxNode).AddChild(hairNode);
            
            Debug.Log("Mock character data created successfully");
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
                case "character.nx":
                    CreateMockCharacterData();
                    break;
                default:
                    // Create minimal structure
                    break;
            }
        }
    }
}