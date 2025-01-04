using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JackCayc924.Whiteboard
{
    public class ResetDrawingMaterials : MonoBehaviour
    {
        public DragMarker[] DrawingMaterials;
        public UpdateTipPosition MarkerHandler;
        private Vector3[] positions;
        void Start()
        {
            positions = new Vector3[DrawingMaterials.Length];

            for (int i = 0; i < DrawingMaterials.Length; i++)
            {
                positions[i] = DrawingMaterials[i].transform.position;
            }
        }

        public void ResetMaterials()
        {
            MarkerHandler.SetToggle(false);
            for (int i = 0; i < DrawingMaterials.Length; i++)
            {
                DrawingMaterials[i].transform.position = positions[i];
            }
        }
    }
}
