using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomInfoManager : MonoBehaviour
{
    public TileMapManager tmm;

    public Animator animator; // Assign this in the Inspector
    public Animator inputOutputObjAnimator;

    public GameObject inputOutputObj;
    public Image topArrow;
    public Image bottomArrow;
    public Image rightArrow;
    public Image leftArrow;

    public Transform roomInfoPanel;
    public Transform roomMainPanel;
    public Transform roomLittleInfoPanel;
    public Transform selectRecipeFrame;
    public Button rlRotateButton;
    public Button rlDestroyButton;
    public Button rlBackButton;
    public Button ChangeButton;
    public Button RotateButton;
    public Button DestroyButton;
    public Button MainBackButton;
    public Button SelectRecipeBackButton;

    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI rlNameText;
    public TextMeshProUGUI timeText;

    public Transform recipesContent;
    public GameObject recipeFramePrefab;
    public Transform inputContent;
    public Transform outputContent;
    public GameObject recipeResourceFramePrefab;

    private SaveLoadDataManager sm;
    private PlayerData playerData;

    public PixelData choosenPixelData;
    private List<TextMeshProUGUI> resourceHolderTexts = new List<TextMeshProUGUI>();
    private List<ResourceRequirement> resourceRequirements = new List<ResourceRequirement>();

    private string openAnimationName = "RoomInfoPanelOpen";
    private string closeAnimationName = "RoomInfoPanelClose";
    private string inpOutObjAnimationName = "InputOutputArrows";

    private Color32 activeColor = new Color32(200, 232, 255, 255);
    private Color32 deactiveColor = new Color32(200, 232, 255, 100);
    private Color32 moneyActiveColor = new Color32(60, 255, 0, 255);
    private Color32 moneyDeactiveColor = new Color32(60, 255, 0, 100);
    IEnumerator Start()
    {
        sm = GameObject.FindGameObjectWithTag("SaveLoadDataManager").GetComponent<SaveLoadDataManager>();
        while (!sm.Initialized) // Wait until Player Data loaded.
        {
            yield return null;
        }

        playerData = sm.playerData;

        ChangeButton.onClick.AddListener(() => ChangeFrames(selectRecipeFrame));
        ChangeButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        RotateButton.onClick.AddListener(() => RotateRoom());
        RotateButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        rlRotateButton.onClick.AddListener(() => RotateRoom());
        rlRotateButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        DestroyButton.onClick.AddListener(() => DestroyRoom());
        DestroyButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        rlDestroyButton.onClick.AddListener(() => DestroyRoom());
        rlDestroyButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        MainBackButton.onClick.AddListener(() => OpenClosePanel(false));
        MainBackButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        rlBackButton.onClick.AddListener(() => OpenClosePanel(false));
        rlBackButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        SelectRecipeBackButton.onClick.AddListener(() => 
        {
            PlaySound.PlayButtonClickSound();
            if (choosenPixelData != null && choosenPixelData.rIndex != -1)
            {
                ChangeFrames(roomMainPanel);
            }
            else
            {
                OpenClosePanel(false);
            }
        }
        );

        InvokeRepeating("Update10", 0.1f, 0.1f);
    }
    private void Update10()
    {
        for (int i = 0 ; i < resourceRequirements.Count; i++)
        {
            if (i < resourceHolderTexts.Count)
            {
                ResourceRequirement resourceRequirement = resourceRequirements[i];
                TextMeshProUGUI resourceHolderText = resourceHolderTexts[i];

                ResourceRequirement res2 = choosenPixelData.storedResources.Find(r => r.index == resourceRequirement.index);
                if (res2 != null)
                {
                    int currentQuantity = res2.quantity;
                    if (res2.quantity >= resourceRequirement.quantity)
                    {
                        resourceHolderText.text = "<color=#00ff00>[" + currentQuantity + "/" + resourceRequirement.quantity + "]";
                    }
                    else
                    {
                        resourceHolderText.text = "<color=#ff0000>[" + currentQuantity + "/" + resourceRequirement.quantity + "]";
                    }
                }
                else
                {
                    resourceHolderText.text = "<color=#ff0000>[0/" + resourceRequirement.quantity + "]";
                }
            }
        }
    }
    public void OpenClosePanel(bool openClose)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(openAnimationName) && openClose == false)
        {
            animator.Play(closeAnimationName);
            tmm.clearTopAnimationTilemap();
        }
        else if (stateInfo.IsName(closeAnimationName) && openClose == true)
        {
            animator.Play(openAnimationName);
        }

        SetInputOutputFrame(choosenPixelData, openClose);
    }
    public void SetUpPanel(PixelData room)
    {
        choosenPixelData = room;
        if (tmm.producerRoomIndexes.Contains(room.index)) // Producer room indexes
        {
            List<ResourceData> recipes = sm.resourceDataDatabase.recipes[room.index];
            if (recipes == null) return;

            RoomData roomData = sm.roomDataDatabase.rooms[room.index];
            if (roomData.energyValue < 0)
            {
                roomNameText.text = roomData.name + "<color=#FF9100> [Consumes " + roomData.energyValue * -1 + " Energy]";
            }
            else if (roomData.energyValue > 0)
            {
                roomNameText.text = roomData.name + "<color=#FF9100> [Produces " + GlobalFunctions.NumberMakeUp(roomData.energyValue) + " Energy]";
            }
            else
            {
                roomNameText.text = roomData.name;
            }

            // Recipe settings
            DestroyAllChildren(recipesContent);
            foreach (ResourceData recipeData in recipes)
            {
                GameObject RecipeFrame = Instantiate(recipeFramePrefab, recipesContent);

                Image resourceImage = RecipeFrame.transform.Find("ResourceImage").GetComponent<Image>();
                TextMeshProUGUI nameText = RecipeFrame.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI lockText = RecipeFrame.transform.Find("LockText").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI requirementLevelText = RecipeFrame.transform.Find("RequirementLevelText").GetComponent<TextMeshProUGUI>();
                Image lockImage = RecipeFrame.transform.Find("LockImage").GetComponent<Image>();
                
                resourceImage.sprite = sm.resourceImages[recipeData.image_name];
                nameText.text = recipeData.name;
                requirementLevelText.text = "$" + GlobalFunctions.NumberMakeUp(recipeData.sellPrice) + "/ea";

                lockText.gameObject.SetActive(false);

                if (playerData.level.skillTreeNodes[recipeData.research_node_index].hasNode) // Can do
                {
                    lockImage.gameObject.SetActive(false);
                    requirementLevelText.color = moneyActiveColor;
                    resourceImage.color = activeColor;
                    nameText.color = activeColor;
                }
                else
                {
                    lockImage.gameObject.SetActive(true);
                    requirementLevelText.color = moneyDeactiveColor;
                    resourceImage.color = deactiveColor;
                    nameText.color = deactiveColor;
                }

                RecipeFrame.GetComponent<Button>().onClick.AddListener(() => SetUpRecipe(recipeData, false));
                RecipeFrame.GetComponent<Button>().onClick.AddListener(PlaySound.PlayButtonClickSound);
            }

            if (room.rIndex == -1) // Recipe yoksa
            {
                ChangeFrames(selectRecipeFrame);
            }
            else
            {
                SetUpRecipeResources(sm.resourceDataDatabase.resources[room.rIndex], false);
                ChangeFrames(roomMainPanel);
            }
        }
        else if (room.index == tmm.dropperIndex)
        {
            List<Resource> resources = playerData.resources.resources;
            if (resources == null) return;

            RoomData roomData = sm.roomDataDatabase.rooms[room.index];
            if (roomData.energyValue < 0)
            {
                roomNameText.text = roomData.name + "<color=#FF9100> [Consumes " + roomData.energyValue * -1 + " Energy]";
            }
            else if (roomData.energyValue > 0)
            {
                roomNameText.text = roomData.name + "<color=#FF9100> [Produces " + GlobalFunctions.NumberMakeUp(roomData.energyValue) + " Energy]";
            }
            else
            {
                roomNameText.text = roomData.name;
            }

            DestroyAllChildren(recipesContent);
            foreach (Resource resource in resources)
            {
                if (resource.quantity >= 1)
                {
                    ResourceData recipeData = resource.resourceData;

                    GameObject RecipeFrame = Instantiate(recipeFramePrefab, recipesContent);

                    Image resourceImage = RecipeFrame.transform.Find("ResourceImage").GetComponent<Image>();
                    TextMeshProUGUI nameText = RecipeFrame.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI lockText = RecipeFrame.transform.Find("LockText").GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI requirementLevelText = RecipeFrame.transform.Find("RequirementLevelText").GetComponent<TextMeshProUGUI>();
                    Image lockImage = RecipeFrame.transform.Find("LockImage").GetComponent<Image>();

                    resourceImage.sprite = sm.resourceImages[recipeData.image_name];
                    nameText.text = recipeData.name;
                    lockText.text = GlobalFunctions.NumberMakeUp(resource.quantity);
                    requirementLevelText.text = "$" + GlobalFunctions.NumberMakeUp(recipeData.sellPrice) + "/ea";

                    lockImage.gameObject.SetActive(false);
                    lockText.gameObject.SetActive(true);
                    requirementLevelText.color = moneyActiveColor;
                    resourceImage.color = activeColor;
                    nameText.color = activeColor;
                    lockText.color = activeColor;

                    RecipeFrame.GetComponent<Button>().onClick.AddListener(() => SetUpRecipe(recipeData, true));
                    RecipeFrame.GetComponent<Button>().onClick.AddListener(PlaySound.PlayButtonClickSound);
                }
            }

            if (room.rIndex == -1) // Recipe yoksa
            {
                ChangeFrames(selectRecipeFrame);
            }
            else
            {
                SetUpRecipeResources(sm.resourceDataDatabase.resources[room.rIndex], true);
                ChangeFrames(roomMainPanel);
            }
        }
        else
        {
            RoomData roomData = sm.roomDataDatabase.rooms[room.index];
            if (roomData.energyValue < 0)
            {
                rlNameText.text = roomData.name + "<color=#FF9100> [Consumes " + roomData.energyValue * -1 + " Energy]";
            }
            else if (roomData.energyValue > 0)
            {
                rlNameText.text = roomData.name + "<color=#FF9100> [Produces " + GlobalFunctions.NumberMakeUp(roomData.energyValue) + " Energy]";
            }
            else
            {
                rlNameText.text = roomData.name;
            }
            ChangeFrames(roomLittleInfoPanel);
        }
    }
    private void SetUpRecipe(ResourceData recipeData, bool isDropper)
    {
        if (choosenPixelData == null) return;

        if (playerData.level.skillTreeNodes[recipeData.research_node_index].hasNode || isDropper) // Can do
        {
            choosenPixelData.rIndex = recipeData.index;

            SetUpRecipeResources(recipeData, isDropper);
            ChangeFrames(roomMainPanel);
            tmm.PlacePixel(choosenPixelData, true);
            tmm.StartResourceSpawnCoroutine(choosenPixelData);
        }
    }
    public void SetUpRecipeResources(ResourceData recipeData, bool isDropper)
    {
        if (choosenPixelData == null && choosenPixelData.rIndex == -1) return;

        float timePerResource = recipeData.time;
        if (isDropper) timePerResource *= 2;
        float quantityPerMin = 60f/timePerResource;

        timeText.text = timePerResource.ToString("0.00") + " sec";

        DestroyAllChildren(inputContent);

        resourceHolderTexts = new List<TextMeshProUGUI>();
        resourceRequirements = new List<ResourceRequirement>();
        if (!isDropper)
        {
            foreach (ResourceRequirement resourceRequirement in recipeData.resource_requirements)
            {

                GameObject ResourceRequirementFrame = Instantiate(recipeResourceFramePrefab, inputContent);

                ResourceRequirementFrame.transform.Find("QuantityNameText").GetComponent<TextMeshProUGUI>().text = resourceRequirement.quantity + " " + sm.resourceDataDatabase.resources[resourceRequirement.index].name;
                ResourceRequirementFrame.transform.Find("ResourceImage").GetComponent<Image>().sprite = sm.resourceImages[sm.resourceDataDatabase.resources[resourceRequirement.index].image_name];
                ResourceRequirementFrame.transform.Find("MinuteText").GetComponent<TextMeshProUGUI>().text = (resourceRequirement.quantity * quantityPerMin).ToString("0") + "/min";
                resourceHolderTexts.Add(ResourceRequirementFrame.transform.Find("ResourceHolderText").GetComponent<TextMeshProUGUI>());
                resourceRequirements.Add(resourceRequirement);
            }
        }

        DestroyAllChildren(outputContent);
        GameObject OutputFrame = Instantiate(recipeResourceFramePrefab, outputContent);
        OutputFrame.transform.Find("QuantityNameText").GetComponent<TextMeshProUGUI>().text = "1 " + sm.resourceDataDatabase.resources[recipeData.index].name;
        OutputFrame.transform.Find("ResourceImage").GetComponent<Image>().sprite = sm.resourceImages[sm.resourceDataDatabase.resources[recipeData.index].image_name];
        OutputFrame.transform.Find("MinuteText").GetComponent<TextMeshProUGUI>().text = quantityPerMin.ToString("0") + "/min";
        OutputFrame.transform.Find("ResourceHolderText").GetComponent<TextMeshProUGUI>().text = "$" + GlobalFunctions.NumberMakeUp(recipeData.sellPrice) + "/ea";
        OutputFrame.transform.Find("ResourceHolderText").GetComponent<TextMeshProUGUI>().color = moneyActiveColor;
    }
    public void SetInputOutputFrame(PixelData choosenPD, bool openClose)
    {
        if (choosenPD == null || !(tmm.producerRoomIndexes.Contains(choosenPD.index) || choosenPD.index == tmm.dropperIndex))
        {
            inputOutputObj.SetActive(false);
            return;
        }

        inputOutputObj.SetActive(openClose);
        if (openClose) inputOutputObjAnimator.Play(inpOutObjAnimationName, -1, 0f);

        inputOutputObj.transform.position = tmm.getWorldPositionFromTile(choosenPD);
        inputOutputObj.transform.rotation = Quaternion.Euler(0, 0, choosenPD.rotation);

        if (choosenPD.index == 1 || choosenPD.index == 2 || choosenPD.index == tmm.dropperIndex)
        {
            topArrow.enabled = true;
            bottomArrow.enabled = false;
            rightArrow.enabled = false;
            leftArrow.enabled = false;
        }
        else
        {
            topArrow.enabled = true;
            bottomArrow.enabled = true;
            rightArrow.enabled = true;
            leftArrow.enabled = true;
        }
    }

    private void RotateRoom()
    {
        if (choosenPixelData == null) return;

        tmm.RotateRoom(choosenPixelData);
    }
    private void DestroyRoom()
    {
        if (choosenPixelData == null) return;

        resourceHolderTexts = new List<TextMeshProUGUI>();
        resourceRequirements = new List<ResourceRequirement>();
        tmm.DestroyRoom(choosenPixelData, false);
        choosenPixelData = null;
    }


    public void ChangeFrames(Transform targetFrame)
    {
        foreach (Transform child in roomInfoPanel)
        {
            if (child == targetFrame)
            {
                child.gameObject.SetActive(true);
            }
            else 
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void DestroyAllChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }
}
