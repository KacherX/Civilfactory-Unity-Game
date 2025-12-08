using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static UnityEngine.UI.Image;

public class EditModeManager : MonoBehaviour
{
    public Animator editModeAnimator; // Assign this in the Inspector
    public Animator saveBlueprintPanelAnimator; // Assign this in the Inspector

    public TileMapManager tmm;
    public RoomInfoManager rim;
    public BlueprintManager bm;

    public Button EditModeButton;
    public Button CancelButton;
    public Button CopyButton;
    public Button SaveButton;
    public Button MoveButton;
    public Button DeleteButton;
    public TextMeshProUGUI EditModeInfoText;
    public Button SaveCancelButton;
    public Button SaveBlueprintButton;
    public TMP_InputField BlueprintNameInputField;

    public GameObject selectViewObject;
    private RectTransform selectViewCanvas;

    private TextMeshProUGUI SaveBlueprintInfoText; 

    private Vector3 startPos;
    private Vector3Int startCellPos;
    private Vector3Int endCellPos;
    private Vector3Int origin;
    private Vector3Int mouseTilePosition;
    private int rotateAngle = 0;
    private int previousRotateAngle = 0;
    private double totalPrice = 0;
    private bool isSelecting = false;
    private bool modeIsCopy = false;
    private bool modeIsBlueprintPlace = false;
    private BlueprintData renameBlueprint;

    public List<PixelData> selectedTiles = new List<PixelData>();
    public List<PixelData> moveTiles = new List<PixelData>();
    public List<PixelData> saveTiles = new List<PixelData>();

    private SaveLoadDataManager sm;
    private PlayerData playerData;

    private string openAnimationNameEditMode = "EditModePanelOpen";
    private string closeAnimationNameEditMode = "EditModePanelClose";
    private string openAnimationNameSavePanel = "SaveBlueprintPanelOpen";
    private string closeAnimationNameSavePanel = "SaveBlueprintPanelClose";
    IEnumerator Start()
    {
        SaveBlueprintInfoText = SaveBlueprintButton.transform.Find("InfoText").GetComponent<TextMeshProUGUI>();

        selectViewCanvas = selectViewObject.transform.Find("SelectViewCanvas").GetComponent<RectTransform>();
        selectViewObject.SetActive(false);

        sm = GameObject.FindGameObjectWithTag("SaveLoadDataManager").GetComponent<SaveLoadDataManager>();
        while (!sm.Initialized) // Wait until Player Data loaded.
        {
            yield return null;
        }

        playerData = sm.playerData;

        EditModeButton.onClick.AddListener(() => {
            if (playerData.session.editMode)
                CancelButtonPressed();
            else
                ActivateEditMode();
        });
        EditModeButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        CancelButton.onClick.AddListener(CancelButtonPressed);
        CancelButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        CopyButton.onClick.AddListener(CopyButtonPressed);
        CopyButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        MoveButton.onClick.AddListener(MoveButtonPressed);
        MoveButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        DeleteButton.onClick.AddListener(DeleteButtonPressed);
        DeleteButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        SaveButton.onClick.AddListener(SaveButtonPressed);
        SaveButton.onClick.AddListener(PlaySound.PlayButtonClickSound);

        SaveCancelButton.onClick.AddListener(SaveCancelButtonPressed);
        SaveCancelButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        SaveBlueprintButton.onClick.AddListener(SaveBlueprintButtonPressed);
        SaveBlueprintButton.onClick.AddListener(PlaySound.PlayButtonClickSound);

        BlueprintNameInputField.onValidateInput += ValidateInput;

        InvokeRepeating("Update10", 0.1f, 0.1f);
    }
    void Update()
    {
        if (sm.Initialized && playerData != null)
        {
            CheckKeyPresses();
            SelectTiles();
            MoveSelectedTiles();
            RotateTilemapAroundOrigin();
        }
    }
    private void Update10()
    {
        if (modeIsCopy)
        {
            if (playerData.currency.money >= totalPrice)
                EditModeInfoText.text = "<color=#3cff00>$" + GlobalFunctions.NumberMakeUp(totalPrice);
            else
                EditModeInfoText.text = "<color=#ff0000>$" + GlobalFunctions.NumberMakeUp(totalPrice);
        }
    }
    public void OpenClosePanelEditMode(bool openClose)
    {
        AnimatorStateInfo stateInfo = editModeAnimator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(openAnimationNameEditMode) && openClose == false)
        {
            editModeAnimator.Play(closeAnimationNameEditMode);
        }
        else if (stateInfo.IsName(closeAnimationNameEditMode) && openClose == true)
        {
            editModeAnimator.Play(openAnimationNameEditMode);
        }
    }
    public void OpenCloseSaveBlueprintPanel(bool openClose)
    {
        AnimatorStateInfo stateInfo = saveBlueprintPanelAnimator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(openAnimationNameSavePanel) && openClose == false)
        {
            saveBlueprintPanelAnimator.Play(closeAnimationNameSavePanel);
        }
        else if (stateInfo.IsName(closeAnimationNameSavePanel) && openClose == true)
        {
            saveBlueprintPanelAnimator.Play(openAnimationNameSavePanel);
        }
    }
    private void CheckKeyPresses()
    {
        if (playerData.session.editMode)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelButtonPressed();
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                DeleteButtonPressed();
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                MoveButtonPressed();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                CopyButtonPressed();
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                SaveButtonPressed();
            }
        }
    }
    private void ActivateEditMode()
    {
        if (!playerData.session.anyModeIsActive())
        {
            playerData.session.editMode = true;
            isSelecting = false;
            bm.OpenCloseBlueprintsPanel(false);
            tmm.OpenClosePanel(false);
            rim.OpenClosePanel(false);

            selectedTiles.Clear();
            tmm.clearTopAnimationTilemap();
            EditModeInfoText.text = "<color=#5EB9FF>" + selectedTiles.Count + "</color> Objects Selected";
            OpenClosePanelEditMode(true);
        }
    }
    private void CancelButtonPressed()
    {
        if (playerData.session.editMode)
        {
            playerData.session.editMode = false;
            isSelecting = false;
            modeIsBlueprintPlace = false;

            selectedTiles.Clear();
            tmm.clearTopAnimationTilemap();
            OpenClosePanelEditMode(false);

            if (playerData.session.moveMode)
            {
                playerData.session.moveMode = false;

                if (!modeIsCopy)
                {
                    foreach (PixelData pixel in moveTiles)
                    {
                        pixel.isMoving = false;
                        tmm.PlacePixel(pixel, true);
                        if (pixel.rIndex != -1)
                        {
                            tmm.StartResourceSpawnCoroutine(pixel);
                        }
                    }
                }

                modeIsCopy = false;
                rotateAngle = 0;
                moveTiles.Clear();
                tmm.previewResourceTilemap.ClearAllTiles();
                tmm.previewRoomTilemap.ClearAllTiles();
            }
            if (playerData.session.saveMode)
            {
                playerData.session.saveMode = false;

                BlueprintNameInputField.text = "";
                saveTiles.Clear();
                OpenCloseSaveBlueprintPanel(false);
            }
        }
    }
    private void SaveCancelButtonPressed()
    {
        if (playerData.session.renameBlueprintMode)
        {
            renameBlueprint = null;
            playerData.session.renameBlueprintMode = false;
            BlueprintNameInputField.text = "";
            OpenCloseSaveBlueprintPanel(false);
        }
        if (playerData.session.editMode && playerData.session.saveMode)
        {
            playerData.session.saveMode = false;

            BlueprintNameInputField.text = "";
            selectedTiles.Clear();
            saveTiles.Clear();
            tmm.clearTopAnimationTilemap();
            EditModeInfoText.text = "<color=#5EB9FF>" + selectedTiles.Count + "</color> Objects Selected";

            OpenCloseSaveBlueprintPanel(false);
        }
    }
    private void SaveBlueprintButtonPressed()
    {
        string blueprintName = BlueprintNameInputField.text;
        if (playerData.session.renameBlueprintMode && renameBlueprint != null && blueprintName.Length > 0 && playerData.blueprints.BlueprintNameIsExclusive(blueprintName))
        {
            sm.RenameBlueprint(renameBlueprint.blueprintName, blueprintName);
            renameBlueprint.blueprintName = blueprintName;
            renameBlueprint.UIFrame.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = renameBlueprint.blueprintName + " - <color=#5EB9FF>" + renameBlueprint.objectCount + "</color> Objects";
            renameBlueprint = null;
            playerData.session.renameBlueprintMode = false;
            BlueprintNameInputField.text = "";
            OpenCloseSaveBlueprintPanel(false);
        }
        if (playerData.session.editMode && playerData.session.saveMode && blueprintName.Length > 0 && playerData.blueprints.BlueprintNameIsExclusive(blueprintName))
        {
            playerData.session.saveMode = false;

            Blueprint blueprint = new Blueprint();
            blueprint.pixels = saveTiles.ToList();
            blueprint.rotateAngle = rotateAngle;

            BlueprintData blueprintData = new BlueprintData();
            blueprintData.blueprintName = blueprintName;
            blueprintData.objectCount = blueprint.pixels.Count;
            blueprintData.blueprint = blueprint;

            playerData.blueprints.AddBlueprint(blueprintData);

            BlueprintNameInputField.text = "";
            selectedTiles.Clear();
            saveTiles.Clear();
            tmm.clearTopAnimationTilemap();
            EditModeInfoText.text = "<color=#5EB9FF>" + selectedTiles.Count + "</color> Objects Selected";

            OpenCloseSaveBlueprintPanel(false);
        }
    }
    private void SaveButtonPressed() // Kopyala yapistir
    {
        if (playerData.session.editMode && !playerData.session.moveMode && !playerData.session.saveMode && selectedTiles.Count > 0 && playerData.blueprints.blueprints.Count < 20)
        {
            playerData.session.editMode = false;
            playerData.session.saveMode = true;
            isSelecting = false;

            saveTiles = selectedTiles.ToList();

            SaveBlueprintInfoText.text = "Save Blueprint";
            OpenCloseSaveBlueprintPanel(true);

            playerData.session.editMode = true;
        }
    }
    private void CopyButtonPressed() // Kopyala yapistir
    {
        if (playerData.session.editMode && !playerData.session.moveMode && !playerData.session.saveMode && selectedTiles.Count > 0)
        {
            playerData.session.editMode = false;
            playerData.session.moveMode = true;
            isSelecting = false;
            modeIsCopy = true;
            rotateAngle = 0;

            totalPrice = 0;
            foreach (PixelData pixel in selectedTiles)
            {
                totalPrice += sm.roomDataDatabase.rooms[pixel.index].roomPrice;
                moveTiles.Add(pixel);
            }

            ActivateEditMode();
        }
    }
    public void PlaceBlueprintMode(Blueprint blueprint)
    {
        playerData.session.moveMode = true;
        modeIsCopy = true;
        modeIsBlueprintPlace = true;

        moveTiles = blueprint.pixels;
        rotateAngle = blueprint.rotateAngle;
        previousRotateAngle = blueprint.rotateAngle;

        totalPrice = 0;
        int left = 1000000;
        int right = -1000000;
        int top = -1000000;
        int bottom = 1000000;
        foreach (PixelData pixel in moveTiles)
        {
            totalPrice += sm.roomDataDatabase.rooms[pixel.index].roomPrice;
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
        origin = new Vector3Int((left + right) / 2, (bottom + top) / 2, 0);

        ActivateEditMode();
    }
    public void RenameBlueprintMode(BlueprintData blueprint)
    {
        renameBlueprint = blueprint;
        playerData.session.renameBlueprintMode = true;
        SaveBlueprintInfoText.text = "Rename Blueprint";
        OpenCloseSaveBlueprintPanel(true);
    }
    private void MoveButtonPressed()
    {
        if (playerData.session.editMode && !playerData.session.moveMode && !playerData.session.saveMode && selectedTiles.Count > 0)
        {
            playerData.session.editMode = false;
            playerData.session.moveMode = true;
            isSelecting = false;
            modeIsCopy = false;
            rotateAngle = 0;

            foreach (PixelData pixel in selectedTiles)
            {
                moveTiles.Add(pixel);
                pixel.isMoving = true;

                Vector3Int pos = new Vector3Int(pixel.x, pixel.y, 0);
                tmm.roomTilemap.SetTile(pos, null);
                tmm.resourceTilemap.SetTile(pos, null);
                tmm.backgroundTilemap.SetTile(pos, sm.tiles["empty_pixel_room"]);
            }

            ActivateEditMode();
        }
    }
    private void DeleteButtonPressed()
    {
        if (playerData.session.editMode && !playerData.session.moveMode && !playerData.session.saveMode && selectedTiles.Count > 0)
        {
            playerData.session.editMode = false;
            isSelecting = false;

            foreach (PixelData pixel in selectedTiles) 
            {
                tmm.DestroyRoom(pixel, false);
            }

            ActivateEditMode();
        }
    }

    void MoveSelectedTiles()
    {
        if (playerData.session.editMode && playerData.session.moveMode)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            Vector3Int tilePosition = tmm.roomTilemap.WorldToCell(mouseWorldPos);
            Vector3Int diff = mouseTilePosition - origin;
            if (mouseTilePosition != tilePosition || rotateAngle != previousRotateAngle)
            {
                mouseTilePosition = tilePosition;
                previousRotateAngle = rotateAngle;
                diff = mouseTilePosition - origin;

                tmm.previewResourceTilemap.ClearAllTiles();
                tmm.previewRoomTilemap.ClearAllTiles();
                tmm.clearTopAnimationTilemap();

                foreach (PixelData pixel in moveTiles)
                {
                    tmm.PlacePreview(pixel, diff.x, diff.y, origin, rotateAngle);
                }
            }

            if (Input.GetMouseButtonDown(0) && tmm.CanPlace(moveTiles, diff.x, diff.y, origin, rotateAngle) && (!modeIsCopy || (modeIsCopy && playerData.currency.money >= totalPrice)))
            {
                if (modeIsCopy) playerData.currency.money -= totalPrice;
                playerData.session.moveMode = false;
                EditModeInfoText.text = "<color=#5EB9FF>0</color> Objects Selected";

                List<PixelData> newPixels = new List<PixelData>();
                foreach (PixelData pixel in moveTiles)
                {
                    int turnAmount = rotateAngle / 90;

                    Vector2Int diffOrigin = new Vector2Int(origin.x + diff.x, origin.y + diff.y);
                    Vector2Int targetPos = new Vector2Int(pixel.x + diff.x, pixel.y + diff.y);
                    while (turnAmount > 0)
                    {
                        float newX = (targetPos - diffOrigin).y;
                        float newY = -(targetPos - diffOrigin).x;
                        targetPos = new Vector2Int(Mathf.FloorToInt(newX + diffOrigin.x), Mathf.FloorToInt(newY + diffOrigin.y));
                        turnAmount -= 1;
                    }
                    
                    PixelData newPixel = new PixelData(targetPos.x, targetPos.y);
                    float newRotation = (pixel.rotation - rotateAngle) % 360;
                    if (newRotation < 0) newRotation += 360;

                    newPixel.index = pixel.index;
                    newPixel.rIndex = pixel.rIndex;
                    newPixel.rotation = newRotation;

                    newPixels.Add(newPixel);

                    if (!modeIsCopy)
                    {
                        tmm.DestroyRoom(pixel, true);
                    }
                }
                foreach (PixelData newPixel in newPixels)
                {
                    Vector2Int targetPos = new Vector2Int(newPixel.x, newPixel.y);
                    PixelData targetPixel = tmm.generatedPixels[targetPos];
                    targetPixel.index = newPixel.index;
                    targetPixel.rIndex = newPixel.rIndex;
                    targetPixel.rotation = newPixel.rotation;

                    tmm.PlacePixel(targetPixel, true);
                    tmm.CalculateEnergyChange(targetPixel, 1);
                    if (targetPixel.rIndex != -1)
                    {
                        tmm.StartResourceSpawnCoroutine(targetPixel);
                    }
                }

                modeIsCopy = false;
                rotateAngle = 0;
                moveTiles.Clear();
                tmm.previewResourceTilemap.ClearAllTiles();
                tmm.previewRoomTilemap.ClearAllTiles();
                tmm.clearTopAnimationTilemap();

                if (modeIsBlueprintPlace)
                {
                    modeIsBlueprintPlace = false;
                    CancelButtonPressed();
                }
            }
        }
    }

    void SelectTiles()
    {
        if (playerData.session.editMode && !playerData.session.moveMode && !playerData.session.saveMode)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                startPos = mouseWorldPos;

                selectViewObject.SetActive(true);
                selectViewObject.transform.position = new Vector3(startPos.x, startPos.y, 0);
                selectViewCanvas.sizeDelta = new Vector2(0, 0);

                startCellPos = tmm.roomTilemap.WorldToCell(startPos);
                isSelecting = true;
            }

            if (isSelecting)
            {
                Vector3 currentPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                float xDiff = currentPos.x - startPos.x;
                float yDiff = currentPos.y - startPos.y;

                if (xDiff > 0 && yDiff > 0)
                {
                    selectViewCanvas.sizeDelta = new Vector2(xDiff, yDiff);
                    selectViewCanvas.pivot = new Vector2(0, 0);
                }
                else if (xDiff > 0 && yDiff < 0)
                {
                    selectViewCanvas.sizeDelta = new Vector2(xDiff, -yDiff);
                    selectViewCanvas.pivot = new Vector2(0, 1);
                }
                else if (xDiff < 0 && yDiff > 0)
                {
                    selectViewCanvas.sizeDelta = new Vector2(-xDiff, yDiff);
                    selectViewCanvas.pivot = new Vector2(1, 0);
                }
                else if (xDiff < 0 && yDiff < 0)
                {
                    selectViewCanvas.sizeDelta = new Vector2(-xDiff, -yDiff);
                    selectViewCanvas.pivot = new Vector2(1, 1);
                }
            }

            if (Input.GetMouseButtonUp(0) && isSelecting)
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                endCellPos = tmm.roomTilemap.WorldToCell(mouseWorldPos);
                isSelecting = false;

                selectViewObject.SetActive(false);

                SelectTilesBetween(startCellPos, endCellPos);
            }
        }
        else if (selectViewObject.activeSelf) selectViewObject.SetActive(false);
    }

    void SelectTilesBetween(Vector3Int start, Vector3Int end)
    {
        selectedTiles.Clear();
        tmm.clearTopAnimationTilemap();

        int xMin = Mathf.Min(start.x, end.x);
        int xMax = Mathf.Max(start.x, end.x);
        int yMin = Mathf.Min(start.y, end.y);
        int yMax = Mathf.Max(start.y, end.y);

        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                Vector2Int posV2 = new Vector2Int(x, y);
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (tmm.roomTilemap.GetTile(pos) != null)
                {
                    selectedTiles.Add(tmm.generatedPixels[posV2]);
                    tmm.topAnimationTilemap.SetTile(pos, sm.tiles["select_pixel_room"]);
                }
            }
        }
        int left = 1000000;
        int right = -1000000;
        int top = -1000000;
        int bottom = 1000000;
        foreach (PixelData pixel in selectedTiles)
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

        origin = new Vector3Int((left + right) / 2, (bottom + top) / 2, 0);

        EditModeInfoText.text = "<color=#5EB9FF>" + selectedTiles.Count + "</color> Objects Selected";
    }

    void RotateTilemapAroundOrigin()
    {
        if (Input.GetKeyDown(KeyCode.R) && playerData.session.editMode && playerData.session.moveMode)
        {
            rotateAngle += 90;
            if (rotateAngle >= 360) rotateAngle -= 360;
        }
    }
    private char ValidateInput(string text, int charIndex, char addedChar)
    {
        // Allow letters and digits only
        if (char.IsLetterOrDigit(addedChar) || addedChar == '_')
            return addedChar;

        // Reject spaces, symbols, etc.
        return '\0';
    }
}
