using UnityEngine;
using Mirror;

namespace JackCayc924.Whiteboard
{
    public class CurrentlyHeldWhiteboardObj : NetworkBehaviour
    {
        public UpdateTipPosition UpdateTipPosition;
        [HideInInspector]
        public GameObject WhiteboardObject;
        private Camera currentPlayerCamera;

        private void OnEnable()
        {
            PlayerCamera.OnPlayerCameraSpawned += HandleNewPlayerCamera;
        }

        private void HandleNewPlayerCamera(Camera camera)
        {
            currentPlayerCamera = camera;
        }

        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                RaycastHit hit;
                if (currentPlayerCamera == null) return;

                Ray ray = currentPlayerCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform.gameObject.tag == "Grabbable" || hit.transform.gameObject.tag == "Maneuverable")
                    {
                        DragMarker marker = hit.transform.gameObject.GetComponent<DragMarker>();
                        if (marker != null)
                        {
                            if (WhiteboardObject != null)
                            {
                                if (WhiteboardObject.gameObject.name != hit.transform.gameObject.name)
                                {
                                    UpdateTipPosition.ResetMarker();
                                    WhiteboardObject = hit.transform.gameObject;
                                }
                            }
                            else
                            {
                                WhiteboardObject = hit.transform.gameObject;
                            }
                        }
                    }
                }
            }
        }

        public GameObject GetObject()
        {
            if (WhiteboardObject != null)
            {
                return WhiteboardObject;
            }
            else
            {
                return null;
            }
        }

        public void ResetCurrentObject()
        {
            WhiteboardObject = null;
        }

        public void SetCurrentWhiteboardObjectVR(GameObject gameObject)
        {
            if (Valve.VR.SteamVR.initializedState == Valve.VR.SteamVR.InitializedStates.InitializeSuccess)
            {
                WhiteboardObject = null;
                WhiteboardObject = gameObject;
            }
        }
    }
}
