using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
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

        public bool IsRecording { get; private set; }
        string timeStamp;

        void Start()
        {
            sender = gameObject.GetComponent<ARKitSender>();

            InitSubSenders();
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
            if(!IsRecording) return;

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

            if(tex2D == null || texture.width != tex2D?.width || texture.height != tex2D?.height)
                tex2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, true);

            var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.CopyTexture(texture, rt);

            AsyncGPUReadback.Request(rt, 0, request => {
                if (request.hasError)
                {
                    Debug.LogError("Failed download texture data");
                    RenderTexture.ReleaseTemporary(rt);
                }
                else
                {
                    // Save AR data
                    SafeCreateDirectory(Application.persistentDataPath + "/" + timeStamp);
                    saveARtoFile(data);

                    var texData = request.GetData<Color32>();
                    tex2D.LoadRawTextureData(texData);

                    WriteTextureToFile(args.timestampNs.ToString());

                    RenderTexture.ReleaseTemporary(rt);
                }
            });
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
            IsRecording = !IsRecording;

            if(IsRecording) timeStamp = DateTime.Now.ToString("yyyyMMddHHmm");
            else AsyncGPUReadback.WaitAllRequests();
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