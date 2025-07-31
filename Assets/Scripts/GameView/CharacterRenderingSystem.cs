using UnityEngine;
using System.Collections.Generic;

namespace MapleUnity.GameView
{
    /// <summary>
    /// Character rendering system that replicates the exact positioning logic from the original MapleStory client.
    /// Based on analysis of the C++ source code from HeavenClient.
    /// </summary>
    public class CharacterRenderingSystem : MonoBehaviour
    {
        [System.Serializable]
        public class AttachmentPoint
        {
            public string name;
            public Vector2 position;
        }

        [System.Serializable]
        public class BodyPartData
        {
            public string partName;
            public List<AttachmentPoint> attachmentPoints = new List<AttachmentPoint>();
            
            public Vector2 GetAttachmentPoint(string pointName)
            {
                var point = attachmentPoints.Find(p => p.name == pointName);
                return point != null ? point.position : Vector2.zero;
            }
        }

        // Core positioning formulas from the C++ client
        public static class PositioningFormulas
        {
            /// <summary>
            /// Calculate arm position based on the original formula:
            /// arm_position = arm.hand - arm.navel + body.navel
            /// </summary>
            public static Vector2 CalculateArmPosition(BodyPartData arm, BodyPartData body)
            {
                Vector2 armHand = arm.GetAttachmentPoint("hand");
                Vector2 armNavel = arm.GetAttachmentPoint("navel");
                Vector2 bodyNavel = body.GetAttachmentPoint("navel");
                
                return armHand - armNavel + bodyNavel;
            }

            /// <summary>
            /// Calculate head position based on the original formula:
            /// head_position = body.neck - head.neck
            /// </summary>
            public static Vector2 CalculateHeadPosition(BodyPartData body, BodyPartData head)
            {
                Vector2 bodyNeck = body.GetAttachmentPoint("neck");
                Vector2 headNeck = head.GetAttachmentPoint("neck");
                
                return bodyNeck - headNeck;
            }

            /// <summary>
            /// Calculate face position based on the original formula:
            /// face_position = body.neck - head.neck + head.brow
            /// </summary>
            public static Vector2 CalculateFacePosition(BodyPartData body, BodyPartData head)
            {
                Vector2 bodyNeck = body.GetAttachmentPoint("neck");
                Vector2 headNeck = head.GetAttachmentPoint("neck");
                Vector2 headBrow = head.GetAttachmentPoint("brow");
                
                return bodyNeck - headNeck + headBrow;
            }

            /// <summary>
            /// Calculate hair position based on the original formula:
            /// hair_position = head.brow - head.neck + body.neck
            /// </summary>
            public static Vector2 CalculateHairPosition(BodyPartData body, BodyPartData head)
            {
                Vector2 headBrow = head.GetAttachmentPoint("brow");
                Vector2 headNeck = head.GetAttachmentPoint("neck");
                Vector2 bodyNeck = body.GetAttachmentPoint("neck");
                
                return headBrow - headNeck + bodyNeck;
            }
        }

        // Drawing order constants (Z-layer values)
        public static class DrawingOrder
        {
            public const int HAIR_BELOW_BODY = -10;
            public const int CAPE = -9;
            public const int SHIELD_BELOW_BODY = -8;
            public const int WEAPON_BELOW_BODY = -7;
            public const int HAT_BELOW_BODY = -6;
            public const int BODY = 0; // Base layer
            public const int GLOVES_OVER_BODY = 1;
            public const int SHOES = 2;
            public const int ARM_BELOW_HEAD = 3;
            public const int PANTS = 4;
            public const int TOP = 5;
            public const int ARM_BELOW_HEAD_OVER_MAIL = 6;
            public const int SHIELD_OVER_HAIR = 7;
            public const int EARRINGS = 8;
            public const int HEAD = 9;
            public const int HAIR_SHADE = 10;
            public const int HAIR_DEFAULT = 11;
            public const int FACE = 12;
            public const int FACE_ACC = 13;
            public const int EYE_ACC = 14;
            public const int HAT = 15;
            public const int WEAPON = 16;
            public const int ARM = 17;
            public const int GLOVES = 18;
            public const int HAND_OVER_WEAPON = 19;
        }

        [Header("Body Part References")]
        public GameObject bodyObject;
        public GameObject armObject;
        public GameObject headObject;
        public GameObject faceObject;
        public GameObject hairObject;

        [Header("Debug Settings")]
        public bool showAttachmentPoints = false;
        public bool logPositionCalculations = false;

        private BodyPartData bodyData;
        private BodyPartData armData;
        private BodyPartData headData;

        void Start()
        {
            // Initialize body part data from the GameObjects
            InitializeBodyPartData();
        }

        void InitializeBodyPartData()
        {
            // In a real implementation, these would be loaded from the NX/asset data
            // For now, using example values from the analysis
            
            bodyData = new BodyPartData { partName = "body" };
            bodyData.attachmentPoints.Add(new AttachmentPoint { name = "navel", position = Vector2.zero });
            bodyData.attachmentPoints.Add(new AttachmentPoint { name = "neck", position = new Vector2(-2, -30) });

            armData = new BodyPartData { partName = "arm" };
            armData.attachmentPoints.Add(new AttachmentPoint { name = "navel", position = Vector2.zero });
            armData.attachmentPoints.Add(new AttachmentPoint { name = "hand", position = new Vector2(-10, 5) });

            headData = new BodyPartData { partName = "head" };
            headData.attachmentPoints.Add(new AttachmentPoint { name = "neck", position = Vector2.zero });
            headData.attachmentPoints.Add(new AttachmentPoint { name = "brow", position = new Vector2(-1, -13) });
        }

        public void PositionCharacterParts(Vector2 characterPosition, bool flipped)
        {
            float flipScale = flipped ? -1f : 1f;

            // 1. Position body (base reference)
            if (bodyObject != null)
            {
                bodyObject.transform.position = characterPosition;
                bodyObject.transform.localScale = new Vector3(flipScale, 1, 1);
                SetSortingOrder(bodyObject, DrawingOrder.BODY);
            }

            // 2. Position arm
            if (armObject != null && bodyData != null && armData != null)
            {
                Vector2 armPosition = PositioningFormulas.CalculateArmPosition(armData, bodyData);
                armObject.transform.position = characterPosition + armPosition;
                armObject.transform.localScale = new Vector3(flipScale, 1, 1);
                SetSortingOrder(armObject, DrawingOrder.ARM_BELOW_HEAD);
                
                if (logPositionCalculations)
                {
                    Debug.Log($"Arm Position: base={characterPosition}, shift={armPosition}, final={characterPosition + armPosition}");
                }
            }

            // 3. Position head
            if (headObject != null && bodyData != null && headData != null)
            {
                Vector2 headPosition = PositioningFormulas.CalculateHeadPosition(bodyData, headData);
                headObject.transform.position = characterPosition + headPosition;
                headObject.transform.localScale = new Vector3(flipScale, 1, 1);
                SetSortingOrder(headObject, DrawingOrder.HEAD);
                
                if (logPositionCalculations)
                {
                    Debug.Log($"Head Position: base={characterPosition}, shift={headPosition}, final={characterPosition + headPosition}");
                }
            }

            // 4. Position face
            if (faceObject != null && bodyData != null && headData != null)
            {
                Vector2 facePosition = PositioningFormulas.CalculateFacePosition(bodyData, headData);
                faceObject.transform.position = characterPosition + facePosition;
                faceObject.transform.localScale = new Vector3(flipScale, 1, 1);
                SetSortingOrder(faceObject, DrawingOrder.FACE);
                
                if (logPositionCalculations)
                {
                    Debug.Log($"Face Position: base={characterPosition}, shift={facePosition}, final={characterPosition + facePosition}");
                }
            }

            // 5. Position hair
            if (hairObject != null && bodyData != null && headData != null)
            {
                Vector2 hairPosition = PositioningFormulas.CalculateHairPosition(bodyData, headData);
                hairObject.transform.position = characterPosition + hairPosition;
                hairObject.transform.localScale = new Vector3(flipScale, 1, 1);
                SetSortingOrder(hairObject, DrawingOrder.HAIR_DEFAULT);
                
                if (logPositionCalculations)
                {
                    Debug.Log($"Hair Position: base={characterPosition}, shift={hairPosition}, final={characterPosition + hairPosition}");
                }
            }
        }

        void SetSortingOrder(GameObject obj, int order)
        {
            var spriteRenderer = obj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = order;
            }
        }

        void OnDrawGizmos()
        {
            if (!showAttachmentPoints) return;

            // Draw attachment points for debugging
            if (bodyData != null && bodyObject != null)
            {
                DrawAttachmentPoints(bodyObject.transform.position, bodyData, Color.blue);
            }

            if (armData != null && armObject != null)
            {
                DrawAttachmentPoints(armObject.transform.position, armData, Color.red);
            }

            if (headData != null && headObject != null)
            {
                DrawAttachmentPoints(headObject.transform.position, headData, Color.green);
            }
        }

        void DrawAttachmentPoints(Vector3 basePosition, BodyPartData data, Color color)
        {
            Gizmos.color = color;
            foreach (var point in data.attachmentPoints)
            {
                Vector3 worldPos = basePosition + new Vector3(point.position.x, point.position.y, 0);
                Gizmos.DrawWireSphere(worldPos, 2f);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(worldPos, point.name);
                #endif
            }
        }
    }
}