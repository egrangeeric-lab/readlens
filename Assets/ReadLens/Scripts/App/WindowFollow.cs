using System.Collections;
using Oculus.Interaction;
using UnityEngine;

namespace ReadLens.App
{
    // Keeps the window findable: it opens in front of the reader, and if it stays out of view for a moment
    // it glides back in front. Grabbing always wins.
    public class WindowFollow : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] PointableElement grab;
        [SerializeField] Transform surface;   // what the eyes see (compositor layer mesh)
        [SerializeField] CanvasGroup content; // window UI, faded in at launch
        [SerializeField] float distance = 0.5f, drop = 0.12f, tilt = 12f;

        const float LostAngle = 55f, LostSeconds = 2.5f, FarDistance = 1.5f, OpenSeconds = 0.6f;
        float lostFor;
        bool moving;
        Vector3 targetPos;
        Quaternion targetRot;

        IEnumerator Start()
        {
            content.alpha = 0f;
            // The head pose is only valid after tracking starts.
            for (int i = 0; i < 10; i++) yield return null;
            ComputeTarget();
            transform.SetPositionAndRotation(targetPos, targetRot);
            yield return Open();
        }

        // The window unfolds: grows from 70% with a slight overshoot, rises a little and fades in.
        // Only the visible surface is scaled: scaling the canvas would resize its render texture every frame.
        IEnumerator Open()
        {
            var restPos = surface.localPosition;
            for (float t = 0f; t < 1f;)
            {
                t = Motion.Step(t, OpenSeconds);
                float k = Motion.EaseOutBack(t);
                surface.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, k);
                surface.localPosition = restPos + Vector3.down * (0.04f * (1f - Motion.EaseOut(t)));
                content.alpha = Motion.EaseOut(t);
                yield return null;
            }
            surface.localScale = Vector3.one;
            surface.localPosition = restPos;
            content.alpha = 1f;
        }

        public void BringHere()
        {
            ComputeTarget();
            moving = true;
        }

        void LateUpdate()
        {
            if (grab != null && grab.SelectingPointsCount > 0)
            {
                moving = false;
                lostFor = 0;
                return;
            }
            if (!moving)
            {
                var toWindow = transform.position - head.position;
                bool lost = Vector3.Angle(head.forward, toWindow) > LostAngle || toWindow.magnitude > FarDistance;
                lostFor = lost ? lostFor + Time.deltaTime : 0;
                if (lostFor > LostSeconds) BringHere();
            }
            if (!moving) return;
            float t = 1f - Mathf.Exp(-5f * Time.deltaTime);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, targetPos, t), Quaternion.Slerp(transform.rotation, targetRot, t));
            if ((transform.position - targetPos).sqrMagnitude < 1e-5f) moving = false;
        }

        void ComputeTarget()
        {
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            forward = forward.sqrMagnitude < 1e-4f ? Vector3.forward : forward.normalized;
            targetPos = head.position + forward * distance + Vector3.down * drop;
            targetRot = Quaternion.LookRotation(forward) * Quaternion.Euler(tilt, 0f, 0f);
            lostFor = 0;
        }
    }
}
