using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

namespace JackCayc924.Whiteboard
{
    public class DragMarker : MonoBehaviour
    {
        public Vector3 Custom
        {
            get { return CustomOffset; }
            set
            {
                CustomOffset = value;
                var eh = CustomOffset;
                if (eh != null)
                    eh = Vector3.zero;
            }
        }

        [Header("Drag Marker Params")]
        public Marker marker;
        public GameObject PencilTip;
        public Vector3 CustomOffset;
        public Toggle isDrawingToggle;

        [Header("VR Specific Params")]
        public ActionsVRWhiteboard Actions;
        private bool SteamVRActive = false;
        private ImagePaster ImagePaster;

        private void Start()
        {
            if (SteamVR.initializedState == SteamVR.InitializedStates.InitializeSuccess)
            {
                SteamVRActive = true;
            }

            ImagePaster = FindObjectOfType<ImagePaster>();
        }

        void Update()
        {
            if (Input.GetMouseButton(0) && isDrawingToggle.isOn)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    PencilTip.transform.position = hit.point + Custom;
                }
            }

            if(Actions.HoldingTriggerLeft() && isDrawingToggle.isOn && !ImagePaster.IsLoadingImage())
            {
                PerformRaycastForVRController(Actions.LeftHand, isDrawingToggle.isOn);
            }

            if (Actions.HoldingTriggerRight() && isDrawingToggle.isOn && !ImagePaster.IsLoadingImage())
            {
                PerformRaycastForVRController(Actions.RightHand, isDrawingToggle.isOn);
            }
        }

        private void PerformRaycastForVRController(SteamVR_Behaviour_Pose controllerPose, bool isDrawing)
        {
            if (controllerPose != null && isDrawing)
            {
                Ray ray = new Ray(controllerPose.transform.position, controllerPose.transform.forward);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    PencilTip.transform.position = hit.point + CustomOffset;
                }
            }
        }

        public Marker GetMarkerReference()
        {
            return marker;
        }
    }
}
