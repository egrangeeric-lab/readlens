using UnityEngine;

namespace ReadLens.App
{
    // Capture feedback drawn in the world (eye buffer, over the real page): the text region lights up,
    // a bright band sweeps top to bottom, then the region shrinks into the window and fades.
    // A single 10-vertex mesh in world space, rebuilt in place while playing; no collider, never blocks input.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ScanEffect : MonoBehaviour
    {
        const float SweepTime = 0.6f, FlyTime = 0.4f, ReducedTime = 0.35f;
        const float RegionAlpha = 0.3f, BandAlpha = 0.85f, BandHeight = 0.18f, EndScale = 0.15f;

        static readonly Color Region = UIKit.Accent;
        static readonly Color Band = Color.Lerp(UIKit.Accent, Color.white, 0.6f); // lighter than the page tint, reads as light

        readonly Vector3[] verts = new Vector3[10];
        readonly Color[] colors = new Color[10];
        Mesh mesh;
        MeshRenderer meshRenderer;
        Vector3 tl, tr, bl, br, target;
        float time;

        void Awake()
        {
            mesh = new Mesh { name = "Scan" };
            mesh.MarkDynamic();
            mesh.vertices = verts;
            mesh.colors = colors;
            // 0-3: region quad. 4-9: band, three rows (transparent, bright, transparent) for a soft glow.
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 3, 4, 5, 6, 6, 5, 7, 6, 7, 8, 8, 7, 9 };
            GetComponent<MeshFilter>().sharedMesh = mesh;
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = false;
            enabled = false;
        }

        // Corners of the text region in world space (top-left, top-right, bottom-left, bottom-right), and where to fly.
        public void Play(Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight, Vector3 window)
        {
            tl = topLeft; tr = topRight; bl = bottomLeft; br = bottomRight; target = window;
            time = 0f;
            meshRenderer.enabled = enabled = true;
            Update();
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;
            bool reduced = Motion.Reduced;
            float total = reduced ? ReducedTime : SweepTime + FlyTime;
            if (time >= total)
            {
                meshRenderer.enabled = enabled = false;
                return;
            }

            float regionAlpha, bandAlpha = 0f, sweep = 0f, fly = 0f;
            if (reduced) regionAlpha = RegionAlpha * (1f - time / ReducedTime);
            else if (time < SweepTime)
            {
                regionAlpha = RegionAlpha * Mathf.Min(1f, time / 0.12f);
                sweep = Mathf.SmoothStep(0f, 1f, time / SweepTime);
                bandAlpha = BandAlpha * Mathf.Min(1f, (SweepTime - time) / 0.1f + 0.3f);
            }
            else
            {
                fly = Motion.EaseOut((time - SweepTime) / FlyTime);
                regionAlpha = RegionAlpha * (1f - fly);
            }

            // Fly: every corner moves towards the window while the shape shrinks around it.
            var center = (tl + tr + bl + br) * 0.25f;
            Vector3 Corner(Vector3 c) => Vector3.Lerp(c, target + (c - center) * EndScale, fly);
            Vector3 a = Corner(tl), b = Corner(tr), c2 = Corner(bl), d = Corner(br);
            verts[0] = a; verts[1] = b; verts[2] = c2; verts[3] = d;
            for (int i = 0; i < 4; i++) colors[i] = new Color(Region.r, Region.g, Region.b, regionAlpha);

            // Band rows at sweep - h, sweep, sweep + h (fractions of the region height, clamped to it).
            for (int row = 0; row < 3; row++)
            {
                float v = Mathf.Clamp01(sweep + (row - 1) * BandHeight * 0.5f);
                verts[4 + row * 2] = Vector3.Lerp(a, c2, v);
                verts[5 + row * 2] = Vector3.Lerp(b, d, v);
                var col = new Color(Band.r, Band.g, Band.b, row == 1 ? bandAlpha : 0f);
                colors[4 + row * 2] = colors[5 + row * 2] = col;
            }
            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }
    }
}
