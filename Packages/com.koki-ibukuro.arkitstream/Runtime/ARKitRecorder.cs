using System;
using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections.LowLevel.Unsafe;
using ARKitStream.Internal;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

// Save AR Related data and camera images
namespace ARKitStream
{
    public sealed class ARKitRecorder : MonoBehaviour
    {
        [SerializeField] ARCameraManager cameraManager = null;
        [SerializeField] ARKitSender _sender;
        internal event Action<ARKitRemotePacket> PacketTransformer;
        Texture2D mTexture;
        XRCpuImage image;
        Texture2D Tex2D;
        byte[] bytes;
        String ext;
        RecordController recordController;
        public string SavePath;

        bool _received;
        string _timeStamp;

        void Start()
        {
            if (Application.isEditor)
            {
                Destroy(gameObject);
                return;
            }

            recordController = GameObject.Find("Record Button").GetComponent<RecordController>();

            // Set event for cameraManager
            // cameraManager.frameReceived += OnCameraFrameReceived;
            InitSubSenders();
        }

        void OnDestroy()
        {
            if (cameraManager != null)
            {
                // cameraManager.frameReceived -= OnCameraFrameReceived;
            }
        }

        void OnValidate()
        {
            if (cameraManager == null)
            {
                cameraManager = FindObjectOfType<ARCameraManager>();
            }
        }

        // Event with getting ARCameraFrame for cameraManager
        internal unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs args, RenderTexture texture)
        {
            // _received = true;
            // Get AR data
            var packet = new ARKitRemotePacket()
            {
                cameraFrame = new ARKitRemotePacket.CameraFrameEvent()
                {
                    timestampNs = args.timestampNs.Value,
                    projectionMatrix = args.projectionMatrix.Value,
                    displayMatrix = args.displayMatrix.Value
                }
            };
            if (PacketTransformer != null)
            {
                PacketTransformer(packet);
            }

            byte[] data = packet.Serialize();
            if (recordController.IsRecord == true)
            {
                // Save AR data
                SafeCreateDirectory(Application.persistentDataPath + "/" + recordController.RecordTime);
                saveARtoFile(data);

                RenderTexture.active = texture;
                if(Tex2D == null || texture.width != Tex2D?.width || texture.height != Tex2D?.height)
                    Tex2D = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false, true);

                Tex2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                RenderTexture.active = null;

                WriteTextureToFile(args.timestampNs.ToString());
            }
        }

        // Save AR data
        void saveARtoFile(byte[] data)
        {
            SavePath = Application.persistentDataPath + "/" + recordController.RecordTime + "/saved-ardata.bytes";
            using (var fs = new FileStream(SavePath, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                fs.Write(data, 0, data.Length);
                for (int i = 0; i < 5; i++) fs.WriteByte(0x1e); // record separator
            }
        }

        // Save camera image
        public void WriteTextureToFile(string filename)
        {
            if(Tex2D == null)
                throw new System.ArgumentNullException("texture");

            var path = Path.Combine(Application.persistentDataPath, recordController.RecordTime, "imgs", filename);
            SafeCreateDirectory(Path.Combine(Application.persistentDataPath, recordController.RecordTime, "imgs"));

            if (string.IsNullOrEmpty(path))
                throw new System.InvalidOperationException("No path specified");

            bytes = Tex2D.EncodeToJPG();
            ext = ".jpg";
            if (bytes.Length > 0)
            {
                File.WriteAllBytes(path + ext, bytes);
            }
        }

        public static DirectoryInfo SafeCreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return null;
            }
            return Directory.CreateDirectory(path);
        }

        void InitSubSenders()
        {
            TrackedPoseSender.TryCreate(_sender);
            ARKitFaceSender.TryCreate(_sender);
            ARKitOcclusionSender.TryCreate(_sender);
            ARKitPlaneSender.TryCreate(_sender);
            ARKitHumanBodySender.TryCreate(_sender);
        }

    }
}