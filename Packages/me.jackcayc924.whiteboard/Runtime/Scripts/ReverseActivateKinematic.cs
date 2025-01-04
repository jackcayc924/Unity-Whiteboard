using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visualization.Tools.Whiteboard
{
    public class ReverseActivateKinematic : MonoBehaviour
    {
        private Rigidbody rigidBody;

        void Start()
        {
            rigidBody = gameObject.GetComponent<Rigidbody>();
        }

        public void ActivateKinematic()
        {
            if (rigidBody != null)
            {
                rigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rigidBody.isKinematic = true;
            }
        }

        public void DeactivateKinematic()
        {
            if (rigidBody != null)
            {
                rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidBody.isKinematic = false;
            }
        }
    }
}
