using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace JackCayc924.Whiteboard
{
    public struct MarkerColorMessage : NetworkMessage
    {
        public int markerId;
        public Color color;
    }

    public class NetworkMarkerColor : NetworkBehaviour
    {
        public List<Marker> markers;

        private bool sendUpdate = false;
        private DateTime lastUpdate = DateTime.Now;
        private float timeout = 100f;

        private void Start()
        {
            // Example: set eraser color to black
            foreach (Marker marker in markers)
            {
                if (marker.name.ToLower().Contains("eraser"))
                {
                    Color c = new Color32(0, 0, 0, 255);
                    marker.GetMarker().GetComponent<MeshRenderer>().material.color = c;
                    marker.SetEraserCurrentColor(c);
                }
            }
        }

        private void Update()
        {
            if (!sendUpdate)
            {
                sendUpdate = (DateTime.Now - lastUpdate) > TimeSpan.FromMilliseconds(timeout);
            }
        }

        // 2) Register message handler(s) on client/server
        public override void OnStartServer()
        {
            // when the server starts, register handler for MarkerColorMessage from clients
            NetworkServer.RegisterHandler<MarkerColorMessage>(OnServerReceiveMarkerColor);
        }

        public override void OnStartClient()
        {
            // when the client starts, register handler for MarkerColorMessage from server
            NetworkClient.RegisterHandler<MarkerColorMessage>(OnClientReceiveMarkerColor);
        }

        // 3) Client → Server: send a color update
        public void SendColorUpdate(Color color, int markerId)
        {
            if (isClient)
            {
                MarkerColorMessage msg = new MarkerColorMessage
                {
                    markerId = markerId,
                    color = color
                };
                NetworkClient.Send(msg);
            }
        }

        // 4) Server receives the message from a client
        private void OnServerReceiveMarkerColor(NetworkConnectionToClient conn, MarkerColorMessage msg)
        {
            // We might want to do server-side logic or checks here.
            // Then broadcast to all clients the same color update:
            NetworkServer.SendToAll(msg);
        }

        // 5) Client receives the message from the server
        private void OnClientReceiveMarkerColor(MarkerColorMessage msg)
        {
            // Update color locally
            List<Marker> matchingMarkers = markers.Where(m => m.id == msg.markerId).ToList();
            if (matchingMarkers.Count > 0)
            {
                matchingMarkers.First().ChangeColor(msg.color);
            }
        }
    }
}
