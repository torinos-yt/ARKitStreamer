using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using System.IO;
using ARKitStream.Internal;

// Replay AR data and camera images
namespace ARKitStream
{
    public sealed class ARKitReproducer : MonoBehaviour
    {

        [SerializeField] ARCameraManager cameraManager = null;
        [SerializeField] uint port = 8888;
        [SerializeField] int targetFrameRate = 30;
        [SerializeField] ARKitReceiver _receiver;

        RenderTexture renderTexture;
        public string SavePath;
        [SerializeField] string dirPath;
        Texture2D tex;
        IEnumerator<byte[]> coroutine;
        Camera _camera;
        float m_LastLocalizeTime = 0;
        float m_LocalizationInterval = 4;
        bool isLocalizing = false;

        public Texture2D Texture => tex;

        void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
        }

        void Start()
        {

            if (!Application.isEditor)
            {
                Destroy(gameObject);
                return;
            }

            SavePath = Application.persistentDataPath + "/saved-ardata.bytes";
            dirPath = Application.persistentDataPath + "/imgs/";
            tex = new Texture2D(1440, 1920);

            // Prepare coroutine for loading AR data
            coroutine = LoadARFileCoroutine();
            cameraManager.frameReceived += OnCameraFrameReceived;

            // Prepare camera texture
            var commandBuffer = new CommandBuffer();
            commandBuffer.name = "CameraView";
            commandBuffer.Blit(tex, BuiltinRenderTextureType.CameraTarget);
            _camera = GameObject.Find("AR Camera").GetComponent<Camera>();
            _camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, commandBuffer);

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(1920, 1440, 0, RenderTextureFormat.ARGB32);
            }

        }

        void OnDestroy()
        {
            if (cameraManager != null)
            {
                cameraManager.frameReceived -= OnCameraFrameReceived;
            }
        }

        void OnValidate()
        {
            if (cameraManager == null)
            {
                cameraManager = FindObjectOfType<ARCameraManager>();
            }
        }

        void Update()
        {
            coroutine.MoveNext();
            if (coroutine.Current.Length > 0)
            {
                _receiver.Packet = ARKitRemotePacket.Deserialize(coroutine.Current);
            }
        }

        // Coroutine for loading saved AR data
        public IEnumerator<byte[]> LoadARFileCoroutine()
        {
            const byte RecordSeparator = 0x1e;

            using (var stream = File.OpenRead(SavePath))
            {
                for ( ; ; )
                {
                    var data = stream.ReadByte();
                    if (data < 0)
                    {
                        yield break;
                    }
                    using (var mem = new MemoryStream())
                    {
                        // Load each record data
                        for ( ; ; )
                        {
                            if (data == RecordSeparator)
                            {
                                var tmp = new int[4];
                                for (int i = 0; i < 4; i++) tmp[i] = stream.ReadByte();
                                // use five sequential 0x1e as a record separator
                                if (tmp[0] == 0x1e && tmp[1] == 0x1e && tmp[2] == 0x1e && tmp[3] == 0x1e)
                                {
                                    break;
                                }
                                else
                                {
                                    mem.WriteByte((byte)data);
                                    for (int i = 0; i < 4; i++) mem.WriteByte((byte)tmp[i]);
                                }
                            }
                            else
                            {
                                mem.WriteByte((byte)data);
                            }
                            data = stream.ReadByte();

                        }
                        var record = mem.ToArray();
                        yield return record;
                    }
                }
            }
        }

        // Set texture by captured camera image
        void OnCameraFrameReceived(ARCameraFrameEventArgs args)
        {
            // Debug.Log("OnFrame");
            var cameraImagePath = dirPath + args.timestampNs.Value + ".jpg";
            if (File.Exists(cameraImagePath))
            {
                FileStream stream = File.OpenRead(cameraImagePath);
                var data = new byte[stream.Length];
                stream.Read(data, 0, (int)stream.Length);
                tex.LoadImage(data);
                float curTime = Time.unscaledTime;
                stream.Close();
            }

        }

    }
}