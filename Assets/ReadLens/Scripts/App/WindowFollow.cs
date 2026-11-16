using System.Collections;
using Oculus.Interaction;
using UnityEngine;

namespace ReadLens.App
{
    // Keeps the window findable: if it stays out of view for a moment it glides back in front of the reader.
    // Pinned windows never move on their own. Grabbing always wins.
    public class WindowFollow : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] PointableElement grab;
        [SerializeField] float distance = 0.5f, drop = 0.12f, tilt = 12f;

        public bool Pinned;

        const float LostAngle = 55f, LostSeconds = 2.5f, FarDistance = 1.5f;
        float lostFor;
        bool moving;
        Vector3 targetPos;
        Quaternion targetRot;

        IEnumerator Start()
        {
            // The head pose is only valid after tracking starts.
            for (int i = 0; i < 10; i++) yield return null;
            ComputeTarget();
            transform.SetPositionAndRotation(targetPos, targetRot);
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
            if (!Pinned && !moving)
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
