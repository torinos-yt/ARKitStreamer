using System;
using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using ARKitStream.Internal;

// Save AR Related data and camera images
namespace ARKitStream
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ARKitSender))]
    public sealed class ARKitRecorder : MonoBehaviour
    {
        [SerializeField] ARCameraManager cameraManager = null;

        internal event Action<ARKitRemotePacket> PacketTransformer;

        ARKitSender sender;

        Texture2D tex2D;
        byte[] bytes;
        string ext;

        bool isRecording;
        string timeStamp;

        void Start()
        {
            sender = gameObject.GetComponent<ARKitSender>();

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
        internal void OnCameraFrameReceived(ARCameraFrameEventArgs args, RenderTexture texture)
        {
            if(!isRecording) return;

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

            // Save AR data
            SafeCreateDirectory(Application.persistentDataPath + "/" + timeStamp);
            saveARtoFile(data);

            RenderTexture.active = texture;
            if(tex2D == null || texture.width != tex2D?.width || texture.height != tex2D?.height)
                tex2D = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false, true);

            tex2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            RenderTexture.active = null;

            WriteTextureToFile(args.timestampNs.ToString());
        }

        // Save AR data
        void saveARtoFile(byte[] data)
        {
            var savePath = Application.persistentDataPath + "/" + timeStamp + "/saved-ardata.bytes";
            using (var fs = new FileStream(savePath, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                fs.Write(data, 0, data.Length);
                for (int i = 0; i < 5; i++) fs.WriteByte(0x1e); // record separator
            }
        }

        // Save camera image
        public void WriteTextureToFile(string filename)
        {
            if(tex2D == null)
                throw new System.ArgumentNullException("texture");

            var path = Path.Combine(Application.persistentDataPath, timeStamp, "imgs", filename);
            SafeCreateDirectory(Path.Combine(Application.persistentDataPath, timeStamp, "imgs"));

            if (string.IsNullOrEmpty(path))
                throw new System.InvalidOperationException("No path specified");

            bytes = tex2D.EncodeToJPG();
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

        public void ToggleRecord()
        {
            isRecording = !isRecording;

            if(isRecording) timeStamp = DateTime.Now.ToString("yyyyMMddHHmm");
        }

        void InitSubSenders()
        {
            TrackedPoseSender.TryCreate(sender);
            ARKitFaceSender.TryCreate(sender);
            ARKitOcclusionSender.TryCreate(sender);
            ARKitPlaneSender.TryCreate(sender);
            ARKitHumanBodySender.TryCreate(sender);
        }

    }
}