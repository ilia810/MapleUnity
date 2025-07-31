using UnityEngine;
using System.Collections;

namespace MapleUnity.GameView
{
    /// <summary>
    /// Test component to validate character rendering positioning matches the original client.
    /// </summary>
    public class CharacterRenderingTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        public CharacterRenderingSystem renderingSystem;
        public Vector2 testPosition = new Vector2(500, 300);
        public bool testFlipped = false;
        
        [Header("Test Sprites")]
        public Sprite bodySprite;
        public Sprite armSprite;
        public Sprite headSprite;
        public Sprite faceSprite;
        public Sprite hairSprite;
        
        [Header("Known Test Values")]
        [TextArea(3, 10)]
        public string expectedPositions = @"Expected positions for stand1, frame 0:
Body: (500, 300)
Arm: (490, 305)
Head: (498, 270)
Face: (497, 257)
Hair: (497, 257)";
        
        private bool isInitialized = false;
        
        void Start()
        {
            StartCoroutine(InitializeTest());
        }
        
        IEnumerator InitializeTest()
        {
            // Wait a frame to ensure everything is initialized
            yield return null;
            
            if (renderingSystem == null)
            {
                renderingSystem = GetComponent<CharacterRenderingSystem>();
                if (renderingSystem == null)
                {
                    renderingSystem = gameObject.AddComponent<CharacterRenderingSystem>();
                }
            }
            
            CreateTestCharacter();
            isInitialized = true;
            
            // Run initial test
            RunPositionTest();
        }
        
        void CreateTestCharacter()
        {
            // Create body
            if (renderingSystem.bodyObject == null)
            {
                renderingSystem.bodyObject = CreateBodyPart("Body", bodySprite);
            }
            
            // Create arm
            if (renderingSystem.armObject == null)
            {
                renderingSystem.armObject = CreateBodyPart("Arm", armSprite);
            }
            
            // Create head
            if (renderingSystem.headObject == null)
            {
                renderingSystem.headObject = CreateBodyPart("Head", headSprite);
            }
            
            // Create face
            if (renderingSystem.faceObject == null)
            {
                renderingSystem.faceObject = CreateBodyPart("Face", faceSprite);
            }
            
            // Create hair
            if (renderingSystem.hairObject == null)
            {
                renderingSystem.hairObject = CreateBodyPart("Hair", hairSprite);
            }
        }
        
        GameObject CreateBodyPart(string name, Sprite sprite)
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(transform);
            
            var spriteRenderer = part.AddComponent<SpriteRenderer>();
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
            else
            {
                // Create a placeholder colored square if no sprite provided
                spriteRenderer.color = GetPlaceholderColor(name);
            }
            
            return part;
        }
        
        Color GetPlaceholderColor(string partName)
        {
            switch (partName)
            {
                case "Body": return new Color(0.5f, 0.5f, 1f, 0.8f); // Light blue
                case "Arm": return new Color(1f, 0.5f, 0.5f, 0.8f); // Light red
                case "Head": return new Color(0.5f, 1f, 0.5f, 0.8f); // Light green
                case "Face": return new Color(1f, 1f, 0.5f, 0.8f); // Light yellow
                case "Hair": return new Color(1f, 0.5f, 1f, 0.8f); // Light magenta
                default: return Color.gray;
            }
        }
        
        public void RunPositionTest()
        {
            if (!isInitialized) return;
            
            Debug.Log("=== Character Rendering Position Test ===");
            Debug.Log($"Test Position: {testPosition}");
            Debug.Log($"Flipped: {testFlipped}");
            
            // Enable logging
            renderingSystem.logPositionCalculations = true;
            
            // Position the character
            renderingSystem.PositionCharacterParts(testPosition, testFlipped);
            
            // Log actual positions
            LogActualPositions();
            
            // Compare with expected
            CompareWithExpected();
        }
        
        void LogActualPositions()
        {
            Debug.Log("=== Actual Positions ===");
            
            if (renderingSystem.bodyObject != null)
                Debug.Log($"Body: {renderingSystem.bodyObject.transform.position}");
                
            if (renderingSystem.armObject != null)
                Debug.Log($"Arm: {renderingSystem.armObject.transform.position}");
                
            if (renderingSystem.headObject != null)
                Debug.Log($"Head: {renderingSystem.headObject.transform.position}");
                
            if (renderingSystem.faceObject != null)
                Debug.Log($"Face: {renderingSystem.faceObject.transform.position}");
                
            if (renderingSystem.hairObject != null)
                Debug.Log($"Hair: {renderingSystem.hairObject.transform.position}");
        }
        
        void CompareWithExpected()
        {
            // In a real test, we would compare with known good values
            // from the original client
            Debug.Log("=== Comparison ===");
            Debug.Log(expectedPositions);
            Debug.Log("Note: Compare these values with screenshots from the original client");
        }
        
        void OnGUI()
        {
            if (!isInitialized) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Character Rendering Test", GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Position X:");
            float newX = GUILayout.HorizontalSlider(testPosition.x, 0, 1000);
            GUILayout.Label(testPosition.x.ToString("F0"));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Position Y:");
            float newY = GUILayout.HorizontalSlider(testPosition.y, 0, 600);
            GUILayout.Label(testPosition.y.ToString("F0"));
            GUILayout.EndHorizontal();
            
            testPosition = new Vector2(newX, newY);
            
            bool newFlipped = GUILayout.Toggle(testFlipped, "Flipped");
            if (newFlipped != testFlipped)
            {
                testFlipped = newFlipped;
            }
            
            if (GUILayout.Button("Update Position"))
            {
                RunPositionTest();
            }
            
            renderingSystem.showAttachmentPoints = GUILayout.Toggle(
                renderingSystem.showAttachmentPoints, 
                "Show Attachment Points"
            );
            
            GUILayout.EndArea();
        }
    }
}