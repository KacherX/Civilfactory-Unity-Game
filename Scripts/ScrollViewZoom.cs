using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollViewZoom : MonoBehaviour
{
    private RectTransform targetUI;
    private float zoomSpeed = 0.5f;
    public float minScale = 0.25f;
    public float maxScale = 3f;

    private void Start()
    {
        targetUI = GetComponent<RectTransform>();
    }
    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            Vector3 newScale = targetUI.localScale + Vector3.one * scroll * zoomSpeed;

            newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
            newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
            newScale.z = 1f;

            targetUI.localScale = newScale;
        }
    }
}
