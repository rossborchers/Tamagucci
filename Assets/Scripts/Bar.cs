using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Bar : MonoBehaviour
{
    public float PixelPerfectMinValue = 0.1f;
    public float MaxWidth;
    public Transform BarTransform;
    public TextMeshPro Text;

    public float MaxThreshold = 1;
    public float MinThreshold = 1;

    public Color NormalColor = Color.white;
    public Color MaxColor = Color.red;
    public Color MinColor = Color.green;
    public bool InvertMaxMin;

    private SpriteRenderer _spriteRenderer;
    private void Awake()
    {
        _spriteRenderer = BarTransform.GetComponent<SpriteRenderer>();
    }

    public void SetValue(float value, float max)
    {
        if (Math.Abs(value - max) < MaxThreshold)
        {
            Color c = (InvertMaxMin) ? MinColor : MaxColor;
            _spriteRenderer.color = c;
            Text.color = c;
        }
        else if (Math.Abs(value) < MinThreshold)
        {
            Color c = (InvertMaxMin) ? MaxColor : MinColor;
            _spriteRenderer.color = c;
            Text.color = c;
        }
        else
        {
            _spriteRenderer.color = NormalColor;
            Text.color = NormalColor;
        }
        
        float norm = Mathf.Clamp01(value / max);
        float size = norm * MaxWidth;
        float halfSize = size / 2;

        //round to something pixel perfect so we dont get weird jitter
        size = Mathf.Round(size/PixelPerfectMinValue)*PixelPerfectMinValue;
        halfSize = Mathf.Round(halfSize/PixelPerfectMinValue)*PixelPerfectMinValue;
        
        BarTransform.localScale = new Vector3(size, BarTransform.localScale.y, BarTransform.localScale.z);
        BarTransform.localPosition = new Vector3(halfSize, BarTransform.localPosition.y, BarTransform.localPosition.z);
    }
}
