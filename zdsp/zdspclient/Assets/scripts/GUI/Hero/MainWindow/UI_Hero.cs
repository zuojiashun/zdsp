﻿using System.Collections;
using UnityEngine;
using Zealot.Common;

public class UI_Hero : BaseWindowBehaviour
{
    [SerializeField] DefaultToggleInGroup tabController;

    private UI_Hero_Info uiHeroInfo;
    private UI_Hero_HeroBonds uiHeroBonds;
    private UI_Hero_Exploration uiExploration;

    public int SelectHero { get; set; }

    public override void OnRegister()
    {
        base.OnRegister();

        uiHeroInfo = tabController.GetPageContent(0).GetComponent<UI_Hero_Info>();
        uiHeroInfo.Setup();
        uiHeroBonds = tabController.GetPageContent(1).GetComponent<UI_Hero_HeroBonds>();
        uiExploration = tabController.GetPageContent(2).GetComponent<UI_Hero_Exploration>();
    }

    public override void OnOpenWindow()
    {
        base.OnOpenWindow();
        StartCoroutine(LateInit());
    }

    private IEnumerator LateInit()
    {
        yield return null;
        uiHeroInfo.Init(SelectHero);
        SelectHero = 0; // reset for next window open
    }

    public override void OnCloseWindow()
    {
        base.OnCloseWindow();
        uiHeroInfo.CleanUp();
        uiHeroBonds.CleanUp();
        uiExploration.CleanUp();
    }

    public void GoToTab(int index)
    {
        tabController.GoToPage(index);
    }

    public void OnSummonedHeroChanged()
    {
        uiHeroInfo.OnSummonedHeroChanged();
    }

    public void OnHeroAdded(Hero hero)
    {
        GameObject obj = UIManager.GetWindowGameObject(WindowType.DialogHeroBonds);
        if (obj != null && obj.activeInHierarchy)
            obj.GetComponent<UI_Hero_BondsDialog>().Refresh(hero, true);

        uiHeroInfo.OnHeroAdded(hero);

        if (uiHeroBonds.gameObject.activeInHierarchy)
            uiHeroBonds.RefreshList(hero.HeroId);
    }

    public void OnHeroUpdated(Hero oldHero, Hero newHero)
    {
        GameObject obj = UIManager.GetWindowGameObject(WindowType.DialogHeroBonds);
        if (obj != null && obj.activeInHierarchy)
            obj.GetComponent<UI_Hero_BondsDialog>().Refresh(newHero, false);

        uiHeroInfo.OnHeroUpdated(oldHero, newHero);

        if (uiHeroBonds.gameObject.activeInHierarchy)
            uiHeroBonds.RefreshList(newHero.HeroId);
    }

    public void OnInterestRandomSpinResult(byte interest)
    {
        uiHeroInfo.OnInterestRandomSpinResult(interest);
    }

    public void OnExplorationsUpdated()
    {
        if (uiExploration.gameObject.activeInHierarchy)
            uiExploration.Refresh();
    }
}