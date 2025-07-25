using UnityEngine;
using MapleClient.GameLogic.Core;

namespace MapleClient.GameView
{
    public class LadderView : MonoBehaviour
    {
        private LadderInfo ladder;
        private LineRenderer lineRenderer;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.05f;
                lineRenderer.endWidth = 0.05f;
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = new Color(0.5f, 0.3f, 0.1f, 0.8f); // Brown color for ladder
                lineRenderer.endColor = new Color(0.5f, 0.3f, 0.1f, 0.8f);
                lineRenderer.positionCount = 2;
            }
        }

        public void SetLadder(LadderInfo ladder)
        {
            this.ladder = ladder;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (ladder != null && lineRenderer != null)
            {
                // Convert from game logic coordinates to Unity coordinates
                lineRenderer.SetPosition(0, new Vector3(ladder.X / 100f, ladder.Y1 / 100f, 0));
                lineRenderer.SetPosition(1, new Vector3(ladder.X / 100f, ladder.Y2 / 100f, 0));
            }
        }
    }
}