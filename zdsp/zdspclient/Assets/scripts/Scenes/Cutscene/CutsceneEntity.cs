﻿using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum CutsceneTriggerType
{   
    TriggerBox = 0,
    RealmStart,
    Quest,
    UI,
    EventTrigger,
    AutoStart 
}

public class CutsceneEntity : MonoBehaviour
{
    #region Inspector
    public string CutsceneName;
    public CutsceneTriggerType cutsceneTriggerType;
    public TimelineAssist Cutscene; 
    public string cutscenePath;   

    public UnityEvent OnCutsceneFinished;
    private bool skipped = false;
    private bool destroyed = false;
    #endregion

    void Awake()
    {
        if (PhotonNetwork.connected && Cutscene != null )//in game Mode, it is turnned off , which start time is controlled by code.
            Cutscene.gameObject.SetActive(false);
    }

    void Start()
    {
        if (cutsceneTriggerType == CutsceneTriggerType.AutoStart && !PhotonNetwork.connected)
            PlayCutscene();
    }

    void OnValidate()
    {
        gameObject.tag = "Cutscene";
    }

    public void PlayCutscene()
    {
        skipped = false;
        destroyed = false;
        if (Cutscene != null)
            StartCutscene();
        else if (!string.IsNullOrEmpty(cutscenePath))
        {
#if UNITY_EDITOR
            GameObject cutscenePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/" + cutscenePath);
            OnPrefabLoaded(cutscenePrefab);
#else
            UIManager.StartHourglass(120);
            GameInfo.gCombat.CutsceneManager.CutsceneLoading = true;

            AssetLoader.Instance.LoadAsync<GameObject>(cutscenePath, OnPrefabLoaded);
#endif
        }
    }

    public void SkipCutScene()
    {
        if (Cutscene != null)
            Cutscene.Skip();
        else
            skipped = true;
    }

    public void CleanUp()
    {
        if (Cutscene != null)
            Cutscene.Skip();
        else
            destroyed = true;
    }

    private void StartCutscene()
    {
        if (Cutscene.IsPlaying())
            return;
        if (GameInfo.gLocalPlayer != null)
            GameInfo.gCombat.CutsceneManager.OnStartCutscene(this);
        Cutscene.Play(Cutscene_CutsceneFinished);
    }

    private void Cutscene_CutsceneFinished()
    {
        if (GameInfo.gLocalPlayer != null)
            GameInfo.gCombat.CutsceneManager.OnCutsceneFinished(this);
        OnCutsceneFinished.Invoke();
    }

    private void OnPrefabLoaded(GameObject cutscenePrefab)
    {
        if (destroyed)
            return;
        if (skipped)
            Debug.Log("skipped before prefab loaded " + cutscenePath);
        else if (cutscenePrefab == null)
            Debug.Log(string.Concat("Error loading cutscene assetbundle ", cutscenePath));
        else
        {
            GameObject go = Instantiate(cutscenePrefab);
            go.transform.SetParent(transform, false);
            Cutscene = go.GetComponent<TimelineAssist>();
            if (Cutscene != null)
                StartCutscene();
        }

        GameInfo.gCombat.CutsceneManager.CutsceneLoading = false;
        UIManager.StopHourglass();
    }
}
