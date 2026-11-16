using System;
using Oculus.Interaction.Input;
using UnityEngine;

namespace ReadLens.App
{
    // "Photographer's frame": both hands make an L (thumb + index out, other fingers folded)
    // around the text to read. Holding it briefly fires OnFramed with the two corners.
    public class FrameGesture : MonoBehaviour
    {
        [SerializeField] LineRenderer outline;
        [SerializeField] Transform head;

        public event Action<Vector3, Vector3> OnFramed;
        public Func<bool> Enabled = () => true;

        const float HoldSeconds = 0.8f, Cooldown = 2f, MinSize = 0.1f;
        Hand left, right;
        float held, coolUntil;

        void Update()
        {
            outline.enabled = false;
            if (!Enabled() || Time.time < coolUntil) { held = 0; return; }
            FindHands();
            if (left == null || right == null || !IsL(left, out var a) || !IsL(right, out var b) || Vector3.Distance(a, b) < MinSize)
            {
                held = 0;
                return;
            }
            held += Time.deltaTime;
            Draw(a, b, Mathf.Clamp01(held / HoldSeconds));
            if (held < HoldSeconds) return;
            held = 0;
            coolUntil = Time.time + Cooldown;
            OnFramed?.Invoke(a, b);
        }

        void FindHands()
        {
            if (left != null && right != null) return;
            foreach (var h in FindObjectsByType<Hand>(FindObjectsSortMode.None))
            {
                if (h.Handedness == Handedness.Left && left == null) left = h;
                if (h.Handedness == Handedness.Right && right == null) right = h;
            }
        }

        // Ratios rather than centimetres so it works for small (children's) hands too.
        static bool IsL(Hand h, out Vector3 corner)
        {
            corner = default;
            if (!h.IsConnected || !h.IsHighConfidence) return false;
            if (!P(h, HandJointId.HandWristRoot, out var wrist) || !P(h, HandJointId.HandIndex1, out var index1) ||
                !P(h, HandJointId.HandIndexTip, out var indexTip) || !P(h, HandJointId.HandThumb2, out var thumb2) ||
                !P(h, HandJointId.HandThumbTip, out var thumbTip) || !P(h, HandJointId.HandMiddle1, out var middle1) ||
                !P(h, HandJointId.HandMiddleTip, out var middleTip) || !P(h, HandJointId.HandRingTip, out var ringTip))
                return false;
            float palm = Vector3.Distance(wrist, middle1);
            bool indexOut = Vector3.Distance(indexTip, wrist) > 1.6f * palm;
            bool othersFolded = Vector3.Distance(middleTip, wrist) < 1.3f * palm && Vector3.Distance(ringTip, wrist) < 1.3f * palm;
            bool thumbOut = Vector3.Distance(thumbTip, index1) > 0.7f * palm;
            float angle = Vector3.Angle(indexTip - index1, thumbTip - thumb2);
            corner = (index1 + thumb2) * 0.5f;
            return indexOut && othersFolded && thumbOut && angle > 45f && angle < 135f;
        }

        static bool P(Hand h, HandJointId id, out Vector3 p)
        {
            bool ok = h.GetJointPose(id, out var pose);
            p = pose.position;
            return ok;
        }

        // Rectangle with corners a and b, facing the reader; brightens as the hold completes.
        void Draw(Vector3 a, Vector3 b, float progress)
        {
            var toMid = (a + b) * 0.5f - head.position;
            var right = Vector3.ProjectOnPlane(head.right, toMid).normalized;
            var up = Vector3.Cross(toMid, right).normalized;
            var d = b - a;
            var p2 = a + right * Vector3.Dot(d, right);
            var p4 = a + up * Vector3.Dot(d, up);
            outline.enabled = true;
            outline.positionCount = 4;
            outline.loop = true;
            outline.SetPositions(new[] { a, p2, b, p4 });
            outline.widthMultiplier = Mathf.Lerp(0.002f, 0.006f, progress);
            var c = UIKit.Accent;
            c.a = Mathf.Lerp(0.35f, 1f, progress);
            outline.startColor = outline.endColor = c;
        }
    }
}
