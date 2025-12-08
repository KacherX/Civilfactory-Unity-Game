using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class ButtonsManager : MonoBehaviour // Also Buy Land Manager
{
    public Animator animator; // Assign this in the Inspector
    public Animator buyLandAnimator; // Assign this in the Inspector
    public Button ButtonsPanelButton;

    public TileMapManager tmm;
    public RoomInfoManager rim;
    public BlueprintManager bm;

    public Tilemap landTileMap;

    public Button BuyLandButton;
    public TextMeshProUGUI LandPriceText;
    public TextMeshProUGUI BuyLandPriceText;
    public Button BuyLandCancelButton;
    public Button BuyLandExpandButton;

    public Sprite openButtonSprite;
    public Sprite closeButtonSprite;

    private SaveLoadDataManager sm;
    private PlayerData playerData;

    private List<Vector2Int> generatedNeighborLands = new List<Vector2Int>();
    private Vector2Int selectedNeighborLand;

    private string openAnimationName = "ButtonsPanelOpen";
    private string closeAnimationName = "ButtonsPanelClose";
    private string openAnimationNameBuyLand = "BuyLandPanelOpen";
    private string closeAnimationNameBuyLand = "BuyLandPanelClose";
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

        ButtonsPanelButton.onClick.AddListener(OpenClosePanel);
        ButtonsPanelButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        BuyLandButton.onClick.AddListener(() => {
            if (playerData.session.buyLandMode)
                ExpandCancelButtonClicked(false);
            else
                BuyLandSetUp();
        });
        BuyLandButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        BuyLandExpandButton.onClick.AddListener(() => ExpandCancelButtonClicked(true));
        BuyLandExpandButton.onClick.AddListener(PlaySound.PlayButtonClickSound);
        BuyLandCancelButton.onClick.AddListener(() => ExpandCancelButtonClicked(false));
        BuyLandCancelButton.onClick.AddListener(PlaySound.PlayButtonClickSound);

        playerData.currency.OnCurrencyChanged += UpdateLandPrice;
        playerData.lands.OnLandAdded += UpdateLandPrice;
        UpdateLandPrice();
    }
    void Update()
    {
        if (sm.Initialized)
        {
            CheckTileClick();
        }
    }
    public void UpdateLandPrice()
    {
        double LandPrice = playerData.lands.calculateLandPrice();
        BuyLandPriceText.text = "$" + GlobalFunctions.NumberMakeUp(LandPrice);
        LandPriceText.text = "$" + GlobalFunctions.NumberMakeUp(LandPrice);
        if (playerData.currency.money >= LandPrice)
        {
            BuyLandPriceText.color = canBuyColor;
            LandPriceText.color = canBuyColor;
        }
        else
        {
            BuyLandPriceText.color = cantBuyColor;
            LandPriceText.color = cantBuyColor;
        }
    }
    void OpenClosePanel()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(openAnimationName))
        {
            animator.Play(closeAnimationName);
            ButtonsPanelButton.GetComponent<Image>().sprite = openButtonSprite;
        }
        else
        {
            animator.Play(openAnimationName);
            ButtonsPanelButton.GetComponent<Image>().sprite = closeButtonSprite;
        }
    }
    public void OpenClosePanelBuyLandMode(bool openClose)
    {
        AnimatorStateInfo stateInfo = buyLandAnimator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(openAnimationNameBuyLand) && openClose == false)
        {
            buyLandAnimator.Play(closeAnimationNameBuyLand);
        }
        else if (stateInfo.IsName(closeAnimationNameBuyLand) && openClose == true)
        {
            buyLandAnimator.Play(openAnimationNameBuyLand);
        }
    }
    void CheckTileClick()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && playerData.session.buyLandMode)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            Vector3Int tilePosition = landTileMap.WorldToCell(mouseWorldPos);
            Vector2Int tilePositionV2 = new Vector2Int(tilePosition.x, tilePosition.y);
            if (landTileMap.GetTile(tilePosition) != null)
            {
                PlaySound.PlayButtonClickSound();

                foreach (Vector2Int landVector in generatedNeighborLands)
                {
                    Vector3Int landVectorV3 = new Vector3Int(landVector.x, landVector.y, 0);
                    if (landTileMap.GetTile(landVectorV3) != null)
                    {
                        landTileMap.SetTile(landVectorV3, sm.tiles["new_pixel_room"]);
                    }
                }

                selectedNeighborLand = tilePositionV2;
                landTileMap.SetTile(tilePosition, sm.tiles["new_pixel_room_select"]);

                OpenClosePanelBuyLandMode(true);
            }
        }
    }
    private void ExpandCancelButtonClicked(bool isExpand)
    {
        if (playerData.session.buyLandMode)
        {
            double LandPrice = playerData.lands.calculateLandPrice();
            if (isExpand && playerData.currency.money >= LandPrice && selectedNeighborLand != null)
            {
                playerData.currency.money -= LandPrice;
                playerData.lands.AddLand(new LandData(selectedNeighborLand.x, selectedNeighborLand.y));
                tmm.PlaceLand(selectedNeighborLand.x, selectedNeighborLand.y);
            }

            OpenClosePanelBuyLandMode(false);
            ClearLands();
            playerData.session.buyLandMode = false;
        }
    }
    private void BuyLandSetUp()
    {
        if (!playerData.session.anyModeIsActive())
        {
            ClearLands();
            foreach (LandData land in playerData.lands.lands)
            {
                GenerateLand(land.x + 1, land.y);
                GenerateLand(land.x - 1, land.y);
                GenerateLand(land.x, land.y + 1);
                GenerateLand(land.x, land.y - 1);
            }

            playerData.session.buyLandMode = true;
            tmm.OpenClosePanel(false);
            rim.OpenClosePanel(false);
            bm.OpenCloseBlueprintsPanel(false);
        }
    }
    private void GenerateLand(int x, int y)
    {
        Vector2Int landVector = new Vector2Int(x, y);
        if (!HasLandAt(x, y) && !generatedNeighborLands.Contains(landVector)) // komsu yoksa
        {
            Vector3Int landVectorV3 = new Vector3Int(landVector.x, landVector.y, 0);

            generatedNeighborLands.Add(landVector);
            landTileMap.SetTile(landVectorV3, sm.tiles["new_pixel_room"]);
        }
    }

    private void ClearLands()
    {
        foreach (Vector2Int landVector in generatedNeighborLands)
        {
            Vector3Int landVectorV3 = new Vector3Int(landVector.x, landVector.y, 0);
            if (landTileMap.GetTile(landVectorV3) != null)
            {
                landTileMap.SetTile(landVectorV3, null);
            }
        }
        generatedNeighborLands.Clear();
    }

    public bool HasLandAt(int x, int y)
    {
        if (x > 2 || x < -2 || y > 2 || y < -2) return true; // bounds for 5x5 matrix
        foreach (LandData land in playerData.lands.lands)
        {
            if (land.x == x && land.y == y)
            {
                return true;
            }
        }
        return false;
    }
}
