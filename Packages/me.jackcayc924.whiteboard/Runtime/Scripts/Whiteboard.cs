using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JackCayc924.Whiteboard
{
    public class Whiteboard : MonoBehaviour
    {
        public Texture2D texture;
        public Vector2 textureSize = new Vector2(2048, 2048);
        public bool hasDrawing = false;

        private Renderer r;
        private RenderTexture _renderTexture;
        private RenderTexture _tempRenderTexture;

        void Awake()
        {
            r = GetComponent<Renderer>();
            InitializeTextures();
        }

        public void InitializeTextures()
        {
            texture = new Texture2D((int)textureSize.x, (int)textureSize.y);
            r.material.mainTexture = texture;

            _renderTexture = new RenderTexture((int)textureSize.x, (int)textureSize.y, 0);
            _renderTexture.enableRandomWrite = true;
            _renderTexture.Create();

            _tempRenderTexture = new RenderTexture((int)textureSize.x, (int)textureSize.y, 0);
            _tempRenderTexture.enableRandomWrite = true;
            _tempRenderTexture.Create();

            r.material.mainTexture = _renderTexture;
        }

        public RenderTexture GetRenderTexture()
        {
            return _renderTexture;
        }

        public RenderTexture GetTempRenderTexture()
        {
            return _tempRenderTexture;
        }
    }
}
