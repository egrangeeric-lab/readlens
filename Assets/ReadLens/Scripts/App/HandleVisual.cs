using Oculus.Interaction;
using UnityEngine;

namespace ReadLens.App
{
    // Quest-style window handle: a thin white bar, dim at rest, brighter and a little wider
    // when a hand hovers it, fully lit while held.
    public class HandleVisual : MonoBehaviour
    {
        [SerializeField] Transform bar;

        const float Rest = 0.45f, Hover = 0.8f, Held = 1f, Speed = 6f;
        IInteractableView[] views;
        Material material;
        Vector3 restScale;
        float level = Rest;

        void Awake()
        {
            views = GetComponentsInChildren<IInteractableView>(true); // the grab + ray-grab interactables
            material = bar.GetComponent<Renderer>().material;
            restScale = bar.localScale;
        }

        void Update()
        {
            float target = Rest;
            foreach (var v in views)
            {
                if (v.State == InteractableState.Select) target = Held;
                else if (v.State == InteractableState.Hover && target < Hover) target = Hover;
            }
            level = Mathf.MoveTowards(level, target, Speed * Time.deltaTime);
            material.color = new Color(1f, 1f, 1f, level);
            float grow = Mathf.InverseLerp(Rest, Held, level);
            bar.localScale = new Vector3(restScale.x, restScale.y * (1f + 0.25f * grow), restScale.z);
        }
    }
}
