using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SkillTreeNodeData // Burasi databaseye save edilen kisim.
{
    public event Action<UIClassMapNode, bool> OnNodeChanged;

    [NonSerialized] public SkillTreeNode node;
    [NonSerialized] public UIClassMapNode UInode;
    public bool _hasNode; // Kullanici sadece burayla etkilesime giriyor diger taraflar databasede sakli.

    public bool hasNode
    {
        get => _hasNode;
        set
        {
            _hasNode = value;
            if (UInode != null)
            {
                OnNodeChanged?.Invoke(UInode, true);
            }
        }
    }
}

[Serializable]
public class SkillTreeNode // Burasi .JSON'dan okunan kisim
{
    public List<int> connectedNodeIndexes = new List<int>();
    public string nodeType = "Small";
    public float creationAngle = -1f;
    public float distance = -1f;
    public bool connectedToStarter = false;

    public int index;
    public int resourceIndex = -1;
}
public class UIClassMapNode // Essential for all skill trees
{
    public List<UIClassMapNode> connectedNodes = new List<UIClassMapNode>();
    public List<UIClassMapNode> edgeNodes = new List<UIClassMapNode>();
    public List<GameObject> edgeUIs = new List<GameObject>();
    public UIClassMapNode creatorNode = null;
    public GameObject UINode;
    public float creationAngle = 0f;

    public object node;
}

public class SkillTreeManager : MonoBehaviour
{
    private SaveLoadDataManager sm;
    private PlayerData playerData;

    public GameObject InfoPanel;
    public GameObject ClassMapContent;
    public GameObject ClassPanelNode;
    public Image SkillPanelNodeImage;
    public GameObject NodeFramePrefab;

    public GameObject EdgesContent;
    public GameObject NodeEdgePrefab;

    public Button researchTreeButton;
    public Button backButton;
    public GameObject SkillTreePanel;
    public TextMeshProUGUI pointsText;
    public TextMeshProUGUI buttonPointsText;

    public List<UIClassMapNode> allNodes = new List<UIClassMapNode>();

    private RectTransform contentRt;

    private UIClassMapNode StarterNode;
    private UIClassMapNode lastBoughtNode;
    private Color notAccessableColor = new Color32(30, 58, 78, 255);
    private Color accesableColor = new Color32(40, 80, 169, 255);
    private Color boughtColor = new Color32(94, 185, 255, 255);
    IEnumerator Start()
    {
        contentRt = ClassMapContent.GetComponent<RectTransform>();
        sm = GameObject.FindGameObjectWithTag("SaveLoadDataManager").GetComponent<SaveLoadDataManager>();
        while (!sm.Initialized) // Wait until Player Data loaded.
        {
            yield return null;
        }

        playerData = sm.playerData;

        backButton.onClick.AddListener(() => backButtonClicked());
        researchTreeButton.onClick.AddListener(() => openButtonClicked());

        playerData.level.OnLevelChanged += onNodeChanged;
        onNodeChanged(null, false);

        SetSkillTree();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && SkillTreePanel.activeSelf)
        {
            SkillTreePanel.SetActive(false);
        }
    }
    private void openButtonClicked()
    {
        PlaySound.PlayButtonClickSound();
        if (!playerData.session.anyModeIsActive())
        {
            if (lastBoughtNode != null) centerContentToNode(lastBoughtNode);

            SkillTreePanel.SetActive(true);
        }
    }
    private void backButtonClicked()
    {
        PlaySound.PlayButtonClickSound();
        SkillTreePanel.SetActive(false);
    }
    public void SetSkillTree()
    {
        createNodes();
        putNodesOnUI();
        setInitialNodes();
    }
    private void createNodes()
    {
        allNodes = new List<UIClassMapNode>();

        StarterNode = new UIClassMapNode();
        StarterNode.node = new SkillTreeNodeData();
        StarterNode.UINode = ClassPanelNode;
        allNodes.Add(StarterNode);
        (StarterNode.node as SkillTreeNodeData).hasNode = true;

        for (int i = 0; i < playerData.level.skillTreeNodes.Count; i++) // Ilk ayarlar
        {
            SkillTreeNodeData node = playerData.level.skillTreeNodes[i];
            node.OnNodeChanged += onNodeChanged;

            UIClassMapNode UInode = new UIClassMapNode();
            allNodes.Add(UInode); // Index equality

            node.UInode = UInode;
            UInode.node = node;

            if (node.node.connectedToStarter)
            {
                StarterNode.connectedNodes.Add(UInode);
            }
        }
        for (int i = 0; i < playerData.level.skillTreeNodes.Count; i++) // Connections
        {
            SkillTreeNodeData node = playerData.level.skillTreeNodes[i];
            UIClassMapNode UInode = node.UInode;
            for (int k = 0; k < node.node.connectedNodeIndexes.Count; k++)
            {
                UIClassMapNode otherNode = allNodes[node.node.connectedNodeIndexes[k] + 1]; // Starter Nodeden dolayi +1
                UInode.connectedNodes.Add(otherNode);
            }
        }
    }

    private void putNodesOnUI()
    {
        for (int j = 0; j < allNodes.Count; ++j)
        {
            UIClassMapNode currentNode = allNodes[j];
            int connectedNodesCount = currentNode.connectedNodes.Count;
            int createCount = 0;
            for (int i = 0; i < connectedNodesCount; i++)
            {
                UIClassMapNode node = currentNode.connectedNodes[i];
                node.connectedNodes.Add(currentNode);
                if (node.UINode == null) // Yeni Node olusturma
                {
                    SkillTreeNodeData classMapNodeData = node.node as SkillTreeNodeData;
                    GameObject clone = Instantiate(NodeFramePrefab, ClassMapContent.transform);
                    RectTransform rt = clone.GetComponent<RectTransform>();
                    NodeHoverDetector hd = clone.GetComponent<NodeHoverDetector>();
                    hd.infoPanel = InfoPanel;
                    hd.recipeData = sm.resourceDataDatabase.resources[classMapNodeData.node.resourceIndex];
                    hd.enabled = true;

                    NodeClickDetector clickDetector = clone.GetComponent<NodeClickDetector>();
                    clickDetector.node = node;
                    clickDetector.skillMapNode = classMapNodeData;
                    clickDetector.playerData = playerData;

                    Image img = clone.transform.Find("Background").transform.Find("NodeImage").GetComponent<Image>();
                    img.sprite = sm.resourceImages[sm.resourceDataDatabase.resources[classMapNodeData.node.resourceIndex].image_name];

                    int nodeSize = 50;
                    if (classMapNodeData.node.nodeType == "Medium")
                    {
                        nodeSize = 75;
                    }
                    else if (classMapNodeData.node.nodeType == "Large")
                    {
                        nodeSize = 115;
                    }


                    float anglePerI = 360f / connectedNodesCount;
                    float creationAngle = classMapNodeData.node.creationAngle;
                    if (creationAngle == -1f)
                    {
                        if (currentNode.creatorNode == null)
                        {
                            creationAngle = anglePerI * createCount;
                        }
                        else
                        {
                            creationAngle = currentNode.creationAngle - 180 + (anglePerI * (createCount + 1));
                            if (creationAngle < 0)
                            {
                                creationAngle = 360 + creationAngle;
                            }
                            else if (creationAngle >= 360)
                            {
                                creationAngle = creationAngle - 360;
                            }
                        }
                    }

                    float distance = classMapNodeData.node.distance;
                    if (distance == -1f)
                    {
                        distance = 150f;
                    }

                    rt.anchoredPosition = findPositionWithDistance(distance, creationAngle, currentNode.UINode.GetComponent<RectTransform>().anchoredPosition);
                    rt.sizeDelta = new Vector2(nodeSize, nodeSize);
                    node.UINode = clone;
                    node.creationAngle = creationAngle;
                    node.creatorNode = currentNode;
                    createCount++;

                    createNodeEdge(node, currentNode);
                }
                else if (node != currentNode && !node.edgeNodes.Contains(currentNode)) // Var olan nodeye baglanti cekme.
                {
                    createNodeEdge(node, currentNode);
                }
            }
        }
    }
    private Vector2 findPositionWithDistance(float distance, float angle, Vector2 originPosition)
    {
        // angle = angle + 30; // Rotatelemek istersen
        float radians = angle * Mathf.Deg2Rad;

        float x = originPosition.x + distance * Mathf.Cos(radians);
        float y = originPosition.y - distance * Mathf.Sin(radians);

        return new Vector2(x, y);
    }
    private void createNodeEdge(UIClassMapNode node1, UIClassMapNode node2)
    {
        Vector2 pos1 = node1.UINode.GetComponent<RectTransform>().anchoredPosition;
        Vector2 pos2 = node2.UINode.GetComponent<RectTransform>().anchoredPosition;

        Vector2 midpoint = new Vector2((pos1.x + pos2.x) / 2, (pos1.y + pos2.y) / 2);
        float distance = (pos2 - pos1).magnitude;

        float angle;
        if (pos1.x == pos2.x)
        {
            angle = 90f;
        }
        else if (pos1.y == pos2.y)
        {
            angle = 0f;
        }
        else
        {
            angle = Mathf.Atan((pos2.y - pos1.y) / (pos2.x - pos1.x)) * Mathf.Rad2Deg;
        }

        GameObject clone = Instantiate(NodeEdgePrefab, EdgesContent.transform);
        RectTransform rt = clone.GetComponent<RectTransform>();

        rt.anchoredPosition = midpoint;
        rt.eulerAngles = new Vector3(0, 0, angle);
        rt.sizeDelta = new Vector2(distance, rt.sizeDelta.y);

        node1.edgeNodes.Add(node2);
        node2.edgeNodes.Add(node1);
        node1.edgeUIs.Add(clone);
        node2.edgeUIs.Add(clone);
    }
    private void centerContentToNode(UIClassMapNode node)
    {
        RectTransform rt = node.UINode.GetComponent<RectTransform>();
        contentRt.anchoredPosition = new Vector2(-rt.anchoredPosition.x, -rt.anchoredPosition.y);
    }
    private void setInitialNodes()
    {
        for (int k = 0; k < allNodes.Count; k++)
        {
            updateNode(allNodes[k]);
        }

        int availablePoints = playerData.level.calculateAvailableResearchPoints();
        pointsText.text = "<color=#5EB9FF>" + availablePoints + "</color> Research Points";
    }
    void onNodeChanged(UIClassMapNode node, bool isNode)
    {
        if (isNode)
        {
            updateNode(node);
            centerContentToNode(node);
            lastBoughtNode = node;
        }

        int availablePoints = playerData.level.calculateAvailableResearchPoints();
        pointsText.text = "<color=#5EB9FF>" + availablePoints + "</color> Research Points";
        buttonPointsText.text = availablePoints + " Research Points";
    }
    private void updateNode(UIClassMapNode currentNode)
    {
        Image NodeImg = currentNode.UINode.transform.Find("Background").GetComponent<Image>();
        int edgeNodesCount = currentNode.edgeNodes.Count;

        SkillTreeNodeData classMapNodeData = currentNode.node as SkillTreeNodeData;
        bool currentNodeHasNode = classMapNodeData.hasNode;
        bool anyEdgeHasNode = false;
        for (int i = 0; i < edgeNodesCount; i++) // Komsulari dolas
        {
            UIClassMapNode edgeNode = currentNode.edgeNodes[i];
            GameObject edgeUI = currentNode.edgeUIs[i];
            Image EdgeImg = edgeUI.transform.Find("Background").GetComponent<Image>();
            Image EdgeNodeImg = edgeNode.UINode.transform.Find("Background").GetComponent<Image>();

            bool edgeHasNode = (edgeNode.node as SkillTreeNodeData).hasNode;

            if (currentNodeHasNode == true)
            {
                if (edgeHasNode == true)
                {
                    anyEdgeHasNode = true;
                    EdgeImg.color = boughtColor;
                    EdgeNodeImg.color = boughtColor;
                }
                else
                {
                    EdgeImg.color = accesableColor;
                    EdgeNodeImg.color = accesableColor;
                }
            }
            else
            {
                if (edgeHasNode == true)
                {
                    anyEdgeHasNode = true;
                    EdgeImg.color = accesableColor;
                    EdgeNodeImg.color = boughtColor;
                }
                else
                {
                    EdgeImg.color = notAccessableColor;
                }
            }
        }
        if (currentNodeHasNode == true)
        {
            NodeImg.color = boughtColor;
            //Set node effects.
        }
        else
        {
            if (anyEdgeHasNode == true)
            {
                NodeImg.color = accesableColor;
            }
            else
            {
                NodeImg.color = notAccessableColor;
            }
        }
    }
}