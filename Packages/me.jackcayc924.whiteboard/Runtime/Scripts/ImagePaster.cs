using System.IO;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;  // If you use Unity UI
using System.Windows.Forms; // If you're actually using WinForms for file dialogs
using SixLabors.ImageSharp;  // Need to handle the name conflict with UnityEngine.Image
using Image = SixLabors.ImageSharp.Image; // Resolve conflict
using Mirror;

namespace JackCayc924.Whiteboard
{
    /// <summary>
    /// The data we send/receive across the network when pasting images.
    /// </summary>
    public struct ImagePasteMessage : NetworkMessage
    {
        public string filePath;
        public float posX;
        public float posY;
        public float sizeX;
        public float sizeY;
        public float rotation;
    }

    public class ImagePaster : NetworkBehaviour
    {
        [Header("Image-Paste Settings")]
        public Shader ImagePasteShader;
        public LoadingManager LoadingManager;
        public ActionsVRWhiteboard VR_ACTIONS;

        private Material _imagePasteMaterial;
        private Texture2D _imageTexture;
        private Whiteboard _whiteboard;

        private Vector2 _imagePosition = new Vector2(0.5f, 0.5f);
        private Vector2 _imageSize;
        private float _imageRotation = -Mathf.PI / 2; // Rotate 90 degrees right
        private bool _isGhostVisible = false;

        private string pictureFilePath;
        private RenderTexture _originalTexture;
        private bool _originalTextureCaptured = false;
        private PageHandler pageHandler;
        private float aspectRatio;
        private float scaleFactor = 1.0f;
        private bool _isLoadingImage = false;

        // We'll store images in persistentDataPath/WhiteboardImages by default:
        private string appDataImagePath;

        #region Mirror: Register/Unregister Handlers

        public override void OnStartServer()
        {
            // The server will listen for ImagePasteMessage from clients:
            NetworkServer.RegisterHandler<ImagePasteMessage>(OnServerReceivePasteMessage);
        }

        public override void OnStartClient()
        {
            // Every client (including host) listens for the server's broadcast:
            NetworkClient.RegisterHandler<ImagePasteMessage>(OnClientReceivePasteMessage);
        }

        // Server receives from one client, relays to all
        private void OnServerReceivePasteMessage(NetworkConnectionToClient conn, ImagePasteMessage msg)
        {
            // Optionally do server-side checks, e.g. validate file path
            // Relay to all (including original sender)
            NetworkServer.SendToAll(msg);
        }

        // Clients receive the broadcast, perform local paste
        private void OnClientReceivePasteMessage(ImagePasteMessage msg)
        {
            Debug.Log($"[ClientReceivePasteMessage] file={msg.filePath}, pos=({msg.posX},{msg.posY}) size=({msg.sizeX},{msg.sizeY}) rot={msg.rotation}");
            StartCoroutine(LoadAndPasteImage(
                msg.filePath,
                new Vector2(msg.posX, msg.posY),
                new Vector2(msg.sizeX, msg.sizeY),
                msg.rotation
            ));
        }

        #endregion

        private void Start()
        {
            EnsureDirectoryExists();

            if (ImagePasteShader == null)
            {
                Debug.LogError("ImagePasteShader is not assigned.");
                return;
            }

            _imagePasteMaterial = new Material(ImagePasteShader);

            pageHandler = FindObjectOfType<PageHandler>();
            if (pageHandler != null)
            {
                _whiteboard = pageHandler.GetCurrentWhiteboard();
            }

            if (_whiteboard == null)
            {
                Debug.LogError("Whiteboard not found in the scene.");
            }

            _originalTexture = new RenderTexture(
                (int)_whiteboard.textureSize.x,
                (int)_whiteboard.textureSize.y,
                0
            );
        }

        /// <summary>
        /// Ensure the local directory exists. Using persistentDataPath in this example.
        /// </summary>
        private void EnsureDirectoryExists()
        {
            appDataImagePath = Path.Combine(UnityEngine.Application.persistentDataPath, "WhiteboardImages");
            if (!Directory.Exists(appDataImagePath))
            {
                Directory.CreateDirectory(appDataImagePath);
                Debug.Log($"Created directory at: {appDataImagePath}");
            }
        }

        private void Update()
        {
            HandleMouseInput();
            ScaleImage();
            CancelImageLoading();

            if (_isGhostVisible)
            {
                if (VR_ACTIONS && VR_ACTIONS.VR_ON())
                {
                    Ray ray = new Ray(VR_ACTIONS.LeftHand.transform.position, VR_ACTIONS.LeftHand.transform.forward);
                    UpdateGhostImage(ray);
                }
                else
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    UpdateGhostImage(ray);
                }
            }
        }

        #region Open File Dialog

        public void OpenFileDialog()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = appDataImagePath;
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureFilePath = openFileDialog.FileName;
                    FileInfo fileInfo = new FileInfo(pictureFilePath);
                    LoadFile(fileInfo, true);
                }
            }
        }

        #endregion

        #region Pasting Logic

        /// <summary>
        /// Send a message so other clients can paste the same image.
        /// Optionally also do a local paste immediately so we don't rely on roundtrip.
        /// </summary>
        private void SendImagePasteMessage(string filePath, Vector2 pos, Vector2 size, float rotation)
        {
            if (!isClient)
            {
                Debug.LogWarning("Not a client—cannot send ImagePasteMessage over network.");
                return;
            }

            var msg = new ImagePasteMessage
            {
                filePath = filePath,
                posX = pos.x,
                posY = pos.y,
                sizeX = size.x,
                sizeY = size.y,
                rotation = rotation
            };

            NetworkClient.Send(msg);
            Debug.Log($"[SendImagePasteMessage] {msg.filePath}, pos=({pos.x},{pos.y}) size=({size.x},{size.y}) rot={rotation}");
        }

        public void PasteImage()
        {
            if (_whiteboard == null || _imagePasteMaterial == null || _imageTexture == null)
            {
                Debug.LogError("Whiteboard, ImagePasteMaterial, or ImageTexture is not initialized.");
                return;
            }

            _imagePasteMaterial.SetFloat("_IsGhost", 0.0f);

            // Render the material to the temporary render texture
            Graphics.Blit(_whiteboard.GetRenderTexture(), _whiteboard.GetTempRenderTexture(), _imagePasteMaterial);
            // Copy the temporary render texture back to the main render texture
            Graphics.Blit(_whiteboard.GetTempRenderTexture(), _whiteboard.GetRenderTexture());

            Debug.Log($"Pasted image at position: {_imagePosition} with size: {_imageSize}");

            _isGhostVisible = false;

            // Update the original texture to the new state
            Graphics.Blit(_whiteboard.GetRenderTexture(), _originalTexture);

            pictureFilePath = null;
        }

        #endregion

        #region File Loading

        public void LoadFile(FileInfo selectedFile, bool sendOverNetwork = true)
        {
            if (File.Exists(selectedFile.FullName))
            {
                SwitchToCurrentWhiteboard();
                LoadingManager.ShowLoadingScreen("Loading Image...");
                StartCoroutine(LoadImage(selectedFile.FullName));
            }
            else
            {
                Debug.LogError("Image file not found at path: " + selectedFile.FullName);
            }
        }

        public IEnumerator LoadImage(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError("Image file not found at path: " + path);
                yield break;
            }

            try
            {
                using (Image img = Image.Load(path))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        img.SaveAsPng(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        int width = img.Width;
                        int height = img.Height;
                        _imageTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);

                        aspectRatio = (float)width / height;
                        float threshold = 1.05f;

                        if (aspectRatio > threshold)
                        {
                            Portrait();
                        }
                        else if (aspectRatio < 1 / threshold)
                        {
                            Landscape();
                        }
                        else
                        {
                            _imageSize = new Vector2(0.5f, 0.15f);
                        }

                        _imageTexture.LoadImage(memoryStream.ToArray());
                    }

                    _isLoadingImage = true;
                    _imagePasteMaterial.SetTexture("_ImageTex", _imageTexture);
                    _isGhostVisible = true;

                    if (!_originalTextureCaptured)
                    {
                        Graphics.Blit(_whiteboard.GetRenderTexture(), _originalTexture);
                        _originalTextureCaptured = true;
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                Debug.LogError("Out of memory loading image. Possibly > 2000x2000. Resize and try again.");
                LoadingManager.ShowLoadingScreenCustomMsg("Out of memory while loading image.");
                LoadingManager.loadingStatusText.text = "";
            }
            catch (Exception ex)
            {
                Debug.LogError("Error loading image: " + ex.Message);
                LoadingManager.ShowLoadingScreenCustomMsg("Failed to load image. Please try again.");
                LoadingManager.loadingStatusText.text = "";
            }

            LoadingManager.HideLoadingScreen();
            yield return null;
        }

        private IEnumerator LoadAndPasteImage(string path, Vector2 position, Vector2 size, float rotation)
        {
            LoadingManager.ShowLoadingScreen("Incoming Image...");
            yield return new WaitForSeconds(1);

            if (File.Exists(path))
            {
                using (Image img = Image.Load(path))
                {
                    int width = img.Width;
                    int height = img.Height;

                    _imageTexture = new Texture2D(width, height);
                    byte[] fileData = File.ReadAllBytes(path);
                    _imageTexture.LoadImage(fileData);

                    _imagePosition = position;
                    _imageSize = size;
                    _imageRotation = rotation;

                    _imagePasteMaterial.SetTexture("_ImageTex", _imageTexture);

                    if (!_originalTextureCaptured)
                    {
                        Graphics.Blit(_whiteboard.GetRenderTexture(), _originalTexture);
                        _originalTextureCaptured = true;
                    }

                    Graphics.Blit(_originalTexture, _whiteboard.GetRenderTexture());

                    // Actually paste
                    _imagePasteMaterial.SetVector("_ImagePosition", new Vector4(_imagePosition.x, _imagePosition.y, 0, 0));
                    _imagePasteMaterial.SetVector("_ImageSize", new Vector4(_imageSize.x, _imageSize.y, 0, 0));
                    _imagePasteMaterial.SetFloat("_ImageRotation", _imageRotation);

                    PasteImage();
                    LoadingManager.CancelLoading();
                }
            }
            else
            {
                Debug.LogError("Image file not found at path: " + path);
            }

            LoadingManager.HideLoadingScreen();
        }

        private void SwitchToCurrentWhiteboard()
        {
            if (pageHandler != null)
            {
                _whiteboard = pageHandler.GetCurrentWhiteboard();
            }
            _originalTextureCaptured = false;
        }

        #endregion

        #region Ghost & Input

        private void HandleMouseInput()
        {
            // Left-click in desktop
            if (Input.GetMouseButtonDown(0) && !string.IsNullOrEmpty(pictureFilePath))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider != null && hit.collider.gameObject == _whiteboard.gameObject)
                {
                    // (1) Optionally do a local immediate paste:
                    // PasteImage();

                    // (2) Then send the message so other clients can do it too
                    SendImagePasteMessage(
                        pictureFilePath,
                        _imagePosition,
                        _imageSize,
                        _imageRotation
                    );

                    LoadingManager.CancelLoading();
                }
            }

            // VR trigger logic
            if (VR_ACTIONS && VR_ACTIONS.HoldingTriggerLeft() && !string.IsNullOrEmpty(pictureFilePath))
            {
                Ray ray = new Ray(VR_ACTIONS.LeftHand.transform.position, VR_ACTIONS.LeftHand.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider != null && hit.collider.gameObject == _whiteboard.gameObject)
                {
                    // (1) local immediate paste if desired:
                    // PasteImage();

                    // (2) broadcast
                    SendImagePasteMessage(
                        pictureFilePath,
                        _imagePosition,
                        _imageSize,
                        _imageRotation
                    );

                    LoadingManager.CancelLoading();
                }
            }
        }

        private void UpdateGhostImage(Ray ray)
        {
            // Restore original texture
            Graphics.Blit(_originalTexture, _whiteboard.GetRenderTexture());

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider != null && hit.collider.gameObject == _whiteboard.gameObject)
                {
                    Vector3 hitPoint = hit.point;

                    float normalizedX = Mathf.InverseLerp(1.21f, -1.78f, hitPoint.x);
                    float normalizedY = Mathf.InverseLerp(2.12f, 1.12f, hitPoint.y);

                    _imagePosition = new Vector2(normalizedY, normalizedX);

                    _imagePasteMaterial.SetVector("_ImagePosition", new Vector4(_imagePosition.x, _imagePosition.y, 0, 0));
                    _imagePasteMaterial.SetVector("_ImageSize", new Vector4(_imageSize.x, _imageSize.y, 0, 0));
                    _imagePasteMaterial.SetFloat("_ImageRotation", _imageRotation);
                    _imagePasteMaterial.SetFloat("_IsGhost", 1.0f);

                    Graphics.Blit(_whiteboard.GetRenderTexture(), _whiteboard.GetTempRenderTexture(), _imagePasteMaterial);
                    Graphics.Blit(_whiteboard.GetTempRenderTexture(), _whiteboard.GetRenderTexture());
                }
            }
        }

        private void CancelImageLoading()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_isLoadingImage)
                {
                    Debug.Log("Image loading canceled.");
                    _isLoadingImage = false;
                    pictureFilePath = null;
                    scaleFactor = 1.0f;
                    _isGhostVisible = false;

                    Graphics.Blit(_originalTexture, _whiteboard.GetRenderTexture());
                    LoadingManager.CancelLoading();
                }
            }
        }

        #endregion

        #region Scale Controls

        private void ScaleImage()
        {
            if (Input.GetAxis("Mouse ScrollWheel") > 0f)
            {
                float factor = 1.5f;
                Vector2 oldSize = _imageSize;
                _imageSize *= factor;
                Vector2 sizeChange = _imageSize - oldSize;
                _imagePosition -= sizeChange / 2;
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
            {
                float factor = 0.75f;
                Vector2 oldSize = _imageSize;
                _imageSize *= factor;
                Vector2 sizeChange = _imageSize - oldSize;
                _imagePosition -= sizeChange / 2;
            }
        }

        public void ScaleUpButton()
        {
            float factor = 1.5f;
            Vector2 oldSize = _imageSize;
            _imageSize *= factor;
            Vector2 sizeChange = _imageSize - oldSize;
            _imagePosition -= sizeChange / 2;
        }

        public void ScaleDownButton()
        {
            float factor = 0.75f;
            Vector2 oldSize = _imageSize;
            _imageSize *= factor;
            Vector2 sizeChange = _imageSize - oldSize;
            _imagePosition -= sizeChange / 2;
        }

        #endregion

        #region Orientation Helpers

        public void Portrait()
        {
            _imageSize = new Vector2(0.5f, 0.15f * aspectRatio);
        }

        public void Landscape()
        {
            _imageSize = new Vector2(0.5f * aspectRatio, 0.5f);
        }

        #endregion

        public bool IsLoadingImage()
        {
            return _isLoadingImage;
        }
    }
}
