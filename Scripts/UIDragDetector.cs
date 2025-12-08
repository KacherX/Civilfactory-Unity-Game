using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[Serializable]
public class UIPositions
{
    public Vector2 blueprintPanelPosition = new Vector2(0, 0);
}

public class UIDragDetector : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject parentUI;
    private RectTransform rectTransform;
    private Canvas canvas;

    private SaveLoadDataManager sm;
    private PlayerData playerData;
    IEnumerator Start()
    {
        sm = GameObject.FindGameObjectWithTag("SaveLoadDataManager").GetComponent<SaveLoadDataManager>();
        while (!sm.Initialized) // Wait until Player Data loaded.
        {
            yield return null;
        }

        playerData = sm.playerData;

        rectTransform = parentUI.GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        rectTransform.anchoredPosition = playerData.uiPositions.blueprintPanelPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Optional: Add logic when drag starts
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // Move the UI element with the drag
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        playerData.uiPositions.blueprintPanelPosition = rectTransform.anchoredPosition;
    }
}
