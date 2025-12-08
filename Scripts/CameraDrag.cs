using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraDrag : MonoBehaviour
{
    private Vector3 dragOrigin;
    public Camera cam;

    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 5f;

    private float cameraHeight;
    private float cameraWidth;
    private Vector3 cameraCenter;

    private float tileSize = 0.6f - 0.0625f / 2f;
    void Start()
    {
        cam = Camera.main;
        cameraHeight = 10;
        cameraWidth = 10 * cam.aspect;
    }

    void Update()
    {
        DragCamera();
        ZoomCamera();
    }

    void DragCamera()
    {
        if (Input.GetMouseButtonDown(1))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            cam.transform.position += difference;

            Vector3 pos = cam.transform.position;

            Bounds cameraBounds = GetCameraBounds();

            pos.x = Mathf.Clamp(pos.x, cameraBounds.min.x, cameraBounds.max.x);
            pos.y = Mathf.Clamp(pos.y, cameraBounds.min.y, cameraBounds.max.y);

            cam.transform.position = pos;
        }
    }
    void ZoomCamera()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                cam.orthographicSize -= scroll * zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
        }
    }
    public void SetMainCamera(PlayerData playerData)
    {
        int left = 1000000;
        int right = -1000000;
        int top = -1000000;
        int bottom = 1000000;
        foreach (PixelData pixel in playerData.pixels.pixels)
        {
            if (pixel.x <= left)
            {
                left = pixel.x;
            }
            if (pixel.x >= right)
            {
                right = pixel.x;
            }
            if (pixel.y <= bottom)
            {
                bottom = pixel.y;
            }
            if (pixel.y >= top)
            {
                top = pixel.y;
            }
        }

        float leftPos = left * tileSize;
        float rightPos = right * tileSize;
        float topPos = top * tileSize;
        float bottomPos = bottom * tileSize;

        cameraCenter = new Vector3((leftPos + rightPos) / 2f + tileSize / 2f, (bottomPos + topPos) / 2f + tileSize / 2f, -10);
        cam.transform.position = cameraCenter;

        float horDif = (rightPos - leftPos)/cameraWidth + 0.5f;
        float vertDif = (topPos - bottomPos)/cameraHeight + 0.5f;
        float maxDif = Mathf.Max(horDif, vertDif);
        if (maxDif >= 1f)
        {
            maxZoom = maxDif * 5f;
        }
        else
        {
            maxZoom = 5f;
        }
    }
    public Bounds GetCameraBounds()
    {
        float height = 2f * maxZoom;
        float width = height * cam.aspect;

        return new Bounds(cameraCenter, new Vector3(width, height, 0f));
    }
}
