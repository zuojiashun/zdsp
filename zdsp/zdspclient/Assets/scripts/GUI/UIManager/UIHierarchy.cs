﻿using OrbCreationExtensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIHierarchy : MonoBehaviour
{
    public GameObject PlayerLabelPrefab;
    public GameObject PlayerLabelExtPrefab;
    public GameObject DmgLabelPrefab;
    public GameObject CastingAndCutscenePrefab;
    public GameObject NpcLabelPrefab;

    private List<GameObject> lowSettingObj;//will hide all the gameobject when player choose low setting
    private GameObject eventSystemObj;

    void Awake()
    {
        UIManager.RegisterUIHierarchy(this);

        var windowComponents = gameObject.GetComponentsInChildren<UIWindow>(true);
        for (int i = 0; i < windowComponents.Length; i++)
            windowComponents[i].RegisterWindow();

        UIManager.AlertManager2 = new AlertManagerVersion2();
        var alertComponents = gameObject.GetComponentsInChildren<AlertComponentVersion2>(true);
        for (int i = 0; i < alertComponents.Length; i++)
        {
            alertComponents[i].Off();
            UIManager.AlertManager2.RegisterAlert(alertComponents[i]);
        }

        lowSettingObj = transform.FindObjectsWithTag("LowSettingUI");
    }

    public void SetupEventSystem()
    {
        if (EventSystem.current == null)
        {
            eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
        else
        {
            eventSystemObj = EventSystem.current.gameObject;
        }
        DontDestroyOnLoad(eventSystemObj);

        EventSystem.current.pixelDragThreshold = 15;  // for joystick
    }

    public void DestroyHierarchy()
    {
        SceneLoader.Instance.OnCombatHierarchyDestroyed();
        Destroy(gameObject);
        if (eventSystemObj != null)
            Destroy(eventSystemObj);
    }

    void OnDestroy()
    {
        UIManager.DestroyAllWindows();
    }

    public void SetVisibilityLowSettingObject(bool isvisible)
    {
        for (int i = 0; i < lowSettingObj.Count; i++)
        {
            lowSettingObj[i].SetActive(isvisible);
        }
    }

}