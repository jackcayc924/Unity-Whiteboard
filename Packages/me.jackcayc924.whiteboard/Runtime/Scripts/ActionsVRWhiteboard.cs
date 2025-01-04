using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

namespace JackCayc924.Whiteboard
{
    public class ActionsVRWhiteboard : MonoBehaviour
    {
        void Start()
        {
            if (SteamVR.initializedState == SteamVR.InitializedStates.InitializeSuccess)
            {
                SteamVRActive = true;
            }
        }

        public SteamVR_Action_Boolean press = null;
        public SteamVR_Input_Sources LeftHandType = SteamVR_Input_Sources.LeftHand;
        public SteamVR_Input_Sources RightHandType = SteamVR_Input_Sources.RightHand;
        public SteamVR_Behaviour_Pose LeftHand, RightHand;
        private bool SteamVRActive = false;
        private bool wasLeftTriggerHeld = false;
        private bool wasRightTriggerHeld = false;

        void Update()
        {
            if (SteamVRActive)
            {
                if (press.GetState(LeftHandType))
                {
                    wasLeftTriggerHeld = true;
                }
                else if (wasLeftTriggerHeld)
                {
                    wasLeftTriggerHeld = false;
                }

                if (press.GetState(RightHandType))
                {
                    wasRightTriggerHeld = true;
                }
                else if (wasRightTriggerHeld)
                {
                    wasRightTriggerHeld = false;
                }
            }
        }

        public bool HoldingTriggerLeft()
        {
            return wasLeftTriggerHeld;
        }

        public bool HoldingTriggerRight()
        {
            return wasRightTriggerHeld;
        }

        public bool HoldingTrigger()
        {
            return wasLeftTriggerHeld || wasRightTriggerHeld;
        }

        public bool VR_ON()
        {
            return XRSettings.enabled ? true : false;
        }

    }
}
