using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JackCayc924.Whiteboard
{
    public class DetectOrientation : MonoBehaviour
    {
        public GameObject WhiteboardGameobject;
        public UpdateTipPosition UpdateTipPosition;
        private List<DragMarker> DragMarkers;

        void Awake()
        {
            DragMarkers = FindObjectsOfType<DragMarker>().ToList();
            foreach (DragMarker marker in DragMarkers)
            {
                marker.Custom = Vector3.zero;
            }

            UpdateTipPosition.OffsetVector = Vector3.zero;
            AdjustOffsetsBasedOnRotation();
        }

        public void AdjustOffsetsBasedOnRotation()
        {
            // Calculate the direction "away from the whiteboard" (negative local z-axis)
            Vector3 offsetDirection = WhiteboardGameobject.transform.TransformDirection(Vector3.left);

            // Adjust DragMarker's Custom based on the calculated offset direction
            foreach (DragMarker marker in DragMarkers)
            {
                marker.Custom = offsetDirection * 0.018f;
            }

            // Adjust UpdateTipPosition.OffsetVector dynamically
            UpdateTipPosition.OffsetVector = offsetDirection * 0.1f;
        }
    }
}
