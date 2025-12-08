using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

[Serializable]
public class Level // bu sadece ilk asama yani terrain + resources, buildingleri ayri savelemek gerekecek
{
    public event Action OnXPChanged;
    public event Action<UIClassMapNode, bool> OnLevelChanged;

    public int _level = 1;
    public double _xp;
    [NonSerialized] public double levelXpRequirement;
    public int level
    {
        get => _level;
        set
        {
            _level = value;
            OnLevelChanged?.Invoke(null, false);
        }
    }
    public double xp
    {
        get => _xp;
        set
        {
            _xp = value;
            OnXPChanged?.Invoke();
        }
    }

    public List<SkillTreeNodeData> skillTreeNodes = new List<SkillTreeNodeData>();

    public void checkLevelUp()
    {
        levelXpRequirement = getLevelXpRequirement(level);
        if (xp >= levelXpRequirement)
        {
            double oldLevelXPRequirement = levelXpRequirement;
            level = level + 1;
            levelXpRequirement = getLevelXpRequirement(level);
            xp = xp - oldLevelXPRequirement;
        }
    }
    public double getLevelXpRequirement(int lvl)
    {
        double result = math.pow(1.15d, lvl - 1);
        result = math.pow(result, 1 + (lvl - 1) / 250d);
        return result * 100d;
    }
    public int calculateAvailableResearchPoints()
    {
        int totalHasNodeCount = 0;
        foreach (var node in skillTreeNodes)
        {
            if (node.hasNode)
            {
                totalHasNodeCount++;
            }
        }

        return level - totalHasNodeCount;
    }
}
[Serializable]
public class SkillTreeNodeDatabase
{
    public List<SkillTreeNode> skillTreeNodes = new List<SkillTreeNode>();
}
public class LevelManager : MonoBehaviour
{
    public TextMeshProUGUI LevelText;
    public TextMeshProUGUI PercentText;
    public Image XPBarFill;

    private SaveLoadDataManager sm;
    private PlayerData playerData;
    IEnumerator Start()
    {
        sm = GameObject.FindGameObjectWithTag("SaveLoadDataManager").GetComponent<SaveLoadDataManager>();
        while (!sm.Initialized) // Wait until Player Data loaded.
        {
            yield return null;
        }

        playerData = sm.playerData;

        playerData.level.OnXPChanged += OnXPChanged;
        OnXPChanged();
    }
    void OnXPChanged()
    {
        playerData.level.checkLevelUp();

        float div;
        if (playerData.level.xp > 0)
        {
            div = (float)(playerData.level.xp / playerData.level.levelXpRequirement);
        }
        else
        {
            div = 0;
        }

        XPBarFill.rectTransform.anchorMax = new Vector2(div, 1);
        LevelText.text = "Level " + playerData.level.level;
        PercentText.text = (div * 100).ToString("0.00") + "%";
    }
}
