using System;
using UnityEngine;

namespace ReadLens.App
{
    // Text legibility over raw performance: the scene is light, so spend the GPU on sharpness.
    public class DisplayQuality : MonoBehaviour
    {
        void Start()
        {
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.3f;
            QualitySettings.antiAliasing = 4;
            OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.Off; // no blur where text may sit
            if (OVRManager.display != null && Array.IndexOf(OVRManager.display.displayFrequenciesAvailable, 90f) >= 0)
                OVRManager.display.displayFrequency = 90f; // smoother text while the head moves
        }
    }
}
