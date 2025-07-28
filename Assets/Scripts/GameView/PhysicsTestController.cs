using UnityEngine;
using UnityEngine.UI;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using MapleClient.GameData;
using System.Collections.Generic;
using System.Text;
using Vector2 = UnityEngine.Vector2;  // Resolve ambiguity - use Unity's Vector2

namespace MapleClient.GameView
{
    /// <summary>
    /// Runtime controller for the physics test scene.
    /// Provides UI controls, real-time parameter adjustment, and performance monitoring.
    /// </summary>
    public class PhysicsTestController : MonoBehaviour
    {
        // Game components
        private GameWorld gameWorld;
        private Player player;
        private GameObject playerObject;
        private SimplePlayerController playerController;
        private MapData mapData;
        
        // UI components
        private Transform physicsControlsPanel;
        private Transform debugInfoPanel;
        private Text debugText;
        private Dictionary<string, Slider> parameterSliders = new Dictionary<string, Slider>();
        private Dictionary<string, Text> parameterLabels = new Dictionary<string, Text>();
        
        // Debug settings
        private bool showDebugInfo = true;
        private bool showPhysicsVisuals = true;
        private bool showTrajectory = false;
        private List<Vector3> trajectoryPoints = new List<Vector3>();
        private LineRenderer trajectoryRenderer;
        
        // Performance tracking
        private float frameTimeAccumulator = 0f;
        private int frameCount = 0;
        private float averageFPS = 60f;
        private float lowestFPS = 60f;
        private float updateTimer = 0f;
        
        void Start()
        {
            InitializeGameSystems();
            SetupUI();
            CreateTestPlayer();
            CreateMapData();
        }
        
        void InitializeGameSystems()
        {
            // Create game world and physics manager
            var inputProvider = new UnityInputProvider();
            var mapLoader = new NxMapLoader();
            gameWorld = new GameWorld(inputProvider, mapLoader);
            // PhysicsUpdateManager is internal to GameWorld
            // We'll update physics through GameWorld.UpdatePhysics()
        }
        
        void SetupUI()
        {
            // Find UI panels
            var canvas = GameObject.Find("TestUI");
            if (canvas != null)
            {
                physicsControlsPanel = canvas.transform.Find("PhysicsControls");
                debugInfoPanel = canvas.transform.Find("DebugInfo");
                
                if (physicsControlsPanel != null)
                    CreatePhysicsControls();
                    
                if (debugInfoPanel != null)
                    CreateDebugDisplay();
            }
        }
        
        void CreatePhysicsControls()
        {
            float yPos = -20f;
            
            // Title
            CreateLabel(physicsControlsPanel, "Physics Parameters", new UnityEngine.Vector2(0, yPos), 16, TextAnchor.MiddleCenter);
            yPos -= 30f;
            
            // Walk Speed
            CreateSlider(physicsControlsPanel, "WalkSpeed", "Walk Speed", 0.5f, 2.5f, 1.25f, ref yPos, 
                value => 
                {
                    // Would need reflection or property to modify MaplePhysics constants
                    Debug.Log($"Walk Speed: {value}");
                });
            
            // Walk Acceleration  
            CreateSlider(physicsControlsPanel, "WalkAccel", "Walk Acceleration", 5f, 20f, 14f, ref yPos,
                value => Debug.Log($"Walk Acceleration: {value}"));
                
            // Walk Friction
            CreateSlider(physicsControlsPanel, "WalkFriction", "Walk Friction", 4f, 16f, 8f, ref yPos,
                value => Debug.Log($"Walk Friction: {value}"));
                
            // Jump Power
            CreateSlider(physicsControlsPanel, "JumpPower", "Jump Power", 3f, 8f, 5.55f, ref yPos,
                value => Debug.Log($"Jump Power: {value}"));
                
            // Gravity
            CreateSlider(physicsControlsPanel, "Gravity", "Gravity", 10f, 30f, 20f, ref yPos,
                value => Debug.Log($"Gravity: {value}"));
                
            // Terminal Velocity
            CreateSlider(physicsControlsPanel, "TerminalVel", "Terminal Velocity", 5f, 10f, 6.7f, ref yPos,
                value => Debug.Log($"Terminal Velocity: {value}"));
                
            // Checkboxes
            yPos -= 20f;
            CreateToggle(physicsControlsPanel, "ShowTrajectory", "Show Trajectory", false, new Vector2(10, yPos),
                value => showTrajectory = value);
            yPos -= 25f;
            
            CreateToggle(physicsControlsPanel, "ShowVisuals", "Show Physics Visuals", true, new Vector2(10, yPos),
                value => showPhysicsVisuals = value);
            yPos -= 25f;
            
            // Reset button
            CreateButton(physicsControlsPanel, "ResetParams", "Reset to Defaults", new Vector2(10, yPos), 
                new Vector2(180, 30), ResetPhysicsParameters);
        }
        
        void CreateDebugDisplay()
        {
            debugText = CreateLabel(debugInfoPanel, "", new Vector2(10, -10), 12, TextAnchor.UpperLeft);
            var rect = debugText.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(-10, -10);
        }
        
        void CreateTestPlayer()
        {
            // Create player logic object
            player = new Player();
            player.Position = new MapleClient.GameLogic.Vector2(0, 0);
            player.Speed = 100;
            player.JumpPower = 100;
            player.OnStatsChanged();
            
            // Player is automatically registered with physics through GameWorld
            
            // Create visual player object
            playerObject = new GameObject("TestPlayer");
            playerController = playerObject.AddComponent<SimplePlayerController>();
            playerController.SetGameLogicPlayer(player);
            playerController.SetGameWorld(gameWorld);
            
            // Camera follow
            var cameraFollow = Camera.main.gameObject.AddComponent<SmoothCameraFollow>();
            cameraFollow.target = playerObject.transform;
            cameraFollow.smoothTime = 0.1f;
            
            // Create trajectory renderer
            trajectoryRenderer = playerObject.AddComponent<LineRenderer>();
            trajectoryRenderer.startWidth = 0.05f;
            trajectoryRenderer.endWidth = 0.05f;
            trajectoryRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trajectoryRenderer.startColor = new Color(1f, 1f, 0f, 0.5f);
            trajectoryRenderer.endColor = new Color(1f, 0.5f, 0f, 0.5f);
            trajectoryRenderer.enabled = false;
        }
        
        void CreateMapData()
        {
            // Create map data matching the visual platforms
            mapData = new MapData
            {
                MapId = 999999, // Test map ID
                Platforms = new List<Platform>(),
                Ladders = new List<LadderInfo>()
            };
            
            // Ground
            mapData.Platforms.Add(new Platform 
            { 
                Id = 1, X1 = -1000, Y1 = -200, X2 = 1000, Y2 = -200, 
                Type = PlatformType.Normal 
            });
            
            // Regular platforms
            mapData.Platforms.Add(new Platform 
            { 
                Id = 2, X1 = -700, Y1 = 0, X2 = -300, Y2 = 0, 
                Type = PlatformType.Normal 
            });
            
            mapData.Platforms.Add(new Platform 
            { 
                Id = 3, X1 = 300, Y1 = 100, X2 = 700, Y2 = 100, 
                Type = PlatformType.Normal 
            });
            
            mapData.Platforms.Add(new Platform 
            { 
                Id = 4, X1 = -200, Y1 = 250, X2 = 200, Y2 = 250, 
                Type = PlatformType.Normal 
            });
            
            // One-way platforms
            mapData.Platforms.Add(new Platform 
            { 
                Id = 5, X1 = -450, Y1 = -50, X2 = -150, Y2 = -50, 
                Type = PlatformType.OneWay 
            });
            
            mapData.Platforms.Add(new Platform 
            { 
                Id = 6, X1 = 150, Y1 = 50, X2 = 450, Y2 = 50, 
                Type = PlatformType.OneWay 
            });
            
            // Small platforms
            mapData.Platforms.Add(new Platform 
            { 
                Id = 7, X1 = -725, Y1 = 150, X2 = -675, Y2 = 150, 
                Type = PlatformType.Normal 
            });
            
            mapData.Platforms.Add(new Platform 
            { 
                Id = 8, X1 = 675, Y1 = 150, X2 = 725, Y2 = 150, 
                Type = PlatformType.Normal 
            });
            
            // Slopes
            mapData.Platforms.Add(new Platform 
            { 
                Id = 9, X1 = -800, Y1 = -200, X2 = -600, Y2 = -100, 
                Type = PlatformType.Normal 
            });
            
            mapData.Platforms.Add(new Platform 
            { 
                Id = 10, X1 = 600, Y1 = -200, X2 = 800, Y2 = -100, 
                Type = PlatformType.Normal 
            });
            
            // Ladder
            mapData.Ladders.Add(new LadderInfo 
            { 
                X = 0, Y1 = -2, Y2 = 2
            });
            
            // Initialize player at spawn
            gameWorld.InitializePlayer(1, "TestPlayer", 100, 100, 100, 100, 0, 0);
            
            // For testing, we need to manually set up the world without going through the normal map loading
            // This would normally be done through LoadMap, but we're creating a test environment
            gameWorld.Player.Position = new MapleClient.GameLogic.Vector2(0, 0);
        }
        
        void Update()
        {
            HandleInput();
            UpdatePhysics();
            UpdateDebugDisplay();
            UpdateTrajectory();
        }
        
        void HandleInput()
        {
            // Movement
            player.MoveLeft(Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A));
            player.MoveRight(Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D));
            
            // Jumping
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
                    player.DropThroughPlatform();
                else
                    player.Jump();
            }
            if (Input.GetKeyUp(KeyCode.Space))
                player.ReleaseJump();
                
            // Climbing
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
                player.TryStartClimbing(mapData, true);
                
            player.ClimbUp(Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W));
            player.ClimbDown(Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S));
            
            // Crouching
            player.Crouch(Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S));
            
            // Debug controls
            if (Input.GetKeyDown(KeyCode.Tab))
                showDebugInfo = !showDebugInfo;
                
            if (Input.GetKeyDown(KeyCode.R))
                ResetPlayerPosition();
                
            // Test modifiers
            if (Input.GetKeyDown(KeyCode.Alpha1))
                player.AddMovementModifier(new SlipperyModifier());
            if (Input.GetKeyDown(KeyCode.Alpha2))
                player.AddMovementModifier(new SpeedModifier(1.5f, 3f, "test_speed"));
            if (Input.GetKeyDown(KeyCode.Alpha3))
                player.AddMovementModifier(new StunModifier(2f));
            if (Input.GetKeyDown(KeyCode.Alpha0))
                player.RemoveMovementModifierById("slippery_surface");
        }
        
        void UpdatePhysics()
        {
            // Update physics through GameWorld at fixed timestep
            gameWorld.UpdatePhysics(Time.fixedDeltaTime);
            
            // Track performance
            frameTimeAccumulator += Time.deltaTime;
            frameCount++;
            updateTimer += Time.deltaTime;
            
            if (updateTimer >= 0.5f) // Update every 0.5 seconds
            {
                averageFPS = frameCount / frameTimeAccumulator;
                if (averageFPS < lowestFPS)
                    lowestFPS = averageFPS;
                    
                frameTimeAccumulator = 0f;
                frameCount = 0;
                updateTimer = 0f;
            }
        }
        
        void UpdateDebugDisplay()
        {
            if (!showDebugInfo || debugText == null) 
            {
                if (debugInfoPanel != null)
                    debugInfoPanel.gameObject.SetActive(false);
                return;
            }
            
            if (debugInfoPanel != null)
                debugInfoPanel.gameObject.SetActive(true);
            
            var sb = new StringBuilder();
            sb.AppendLine($"<b>PHYSICS DEBUG INFO</b>");
            sb.AppendLine();
            sb.AppendLine($"<b>Position:</b> ({player.Position.X:F2}, {player.Position.Y:F2})");
            sb.AppendLine($"<b>Velocity:</b> ({player.Velocity.X:F2}, {player.Velocity.Y:F2})");
            sb.AppendLine($"<b>Speed:</b> {Mathf.Sqrt(player.Velocity.X * player.Velocity.X + player.Velocity.Y * player.Velocity.Y):F2} u/s");
            sb.AppendLine($"<b>State:</b> {player.State}");
            sb.AppendLine($"<b>Grounded:</b> {player.IsGrounded}");
            sb.AppendLine();
            sb.AppendLine($"<b>Performance:</b>");
            sb.AppendLine($"  FPS: {averageFPS:F1} (Low: {lowestFPS:F1})");
            sb.AppendLine($"  Physics Interpolation: {gameWorld.GetPhysicsInterpolationFactor():F2}");
            
            var stats = gameWorld.GetPhysicsDebugStats();
            sb.AppendLine($"  Frame Time: {stats.CurrentFrameTime * 1000:F1}ms");
            sb.AppendLine($"  Physics Steps: {stats.TotalPhysicsSteps}");
            
            debugText.text = sb.ToString();
        }
        
        void UpdateTrajectory()
        {
            if (!showTrajectory || trajectoryRenderer == null)
            {
                trajectoryRenderer.enabled = false;
                return;
            }
            
            trajectoryRenderer.enabled = true;
            trajectoryPoints.Clear();
            
            // Simulate trajectory
            Vector2 simPos = new Vector2(player.Position.X, player.Position.Y);
            Vector2 simVel = new Vector2(player.Velocity.X, player.Velocity.Y);
            bool simGrounded = player.IsGrounded;
            
            for (int i = 0; i < 60; i++) // 1 second prediction
            {
                trajectoryPoints.Add(new Vector3(simPos.x, simPos.y, 0));
                
                // Simple physics simulation
                if (!simGrounded)
                    simVel.y -= MaplePhysics.Gravity * MaplePhysics.FIXED_TIMESTEP;
                    
                simPos += simVel * MaplePhysics.FIXED_TIMESTEP;
                
                // Check ground (simplified)
                if (simPos.y <= 0.3f)
                {
                    simPos.y = 0.3f;
                    simGrounded = true;
                    simVel.y = 0;
                }
            }
            
            trajectoryRenderer.positionCount = trajectoryPoints.Count;
            trajectoryRenderer.SetPositions(trajectoryPoints.ToArray());
        }
        
        void ResetPlayerPosition()
        {
            player.Position = new MapleClient.GameLogic.Vector2(0, 0.3f);
            player.Velocity = MapleClient.GameLogic.Vector2.Zero;
            player.IsGrounded = true;
            Debug.Log("Player position reset");
        }
        
        void ResetPhysicsParameters()
        {
            // Reset sliders to default values
            if (parameterSliders.ContainsKey("WalkSpeed"))
                parameterSliders["WalkSpeed"].value = 1.25f;
            if (parameterSliders.ContainsKey("WalkAccel"))
                parameterSliders["WalkAccel"].value = 14f;
            if (parameterSliders.ContainsKey("WalkFriction"))
                parameterSliders["WalkFriction"].value = 8f;
            if (parameterSliders.ContainsKey("JumpPower"))
                parameterSliders["JumpPower"].value = 5.55f;
            if (parameterSliders.ContainsKey("Gravity"))
                parameterSliders["Gravity"].value = 20f;
            if (parameterSliders.ContainsKey("TerminalVel"))
                parameterSliders["TerminalVel"].value = 6.7f;
                
            Debug.Log("Physics parameters reset to defaults");
        }
        
        // UI Helper Methods
        Text CreateLabel(Transform parent, string text, UnityEngine.Vector2 position, int fontSize, TextAnchor anchor)
        {
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(parent);
            
            var textComponent = labelObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = anchor;
            
            var rect = labelObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(-20, 20);
            rect.anchoredPosition = position;
            
            return textComponent;
        }
        
        void CreateSlider(Transform parent, string name, string label, float min, float max, float value, 
            ref float yPos, System.Action<float> onValueChanged)
        {
            // Label
            var labelText = CreateLabel(parent, $"{label}: {value:F2}", new Vector2(0, yPos), 12, TextAnchor.MiddleLeft);
            parameterLabels[name] = labelText;
            yPos -= 20f;
            
            // Slider
            var sliderObj = new GameObject($"Slider_{name}");
            sliderObj.transform.SetParent(parent);
            
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            
            // Background
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform);
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // Fill area
            var fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform);
            var fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = new Vector2(-20, 0);
            fillAreaRect.anchoredPosition = new Vector2(10, 0);
            
            // Fill
            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform);
            var fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.6f, 1f);
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.sizeDelta = new Vector2(10, 0);
            
            // Handle area
            var handleAreaObj = new GameObject("Handle Slide Area");
            handleAreaObj.transform.SetParent(sliderObj.transform);
            var handleAreaRect = handleAreaObj.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = new Vector2(-20, 0);
            handleAreaRect.anchoredPosition = new Vector2(10, 0);
            
            // Handle
            var handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleAreaObj.transform);
            var handleImage = handleObj.AddComponent<Image>();
            handleImage.color = Color.white;
            var handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);
            
            // Setup slider components
            slider.targetGraphic = handleImage;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            
            // Position
            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 1);
            sliderRect.anchorMax = new Vector2(1, 1);
            sliderRect.pivot = new Vector2(0.5f, 1);
            sliderRect.sizeDelta = new Vector2(-20, 20);
            sliderRect.anchoredPosition = new Vector2(0, yPos);
            
            // Callback
            slider.onValueChanged.AddListener(val => 
            {
                labelText.text = $"{label}: {val:F2}";
                onValueChanged?.Invoke(val);
            });
            
            parameterSliders[name] = slider;
            yPos -= 30f;
        }
        
        void CreateToggle(Transform parent, string name, string label, bool value, UnityEngine.Vector2 position,
            System.Action<bool> onValueChanged)
        {
            var toggleObj = new GameObject($"Toggle_{name}");
            toggleObj.transform.SetParent(parent);
            
            var toggle = toggleObj.AddComponent<Toggle>();
            
            // Background
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(toggleObj.transform);
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(20, 20);
            bgRect.anchoredPosition = Vector2.zero;
            
            // Checkmark
            var checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(bgObj.transform);
            var checkImage = checkObj.AddComponent<Image>();
            checkImage.color = Color.white;
            var checkRect = checkObj.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.sizeDelta = new Vector2(-4, -4);
            checkRect.anchoredPosition = Vector2.zero;
            
            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform);
            var text = labelObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 12;
            text.color = Color.white;
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(1, 0.5f);
            labelRect.sizeDelta = new Vector2(-30, 20);
            labelRect.anchoredPosition = new Vector2(25, 0);
            
            // Setup toggle
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = value;
            
            // Position
            var toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0, 1);
            toggleRect.anchorMax = new Vector2(0, 1);
            toggleRect.pivot = new Vector2(0, 1);
            toggleRect.sizeDelta = new Vector2(150, 20);
            toggleRect.anchoredPosition = position;
            
            toggle.onValueChanged.AddListener((val) => onValueChanged(val));
        }
        
        void CreateButton(Transform parent, string name, string text, UnityEngine.Vector2 position, UnityEngine.Vector2 size,
            System.Action onClick)
        {
            var buttonObj = new GameObject($"Button_{name}");
            buttonObj.transform.SetParent(parent);
            
            var button = buttonObj.AddComponent<Button>();
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f);
            
            // Text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            var textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            // Setup button
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());
            
            // Position
            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 1);
            buttonRect.anchorMax = new Vector2(0, 1);
            buttonRect.pivot = new Vector2(0, 1);
            buttonRect.sizeDelta = size;
            buttonRect.anchoredPosition = position;
        }
        
        // Event handlers removed - physics is internal to GameWorld
        
        void OnDestroy()
        {
            // Cleanup if needed
        }
    }
    
    // Simple camera follow script
    public class SmoothCameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.3f;
        public Vector3 offset = new Vector3(0, 0, -10);
        
        private Vector3 velocity = Vector3.zero;
        
        void LateUpdate()
        {
            if (target != null)
            {
                Vector3 targetPosition = target.position + offset;
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
            }
        }
    }
}