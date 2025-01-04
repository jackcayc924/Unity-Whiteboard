using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

namespace JackCayc924.Whiteboard
{
    public class UpdateTipPosition : MonoBehaviour
    {
        public Vector3 OffsetVector
        {
            get { return offset; }
            set
            {
                offset = value;
            }
        }

        public DragMarker[] MarkerList;
        public CurrentlyHeldWhiteboardObj CurrentlyHeldWhiteboardObj;
        public Toggle IsDrawingToggle;
        public Toggle DrawWithPointersToggle;
        public Vector3 offset;

        public ActionsVRWhiteboard Actions;
        private bool SteamVRActive = false;
        private Vector3 newPos;

        private void Start()
        {
            SteamVRActive = SteamVR.initializedState == SteamVR.InitializedStates.InitializeSuccess;
        }

        void Update()
        {
            if (CurrentlyHeldWhiteboardObj.GetObject() != null)
            {
                var DragMarkerScript = CurrentlyHeldWhiteboardObj.GetObject().GetComponent<DragMarker>();

                if (DragMarkerScript != null)
                {
                    if (SteamVRActive)
                    {
                        HandleVRInput(DragMarkerScript);
                    }
                    else
                    {
                        HandleDesktopInput(DragMarkerScript);
                    }
                }
            }
        }
        private void HandleVRInput(DragMarker DragMarkerScript)
        {
            if(!DrawWithPointersToggle.isOn)
            {
                return;
            }

            SteamVR_Behaviour_Pose activePose = null;
            if (Actions.HoldingTriggerLeft())
            {
                activePose = Actions.LeftHand;
            }
            else if (Actions.HoldingTriggerRight())
            {
                activePose = Actions.RightHand;
            }

            if (activePose != null && IsDrawingToggle.isOn)
            {
                RaycastHit hit;
                if (Physics.Raycast(activePose.transform.position, activePose.transform.forward, out hit))
                {
                    newPos = hit.point + hit.normal * 0.01f;
                    CurrentlyHeldWhiteboardObj.GetObject().transform.position = newPos;
                }

                var collider = CurrentlyHeldWhiteboardObj.GetObject().GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                DragMarkerScript.enabled = true;
            }
            else
            {
                ResetMarkerPosition(DragMarkerScript);
            }
        }

        private void HandleDesktopInput(DragMarker DragMarkerScript)
        {
            if (Input.GetMouseButton(0) && IsDrawingToggle.isOn)
            {
                newPos = CurrentlyHeldWhiteboardObj.GetObject().transform.position;
                DragMarkerScript.enabled = true;

                var collider = CurrentlyHeldWhiteboardObj.GetObject().GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }
            else
            {
                ResetMarkerPosition(DragMarkerScript);
            }
        }
        private void ResetMarkerPosition(DragMarker DragMarkerScript)
        {
            if (DragMarkerScript.enabled != false)
            {
                DragMarkerScript.enabled = false;
                CurrentlyHeldWhiteboardObj.GetObject().gameObject.transform.position = newPos + offset;
                newPos = CurrentlyHeldWhiteboardObj.GetObject().gameObject.transform.position;
            }
        }

        public void SetToggle(bool status)
        {
            IsDrawingToggle.isOn = status;
        }
        public void ResetMarker()
        {
            var DragMarkerScript = CurrentlyHeldWhiteboardObj.GetObject().GetComponent<DragMarker>();
            if (DragMarkerScript != null)
            {
                if (DragMarkerScript.enabled != false)
                {
                    DragMarkerScript.enabled = false;
                    CurrentlyHeldWhiteboardObj.GetObject().gameObject.transform.position = newPos + offset;
                    newPos = CurrentlyHeldWhiteboardObj.GetObject().gameObject.transform.position;
                }
            }
            CurrentlyHeldWhiteboardObj.ResetCurrentObject();

            foreach (DragMarker marker in MarkerList)
            {
                var collider = marker.GetComponent<Collider>();
                if (collider == null)
                {
                    marker.gameObject.AddComponent<SphereCollider>();
                }
            }
        }
    }
}
