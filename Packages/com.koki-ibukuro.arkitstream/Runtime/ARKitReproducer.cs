using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using System.IO;
using ARKitStream.Internal;

// Replay AR data and camera images
namespace ARKitStream
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ARKitReceiver))]
    public sealed class ARKitReproducer : MonoBehaviour
    {
        [SerializeField] ARCameraManager cameraManager = null;
        [SerializeField] int targetFrameRate = 30;
        [SerializeField] string savePath;

        ARKitReceiver receiver;
        Texture2D tex;
        IEnumerator<byte[]> coroutine;

        public Texture2D ARTexture => tex;
        public bool IsValid { get; private set; }

        void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            IsValid = Directory.Exists(savePath+"/imgs") && File.Exists(savePath+"/saved-ardata.bytes");
        }

        void Start()
        {
            if(!IsValid)
            {
                Debug.LogError("Please specify a valid path containing saved ar datas");
                return;
            }

            receiver = gameObject.GetComponent<ARKitReceiver>();

            tex = new Texture2D(1440, 1920);

            // Prepare coroutine for loading AR data
            coroutine = LoadARFileCoroutine();
            cameraManager.frameReceived += OnCameraFrameReceived;

            // Prepare camera texture
            var commandBuffer = new CommandBuffer();
            commandBuffer.name = "CameraView";
            commandBuffer.Blit(tex, BuiltinRenderTextureType.CameraTarget);

            var camera = cameraManager.GetComponent<Camera>();
            camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, commandBuffer);
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
            if(!IsValid) return;

            coroutine.MoveNext();
            if (coroutine.Current.Length > 0)
            {
                receiver.Packet = ARKitRemotePacket.Deserialize(coroutine.Current);
            }
        }

        // Coroutine for loading saved AR data
        public IEnumerator<byte[]> LoadARFileCoroutine()
        {
            const byte RecordSeparator = 0x1e;

            var packetPath = savePath + "/saved-ardata.bytes";

            using (var stream = File.OpenRead(packetPath))
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
            var imgPath = savePath + "/imgs/";
            var cameraImagePath = imgPath + args.timestampNs.Value + ".jpg";
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