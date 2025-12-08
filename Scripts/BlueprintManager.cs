using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Blueprint
{
    public List<PixelData> pixels = new List<PixelData>();
    public int rotateAngle = 0;
}
[Serializable]
public class BlueprintData
{
    public string blueprintName;
    public int objectCount = 0;

    [NonSerialized] public Blueprint blueprint; // burasi sadece blueprint ilk eklendiginde aktif olacak.
    [NonSerialized] public GameObject UIFrame;
}
[Serializable]
public class BlueprintDatabase
{
    public event Action<BlueprintData> OnBlueprintAdded;
    public event Action<BlueprintData> OnBlueprintRemoved;
    public List<BlueprintData> blueprints = new List<BlueprintData>();
    public void AddBlueprint(BlueprintData blueprint) // Manuel olarak eklendiginde calisir.
    {
        blueprints.Add(blueprint);
        OnBlueprintAdded?.Invoke(blueprint);
    }
    public void RemoveBlueprint(BlueprintData blueprint) // Manuel olarak eklendiginde calisir.
    {
        blueprints.Remove(blueprint);
        OnBlueprintRemoved?.Invoke(blueprint);
    }
    public bool BlueprintNameIsExclusive(string blueprintName)
    {
        return !blueprints.Any(b => b.blueprintName == blueprintName);
    }
}

public class BlueprintManager : MonoBehaviour
{
    public Animator blueprintPanelAnimator; // Assign this in the Inspector

    public EditModeManager emm;

    public Button BlueprintsButton;
    public Button BlueprintsPanelBackButton;
    public TextMeshProUGUI BlueprintsPanelCountText;

    public Transform BlueprintsContent;
    public GameObject BlueprintFramePrefab;

    private SaveLoadDataManager sm;
    private PlayerData playerData;

    private string openAnimationNameBlueprintPanel = "BlueprintsPanelOpen";
    private string closeAnimationNameBlueprintPanel = "BlueprintsPanelClose";
    IEnumerator Start()
    {
        sm = GameObject.FindGameObjectWithTag("SaveLoadDataManager").GetComponent<SaveLoadDataManager>();
        while (!sm.Initialized) // Wait until Player Data loaded.
        {
            yield return null;
        }

        playerData = sm.playerData;

        playerData.blueprints.OnBlueprintAdded += onBlueprintAdded;
        playerData.blueprints.OnBlueprintRemoved += onBlueprintRemoved;

        BlueprintsButton.onClick.AddListener(() => {
            if (!playerData.session.anyModeIsActive())
            {
                AnimatorStateInfo stateInfo = blueprintPanelAnimator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName(closeAnimationNameBlueprintPanel))
                {
                    OpenCloseBlueprintsPanel(true);
                }
                else
                {
                    OpenCloseBlueprintsPanel(false);
                }
            }
        });
        BlueprintsButton.onClick.AddListener(PlaySound.PlayButtonClickSound);

        BlueprintsPanelBackButton.onClick.AddListener(() => OpenCloseBlueprintsPanel(false));
        BlueprintsPanelBackButton.onClick.AddListener(PlaySound.PlayButtonClickSound);

        SetInitialBlueprints();
    }
    public void OpenCloseBlueprintsPanel(bool openClose)
    {
        AnimatorStateInfo stateInfo = blueprintPanelAnimator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(openAnimationNameBlueprintPanel) && openClose == false)
        {
            blueprintPanelAnimator.Play(closeAnimationNameBlueprintPanel);
        }
        else if (stateInfo.IsName(closeAnimationNameBlueprintPanel) && openClose == true)
        {
            blueprintPanelAnimator.Play(openAnimationNameBlueprintPanel);
        }
    }
    void SetInitialBlueprints()
    {
        foreach (BlueprintData blueprintData in playerData.blueprints.blueprints)
        {
            CreateBlueprintFrame(blueprintData);
        }
    }
    void CreateBlueprintFrame(BlueprintData blueprintData)
    {
        BlueprintsPanelCountText.text = playerData.blueprints.blueprints.Count + "/20";

        GameObject BlueprintFrame = Instantiate(BlueprintFramePrefab, BlueprintsContent);
        BlueprintFrame.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = blueprintData.blueprintName + " - <color=#5EB9FF>" + blueprintData.objectCount + "</color> Objects";

        BlueprintFrame.transform.Find("PlaceButton").GetComponent<Button>().onClick.AddListener(() => BlueprintPlaceButtonPressed(blueprintData));
        BlueprintFrame.transform.Find("PlaceButton").GetComponent<Button>().onClick.AddListener(PlaySound.PlayButtonClickSound);
        BlueprintFrame.transform.Find("RenameButton").GetComponent<Button>().onClick.AddListener(() => BlueprintRenameButtonPressed(blueprintData));
        BlueprintFrame.transform.Find("RenameButton").GetComponent<Button>().onClick.AddListener(PlaySound.PlayButtonClickSound);
        BlueprintFrame.transform.Find("DeleteButton").GetComponent<Button>().onClick.AddListener(() => BlueprintDeleteButtonPressed(blueprintData));
        BlueprintFrame.transform.Find("DeleteButton").GetComponent<Button>().onClick.AddListener(PlaySound.PlayButtonClickSound);

        blueprintData.UIFrame = BlueprintFrame;
    }
    void onBlueprintAdded(BlueprintData blueprint)
    {
        CreateBlueprintFrame(blueprint);
        sm.SaveBlueprint(blueprint);
    }
    void onBlueprintRemoved(BlueprintData blueprint)
    {
        BlueprintsPanelCountText.text = playerData.blueprints.blueprints.Count + "/20";
        Destroy(blueprint.UIFrame);

        sm.DeleteBlueprint(blueprint);
    }
    void BlueprintPlaceButtonPressed(BlueprintData blueprint)
    {
        Blueprint loadedBlueprint = sm.LoadBlueprint(blueprint);
        if (loadedBlueprint != null) 
        {
            emm.PlaceBlueprintMode(loadedBlueprint);
        }
        else
        {
            BlueprintDeleteButtonPressed(blueprint);
        }
    }
    void BlueprintRenameButtonPressed(BlueprintData blueprint)
    {
        emm.RenameBlueprintMode(blueprint);
    }
    void BlueprintDeleteButtonPressed(BlueprintData blueprint)
    {
        playerData.blueprints.RemoveBlueprint(blueprint);
    }
}
