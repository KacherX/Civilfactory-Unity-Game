using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class Currency
{
    public event Action OnCurrencyChanged;

    public double _money = 150d;
    public double money
    {
        get => _money;
        set
        {
            _money = value;
            OnCurrencyChanged?.Invoke();
        }
    }
}
public class CurrencyManager : MonoBehaviour
{
    public TextMeshProUGUI moneyText;

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

        playerData.currency.OnCurrencyChanged += OnCurrencyChanged;
        OnCurrencyChanged();
    }
    void OnCurrencyChanged()
    {
        moneyText.text = "$" + GlobalFunctions.NumberMakeUp(playerData.currency.money);
    }
}
