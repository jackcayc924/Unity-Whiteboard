using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace JackCayc924.Whiteboard
{
    public class SetPenSizeSlider : MonoBehaviour
    {
        public List<Marker> markers;
        public CurrentlyHeldWhiteboardObj CurrentlyHeldWhiteboardObj;
        public Slider Slider;
        public TMP_InputField InputField;
        public TextMeshProUGUI PenNumberSize;
 
        /*
        public void SendSizeUpdate()
        {
            if (CurrentlyHeldWhiteboardObj.GetObject() != null)
            {
                NetworkPenSize networkPenSize = CurrentlyHeldWhiteboardObj.GetObject().transform.parent.GetComponent<NetworkPenSize>();
                DragMarker markerReference = CurrentlyHeldWhiteboardObj.GetObject().GetComponent<DragMarker>();
                if(networkPenSize != null && markerReference != null)
                    networkPenSize.ChangePenSize(markerReference.marker.GetID());
            }
        }
        */

        public void SendSizeUpdateButtonInc()
        {
            if (CurrentlyHeldWhiteboardObj.GetObject() != null)
            {
                NetworkPenSize networkPenSize = CurrentlyHeldWhiteboardObj.GetObject().transform.parent.GetComponent<NetworkPenSize>();
                DragMarker markerReference = CurrentlyHeldWhiteboardObj.GetObject().GetComponent<DragMarker>();
                if (networkPenSize != null && markerReference != null)
                {
                    int newValue = markerReference.marker.penSize + int.Parse(InputField.text);
                    networkPenSize.ChangePenSizeButton(markerReference.marker.GetID(), newValue);
                }
            }
        }
            public void SendSizeUpdateButtonDec()
        {
            if (CurrentlyHeldWhiteboardObj.GetObject() != null)
            {
                NetworkPenSize networkPenSize = CurrentlyHeldWhiteboardObj.GetObject().transform.parent.GetComponent<NetworkPenSize>();
                DragMarker markerReference = CurrentlyHeldWhiteboardObj.GetObject().GetComponent<DragMarker>();
                if (networkPenSize != null && markerReference != null)
                {
                    int newValue = markerReference.marker.penSize - int.Parse(InputField.text);
                    if (newValue > 0)
                    {
                        networkPenSize.ChangePenSizeButton(markerReference.marker.GetID(), newValue);
                    }
                }
            }
        }
    }
}
