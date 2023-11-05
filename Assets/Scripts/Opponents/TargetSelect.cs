using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSelect : MonoBehaviour
{
    ThirdPersonController warp;

    void Start()
    {
        warp = FindObjectOfType<ThirdPersonController>();
    }

    private void OnBecameVisible()
    {
        if (!warp.screenTargets.Contains(transform))
            warp.screenTargets.Add(transform);
    }

    private void OnBecameInvisible()
    {
        if (warp.screenTargets.Contains(transform))
            warp.screenTargets.Remove(transform);
    }
}

