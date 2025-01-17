﻿using Kopio.JsonContracts;
using System;
using UnityEngine;
using UnityEngine.UI;
using Zealot.Repository;

public class Hero_BondHeroData : MonoBehaviour
{
    [SerializeField] Image heroImage;
    [SerializeField] Text statusText;
    [SerializeField] string lockedColorHex;

    private int heroId;
    private Toggle toggle;
    private Action<int, bool> OnSelectedCallback;
    private Action<int> OnClickCallback;
    private Color lockedColor = Color.clear;

    public void Init(int heroId, ToggleGroup group, Action<int, bool> selectedCallback)
    {
        this.heroId = heroId;
        toggle = GetComponent<Toggle>();
        toggle.group = group;
        OnSelectedCallback = selectedCallback;
        toggle.onValueChanged.AddListener(OnToggled);
        HeroJson data = HeroRepo.GetHeroById(heroId);
        if (data != null)
            heroImage.sprite = ClientUtils.LoadIcon(data.portraitpath);
    }

    public void Init(int heroId, Action<int> clickCallback)
    {
        this.heroId = heroId;
        Button button = GetComponent<Button>();
        OnClickCallback = clickCallback;
        button.onClick.AddListener(OnClick);
        HeroJson data = HeroRepo.GetHeroById(heroId);
        if (data != null)
            heroImage.sprite = ClientUtils.LoadIcon(data.portraitpath);
    }

    public void SetFulfilled(bool fulfilled, bool heroLocked)
    {
        //if (lockedColor == Color.clear)
        //    ColorUtility.TryParseHtmlString(lockedColorHex, out lockedColor);

        if (fulfilled)
        {
            statusText.text = GUILocalizationRepo.GetLocalizedString("hro_bond_fulfilled");
            statusText.color = Color.white;
            //heroImage.color = Color.white;
        }
        else
        {
            statusText.text = heroLocked ? GUILocalizationRepo.GetLocalizedString("hro_bond_herolocked") : GUILocalizationRepo.GetLocalizedString("hro_bond_unfulfilled");
            statusText.color = Color.red;
            //heroImage.color = lockedColor;
        }
    }

    public void OnToggled(bool isOn)
    {
        if (OnSelectedCallback != null)
            OnSelectedCallback(heroId, isOn);
    }

    public bool IsToggleOn()
    {
        return toggle.isOn;
    }

    public void SetToggleOn(bool value)
    {
        toggle.isOn = value;
    }

    public void OnClick()
    {
        if (OnClickCallback != null)
            OnClickCallback(heroId);
    }
}