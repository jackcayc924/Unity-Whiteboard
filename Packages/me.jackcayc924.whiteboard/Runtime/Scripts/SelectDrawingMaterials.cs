using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JackCayc924.Whiteboard
{
    public class SelectDrawingMaterials : MonoBehaviour
    {
        public DragMarker[] DrawingMaterials;

        public void SelectMaterials()
        {
            for (int i = 0; i < DrawingMaterials.Length; i++)
            {
                var Collider = DrawingMaterials[i].GetComponent<BoxCollider>();
                if (Collider == null)
                {
                    DrawingMaterials[i].gameObject.AddComponent<BoxCollider>();
                }
            }
        }
    }
}
