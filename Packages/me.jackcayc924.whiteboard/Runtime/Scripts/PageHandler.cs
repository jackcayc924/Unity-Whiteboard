using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;
using TMPro;
using Mirror;

namespace JackCayc924.Whiteboard
{
    public struct PageChangeMessage : NetworkMessage
    {
        public string pageDirection; // "left" or "right"
    }

    public class PageHandler : NetworkBehaviour
    {
        public TextMeshProUGUI PageText;
        public Whiteboard WhiteboardGameObject;
        public Toggle SaveNotesToggle;
        public string whiteboardConfigDirectory;

        private int NumberOfPages;
        private GameObject[] WhiteboardList;
        private Dictionary<int, GameObject> WhiteboardWithImportedNotes = new Dictionary<int, GameObject>();
        private Dictionary<GameObject, byte[]> WhiteboardWithByteData = new Dictionary<GameObject, byte[]>();
        private Marker[] Markers;
        private Whiteboard currentWhiteboard;

        private DateTime lastUpdate = DateTime.Now;
        private const float timeout = 100f; // ms
        private bool sendUpdate = false;

        private string documentsPath;

        public override void OnStartServer()
        {
            base.OnStartServer();
            NetworkServer.RegisterHandler<PageChangeMessage>(OnServerReceivePageChange);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            NetworkClient.RegisterHandler<PageChangeMessage>(OnClientReceivePageChange);
        }

        private void Awake()
        {
            InitializePaths();
            LoadOrCreateConfig();
            InitializeWhiteboards();
            SetupNotesSaving();

            Markers = FindObjectsByType<Marker>(FindObjectsSortMode.None);
            PageText.text = $"Page 1/{NumberOfPages}";

            if (isImportingNotes())
            {
                ImportNotes();
                Invoke(nameof(DisableWhiteboards), 2.0f);
            }
            else
            {
                Invoke(nameof(DisableWhiteboards), 2.0f);
            }
        }
        bool isImportingNotes()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string importPath = path + "\\Whiteboard Notes\\BringNotesIn";
            // If that folder has files, assume we’re importing
            return Directory.Exists(importPath) && Directory.GetFiles(importPath, "*.png").Length > 0;
        }


        private void InitializePaths()
        {
            documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Whiteboard Notes");
            if (!Directory.Exists(documentsPath))
            {
                Directory.CreateDirectory(documentsPath);
                Directory.CreateDirectory(Path.Combine(documentsPath, "BringNotesIn"));
            }
        }

        private void LoadOrCreateConfig()
        {
            string configPath = Path.Combine(Application.persistentDataPath, "WhiteboardConfig.json");
            if (!File.Exists(configPath))
            {
                string defaultContent = "{\"pages\": 1}";
                File.WriteAllText(configPath, defaultContent);
                Debug.Log("Created default WhiteboardConfig.json.");
            }

            string jsonContent = File.ReadAllText(configPath);
            JSONNode data = JSON.Parse(jsonContent);
            NumberOfPages = Mathf.Max(1, data["pages"].AsInt);
        }

        private void InitializeWhiteboards()
        {
            WhiteboardList = new GameObject[NumberOfPages];
            WhiteboardList[0] = WhiteboardGameObject.gameObject;

            for (int i = 1; i < NumberOfPages; i++)
            {
                GameObject newWhiteboard = Instantiate(WhiteboardGameObject.gameObject);
                newWhiteboard.name = $"{WhiteboardGameObject.name}_{i}";
                newWhiteboard.transform.SetParent(WhiteboardGameObject.transform.parent);
                newWhiteboard.transform.position = WhiteboardGameObject.transform.position;
                newWhiteboard.transform.rotation = WhiteboardGameObject.transform.rotation;

                var renderer = newWhiteboard.GetComponent<MeshRenderer>();
                renderer.material.mainTexture = new Texture2D((int)WhiteboardGameObject.textureSize.x, (int)WhiteboardGameObject.textureSize.y);

                Whiteboard wb = newWhiteboard.GetComponent<Whiteboard>();
                wb.InitializeTextures();
                WhiteboardList[i] = newWhiteboard;
            }

            currentWhiteboard = WhiteboardGameObject.GetComponent<Whiteboard>();
        }

        private void SetupNotesSaving()
        {
            Application.quitting += SaveNotesOnQuit;
        }

        private void SaveNotesOnQuit()
        {
            if (SaveNotesToggle.isOn)
            {
                foreach (GameObject whiteboard in WhiteboardList)
                {
                    var renderer = whiteboard.GetComponent<Renderer>();
                    if (renderer.material.mainTexture is RenderTexture renderTexture)
                    {
                        SaveWhiteboardTexture(renderTexture);
                    }
                }
            }
        }

        private void SaveWhiteboardTexture(RenderTexture renderTexture)
        {
            RenderTexture tempRT = RenderTexture.GetTemporary(renderTexture.width, renderTexture.height);
            Graphics.Blit(renderTexture, tempRT);

            Texture2D texture = new Texture2D(tempRT.width, tempRT.height, TextureFormat.RGB24, false);
            RenderTexture.active = tempRT;
            texture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
            texture.Apply();

            string fileName = $"Notes_{DateTime.UtcNow:MM_dd_yyyy_HH_mm_ss_fff}.png";
            File.WriteAllBytes(Path.Combine(documentsPath, fileName), texture.EncodeToPNG());

            RenderTexture.ReleaseTemporary(tempRT);
            RenderTexture.active = null;

            Destroy(texture);
        }

        private void Update()
        {
            sendUpdate = (DateTime.Now - lastUpdate).TotalMilliseconds > timeout;
        }

        public void DisableWhiteboards()
        {
            for (int i = 1; i < WhiteboardList.Length; i++)
            {
                WhiteboardList[i].SetActive(false);
            }
        }

        public void SwitchPage(string page)
        {
            int currentIndex = Array.FindIndex(WhiteboardList, wb => wb.activeInHierarchy);

            if (page.ToLower() == "right" && currentIndex < WhiteboardList.Length - 1)
            {
                WhiteboardList[currentIndex].SetActive(false);
                WhiteboardList[currentIndex + 1].SetActive(true);
                SetNewWhiteboard(WhiteboardList[currentIndex + 1]);
                PageText.text = $"Page {currentIndex + 2}/{NumberOfPages}";
            }
            else if (page.ToLower() == "left" && currentIndex > 0)
            {
                WhiteboardList[currentIndex].SetActive(false);
                WhiteboardList[currentIndex - 1].SetActive(true);
                SetNewWhiteboard(WhiteboardList[currentIndex - 1]);
                PageText.text = $"Page {currentIndex}/{NumberOfPages}";
            }

            SendNetworkPageChange(page);
        }

        private void SendNetworkPageChange(string pageDir)
        {
            if (!isClient || !sendUpdate) return;

            lastUpdate = DateTime.Now;
            NetworkClient.Send(new PageChangeMessage { pageDirection = pageDir });
        }

        private void OnServerReceivePageChange(NetworkConnectionToClient conn, PageChangeMessage msg)
        {
            NetworkServer.SendToAll(msg);
        }

        private void OnClientReceivePageChange(PageChangeMessage msg)
        {
            if (isClient)
            {
                SwitchPage(msg.pageDirection);
            }
        }

        private void ImportNotes()
        {
            string importPath = Path.Combine(documentsPath, "BringNotesIn");
            foreach (string file in Directory.GetFiles(importPath, "*.png"))
            {
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(File.ReadAllBytes(file));
                WhiteboardList[0].GetComponent<Renderer>().material.mainTexture = texture;
            }
        }

        private void SetNewWhiteboard(GameObject newWhiteboard)
        {
            currentWhiteboard = newWhiteboard.GetComponent<Whiteboard>();
            foreach (Marker marker in Markers)
            {
                marker.SetNewWhiteboard(currentWhiteboard);
            }
        }

        public GameObject[] GetAllWhiteboards()
        {
            return WhiteboardList;
        }

        public Whiteboard GetCurrentWhiteboard()
        {
            return currentWhiteboard;
        }
    }
}
