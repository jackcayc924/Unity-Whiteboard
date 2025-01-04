using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JackCayc924.Whiteboard
{
    public class HotkeyDrawingToggle : MonoBehaviour
    {
        public UpdateTipPosition UpdateTipPosition;
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                UpdateTipPosition.SetToggle(!UpdateTipPosition.IsDrawingToggle.isOn);
            }
        }
    }
}