using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Resource // Burasi save edilen kisim.
{
    public event Action<Resource> OnQuantityChanged;
    public event Action<Resource> OnSelectedQuantityChanged;

    public int _quantity;
    public int _selectedQuantity;
    public int quantity
    {
        get => _quantity;
        set
        {
            _quantity = value;
            OnQuantityChanged?.Invoke(this);
        }
    }
    public int selectedQuantity
    {
        get => _selectedQuantity;
        set
        {
            _selectedQuantity = value;
            OnSelectedQuantityChanged?.Invoke(this);
        }
    }

    [NonSerialized] public ResourceData resourceData;

    [NonSerialized] public GameObject UIFrame;
    [NonSerialized] public TextMeshProUGUI MaxQuantityText;
    [NonSerialized] public TextMeshProUGUI CurrentQuantityText;
    [NonSerialized] public TextMeshProUGUI ValueText;
    [NonSerialized] public Slider slider;
}

[Serializable]
public class ResourceRequirement
{
    public int index = -1;
    public int quantity = 0;
}
[Serializable]
public class ResourceData
{
    public int index = -1;
    public double sellPrice;
    public string name;
    public string image_name;

    public int room_index;
    public double xp;
    public int research_node_index;
    public float time;
    public ResourceRequirement[] resource_requirements;
}

[Serializable]
public class ResourceDatabase
{
    public List<Resource> resources = new List<Resource>();
}

[Serializable]
public class ResourceDataDatabase
{
    [NonSerialized] public List<ResourceData>[] recipes = new List<ResourceData>[20]; // bu room sayisina esit olsun
    public List<ResourceData> resources = new List<ResourceData>();
}
public class ResourceManager : MonoBehaviour
{
    public Animator animator; // Assign this in the Inspector
    public Button ResourcePanelButton;

    public Sprite openButtonSprite;
    public Sprite closeButtonSprite;

    public Transform resourcesContent;
    public GameObject resourceFramePrefab;

    private SaveLoadDataManager sm;
    private PlayerData playerData;

    private string openAnimationName = "ResourcePanelOpen";
    private string closeAnimationName = "ResourcePanelClose";
    IEnumerator Start()
    {
        sm = GameObject.FindGameObjectWithTag("SaveLoadDataManager").GetComponent<SaveLoadDataManager>();
        while (!sm.Initialized) // Wait until Player Data loaded.
        {
            yield return null;
        }

        playerData = sm.playerData;

        SetResourceEvents();

        ResourcePanelButton.onClick.AddListener(OpenClosePanel);
        ResourcePanelButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
    }

    void OpenClosePanel()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(openAnimationName))
        {
            animator.Play(closeAnimationName);
            ResourcePanelButton.GetComponent<Image>().sprite = openButtonSprite;
        }
        else
        {
            animator.Play(openAnimationName);
            ResourcePanelButton.GetComponent<Image>().sprite = closeButtonSprite;
        }
    }

    void SetResourceEvents()
    {
        foreach (Resource resource in playerData.resources.resources.OrderByDescending(r => r.resourceData.sellPrice).ToList())
        {
            GameObject ResourceFrame = Instantiate(resourceFramePrefab, resourcesContent);
            ResourceFrame.transform.Find("ResourceImage").GetComponent<Image>().sprite = sm.resourceImages[resource.resourceData.image_name];
            ResourceFrame.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = resource.resourceData.name;
            
            resource.UIFrame = ResourceFrame;
            resource.MaxQuantityText = ResourceFrame.transform.Find("MaxQuantityText").GetComponent<TextMeshProUGUI>();
            resource.CurrentQuantityText = ResourceFrame.transform.Find("CurrentQuantityText").GetComponent<TextMeshProUGUI>();
            resource.ValueText = ResourceFrame.transform.Find("ValueText").GetComponent<TextMeshProUGUI>();

            resource.slider = ResourceFrame.transform.Find("QuantitySlider").GetComponent<Slider>();
            resource.slider.onValueChanged.AddListener(value => OnResourceSliderChanged(value, resource));
            ResourceFrame.transform.Find("SellButton").GetComponent<Button>().onClick.AddListener(() => OnSellButtonPressed(playerData.currency, resource));
            ResourceFrame.transform.Find("SellButton").GetComponent<Button>().onClick.AddListener(PlaySound.PlayButtonClickSound);

            resource.OnQuantityChanged += OnResourceQuantityChanged;
            resource.OnSelectedQuantityChanged += OnSelectedQuantityChanged;
            OnResourceQuantityChanged(resource);
            OnSelectedQuantityChanged(resource);
        }
    }
    void OnResourceQuantityChanged(Resource resource)
    {
        if (resource.quantity > 0)
        {
            resource.UIFrame.SetActive(true);
            resource.MaxQuantityText.text = GlobalFunctions.NumberMakeUp(resource.quantity);
        }
        else
        {
            resource.UIFrame.SetActive(false);
        }
    }
    void OnSelectedQuantityChanged(Resource resource)
    {
        resource.CurrentQuantityText.text = GlobalFunctions.NumberMakeUp(resource.selectedQuantity);
        resource.ValueText.text = "$" + GlobalFunctions.NumberMakeUp(resource.selectedQuantity * resource.resourceData.sellPrice);
    }
    void OnResourceSliderChanged(float value, Resource resource)
    {
        resource.selectedQuantity = (int)(value * resource.quantity);
    }
    void OnSellButtonPressed(Currency currency, Resource resource)
    {
        if (resource.selectedQuantity > 0 && resource.selectedQuantity <= resource.quantity)
        {
            currency.money += resource.selectedQuantity * resource.resourceData.sellPrice;
            resource.quantity -= resource.selectedQuantity;
            resource.selectedQuantity = 0;
            resource.slider.value = 0;
        }
    }
}
