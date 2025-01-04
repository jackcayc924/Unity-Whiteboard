using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace JackCayc924.Whiteboard
{
    public class MarkerVR : MonoBehaviour
    {
        //Anotherserialize field here possibly transform of mouse
        public ColorPickerTriangle colorPicker;
        public int penSize = 5;
        public GameObject markerTip;

        [HideInInspector]
        public GameObject WhiteboardObject;

        private Renderer _renderer;
        private Color[] _colors;
        private float tipHeight;
        private RaycastHit touch;
        private Whiteboard _whiteboard;
        private Vector2 _lastTouchPos, _tpos;
        private bool _touchedLastFrame;
        private Quaternion _lastTouchRot;
        private Color currentColor;
        private MarkerVR[] NetworkedMarkers;


        // Start is called before the first frame update
        void Start()
        {
            NetworkedMarkers = GameObject.FindObjectsOfType<MarkerVR>();
            tipHeight = markerTip.transform.localScale.y;
            _colors = Enumerable.Repeat(Color.black, penSize * penSize).ToArray();
        }

        void Update()
        {
            Draw();
        }

        public void changeColor(Color color)
        {
            _colors = null;
            _colors = Enumerable.Repeat(color, penSize * penSize).ToArray();
            currentColor = color;
        }

        private void Draw()
        {
            if (Physics.Raycast(markerTip.transform.position, transform.up, out touch, tipHeight))
            {
                if (touch.transform.CompareTag("Whiteboard"))
                {
                    if (_whiteboard == null)
                    {
                        _whiteboard = touch.transform.GetComponent<Whiteboard>();
                    }

                    _tpos = new Vector2(touch.textureCoord.x, touch.textureCoord.y);

                    var x = (int)(_tpos.x * _whiteboard.textureSize.x - (penSize / 2));
                    var y = (int)(_tpos.y * _whiteboard.textureSize.y - (penSize / 2));

                    if (y < 0 || y > _whiteboard.textureSize.y || x < 0 || x > _whiteboard.textureSize.y)
                    {
                        return;
                    }

                    if (_touchedLastFrame)
                    {
                        _whiteboard.texture.SetPixels(x, y, penSize, penSize, _colors);

                        for (float f = 0.01f; f < 1.00f; f += 0.01f)
                        {
                            var lerpX = (int)Mathf.Lerp(_lastTouchPos.x, x, f);
                            var lerpY = (int)Mathf.Lerp(_lastTouchPos.y, y, f);
                            _whiteboard.texture.SetPixels(lerpX, lerpY, penSize, penSize, _colors);
                        }

                        transform.rotation = _lastTouchRot;

                        _whiteboard.texture.Apply();
                    }

                    _lastTouchPos = new Vector2(x, y);
                    _lastTouchRot = transform.rotation;
                    _touchedLastFrame = true;
                    return;
                }
            }
            _whiteboard = null;
            _touchedLastFrame = false;
        }
        public void SetPenSize(int value)
        {
            penSize = value;
            _colors = Enumerable.Repeat(currentColor, penSize * penSize).ToArray();
        }
    }
}