using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Meta.XR;
using ReadLens.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace ReadLens.App
{
    [Serializable] public class OcrLine { public string text; public int[] box; }
    [Serializable] public class OcrBlock { public string text; public int[] box; public OcrLine[] lines; }
    [Serializable] public class OcrResult
    {
        public int width, height;
        public OcrBlock[] blocks;
        [NonSerialized] public Pose cameraPose; // where the camera was when the frame was taken

        public string ReflowedText => TextReflow.Join(blocks.Select(b => b.lines.Select(l => l.text)));

        // Keeps only the lines whose centre falls inside a pixel rectangle (top-left origin, like the boxes).
        public string TextInside(Rect pixels) => TextReflow.Join(blocks.Select(b => b.lines
            .Where(l => l.box.Length == 4 && pixels.Contains(new Vector2((l.box[0] + l.box[2]) / 2f, (l.box[1] + l.box[3]) / 2f)))
            .Select(l => l.text)));

        // Union of all text boxes, in pixels.
        public bool Bounds(out Rect r)
        {
            r = default;
            var boxes = blocks.Where(b => b.box.Length == 4).Select(b => b.box).ToList();
            if (boxes.Count == 0) return false;
            r = Rect.MinMaxRect(boxes.Min(b => b[0]), boxes.Min(b => b[1]), boxes.Max(b => b[2]), boxes.Max(b => b[3]));
            return true;
        }
    }

    // Reads the passthrough camera without stalling the frame and runs on-device ML Kit OCR
    // (Assets/Plugins/Android/ReadLensOcr.java). Frames stay in memory; nothing is stored or sent.
    public class OcrService : MonoBehaviour
    {
        [SerializeField] PassthroughCameraAccess cameraAccess;

        readonly ConcurrentQueue<Action> mainThread = new ConcurrentQueue<Action>();

        public bool Busy { get; private set; }
        public bool CameraReady => cameraAccess != null && cameraAccess.IsPlaying;
        public PassthroughCameraAccess Camera => cameraAccess;

        public void ReadCamera(Action<OcrResult> onResult, Action<string> onError)
        {
            if (Busy) return;
            if (!CameraReady)
            {
                onError("camera not ready");
                return;
            }
            Busy = true;
            var pose = cameraAccess.GetCameraPose();
            var size = cameraAccess.CurrentResolution;
            AsyncGPUReadback.Request(cameraAccess.GetTexture(), 0, TextureFormat.RGBA32, request =>
            {
                if (request.hasError)
                {
                    Busy = false;
                    onError("camera readback failed");
                    return;
                }
                Recognize(request.GetData<byte>().ToArray(), size.x, size.y, r =>
                {
                    r.cameraPose = pose;
                    onResult(r);
                }, onError);
            });
        }

        void Recognize(byte[] rgba, int width, int height, Action<OcrResult> onResult, Action<string> onError)
        {
            void Done(Action a) => mainThread.Enqueue(() => { Busy = false; a(); });
#if UNITY_ANDROID && !UNITY_EDITOR
            var sbytes = new sbyte[rgba.Length]; // JNI maps sbyte[] to Java byte[]
            Buffer.BlockCopy(rgba, 0, sbytes, 0, rgba.Length);
            using var ocr = new AndroidJavaClass("com.readlens.ocr.ReadLensOcr");
            ocr.CallStatic("recognize", sbytes, width, height, new Callback(
                json => Done(() => onResult(JsonUtility.FromJson<OcrResult>(json))),
                msg => Done(() => onError(msg))));
#else
            Done(() => onError("On-device OCR only runs on the headset."));
#endif
        }

        // Pixel (top-left origin) -> world ray, using the camera pose of that frame.
        public Ray PixelToRay(OcrResult r, Vector2 pixel) =>
            cameraAccess.ViewportPointToRay(new Vector2(pixel.x / r.width, 1f - pixel.y / r.height), r.cameraPose);

        public Vector2 WorldToPixel(OcrResult r, Vector3 world)
        {
            var v = cameraAccess.WorldToViewportPoint(world, r.cameraPose);
            return new Vector2(v.x * r.width, (1f - v.y) * r.height);
        }

        void Update()
        {
            while (mainThread.TryDequeue(out var a)) a();
        }

        class Callback : AndroidJavaProxy
        {
            readonly Action<string> result, error;
            public Callback(Action<string> result, Action<string> error) : base("com.readlens.ocr.ReadLensOcr$Callback")
            {
                this.result = result;
                this.error = error;
            }
            // Called from Java threads; OcrService marshals back to Unity's main thread.
            public void onResult(string json) => result(json);
            public void onError(string message) => error(message);
        }
    }
}
