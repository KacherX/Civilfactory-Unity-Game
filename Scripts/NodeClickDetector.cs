using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NodeClickDetector : MonoBehaviour, IPointerClickHandler
{
    public UIClassMapNode node;
    public SkillTreeNodeData skillMapNode;
    public PlayerData playerData;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (skillMapcheckNodeSituatuon() == "AccesableNode" && playerData.level.calculateAvailableResearchPoints() >= 1)
        {
            skillMapNode.hasNode = true;
        }
    }
    private string skillMapcheckNodeSituatuon()
    {
        bool currentNodeHasNode = skillMapNode.hasNode;
        if (skillMapNode.hasNode)
        {
            return "HasNode";
        }
        else
        {
            int edgeNodesCount = node.edgeNodes.Count;
            for (int i = 0; i < edgeNodesCount; i++) // Komsulari dolas
            {
                UIClassMapNode edgeNode = node.edgeNodes[i];
                SkillTreeNodeData edgeNodeData = edgeNode.node as SkillTreeNodeData;
                bool edgeHasNode = edgeNodeData.hasNode;
                if (edgeNodeData.hasNode)
                {
                    return "AccesableNode";
                }
            }
        }
        return "NonAccesableNode";
    }
}
