﻿using Kopio.JsonContracts;
using System;
using System.Collections.Generic;
using UnityEngine.Events;
using Zealot.Audio;
using Zealot.Common;
using Zealot.Client.Entities;

public static class GameInfo
{
    public static bool DCReconnectingGameServer = false;
    public static bool TransferingServer = false;
    public static PlayerGhost gLocalPlayer;
    public static HUD_DamageLabelPool gDmgLabelPool = null;
    public static string mUser = "";
    public static string mChar = "";
    public static string mCharId = "";
    public static RealmJson mRealmInfo;
    public static RealmState mRealmState = RealmState.Created;
    public static bool mPlayerFirstEnter = true;
    public static bool mOpenArenaOnPlayerSpawn = false;
    public static DateTime mServerStartUpDT;
    public static bool mIsPlayerReady = false;
    public static bool mInspectMode = false;
    public static bool mIsHighSetting = true; //this is for hiding of ui during low setting, temp put until setting ui is out

    public static GameClientState gClientState;
    public static Login gLogin;
    public static UI_Login gUILogin;
    public static PlayerInLobby gLobby;
    public static ClientMain gCombat;
    public static CharacterCreationManager gCharacterCreationManager;

    public static UIShop gUIShop = null;

    public static BaseClientEntity gSelectedEntity;

    public static PlayerSkillCDState gSkillCDState;
    public static bool mRecycleConfirmation = true;

    public static bool mIsOfflineExpHUDReq = true;
    public static string mLastMailDatetime = "";
    public static string mVoiceChatAddress = "";
    public static int mShowTutorialImagesStartLv = 0;
    public static bool firstViewMovie = true;
    public static int TestFeedbackIndex = -1;

    private static UnityEvent playerSpawnEvent;
    private static UnityEvent playerStatsChangedEvent;
    private static UnityEvent combatStatsChangedEvent;

    public static QuestTeleportAction mTeleportAction = null;
    public static Dictionary<int, List<int>> mNpcQuestStatus = new Dictionary<int, List<int>>();
    public static Dictionary<int, int> mSideEffectQuestStatus = new Dictionary<int, int>();
    public static Dictionary<int, int> mCompanionQuestStatus = new Dictionary<int, int>();
    public static List<QuestEventDetailJson> mQuestEventList = new List<QuestEventDetailJson>();
    public static int mQuestEventTriggered = -1;
    public static List<int> mPendingQuestEventList = new List<int>();
    public static bool mWaitingQuestEvent = false;

    public static void OnQuitGame()
    {
        OnLevelChanged();
        if (gDmgLabelPool != null)
            UnityEngine.Object.Destroy(gDmgLabelPool.gameObject);
        mChar = "";
        mLastMailDatetime = "";
        mOpenArenaOnPlayerSpawn = false;
        mPlayerFirstEnter = true;
        mRecycleConfirmation = true;
        mIsOfflineExpHUDReq = true;
        PartyFollowTarget.Reset();
        GameDownload.Instance.Clear();
        if (UIManager.UIHierarchy != null)
            UIManager.UIHierarchy.DestroyHierarchy();
    }

    public static void OnLevelChanged()
    {
        DCReconnectingGameServer = false;
        TransferingServer = false;
        mRealmInfo = null;
        mRealmState = RealmState.Created;
        gLocalPlayer = null;       
        mIsPlayerReady = false;
        if (CutsceneManager.instance != null)
        {
            if (CutsceneManager.instance != null)
                CutsceneManager.instance.StopCutScene();
            gCombat.OnLevelChanged();
        }
        gSelectedEntity = null;
        //gSkillCDState = null;

        if (!PartyFollowTarget.IsPaused())
            PartyFollowTarget.Pause();

        UIManager.OnLevelChanged();

        ObjPoolMgr.Instance.Cleanup();

        if (playerSpawnEvent != null)
        {
            playerSpawnEvent.RemoveAllListeners();
            playerSpawnEvent = null;
        }
        if (playerStatsChangedEvent != null)
        {
            playerStatsChangedEvent.RemoveAllListeners();
            playerStatsChangedEvent = null;
        }
        if (combatStatsChangedEvent != null)
        {
            combatStatsChangedEvent.RemoveAllListeners();
            combatStatsChangedEvent = null;
        }
        if (gDmgLabelPool != null)
            UnityEngine.Object.Destroy(gDmgLabelPool.gameObject);
   
        SoundFX.Instance.CleanUp();
        VoiceChatManager.Instance.CleanUp();
        AssetLoader.Instance.OnLevelChanged();
        Coroutiner.Instance.StopAllCoroutines();
    }

    public static void OnDisconnect()
    {
        if (!DCReconnectingGameServer)
        {
            if (gClientState == GameClientState.Combat && mIsPlayerReady)
            {
                if (gLocalPlayer != null)
                    gLocalPlayer.ForceIdle();
                if (gCombat != null)
                    gCombat.OnDisconnected();
                DCReconnectingGameServer = true;
            }
            if (TransferingServer)
                DCReconnectingGameServer = true;
        }
    }

    public static void OnReconnected()
    {
        UIManager.StopHourglass();
        gClientState = GameClientState.Combat;
        DCReconnectingGameServer = false;
        TransferingServer = false;

        gLocalPlayer = null;
        mRealmInfo = null;
        mRealmState = RealmState.Created;
        mIsPlayerReady = false;
        if (gCombat != null)
            gCombat.OnReconnected();
        gSelectedEntity = null;
        gSkillCDState = null;

        if (playerSpawnEvent != null)
        {
            playerSpawnEvent.RemoveAllListeners();
            playerSpawnEvent = null;
        }
        if (playerStatsChangedEvent != null)
        {
            playerStatsChangedEvent.RemoveAllListeners();
            playerStatsChangedEvent = null;
        }
        if (combatStatsChangedEvent != null)
        {
            combatStatsChangedEvent.RemoveAllListeners();
            combatStatsChangedEvent = null;
        }
    }

    public static BaseClientEntity GetValidSelectedEntity()
    {
        if (gSelectedEntity != null)
        {
            if (gSelectedEntity.Destroyed) //might be destroyed after killing or after moving too far from it
            {
                gSelectedEntity = null; //dereference
                return null;
            }
            else
                return gSelectedEntity; //selected entity is still valid
        }
        return null;
    }

    public static DateTime GetSynchronizedServerDT()
    {
        return gCombat.mTimers.GetSynchronizedServerDT();
    }

    public static long GetSynchronizedTime()
    {
        return gCombat.mTimers.GetSynchronizedTime();
    }

    public static void AddPlayerSpawnListener(UnityAction call)
    {
        if (playerSpawnEvent == null)
            playerSpawnEvent = new UnityEvent();

        playerSpawnEvent.AddListener(call);
    }

    public static void OnLocalPlayerSpawned()
    {
        if (playerSpawnEvent != null)
            playerSpawnEvent.Invoke();
    }

    public static void AddPlayerStatsChangedListener(UnityAction call)
    {
        if (playerStatsChangedEvent == null)
            playerStatsChangedEvent = new UnityEvent();
        playerStatsChangedEvent.AddListener(call);
    }

    public static void OnPlayerStatsChangedSpawned()
    {
        if (playerStatsChangedEvent != null)
            playerStatsChangedEvent.Invoke();
    }

    public static void AddCombatStatsChangedListener(UnityAction call)
    {
        if (combatStatsChangedEvent == null)
            combatStatsChangedEvent = new UnityEvent();
        combatStatsChangedEvent.AddListener(call);
    }

    public static void OnCombatStatsChangedSpawned()
    {
        if (combatStatsChangedEvent != null)
            combatStatsChangedEvent.Invoke();
    }

    public static void ResetJoystick()
    {
        if (gCombat != null)
            gCombat.mPlayerInput.ResetJoystick();
    }

    public static bool HasRealmEnd()
    {
        return mRealmState == RealmState.Ended;
    }

    public static bool IsDoingTutorialRealm()
    {
        return mRealmInfo != null && mRealmInfo.type == RealmType.Tutorial;
    }

    public static bool CanLevelUsePotion()
    {
        //if (mRealmInfo != null && (mRealmInfo.type == RealmType.Arena || mRealmInfo.type == RealmType.Tutorial))
        //    return false;
        return true;
    }
}
