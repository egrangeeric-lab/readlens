using System;
using UnityEngine;

namespace ReadLens.App
{
    // Meta microgestures: with a loosely closed hand, slide the thumb along the side of the index finger
    // (swipe left / right) or tap it (thumb tap). Tiny, one-handed, works while holding a book.
    public class ThumbGestures : MonoBehaviour
    {
        [SerializeField] OVRHand left, right;

        public event Action<int> OnSwipe; // +1 = next, -1 = previous
        public event Action OnTap;

        const float Debounce = 0.35f;
        float quietUntil;

        void Update()
        {
            Poll(left);
            Poll(right);
        }

        void Poll(OVRHand hand)
        {
            if (hand == null) return;
            var gesture = hand.GetMicrogestureType();
            if (Time.time < quietUntil) return;
            switch (gesture)
            {
                case OVRHand.MicrogestureType.SwipeRight: OnSwipe?.Invoke(1); break;
                case OVRHand.MicrogestureType.SwipeLeft: OnSwipe?.Invoke(-1); break;
                case OVRHand.MicrogestureType.ThumbTap: OnTap?.Invoke(); break;
                default: return;
            }
            quietUntil = Time.time + Debounce;
        }
    }
}
