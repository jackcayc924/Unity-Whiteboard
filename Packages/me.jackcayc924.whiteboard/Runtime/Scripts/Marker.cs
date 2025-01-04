using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using TMPro;
using Mirror;

namespace JackCayc924.Whiteboard
{
    /// <summary>
    /// The data we send across the network.
    /// This struct implements NetworkMessage so it can be sent/received by Mirror.
    /// </summary>
    public struct DrawingDataMessage : NetworkMessage
    {
        public int x;
        public int y;
        public Color color;
        public int penSize;
        public bool isEndOfStroke;
    }

    public class DrawingData
    {
        public int x;
        public int y;
        public Color color;
        public int penSize;
        public bool isEndOfStroke;
    }

    public class Marker : NetworkBehaviour
    {
        [Header("Marker Configuration")]
        public int penSize = 5;
        public int id;
        public DragMarker marker;
        public TextMeshProUGUI sizeText;
        public Shader drawShader;
        public bool ToggleDebug;

        private Color[] _colors;
        private float tipHeight;

        private RaycastHit touch;
        private Whiteboard _whiteboard;
        private Vector2 _lastTouchPos, _tpos;
        private bool _touchedLastFrame;
        private Quaternion _lastTouchRot;
        private Color currentColor = Color.white;
        private MeshRenderer mRenderer;
        private bool _textureChanged;

        private Material _drawMaterial;

        private List<Vector2> points = new List<Vector2>();
        // Dictionary: which Whiteboard we’ve drawn on → the list of points we drew
        public Dictionary<Whiteboard, List<DrawingData>> drawnData = new Dictionary<Whiteboard, List<DrawingData>>();
        private Vector2? lastReceivedPoint = null;
        private PageHandler pageHandler;

        // 1) On server/client start, we register message handlers:
        public override void OnStartServer()
        {
            // Only the server needs to receive from clients and then broadcast.
            NetworkServer.RegisterHandler<DrawingDataMessage>(OnServerReceiveDrawingData, false);
        }

        public override void OnStartClient()
        {
            // All clients (including the host) need to handle the server’s broadcast.
            NetworkClient.RegisterHandler<DrawingDataMessage>(OnClientReceiveDrawingData, false);
        }

        // 2) Server receives the drawing data from a client and relays to everyone:
        private void OnServerReceiveDrawingData(NetworkConnectionToClient conn, DrawingDataMessage msg)
        {
            // Optionally do server-side checks or logic here before relaying

            // Relay this message to all clients (including the original sender)
            NetworkServer.SendToAll(msg);
        }

        // 3) Clients receive the relayed data and draw locally:
        private void OnClientReceiveDrawingData(DrawingDataMessage msg)
        {
            // If this is somehow called on the server, skip. Usually OnClientReceive is only called on clients.
            if (!isClient) return;

            if (ToggleDebug)
                Debug.Log($"[{DateTime.Now}] Received Marker Update: x={msg.x}, y={msg.y}, color={msg.color}, penSize={msg.penSize}, isEndOfStroke={msg.isEndOfStroke}");

            // Update local color & pen size
            currentColor = msg.color;
            penSize = msg.penSize;

            Vector2 currentPoint = new Vector2(msg.x, msg.y);

            // If the stroke ended, we clear the last point so we don’t draw a connecting line next time
            if (msg.isEndOfStroke)
            {
                lastReceivedPoint = null;
            }
            else if (IsPointInBounds(currentPoint))
            {
                // If we had a previous point, draw a line from last → current
                if (lastReceivedPoint.HasValue && IsPointInBounds(lastReceivedPoint.Value))
                {
                    DrawLine(lastReceivedPoint.Value, currentPoint);
                }
                else
                {
                    // Draw a single dot if this is the first valid point
                    DrawAtPosition(currentPoint);
                }

                // Remember this point for the next segment
                lastReceivedPoint = currentPoint;
            }
            else
            {
                // Out of bounds
                lastReceivedPoint = null;
            }
        }

        private bool IsPointInBounds(Vector2 point)
        {
            if (_whiteboard == null) return false;
            return point.x >= 0 && point.x < _whiteboard.textureSize.x &&
                   point.y >= 0 && point.y < _whiteboard.textureSize.y;
        }

        // -------------------------------------------------------------------------------------
        // Simply construct a message struct and call NetworkClient.Send(msg).
        // -------------------------------------------------------------------------------------
        private void SendDrawingData(DrawingData data)
        {
            // Only send if we’re a client
            if (!isClient) return;

            var msg = new DrawingDataMessage
            {
                x = data.x,
                y = data.y,
                color = data.color,
                penSize = data.penSize,
                isEndOfStroke = data.isEndOfStroke
            };

            NetworkClient.Send(msg);
            if (ToggleDebug)
            {
                Debug.Log(
                    $"[{DateTime.Now}] Sent Drawing Data: x={data.x}, y={data.y}, " +
                    $"color={data.color}, penSize={data.penSize}, isEndOfStroke={data.isEndOfStroke}"
                );
            }
        }

        // -------------------------------------------------------------------------------------
        // Below is the rest of your original drawing logic, largely unchanged, except
        // the #if NETWORKED_WHITEBOARD blocks are removed (Mirror covers that natively).
        // -------------------------------------------------------------------------------------

        void Start()
        {
            mRenderer = marker.GetComponent<MeshRenderer>();
            tipHeight = marker.transform.localScale.y;

            pageHandler = FindObjectOfType<PageHandler>();
            if (pageHandler != null)
            {
                _whiteboard = pageHandler.GetCurrentWhiteboard();
            }

            if (_whiteboard == null)
            {
                Debug.LogError("Whiteboard not assigned.");
            }

            // Create a unique instance of the draw material for each marker
            _drawMaterial = new Material(drawShader);

            // Set the initial color for the marker
            ChangeColor(currentColor);

            if (ToggleDebug)
                Debug.Log($"Marker {id} initialized with color {currentColor}");
        }

        private void OnDestroy()
        {
            if (_drawMaterial != null)
            {
                Destroy(_drawMaterial);
            }
        }

        void LateUpdate()
        {
            Draw();

            if (Input.GetKeyDown(KeyCode.O))
            {
                ClearWhiteboard();
            }
        }

        private void Draw()
        {
            if (_whiteboard == null || _drawMaterial == null)
            {
                if (ToggleDebug)
                    Debug.LogWarning($"Marker {id}: Whiteboard or DrawMaterial is not initialized.");
                return;
            }

            if (Physics.Raycast(marker.transform.position, transform.up, out touch, marker.transform.localScale.y))
            {
                if (touch.transform.CompareTag("Whiteboard"))
                {
                    if (_whiteboard == null)
                    {
                        _whiteboard = touch.transform.GetComponent<Whiteboard>();
                    }

                    _tpos = new Vector2(touch.textureCoord.x, touch.textureCoord.y);

                    int x = (int)(_tpos.x * _whiteboard.textureSize.x);
                    int y = (int)(_tpos.y * _whiteboard.textureSize.y);

                    // Validate bounds
                    if (y < 0 || y > _whiteboard.textureSize.y || x < 0 || x > _whiteboard.textureSize.x)
                    {
                        if (_touchedLastFrame)
                        {
                            // Send an "end of stroke"
                            var endStroke = new DrawingData
                            {
                                isEndOfStroke = true
                            };
                            SendDrawingData(endStroke);
                        }
                        _touchedLastFrame = false;
                        return;
                    }

                    Vector2 currentPos = new Vector2(x, y);

                    if (_touchedLastFrame)
                    {
                        DrawLine(_lastTouchPos, currentPos);
                    }
                    else
                    {
                        // Draw single dot if first touch
                        DrawAtPosition(currentPos);
                    }

                    // Send our drawing data
                    var data = new DrawingData
                    {
                        x = x,
                        y = y,
                        color = currentColor,
                        penSize = penSize,
                        isEndOfStroke = false
                    };
                    SendDrawingData(data);

                    // Also store it locally in drawnData
                    if (!drawnData.TryGetValue(_whiteboard, out List<DrawingData> dataList))
                    {
                        dataList = new List<DrawingData>();
                        drawnData[_whiteboard] = dataList;
                    }
                    dataList.Add(data);

                    _lastTouchPos = currentPos;
                    _lastTouchRot = transform.rotation;
                    _touchedLastFrame = true;

                    _whiteboard.hasDrawing = true;
                    return;
                }
            }

            // If the raycast doesn’t hit whiteboard, end the stroke if we were drawing
            if (_touchedLastFrame)
            {
                var endStrokeData = new DrawingData
                {
                    isEndOfStroke = true
                };
                SendDrawingData(endStrokeData);

                points.Clear();
            }
            _touchedLastFrame = false;
        }

        private void DrawLine(Vector2 start, Vector2 end)
        {
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.CeilToInt(distance / (penSize / 2f));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 interpolatedPos = Vector2.Lerp(start, end, (float)i / steps);
                DrawAtPosition(interpolatedPos);

                // Also record it in our drawnData
                var data = new DrawingData
                {
                    x = (int)interpolatedPos.x,
                    y = (int)interpolatedPos.y,
                    color = currentColor,
                    penSize = penSize,
                    isEndOfStroke = false
                };
                if (!drawnData.TryGetValue(_whiteboard, out List<DrawingData> dataList))
                {
                    dataList = new List<DrawingData>();
                    drawnData[_whiteboard] = dataList;
                }
                dataList.Add(data);
            }
        }

        private void DrawAtPosition(Vector2 position)
        {
            if (_whiteboard == null) return;

            // Set shader parameters
            _drawMaterial.SetColor("_DrawColor", currentColor);
            _drawMaterial.SetVector("_DrawPosition", new Vector4(
                position.x / _whiteboard.textureSize.x,
                position.y / _whiteboard.textureSize.y,
                0,
                0
            ));
            _drawMaterial.SetFloat("_PenSize", penSize / (float)_whiteboard.textureSize.x);

            // Blit the material onto the whiteboard’s texture
            Graphics.Blit(_whiteboard.GetRenderTexture(), _whiteboard.GetTempRenderTexture(), _drawMaterial);
            Graphics.Blit(_whiteboard.GetTempRenderTexture(), _whiteboard.GetRenderTexture());

            if (ToggleDebug)
                Debug.Log($"Marker {id} updated render texture at position: {position}");
        }

        private void ClearWhiteboard()
        {
            if (_whiteboard == null)
            {
                Debug.LogError("No whiteboard assigned to clear.");
                return;
            }

            // Overdraw old strokes with black (or the background color).
            if (drawnData.TryGetValue(_whiteboard, out List<DrawingData> dataList))
            {
                foreach (DrawingData data in dataList)
                {
                    _drawMaterial.SetColor("_DrawColor", Color.black);
                    _drawMaterial.SetVector("_DrawPosition", new Vector4(
                        data.x / (float)_whiteboard.textureSize.x,
                        data.y / (float)_whiteboard.textureSize.y,
                        0,
                        0
                    ));
                    _drawMaterial.SetFloat("_PenSize", (data.penSize + 2) / (float)_whiteboard.textureSize.x);

                    Graphics.Blit(_whiteboard.GetRenderTexture(), _whiteboard.GetTempRenderTexture(), _drawMaterial);
                    Graphics.Blit(_whiteboard.GetTempRenderTexture(), _whiteboard.GetRenderTexture());
                }
                dataList.Clear();
            }

            _whiteboard.hasDrawing = false;
            if (ToggleDebug)
                Debug.Log($"Marker {id} cleared the whiteboard.");
        }

        public void SetPenSize(int value)
        {
            penSize = value;
            _colors = Enumerable.Repeat(currentColor, penSize * penSize).ToArray();
            if (ToggleDebug)
                Debug.Log($"Marker {id} pen size set to {penSize}");
        }

        public void SetEraserCurrentColor(Color color)
        {
            currentColor = color;
            _colors = null;
            if (mRenderer != null)
            {
                mRenderer.material.color = color;
                _colors = Enumerable.Repeat(color, penSize * penSize).ToArray();
            }
        }

        public GameObject GetMarker()
        {
            return marker.gameObject;
        }

        public TextMeshProUGUI GetSizeText()
        {
            return sizeText;
        }

        public int GetID()
        {
            return id;
        }

        public void SetSizeText(string value)
        {
            sizeText.text = value;
        }

        public void SetNewWhiteboard(Whiteboard whiteboard)
        {
            _whiteboard = whiteboard;
        }

        public Whiteboard GetCurrentWhiteboard()
        {
            return _whiteboard;
        }

        public void ChangeColor(Color color)
        {
            _colors = null;
            if (mRenderer != null)
            {
                mRenderer.material.color = color;
                _colors = Enumerable.Repeat(mRenderer.material.color, penSize * penSize).ToArray();
            }
            currentColor = color;
            if (ToggleDebug)
                Debug.Log($"Marker {id} color changed to {color}");
        }
    }
}
