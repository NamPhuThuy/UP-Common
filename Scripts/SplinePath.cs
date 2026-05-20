using UnityEngine;
using System.Collections.Generic;

namespace NamPhuThuy.Common
{
    [AddComponentMenu("NamPhuThuy/Common/Spline Path")]
    public class SplinePath : MonoBehaviour
    {
        public List<Vector3> points = new List<Vector3>()
        {
            new Vector3(-2, 0, 0),
            new Vector3(-1, 1, 0),
            new Vector3(1, -1, 0),
            new Vector3(2, 0, 0)
        };

        public bool closedLoop = false;
        public Color pathColor = new Color(0.2f, 0.8f, 1f, 1f); // Vibrant light blue
        [Range(0.1f, 2f)] public float handleSize = 0.5f;

        private void OnDrawGizmos()
        {
            if (points == null || points.Count < 2) return;

            Gizmos.color = pathColor;
            
            // Draw lines between points
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 p1 = transform.TransformPoint(points[i]);
                Vector3 p2 = transform.TransformPoint(points[i + 1]);
                Gizmos.DrawLine(p1, p2);
            }

            // Draw end line if closed loop
            if (closedLoop)
            {
                Vector3 pFirst = transform.TransformPoint(points[0]);
                Vector3 pLast = transform.TransformPoint(points[points.Count - 1]);
                Gizmos.DrawLine(pLast, pFirst);
            }

            // Draw small spheres at each point
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 p = transform.TransformPoint(points[i]);
                Gizmos.color = (i == 0) ? Color.green : (i == points.Count - 1) ? Color.red : pathColor;
                Gizmos.DrawSphere(p, handleSize * 0.25f);
            }
        }
    }
}
