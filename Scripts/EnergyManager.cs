using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Energy
{
    public event Action OnEnergyChanged;

    [NonSerialized] public double _maxEnergy = 0;
    [NonSerialized] public double _currentEnergy = 0;

    public double maxEnergy
    {
        get => _maxEnergy;
        set
        {
            _maxEnergy = value;
            OnEnergyChanged?.Invoke();
        }
    }
    public double currentEnergy
    {
        get => _currentEnergy;
        set
        {
            _currentEnergy = value;
            OnEnergyChanged?.Invoke();
        }
    }
}
public class EnergyManager : MonoBehaviour
{
    public TextMeshProUGUI RatioText;
    public Image EnergyBarFill;

    private SaveLoadDataManager sm;
    private PlayerData playerData;

    private Color positiveColor = new Color32(200, 232, 255, 255);
    private Color negativeColor = new Color32(255, 0, 0, 255);
    private Color fillColor = new Color32(255, 185, 94, 255);
    private Color fullColor = new Color32(255, 60, 0, 255);
    IEnumerator Start()
    {
        sm = GameObject.FindGameObjectWithTag("SaveLoadDataManager").GetComponent<SaveLoadDataManager>();
        while (!sm.Initialized) // Wait until Player Data loaded.
        {
            yield return null;
        }
        playerData = sm.playerData;

        playerData.energy.OnEnergyChanged += OnEnergyChanged;
        OnEnergyChanged();
    }
    void OnEnergyChanged()
    {
        float div;
        if (playerData.energy.maxEnergy > 0)
        {
            div = (float)(playerData.energy.currentEnergy / playerData.energy.maxEnergy);
        }
        else
        {
            div = 0;
        }

        RatioText.text = GlobalFunctions.NumberMakeUp(playerData.energy.currentEnergy) + "/" + GlobalFunctions.NumberMakeUp(playerData.energy.maxEnergy);
        if (playerData.energy.currentEnergy > playerData.energy.maxEnergy)
        {
            RatioText.color = negativeColor;
            EnergyBarFill.rectTransform.anchorMax = new Vector2(1, 1);
            EnergyBarFill.color = fullColor;
        }
        else
        {
            RatioText.color = positiveColor;
            EnergyBarFill.rectTransform.anchorMax = new Vector2(div, 1);
            EnergyBarFill.color = fillColor;
        }
    }
}
