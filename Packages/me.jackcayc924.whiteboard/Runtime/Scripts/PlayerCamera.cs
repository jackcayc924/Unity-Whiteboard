using UnityEngine;
using System;
using Mirror;

namespace JackCayc924.Whiteboard
{
    public class PlayerCamera : NetworkBehaviour
    {
        public static event Action<Camera> OnPlayerCameraSpawned;

        public Camera playerCamera;

        void Start()
        {
            //if (!isLocalPlayer) return; // Only process for the local player

            // Assign the player's camera
            foreach (Camera camera in GetComponentsInChildren<Camera>())
            {
                if (camera.enabled)
                {
                    playerCamera = camera;
                    break;
                }
            }

            Debug.Log($"Local Player Camera Assigned: {playerCamera.name}");

            // Notify other scripts about this camera
            OnPlayerCameraSpawned?.Invoke(playerCamera);
        }
    }
}
