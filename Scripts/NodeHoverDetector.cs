using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NodeHoverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    public GameObject infoPanel;

    public ResourceData recipeData;
    public bool reverse = false;

    public GameObject recipeResourceFramePrefab;

    private RectTransform rt;
    private RectTransform layoutRt;
    private TextMeshProUGUI timeText;
    private Transform inputContent;
    private Transform outputContent;

    private RectTransform uiNodeRt;
    private SaveLoadDataManager sm;

    private bool isHovering = false;
    private float defaultResolutionX = 1920f;
    private float defaultResolutionY = 1080f;

    private Color32 moneyActiveColor = new Color32(60, 255, 0, 255);
    private void Start()
    {
        uiNodeRt = gameObject.GetComponent<RectTransform>();
        sm = GameObject.FindGameObjectWithTag("SaveLoadDataManager").GetComponent<SaveLoadDataManager>();

        rt = infoPanel.GetComponent<RectTransform>();
        layoutRt = infoPanel.transform.GetComponent<RectTransform>();

        inputContent = infoPanel.transform.Find("ResearchNodeInfo").transform.Find("InputPanel").transform.Find("InputContent");
        outputContent = infoPanel.transform.Find("ResearchNodeInfo").transform.Find("OutputPanel").transform.Find("OutputContent");
        timeText = infoPanel.transform.Find("ResearchNodeInfo").transform.Find("MiddlePanel").transform.Find("TimeText").GetComponent<TextMeshProUGUI>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        setInfoPanel();
        if (!infoPanel.activeSelf)
        {
            infoPanel.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (infoPanel.activeSelf)
        {
            infoPanel.SetActive(false);
        }
    }
    public void OnPointerMove(PointerEventData eventData)
    {
        if (isHovering)
        {
            //rt.anchoredPosition = new Vector2(uiNodeRt.anchoredPosition.x - 300, uiNodeRt.anchoredPosition.y - 36.25f);
            SetRectOffsets(Input.mousePosition.x, Input.mousePosition.y);
        }
    }
    void SetRectOffsets(float left, float bottom)
    {
        float offsetX = defaultResolutionX / Screen.width;
        float offsetY = defaultResolutionY / Screen.height;
        if (reverse)
        {
            rt.offsetMin = new Vector2(left * offsetX - 300, bottom * offsetY + layoutRt.sizeDelta.y + 36.25f);
            rt.offsetMax = new Vector2(left * offsetX - 300, bottom * offsetY + layoutRt.sizeDelta.y + 36.25f);
        }
        else
        {
            rt.offsetMin = new Vector2(left * offsetX - 300, bottom * offsetY - 36.25f);
            rt.offsetMax = new Vector2(left * offsetX - 300, bottom * offsetY - 36.25f);
        }
    }
    void setInfoPanel()
    {
        float timePerResource = recipeData.time;
        float quantityPerMin = 60f / timePerResource;

        timeText.text = timePerResource.ToString("0.00") + " sec";

        DestroyAllChildren(inputContent);

        foreach (ResourceRequirement resourceRequirement in recipeData.resource_requirements)
        {

            GameObject ResourceRequirementFrame = Instantiate(recipeResourceFramePrefab, inputContent);

            ResourceRequirementFrame.transform.Find("QuantityNameText").GetComponent<TextMeshProUGUI>().text = resourceRequirement.quantity + " " + sm.resourceDataDatabase.resources[resourceRequirement.index].name;
            ResourceRequirementFrame.transform.Find("ResourceImage").GetComponent<Image>().sprite = sm.resourceImages[sm.resourceDataDatabase.resources[resourceRequirement.index].image_name];
            ResourceRequirementFrame.transform.Find("MinuteText").GetComponent<TextMeshProUGUI>().text = (resourceRequirement.quantity * quantityPerMin).ToString("0") + "/min";
        }

        DestroyAllChildren(outputContent);
        GameObject OutputFrame = Instantiate(recipeResourceFramePrefab, outputContent);
        OutputFrame.transform.Find("QuantityNameText").GetComponent<TextMeshProUGUI>().text = "1 " + sm.resourceDataDatabase.resources[recipeData.index].name;
        OutputFrame.transform.Find("ResourceImage").GetComponent<Image>().sprite = sm.resourceImages[sm.resourceDataDatabase.resources[recipeData.index].image_name];
        OutputFrame.transform.Find("MinuteText").GetComponent<TextMeshProUGUI>().text = quantityPerMin.ToString("0") + "/min";
        OutputFrame.transform.Find("MoneyText").GetComponent<TextMeshProUGUI>().text = "$" + GlobalFunctions.NumberMakeUp(recipeData.sellPrice) + "/ea";
        OutputFrame.transform.Find("MoneyText").GetComponent<TextMeshProUGUI>().color = moneyActiveColor;
    }
    private void DestroyAllChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }
}
