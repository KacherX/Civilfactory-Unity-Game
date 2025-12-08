using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[Serializable]
public class PixelData // bu sadece ilk asama yani terrain + resources, buildingleri ayri savelemek gerekecek
{
    public int x;
    public int y;
    public int index = 0;

    public float rotation = 0f;

    public int rIndex = -1;
    public PixelData(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public List<ResourceRequirement> storedResources = new List<ResourceRequirement>();

    [NonSerialized] public int splitterCount = 1; // for splitter only
    [NonSerialized] public bool wasInactive = false; // for setting recipes
    [NonSerialized] public bool isMoving = false; // for moving tiles
    [NonSerialized] public Coroutine resourceSpawnCoroutine;
}
[Serializable]
public class LandData
{
    public int x;
    public int y;
    public LandData(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}
[Serializable]
public class RoomData
{
    public int index;
    public int showOrder = -1;
    public string name;
    public string tile_name;
    public string image_name;
    public string keyName;

    public double roomPrice;
    public double energyValue = 0;

    public KeyCode placeKeyCode;
    public bool shiftRequirement = false;

    [NonSerialized] public TextMeshProUGUI roomPriceText;
}
[Serializable]
public class RoomDataDatabase
{
    public List<RoomData> rooms = new List<RoomData>();
}
[Serializable]
public class PixelDatabase
{
    public List<PixelData> pixels = new List<PixelData>();
}
[Serializable]
public class LandDatabase
{
    public event Action OnLandAdded;
    public List<LandData> lands = new List<LandData>();
    public void AddLand(LandData land)
    {
        lands.Add(land);
        OnLandAdded?.Invoke();
    }
    public double calculateLandPrice()
    {
        int currentLands = lands.Count;
        double result = math.pow(2d, currentLands - 1);
        return result * 800d;
    }
}

public class TileMapManager : MonoBehaviour
{
    public Animator animator; // Assign this in the Inspector

    public CameraDrag cameraDrag;
    public RoomInfoManager roomInfoManager;
    public RoomLoops roomLoops;

    public Tilemap topAnimationTilemap;
    public Tilemap backgroundTilemap;
    public Tilemap roomTilemap;
    public Tilemap resourceTilemap;
    public Tilemap previewRoomTilemap;
    public Tilemap previewResourceTilemap;

    public Transform roomsContent;
    public GameObject RoomFramePrefab;

    public Dictionary<Vector2Int, PixelData> generatedPixels = new Dictionary<Vector2Int, PixelData>();

    private SaveLoadDataManager sm;
    private PlayerData playerData;

    private PixelData choosenPixelData;
    private PixelData lastCursorPixelData;

    private string openAnimationName = "RoomPanelOpen";
    private string closeAnimationName = "RoomPanelClose";

    private int emptyRoomIndex = 0;
    private int splitterRoomIndex = 9;
    private int tripleSplitterRoomIndex = 10;
    public readonly List<int> conveyorRoomIndexes = new List<int> { 6, 9, 10 };
    public readonly List<int> producerRoomIndexes  = new List<int> { 1, 2, 3, 4, 5, 11, 12 };
    public readonly List<int> resourceRequireRooms = new List<int> { 3, 4, 5, 11, 12 };
    private int receiverIndex = 7;
    public readonly int dropperIndex = 8;

    private int defaultAnimSpeed = 29;

    private Color canBuyColor = new Color32(60, 255, 0, 255);
    private Color cantBuyColor = new Color32(255, 0, 0, 255);

    IEnumerator Start()
    {
        sm = GameObject.FindGameObjectWithTag("SaveLoadDataManager").GetComponent<SaveLoadDataManager>();
        while (!sm.Initialized) // Wait until Player Data loaded.
        {
            yield return null;
        }

        playerData = sm.playerData;

        SetRoomSelectMenu();
        LoadAllPixels();
        CalculateInitialEnergy();

        playerData.currency.OnCurrencyChanged += OnCurrencyChanged;
        OnCurrencyChanged();
    }

    void Update()
    {
        if (sm.Initialized && playerData != null)
        {
            CheckTileClick();
            RotateDestroyTile();
            PlaceBulkRooms();
        }
    }
    public void OpenClosePanel(bool openClose)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(openAnimationName) && openClose == false)
        {
            animator.Play(closeAnimationName);
            clearTopAnimationTilemap();
        }
        else if (stateInfo.IsName(closeAnimationName) && openClose == true)
        {
            animator.Play(openAnimationName);
        }
    }
    void OnCurrencyChanged()
    {
        foreach (RoomData roomData in sm.roomDataDatabase.rooms)
        {
            if (roomData.roomPriceText != null) // initialize olduysa
            {
                if (playerData.currency.money >= roomData.roomPrice)
                {
                    roomData.roomPriceText.color = canBuyColor;
                }
                else
                {
                    roomData.roomPriceText.color = cantBuyColor;
                }
            }
        }
    }
    void CalculateInitialEnergy()
    {
        foreach (PixelData pixel in playerData.pixels.pixels)
        {
            double energyValue = sm.roomDataDatabase.rooms[pixel.index].energyValue;
            if (energyValue < 0)
            {
                playerData.energy.currentEnergy += -energyValue;
            }
            else
            {
                playerData.energy.maxEnergy += energyValue;
            }
        }
    }
    public void CalculateEnergyChange(PixelData pixel, int mul)
    {
        double energyValue = sm.roomDataDatabase.rooms[pixel.index].energyValue;
        if (energyValue < 0)
        {
            playerData.energy.currentEnergy += -energyValue*mul;
        }
        else
        {
            playerData.energy.maxEnergy += energyValue*mul;
        }
    }

    public bool CanPlace(List<PixelData> previewPixels, int diffX, int diffY, Vector3Int origin, int angle)
    {
        foreach (PixelData pixel in previewPixels)
        {
            int turnAmount = angle / 90;

            Vector3Int diffOrigin = new Vector3Int(origin.x + diffX, origin.y + diffY, 0);
            Vector3Int pixelPosV3 = new Vector3Int(pixel.x + diffX, pixel.y + diffY, 0);
            while (turnAmount > 0)
            {
                float newX = (pixelPosV3 - diffOrigin).y;
                float newY = -(pixelPosV3 - diffOrigin).x;
                pixelPosV3 = new Vector3Int(Mathf.FloorToInt(newX + diffOrigin.x), Mathf.FloorToInt(newY + diffOrigin.y), 0);
                turnAmount -= 1;
            }
            if (roomTilemap.GetTile(pixelPosV3) != null || backgroundTilemap.GetTile(pixelPosV3) == null)
                return false;
        }
        return true;
    }
    public void PlacePreview(PixelData pixel, int diffX, int diffY, Vector3Int origin, int angle)
    {
        int turnAmount = angle / 90;

        Vector3Int diffOrigin = new Vector3Int(origin.x + diffX, origin.y + diffY, 0);
        Vector3Int pixelPosV3 = new Vector3Int(pixel.x + diffX, pixel.y + diffY, 0);
        while (turnAmount > 0)
        {
            float newX = (pixelPosV3 - diffOrigin).y;
            float newY = -(pixelPosV3 - diffOrigin).x;
            pixelPosV3 = new Vector3Int(Mathf.FloorToInt(newX + diffOrigin.x), Mathf.FloorToInt(newY + diffOrigin.y), 0);
            turnAmount -= 1;
        }

        Vector2Int pixelPosV2 = new Vector2Int(pixelPosV3.x, pixelPosV3.y);

        float newRotation = (pixel.rotation - angle) % 360;
        if (newRotation < 0) newRotation += 360;
        Matrix4x4 newMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, newRotation));

        if (roomTilemap.GetTile(pixelPosV3) != null || backgroundTilemap.GetTile(pixelPosV3) == null)
            topAnimationTilemap.SetTile(pixelPosV3, sm.tiles["select_pixel_room_red"]);
        else
            topAnimationTilemap.SetTile(pixelPosV3, sm.tiles["select_pixel_room"]);

        if (conveyorRoomIndexes.Contains(pixel.index)) // Conveyorsa
        {
            previewRoomTilemap.SetTile(pixelPosV3, sm.tiles[sm.roomDataDatabase.rooms[pixel.index].tile_name]);
            previewRoomTilemap.SetTransformMatrix(pixelPosV3, newMatrix);
        }
        else if (pixel.index == emptyRoomIndex)
        {
            previewRoomTilemap.SetTile(pixelPosV3, null);
            previewResourceTilemap.SetTile(pixelPosV3, null);
        }
        else
        {
            previewRoomTilemap.SetTile(pixelPosV3, sm.tiles["empty_pixel_room_grey"]);
            if (pixel.rIndex != -1)
            {
                previewResourceTilemap.SetTile(pixelPosV3, sm.tiles[sm.resourceDataDatabase.resources[pixel.rIndex].image_name]);
            }
            else
            {
                if (pixel.index != emptyRoomIndex)
                {
                    previewResourceTilemap.SetTile(pixelPosV3, sm.tiles[sm.roomDataDatabase.rooms[pixel.index].image_name]);
                }
                else
                {
                    previewResourceTilemap.SetTile(pixelPosV3, null);
                }
            }

            previewResourceTilemap.SetTransformMatrix(pixelPosV3, newMatrix);
        }
    }
    public void PlacePixel(PixelData pixel, bool isUpdate)
    {
        Vector2Int pixelPosV2 = new Vector2Int(pixel.x, pixel.y);
        Vector3Int pixelPosV3 = new Vector3Int(pixel.x, pixel.y, 0);

        if (!isUpdate) generatedPixels[pixelPosV2] = pixel;

        Matrix4x4 newMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, pixel.rotation));

        if (conveyorRoomIndexes.Contains(pixel.index)) // Conveyorsa
        {
            backgroundTilemap.SetTile(pixelPosV3, null);
            roomTilemap.SetTile(pixelPosV3, sm.tiles[sm.roomDataDatabase.rooms[pixel.index].tile_name]);
            roomTilemap.SetTransformMatrix(pixelPosV3, newMatrix);
        }
        else if (pixel.index == emptyRoomIndex)
        {
            backgroundTilemap.SetTile(pixelPosV3, sm.tiles["empty_pixel_room"]);
            roomTilemap.SetTile(pixelPosV3, null);
            resourceTilemap.SetTile(pixelPosV3, null);
        } 
        else
        {
            if (pixel.rIndex != -1)
            {
                resourceTilemap.SetTile(pixelPosV3, sm.tiles[sm.resourceDataDatabase.resources[pixel.rIndex].image_name]);
            }
            else
            {
                backgroundTilemap.SetTile(pixelPosV3, null);
                roomTilemap.SetTile(pixelPosV3, sm.tiles["empty_pixel_room_grey"]);
                if (pixel.index != emptyRoomIndex)
                {
                    resourceTilemap.SetTile(pixelPosV3, sm.tiles[sm.roomDataDatabase.rooms[pixel.index].image_name]);
                }
                else
                {
                    resourceTilemap.SetTile(pixelPosV3, null);
                }
            }

            resourceTilemap.SetTransformMatrix(pixelPosV3, newMatrix);
        }
        if (!isUpdate) cameraDrag.SetMainCamera(playerData);
    }
    public void PlaceLand(int x, int y)
    {
        for (int i = x*20; i < x*20+20; i++)
        {
            for (int j = y*20; j < y*20+20; j++)
            {
                PixelData newPixel = new PixelData(i, j);
                playerData.pixels.pixels.Add(newPixel);
                PlacePixel(newPixel, false);
            }
        }
        cameraDrag.cam.orthographicSize = cameraDrag.maxZoom;
    }
    void LoadAllPixels()
    {
        foreach (PixelData pixel in playerData.pixels.pixels)
        {
            PlacePixel(pixel, false);
            if (pixel.rIndex != -1)
            {
                StartResourceSpawnCoroutine(pixel);
            }
        }
        cameraDrag.cam.orthographicSize = cameraDrag.maxZoom;
    }

    public void StartResourceSpawnCoroutine(PixelData pixel)
    {
        if (pixel.resourceSpawnCoroutine != null)
        {
            StopCoroutine(pixel.resourceSpawnCoroutine);
        }
        pixel.resourceSpawnCoroutine = StartCoroutine(roomLoops.ResourceSpawnCoroutine(pixel));

        // her coroutine basladiginda animated da baslasin.
        SetAnimatedTile(pixel);
    }
    public void SetAnimatedTile(PixelData pixel)
    {
        Vector3Int pixelPosV3 = new Vector3Int(pixel.x, pixel.y, 0);

        float dropTime = sm.resourceDataDatabase.resources[pixel.rIndex].time;
        if (pixel.index == dropperIndex) dropTime *= 2;
        float speed = defaultAnimSpeed / dropTime;

        AnimatedTile roomTile = Instantiate(sm.tiles["empty_pixel_room_animated"] as AnimatedTile);
        roomTile.m_MinSpeed = speed;
        roomTile.m_MaxSpeed = speed;

        int currentFrame = Mathf.FloorToInt(Time.time * speed) % defaultAnimSpeed;
        roomTile.m_AnimationStartFrame = defaultAnimSpeed - currentFrame;

        backgroundTilemap.SetTile(pixelPosV3, null);
        roomTilemap.SetTile(pixelPosV3, roomTile);
    }
    public void SetInactiveTile(PixelData pixel)
    {
        Vector3Int pixelPosV3 = new Vector3Int(pixel.x, pixel.y, 0);
        backgroundTilemap.SetTile(pixelPosV3, null);
        roomTilemap.SetTile(pixelPosV3, sm.tiles["empty_pixel_room_inactive"]);
    }
    void SetRoomSelectMenu()
    {
        foreach (RoomData roomData in sm.roomDataDatabase.rooms.OrderBy(r => r.showOrder).ToList())
        {
            if (roomData.showOrder != -1)
            {
                GameObject ResourceFrame = Instantiate(RoomFramePrefab, roomsContent);
                ResourceFrame.transform.Find("RoomName").GetComponent<TextMeshProUGUI>().text = roomData.name;
                ResourceFrame.transform.Find("KeyName").GetComponent<TextMeshProUGUI>().text = "[" + roomData.keyName + "]";
                ResourceFrame.transform.Find("RoomImage").GetComponent<Image>().sprite = sm.roomImages[roomData.image_name];
                ResourceFrame.transform.Find("RoomImage").GetComponent<Button>().onClick.AddListener(() => SelectAndPlaceRoom(roomData.index));

                roomData.roomPriceText = ResourceFrame.transform.Find("RoomPrice").GetComponent<TextMeshProUGUI>();
                roomData.roomPriceText.text = "$" + GlobalFunctions.NumberMakeUp(roomData.roomPrice);
            }
        }
    }
    void SelectAndPlaceRoom(int index)
    {
        PlaySound.PlayButtonClickSound();

        double roomPrice = sm.roomDataDatabase.rooms[index].roomPrice;
        if (choosenPixelData != null && playerData.currency.money >= roomPrice)
        {
            playerData.currency.money -= roomPrice;

            choosenPixelData.index = index;
            CalculateEnergyChange(choosenPixelData, 1);
            Vector2Int pixelPosV2 = new Vector2Int(choosenPixelData.x, choosenPixelData.y);
            Vector3Int pixelPosV3 = new Vector3Int(choosenPixelData.x, choosenPixelData.y, 0);

            choosenPixelData.rotation = 0;
            Matrix4x4 newMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 0));

            if (conveyorRoomIndexes.Contains(choosenPixelData.index)) // Conveyorsa
            {
                backgroundTilemap.SetTile(pixelPosV3, null);
                roomTilemap.SetTile(pixelPosV3, sm.tiles[sm.roomDataDatabase.rooms[choosenPixelData.index].tile_name]);
                roomTilemap.SetTransformMatrix(pixelPosV3, newMatrix);
            }
            else if (choosenPixelData.index == emptyRoomIndex)
            {
                backgroundTilemap.SetTile(pixelPosV3, sm.tiles["empty_pixel_room"]);
                roomTilemap.SetTile(pixelPosV3, null);
                resourceTilemap.SetTile(pixelPosV3, null);
            }
            else
            {
                if (choosenPixelData.rIndex != -1)
                {
                    resourceTilemap.SetTile(pixelPosV3, sm.tiles[sm.resourceDataDatabase.resources[choosenPixelData.rIndex].image_name]);
                }
                else
                {
                    backgroundTilemap.SetTile(pixelPosV3, null);
                    roomTilemap.SetTile(pixelPosV3, sm.tiles["empty_pixel_room_grey"]);
                    if (choosenPixelData.index != emptyRoomIndex)
                    {
                        resourceTilemap.SetTile(pixelPosV3, sm.tiles[sm.roomDataDatabase.rooms[choosenPixelData.index].image_name]);
                    }
                    else
                    {
                        resourceTilemap.SetTile(pixelPosV3, null);
                    }
                }

                resourceTilemap.SetTransformMatrix(pixelPosV3, newMatrix);
            }

            choosenPixelData = null;
            OpenClosePanel(false);
        }
    }
    void CheckTileClick()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && !playerData.session.anyModeIsActive())
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            Vector3Int tilePosition = roomTilemap.WorldToCell(mouseWorldPos);
            Vector2Int tilePositionV2 = new Vector2Int(tilePosition.x, tilePosition.y);
            if (roomTilemap.GetTile(tilePosition) == null && backgroundTilemap.GetTile(tilePosition) == null)
            {
                OpenClosePanel(false);
                roomInfoManager.OpenClosePanel(false);
            }
            else
            {
                PlaySound.PlayButtonClickSound();

                if (generatedPixels.ContainsKey(tilePositionV2))
                {
                    if (generatedPixels[tilePositionV2].index == 0) // Empty tile
                    {
                        choosenPixelData = generatedPixels[tilePositionV2];

                        OpenClosePanel(true);
                        roomInfoManager.OpenClosePanel(false);
                    }
                    else
                    {
                        roomInfoManager.SetUpPanel(generatedPixels[tilePositionV2]);

                        OpenClosePanel(false);
                        roomInfoManager.OpenClosePanel(true);
                    }
                }

                clearTopAnimationTilemap();
                topAnimationTilemap.SetTile(tilePosition, sm.tiles["select_pixel_room"]);
            }
        }
    }
    void RotateDestroyTile()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            Vector3Int tilePosition = roomTilemap.WorldToCell(mouseWorldPos);
            Vector2Int tilePositionV2 = new Vector2Int(tilePosition.x, tilePosition.y);
            if (roomTilemap.GetTile(tilePosition) != null && generatedPixels.ContainsKey(tilePositionV2))
            {
                PixelData pixel = generatedPixels[tilePositionV2];
                RotateRoom(pixel);
            }
        }
        if (Input.GetKey(KeyCode.D))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            Vector3Int tilePosition = roomTilemap.WorldToCell(mouseWorldPos);
            Vector2Int tilePositionV2 = new Vector2Int(tilePosition.x, tilePosition.y);
            if (roomTilemap.GetTile(tilePosition) != null && generatedPixels.ContainsKey(tilePositionV2))
            {
                PixelData pixel = generatedPixels[tilePositionV2];
                DestroyRoom(pixel, false);
            }
        }
    }
    void PlaceBulkRooms()
    {
        foreach (RoomData roomData in sm.roomDataDatabase.rooms)
        { 
            if (!playerData.session.anyModeIsActive() && roomData.showOrder != -1 && Input.GetKey(roomData.placeKeyCode) && ((roomData.shiftRequirement && Input.GetKey(KeyCode.LeftShift)) || (!roomData.shiftRequirement && !Input.GetKey(KeyCode.LeftShift))))
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0;
                Vector3Int tilePosition = roomTilemap.WorldToCell(mouseWorldPos);
                Vector2Int tilePositionV2 = new Vector2Int(tilePosition.x, tilePosition.y);
                if ((roomTilemap.GetTile(tilePosition) != null || backgroundTilemap.GetTile(tilePosition) != null) && generatedPixels.ContainsKey(tilePositionV2))
                {
                    PixelData pixel = generatedPixels[tilePositionV2];
                    if (pixel.index == 0 && playerData.currency.money >= roomData.roomPrice) // bossa
                    {
                        playerData.currency.money -= roomData.roomPrice;

                        backgroundTilemap.SetTile(tilePosition, null);
                        if (conveyorRoomIndexes.Contains(roomData.index)) // Conveyorlar ise
                        {
                            roomTilemap.SetTile(tilePosition, sm.tiles[sm.roomDataDatabase.rooms[roomData.index].tile_name]);
                        }
                        else // Others
                        {
                            roomTilemap.SetTile(tilePosition, sm.tiles["empty_pixel_room_grey"]);
                            resourceTilemap.SetTile(tilePosition, sm.tiles[sm.roomDataDatabase.rooms[roomData.index].image_name]);
                        }

                        pixel.index = roomData.index;
                        CalculateEnergyChange(pixel, 1);
                    }
                    if (lastCursorPixelData != null && lastCursorPixelData.index == pixel.index)
                    {
                        Vector2Int previousConveyorPos = new Vector2Int(lastCursorPixelData.x, lastCursorPixelData.y);

                        float angle = -1;
                        if (lastCursorPixelData.x == pixel.x - 1 && lastCursorPixelData.y == pixel.y)
                            angle = 0;
                        else if (lastCursorPixelData.x == pixel.x + 1 && lastCursorPixelData.y == pixel.y)
                            angle = 180;
                        else if (lastCursorPixelData.y == pixel.y + 1 && lastCursorPixelData.x == pixel.x)
                            angle = 270;
                        else if (lastCursorPixelData.y == pixel.y - 1 && lastCursorPixelData.x == pixel.x)
                            angle = 90;

                        if (angle != -1) // Ayni komsuluktaysa
                        {
                            if (!conveyorRoomIndexes.Contains(pixel.index)) // Productive Room
                            {
                                angle = lastCursorPixelData.rotation;

                                Matrix4x4 previousMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle));
                                resourceTilemap.SetTransformMatrix(new Vector3Int(previousConveyorPos.x, previousConveyorPos.y, 0), previousMatrix);

                                Matrix4x4 currentMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle));
                                resourceTilemap.SetTransformMatrix(tilePosition, currentMatrix);
                                pixel.rotation = angle;

                                if (lastCursorPixelData.rIndex != -1)
                                {
                                    pixel.rIndex = lastCursorPixelData.rIndex;
                                    resourceTilemap.SetTile(tilePosition, sm.tiles[sm.resourceDataDatabase.resources[pixel.rIndex].image_name]);
                                    StartResourceSpawnCoroutine(pixel);

                                    if (roomInfoManager.choosenPixelData == pixel) // Eger seciliyse gui degistirme
                                    {
                                        roomInfoManager.SetUpRecipeResources(sm.resourceDataDatabase.resources[pixel.rIndex], pixel.rIndex == dropperIndex);
                                        roomInfoManager.ChangeFrames(roomInfoManager.roomMainPanel);
                                        roomInfoManager.SetInputOutputFrame(pixel, true);
                                    }
                                }
                            }
                            else
                            {
                                Matrix4x4 previousMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle));
                                roomTilemap.SetTransformMatrix(new Vector3Int(previousConveyorPos.x, previousConveyorPos.y, 0), previousMatrix);
                                lastCursorPixelData.rotation = angle;

                                Matrix4x4 currentMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle));
                                roomTilemap.SetTransformMatrix(tilePosition, currentMatrix);
                                pixel.rotation = angle;
                            }
                        }
                    }
                    lastCursorPixelData = pixel;
                }
            }
        }
    }

    public void RotateRoom(PixelData pixel)
    {
        if (!playerData.session.anyModeIsActive())
        {
            Vector3Int tilePosition = new Vector3Int(pixel.x, pixel.y, 0);

            float currentAngle = pixel.rotation;
            if (currentAngle < 0) currentAngle += 360;

            float newAngle = (currentAngle - 90) % 360;
            if (newAngle < 0) newAngle += 360;

            pixel.rotation = newAngle;

            PlacePixel(pixel, true);

            roomInfoManager.SetInputOutputFrame(pixel, true);
        }
    }
    public void DestroyRoom(PixelData pixel, bool force)
    {
        if (pixel.index != 0 && (!playerData.session.anyModeIsActive() || force))
        {
            CalculateEnergyChange(pixel, -1);

            pixel.index = 0;
            Vector2Int pixelPosV2 = new Vector2Int(pixel.x, pixel.y);
            Vector3Int pixelPosV3 = new Vector3Int(pixel.x, pixel.y, 0);

            pixel.rotation = 0f;
            pixel.rIndex = -1;
            pixel.storedResources = new List<ResourceRequirement>();
            if (pixel.resourceSpawnCoroutine != null)
            {
                StopCoroutine(pixel.resourceSpawnCoroutine);
                pixel.resourceSpawnCoroutine = null;
            }
            pixel.splitterCount = 1;
            pixel.wasInactive = false;
            pixel.isMoving = false;

            Matrix4x4 newMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 0));

            PlacePixel(pixel, true);

            roomInfoManager.OpenClosePanel(false);
        }
    }
    public void storeResource(Vector3 position, int rIndex, Vector2 previousDirection)
    {
        Vector3Int tilePosition = roomTilemap.WorldToCell(position);
        Vector2Int tilePositionV2 = new Vector2Int(tilePosition.x, tilePosition.y);
        if (roomTilemap.GetTile(tilePosition) != null && generatedPixels.ContainsKey(tilePositionV2)) // ortada tile varsa
        {
            PixelData pixel = generatedPixels[tilePositionV2];
            if (resourceRequireRooms.Contains(pixel.index) && canTakeResource(pixel, previousDirection)) // Resource require Room
            {
                ResourceRequirement existingResource = pixel.storedResources.Find(r => r.index == rIndex);

                if (existingResource != null)
                {
                    existingResource.quantity += 1;
                }
                else
                {
                    pixel.storedResources.Add(new ResourceRequirement { index = rIndex, quantity = 1 });
                }
            }
            else if (pixel.index == receiverIndex)
            {
                playerData.resources.resources[rIndex].quantity += 1;
            }
        }
    }
    public void clearTopAnimationTilemap()
    {
        topAnimationTilemap.ClearAllTiles();
    }

    private bool canTakeResource(PixelData pixel, Vector2 previousDirection)
    {
        if ((pixel.rotation == 0f && previousDirection == Vector2.down) || (pixel.rotation == 180f && previousDirection == Vector2.up) || (pixel.rotation == 90f && previousDirection == Vector2.right) || (pixel.rotation == 270f && previousDirection == Vector2.left))
        {
            return false;
        }

        return true;
    }

    public Vector2 getDirection(Vector3 position)
    {
        Vector3Int tilePosition = roomTilemap.WorldToCell(position);
        Vector2Int tilePositionV2 = new Vector2Int(tilePosition.x, tilePosition.y);
        if (roomTilemap.GetTile(tilePosition) != null && generatedPixels.ContainsKey(tilePositionV2))
        {
            PixelData pixel = generatedPixels[tilePositionV2];
            
            if (conveyorRoomIndexes.Contains(pixel.index))
            {
                float rotation = pixel.rotation;
                if (pixel.index == splitterRoomIndex || pixel.index == tripleSplitterRoomIndex)
                {
                    pixel.splitterCount++;
                    if (pixel.splitterCount >= 7)
                        pixel.splitterCount = 1;

                    if (pixel.index == splitterRoomIndex)
                    {
                        if (pixel.splitterCount % 2 == 0)
                        {
                            rotation += 180f;
                        }
                    } 
                    else
                    {
                        if (pixel.splitterCount % 3 == 0)
                        {
                            rotation += 180f;
                        }
                        else if (pixel.splitterCount % 3 == 1)
                        {
                            rotation += 90f;
                        }
                    }
                }

                if (rotation >= 360f) rotation -= 360;

                switch (rotation) 
                {
                    case 0f:
                        return Vector2.right;
                    case 90f:
                        return Vector2.up;
                    case 180f:
                        return Vector2.left;
                    case 270f:
                        return Vector2.down;
                    default:
                        print("Rotation is not exist: " + rotation);
                        return Vector2.zero;
                }
            }
            else // Conveyorda degil
            {
                return Vector2.zero;
            }
        }
        else // tile yok
        {
            return Vector2.zero;
        }
    }
    public Vector3 getWorldPositionFromTile(PixelData pixel) // Center
    {
        Vector3Int pixelPosV3 = new Vector3Int(pixel.x, pixel.y, 0);
        Vector3 worldPos = roomTilemap.CellToWorld(pixelPosV3);
        worldPos = new Vector3(worldPos.x + 0.284375f, worldPos.y + 0.284375f, 0);
        return worldPos;
    }
}
