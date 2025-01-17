﻿using UnityEngine;
using UnityEngine.Events;
using Zealot.Common;

public class GameIcon_MaterialConsumable : GameIcon_Base
{
    [SerializeField]
    GameObject iconStatusCannotuse = null;

    [SerializeField]
    GameIconCmpt_StackCount itemStackCount = null;

    [SerializeField]
    GameIconCmpt_SelectCheckmark toggleSelect = null;

    public void Init(int itemId, int stackCount, bool statusCannotUse, bool isNew, bool isToggleSelectOn, UnityAction onClickCallback = null)
    {
        Init(itemId, isNew);
        StatusCannotUse = statusCannotUse;
        SetStackCount(stackCount);
        if (toggleSelect != null)
            SetToggleSelectOn(isToggleSelectOn);
        if (onClickCallback != null)
            SetClickCallback(onClickCallback);
    }

    public void Init(CurrencyType currencyType, int stackCount, bool statusCannotUse, bool isNew, bool isToggleSelectOn, UnityAction onClickCallback = null)
    {
        Init(currencyType, isNew);
        StatusCannotUse = statusCannotUse;
        SetStackCount(stackCount);
        if (toggleSelect != null)
            SetToggleSelectOn(isToggleSelectOn);
        if (onClickCallback != null)
            SetClickCallback(onClickCallback);
    }

    public void InitWithoutCallback(int itemId, int stackCount)
    {
        Init(itemId, stackCount, false, false, false);
    }

    public void InitWithoutCallback(CurrencyType currencyType, int stackCount)
    {
        Init(currencyType, stackCount, false, false, false);
    }

    public void InitWithToolTipView(int itemId, int stackCount)
    {
        Init(itemId, stackCount, false, false, false, () => OnClickShowItemToolTip(stackCount));
    }

    public void SetStackCount(int count)
    {
        itemStackCount.SetStackCount(count);
    }

    // Set stack count to show even if < 2
    public void SetFullStackCount(int stackCount, bool uncapped = false)
    {
        itemStackCount.SetStackCountFull(stackCount, uncapped);
    }

    public void SetStackCount(int invcount, int reqcount)
    {
        itemStackCount.SetStackCount(invcount, reqcount);
    }

    public bool StatusCannotUse
    {
        set { iconStatusCannotuse.SetActive(value); }
    }

    public GameIconCmpt_SelectCheckmark GetToggleSelect()
    {
        return toggleSelect;
    }

    public void SetToggleSelectOn(bool isOn)
    {
        toggleSelect.SetCheckmarkVisible(isOn);
    }
}
