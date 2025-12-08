using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class PlayerData // bu sadece ilk asama yani terrain + resources, buildingleri ayri savelemek gerekecek
{
    public LandDatabase lands = new LandDatabase();
    public PixelDatabase pixels = new PixelDatabase();
    public ResourceDatabase resources = new ResourceDatabase();
    public BlueprintDatabase blueprints = new BlueprintDatabase();
    public Level level = new Level();
    public Energy energy = new Energy();
    public Currency currency = new Currency();

    public UIPositions uiPositions = new UIPositions();

    [NonSerialized] public SessionData session = new SessionData();
}
public class SessionData
{
    public bool buyLandMode = false;
    public bool editMode = false;
    public bool moveMode = false;
    public bool saveMode = false;
    public bool renameBlueprintMode = false;

    public bool anyModeIsActive()
    {
        if (buyLandMode || editMode || renameBlueprintMode) return true;
        return false;
    }
}
public class SaveLoadDataManager : MonoBehaviour
{
    public PlayerData playerData;
    public bool Initialized = false;

    public RoomDataDatabase roomDataDatabase;
    public ResourceDataDatabase resourceDataDatabase;

    public Dictionary<string, Sprite> resourceImages = new Dictionary<string, Sprite>();
    public Dictionary<string, Sprite> roomImages = new Dictionary<string, Sprite>();
    public Dictionary<string, TileBase> tiles = new Dictionary<string, TileBase>();
    void Start()
    {
        if (SteamManager.Initialized)
        {
            SaveLoadData.SetUserEncryptionKey();

            LoadData();

            SetResources();
            SetResearchTree();
            SetRoomDatabase();

            loadResources();

            Initialized = true;

            InvokeRepeating("SaveData", 60, 60);
        }
        else
        {
            Application.Quit();
        }

        setScreenResolution();
    }
    void OnApplicationQuit()
    {
        SaveData();
    }

    public void LoadData()
    {
        if (File.Exists(SaveLoadData.savePath))
        {
            string encryptedJson = File.ReadAllText(SaveLoadData.savePath);
            string json = SaveLoadData.Decrypt(encryptedJson);
            playerData = JsonUtility.FromJson<PlayerData>(json);
        }
        else
        {
            playerData.lands.lands.Add(new LandData(0, 0));
            for (int i = 0; i < 20; i++) 
            {
                for (int j = 0; j < 20; j++)
                {
                    PixelData newPixel = new PixelData(i, j);
                    playerData.pixels.pixels.Add(newPixel);
                }
            }
            playerData.currency.money = 150;
        }
    }
    public void SaveData()
    {
        string json = JsonUtility.ToJson(playerData);
        string encryptedJson = SaveLoadData.Encrypt(json);
        File.WriteAllText(SaveLoadData.savePath, encryptedJson);
    }
    public Blueprint LoadBlueprint(BlueprintData blueprintData)
    {
        string path = SaveLoadData.blueprintSavePath + blueprintData.blueprintName + ".dat";
        if (File.Exists(path))
        {
            string encryptedJson = File.ReadAllText(path);
            string json = SaveLoadData.Decrypt(encryptedJson);
            return JsonUtility.FromJson<Blueprint>(json);
        }

        return null;
    }

    public void SaveBlueprint(BlueprintData blueprintData)
    {
        string path = SaveLoadData.blueprintSavePath + blueprintData.blueprintName + ".dat";
        string json = JsonUtility.ToJson(blueprintData.blueprint);
        string encryptedJson = SaveLoadData.Encrypt(json);
        File.WriteAllText(path, encryptedJson);
    }
    public void DeleteBlueprint(BlueprintData blueprintData)
    {
        string path = SaveLoadData.blueprintSavePath + blueprintData.blueprintName + ".dat";
        if (File.Exists(path)) 
        {
            File.Delete(path);
        }
    }
    public void RenameBlueprint(string oldName, string newName)
    {
        string path = SaveLoadData.blueprintSavePath + oldName + ".dat";
        string newPath = SaveLoadData.blueprintSavePath + newName + ".dat";
        if (File.Exists(path))
        {
            File.Move(path, newPath);
        }
    }

    private void loadResources()
    {
        Sprite[] ResourceImagesResources = Resources.LoadAll<Sprite>("ResourceImages");
        foreach (Sprite image in ResourceImagesResources)
        {
            resourceImages[image.name] = image;
        }
        Sprite[] RoomImagesResources = Resources.LoadAll<Sprite>("RoomImages");
        foreach (Sprite image in RoomImagesResources)
        {
            roomImages[image.name] = image;
        }
        TileBase[] RTiles = Resources.LoadAll<TileBase>("Tiles");
        foreach (TileBase tile in RTiles)
        {
            tiles[tile.name] = tile;
        }
    }

    public void SetResources()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("JsonData/Resources"); // No ".json" extension
        if (jsonFile != null)
        {
            resourceDataDatabase =  JsonUtility.FromJson<ResourceDataDatabase>(jsonFile.text);
        }
        else
        {
            Debug.LogError("Could not load the JSON file!");
            resourceDataDatabase =  new ResourceDataDatabase();
        }

        ResourceDatabase newResources = new ResourceDatabase();
        for (int i = 0; i < resourceDataDatabase.resources.Count; i++) 
        {
            ResourceData resourceData = resourceDataDatabase.resources[i];

            // Kullanici resourcelerini ayarlama
            Resource resource = new Resource();
            resource.resourceData = resourceData;
            if (i < playerData.resources.resources.Count)
            {
                resource.quantity = playerData.resources.resources[i].quantity;
            }

            newResources.resources.Add(resource);

            // Resourceleri gruplandirma
            if (resourceDataDatabase.recipes[resourceData.room_index] == null)
                resourceDataDatabase.recipes[resourceData.room_index] = new List<ResourceData>();
            resourceDataDatabase.recipes[resourceData.room_index].Add(resourceData);
        }

        foreach (List<ResourceData> recipes in resourceDataDatabase.recipes)
        {
            if (recipes != null)
            {
                recipes.Sort((a, b) => a.sellPrice.CompareTo(b.sellPrice));
            }
        }

        playerData.resources = newResources;
    }

    public void SetResearchTree()
    {
        SkillTreeNodeDatabase researchTree;

        TextAsset jsonFile = Resources.Load<TextAsset>("JsonData/ResearchTreeNodes"); // No ".json" extension
        if (jsonFile != null)
        {
            researchTree = JsonUtility.FromJson<SkillTreeNodeDatabase>(jsonFile.text);
        }
        else
        {
            Debug.LogError("Could not load the JSON file!");
            researchTree = new SkillTreeNodeDatabase();
        }

        List<SkillTreeNodeData> skillTreeNodes = new List<SkillTreeNodeData>();

        for (int i = 0; i < researchTree.skillTreeNodes.Count; i++)
        {
            SkillTreeNodeData node = new SkillTreeNodeData();
            node.node = researchTree.skillTreeNodes[i];

            if (i < playerData.level.skillTreeNodes.Count)
            {
                node.hasNode = playerData.level.skillTreeNodes[i].hasNode;
            }

            skillTreeNodes.Add(node);
        }

        playerData.level.skillTreeNodes = skillTreeNodes;
    }
    public void SetRoomDatabase()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("JsonData/Rooms"); // No ".json" extension
        if (jsonFile != null)
        {
            roomDataDatabase = JsonUtility.FromJson<RoomDataDatabase>(jsonFile.text);
        }
        else
        {
            Debug.LogError("Could not load the JSON file!");
            roomDataDatabase = new RoomDataDatabase();
        }
    }



    // SIMDILIK BURAYA KODUM //
    private float aspectRatio = 1920f / 1080f;
    private void setScreenResolution()
    {
        int resolutionWidth = Display.main.systemWidth;
        int resolutionHeight = Display.main.systemHeight;
        float resolution = resolutionWidth / resolutionHeight;
        if (resolution < aspectRatio)
        {
            int targetHeight = Mathf.RoundToInt(resolutionWidth / aspectRatio);
            Screen.SetResolution(resolutionWidth, targetHeight, true);
        }
        else
        {
            int targetWidth = Mathf.RoundToInt(resolutionHeight * aspectRatio);
            Screen.SetResolution(targetWidth, resolutionHeight, true);
        }
    }
}
