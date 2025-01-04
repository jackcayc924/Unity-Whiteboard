using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror; // Mirror namespace

namespace JackCayc924.Whiteboard
{
    // Mirror message to update a Marker’s pen size
    public struct PenSizeMessage : NetworkMessage
    {
        public int markerId;
        public int penSize;
    }

    public class NetworkPenSize : NetworkBehaviour
    {
        // List of Markers in the scene
        public List<Marker> Markers;
        // If you still use a slider, or any other UI
        public Slider Slider;

        private bool sendUpdate = false;
        private DateTime lastUpdate = DateTime.Now;
        private readonly float timeout = 100f; // ms

        // 1) On server → register that we can receive PenSizeMessage from clients
        public override void OnStartServer()
        {
            base.OnStartServer();
            NetworkServer.RegisterHandler<PenSizeMessage>(OnServerReceivePenSizeMessage);
        }

        // 2) On client → register that we can receive PenSizeMessage from the server
        public override void OnStartClient()
        {
            base.OnStartClient();
            NetworkClient.RegisterHandler<PenSizeMessage>(OnClientReceivePenSizeMessage);
        }

        private void Update()
        {
            // Throttle sending every 100ms
            if (!sendUpdate)
            {
                sendUpdate = (DateTime.Now - lastUpdate) > TimeSpan.FromMilliseconds(timeout);
            }
        }

        // 3) Public method to change pen size by button (similar to your original code)
        public void ChangePenSizeButton(int id, int value)
        {
            // Locally update
            foreach (Marker marker in Markers)
            {
                if (marker.GetID() == id)
                {
                    marker.SetPenSize(value);
                    marker.SetSizeText(value.ToString());
                    break;
                }
            }

            // Send to the network if allowed
            if (sendUpdate && isClient)
            {
                lastUpdate = DateTime.Now;  // reset throttle
                PenSizeMessage msg = new PenSizeMessage
                {
                    markerId = id,
                    penSize = value
                };
                NetworkClient.Send(msg);
            }
        }

        // 4) Server receives the pen size message, then relays to all clients
        private void OnServerReceivePenSizeMessage(NetworkConnectionToClient conn, PenSizeMessage msg)
        {
            // You could do server-side validation or logic here
            // Then relay to all clients
            NetworkServer.SendToAll(msg);
        }

        // 5) All clients (including the original) receive the relayed message
        private void OnClientReceivePenSizeMessage(PenSizeMessage msg)
        {
            // If somehow not a client, skip
            if (!isClient) return;

            // Actually update the pen size
            foreach (Marker marker in Markers)
            {
                if (marker != null && marker.GetID() == msg.markerId)
                {
                    marker.SetPenSize(msg.penSize);
                    marker.SetSizeText(msg.penSize.ToString());
                    break;
                }
            }

            // Throttle reset
            sendUpdate = false;
        }
    }
}
