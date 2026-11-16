using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ReadLens.App
{
    // Short UI animations (unscaled time, ease-out, no per-frame allocations). Each component only
    // updates while it animates. With "Reduce motion" on, every animation jumps to its end.
    public static class Motion
    {
        const string Key = "readlens.reduceMotion"; // device setting, not part of the reading profile

        static int reduced = -1; // cached: read every animated frame

        public static bool Reduced
        {
            get
            {
                if (reduced < 0) reduced = PlayerPrefs.GetInt(Key, 0);
                return reduced == 1;
            }
            set
            {
                reduced = value ? 1 : 0;
                PlayerPrefs.SetInt(Key, reduced);
                PlayerPrefs.Save();
            }
        }

        public static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        // Advances 0..1 progress; with reduced motion it completes at once.
        public static float Step(float t, float duration) => Reduced ? 1f : Mathf.Min(1f, t + Time.unscaledDeltaTime / duration);
    }

    // Fades a view in/out through a CanvasGroup (optionally scaling a child) and deactivates it once hidden.
    public class Fade : MonoBehaviour
    {
        CanvasGroup group;
        Transform scaleTarget;
        float fromScale = 1f, duration = 0.18f, progress = 1f, startAlpha;
        bool shown;

        public bool Shown => shown;

        public static Fade On(GameObject go, float duration = 0.18f, Transform scaleTarget = null, float fromScale = 1f)
        {
            var f = go.AddComponent<Fade>();
            f.group = go.AddComponent<CanvasGroup>();
            f.duration = duration;
            f.scaleTarget = scaleTarget;
            f.fromScale = fromScale;
            f.shown = go.activeSelf;
            f.enabled = false;
            return f;
        }

        public void Show(bool on)
        {
            if (on == shown && (on || !gameObject.activeSelf)) return;
            shown = on;
            group.blocksRaycasts = group.interactable = on; // a view fading out can't be pressed
            startAlpha = gameObject.activeSelf ? group.alpha : 0f;
            gameObject.SetActive(true);
            progress = 0f;
            enabled = true;
            Update();
        }

        void Update()
        {
            progress = Motion.Step(progress, duration);
            float k = Motion.EaseOut(progress);
            group.alpha = Mathf.Lerp(startAlpha, shown ? 1f : 0f, k);
            if (scaleTarget != null) scaleTarget.localScale = Vector3.one * Mathf.Lerp(fromScale, 1f, group.alpha);
            if (progress < 1f) return;
            enabled = false;
            if (!shown) gameObject.SetActive(false);
        }
    }

    // Slides content in from the side a page was turned to, fading it in.
    public class Slide : MonoBehaviour
    {
        const float Distance = 90f, Duration = 0.22f;
        RectTransform rect;
        CanvasGroup group;
        float from, progress = 1f;

        public static Slide On(RectTransform target)
        {
            var s = target.gameObject.AddComponent<Slide>();
            s.rect = target;
            s.group = target.gameObject.AddComponent<CanvasGroup>();
            s.enabled = false;
            return s;
        }

        // direction +1: the new content comes from the right (next), -1 from the left (previous).
        public void Play(int direction)
        {
            from = direction * Distance;
            progress = 0f;
            enabled = true;
            Update();
        }

        void Update()
        {
            progress = Motion.Step(progress, Duration);
            float k = Motion.EaseOut(progress);
            var p = rect.anchoredPosition;
            p.x = from * (1f - k);
            rect.anchoredPosition = p;
            group.alpha = Mathf.Lerp(0.2f, 1f, k);
            if (progress >= 1f) enabled = false;
        }
    }

    // Press feedback: the control sinks a little while held (works with poke and ray: both send uGUI pointer events).
    public class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        const float Pressed = 0.94f, Duration = 0.12f;
        Selectable selectable;
        float from = 1f, to = 1f, progress = 1f;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            enabled = false;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (selectable == null || selectable.IsInteractable()) Animate(Pressed);
        }

        public void OnPointerUp(PointerEventData e) => Animate(1f);

        void Animate(float target)
        {
            from = transform.localScale.x;
            to = target;
            progress = 0f;
            enabled = true;
            Update();
        }

        void Update()
        {
            progress = Motion.Step(progress, Duration);
            transform.localScale = Vector3.one * Mathf.Lerp(from, to, Motion.EaseOut(progress));
            if (progress >= 1f) enabled = false;
        }
    }
}
