using DG.Tweening;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EggBreakAnimateUp : MonoBehaviour
{
    public float startY;
    public float endY;

    void OnEnable()
    {
        transform.position = new Vector3(transform.position.x, startY, transform.position.z);
    }
    
    [UsedImplicitly]
    void AnimEggBreak()
    {
        transform.DOMoveY(endY, 0.5f, false);
    }
}
