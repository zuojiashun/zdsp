﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using Zealot.Repository;

public class UI_WorldMap_Country : MonoBehaviour
{
    [SerializeField]
    GameObject mMonsterPrefab;
    [SerializeField]
    GameObject mPlacePrefab;

    [SerializeField]
    GameObject mMonsterParent;
    [SerializeField]
    GameObject mPlaceParent;

    [SerializeField]
    Text mLvReq;
    [SerializeField]
    Text mCountryName;
    UnityAction toggleOnAction;
    UnityAction<List<int>> markerShowAction;

    string mDestinationLvStr = string.Empty;
    List<int> interestIdxLst = new List<int>();

    public void Awake()
    {

    }

    public void OnDisable()
    {
        mDestinationLvStr = string.Empty;
    }

    public void Init(WorldMapCountry wmr, UnityAction action, UnityAction<Sprite> areaSpriteAction, UnityAction<List<int>> markerShowActionX, UnityAction<int> markerHighlightAction)
    {
        interestIdxLst.Clear();

        mCountryName.text = wmr.name;
        mLvReq.text = wmr.lvRange;
        toggleOnAction = action;
        markerShowAction = markerShowActionX;

        for (int i = 0; i < wmr.placeLst.Count; ++i)
        {
            GameObject obj = Instantiate(mPlacePrefab, Vector3.zero, Quaternion.identity);
            obj.transform.SetParent(mPlaceParent.transform, false);

            UI_WorldMap_PlaceInterest wmpi = obj.GetComponent<UI_WorldMap_PlaceInterest>();
            wmpi.Init(wmr.placeLst[i], OnSelectPlaceInterest, areaSpriteAction, markerHighlightAction);

            interestIdxLst.Add(wmr.placeLst[i].interestID);
        }
        for (int i = 0; i < wmr.monLst.Count; ++i)
        {
            GameObject obj = Instantiate(mMonsterPrefab, Vector3.zero, Quaternion.identity);
            obj.transform.SetParent(mMonsterParent.transform, false);

            UI_WorldMap_MonData wmmd = obj.GetComponent<UI_WorldMap_MonData>();
            wmmd.Init(wmr.monLst[i]);
        }
    }

    public void OnClick_GO()
    {
        if (mDestinationLvStr == string.Empty)
            return;

        HUD_MapController.PathFindCalculateToDestination(mDestinationLvStr);
        UIManager.CloseWindow(WindowType.WorldMap);
    }

    public void OnSelectPlaceInterest(string levelname)
    {
        mDestinationLvStr = levelname;
    }

    public void OnToggle(bool tog)
    {
        if (tog == false)
            return;

        //Zoom in camera to territory
        toggleOnAction.Invoke();
        markerShowAction.Invoke(interestIdxLst);
    }
}
