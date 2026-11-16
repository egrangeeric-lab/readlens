using System;
using System.Collections.Concurrent;
using System.Linq;
using ReadLens.Core;
using Unity.Collections;
using UnityEngine;

namespace ReadLens.App
{
    [Serializable] public class OcrLine { public string text; public int[] box; }
    [Serializable] public class OcrBlock { public string text; public int[] box; public OcrLine[] lines; }
    [Serializable] public class OcrResult
    {
        public int width, height;
        public OcrBlock[] blocks;
        public string ReflowedText => TextReflow.Join(blocks.Select(b => b.lines.Select(l => l.text)));
    }

    // Bridge to the on-device ML Kit recognizer (Assets/Plugins/Android/ReadLensOcr.java).
    public class OcrService : MonoBehaviour
    {
        readonly ConcurrentQueue<Action> mainThread = new ConcurrentQueue<Action>();

        public bool Busy { get; private set; }

        public void Recognize(NativeArray<Color32> pixels, int width, int height, Action<OcrResult> onResult, Action<string> onError)
        {
            if (Busy) return;
            Busy = true;
            // JNI maps sbyte[] to Java byte[].
            var bytes = pixels.Reinterpret<byte>(4).ToArray();
            var sbytes = new sbyte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, sbytes, 0, bytes.Length);
            void Done(Action a) => mainThread.Enqueue(() => { Busy = false; a(); });
#if UNITY_ANDROID && !UNITY_EDITOR
            using var ocr = new AndroidJavaClass("com.readlens.ocr.ReadLensOcr");
            ocr.CallStatic("recognize", sbytes, width, height, new Callback(
                json => Done(() => onResult(JsonUtility.FromJson<OcrResult>(json))),
                msg => Done(() => onError(msg))));
#else
            Done(() => onError("On-device OCR only runs on the headset."));
#endif
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
