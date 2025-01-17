﻿using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using Zealot.Common;
using Zealot.Common.RPC;
using Zealot.Repository;


public partial class ClientMain : MonoBehaviour
{
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_TransferServer)]
    public void Ret_TransferServer(int serverid, string serverAddress)
    {
        UIManager.StopHourglass();
        if (!string.IsNullOrEmpty(serverAddress))
        {
            UIManager.ShowLoadingScreen(true);
            GameInfo.TransferingServer = true;
            PhotonNetwork.networkingPeer.TransferGameServer(serverAddress);
        }
        else
        {
            UIManager.ShowSystemMessage(GUILocalizationRepo.GetLocalizedSysMsgByName("sys_ServerNotFound"));
        }
    }

    #region EquipmentModding
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.EquipmentUpgradeEquipmentFailed)]
    public void EquipmentUpgradeEquipmentFailed()
    {
        GameObject uiEquipUpgradeObj = UIManager.GetWindowGameObject(WindowType.EquipUpgrade);
        if(uiEquipUpgradeObj != null)
        {
            UI_EquipmentUpgrade uiEquipUpgrade = uiEquipUpgradeObj.GetComponent<UI_EquipmentUpgrade>();
            if(uiEquipUpgrade != null)
            {
                uiEquipUpgrade.PlayEquipmentUpgradeFailure();
            }
        }
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.EquipmentUpgradeEquipmentSuccess)]
    public void EquipmentUpgradeEquipmentSuccess()
    {
        GameObject uiEquipUpgradeObj = UIManager.GetWindowGameObject(WindowType.EquipUpgrade);
        if(uiEquipUpgradeObj != null)
        {
            UI_EquipmentUpgrade uiEquipUpgrade = uiEquipUpgradeObj.GetComponent<UI_EquipmentUpgrade>();
            if(uiEquipUpgrade != null)
            {
                uiEquipUpgrade.PlayEquipmentUpgradeSuccess();
            }
        }
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.EquipmentReformEquipmentSuccess)]
    public void EquipmentReformEquipmentSuccess()
    {
        GameObject uiEquipReformObj = UIManager.GetWindowGameObject(WindowType.EquipReform);
        if(uiEquipReformObj != null)
        {
            UI_EquipmentReform uiEquipReform = uiEquipReformObj.GetComponent<UI_EquipmentReform>();
            if (uiEquipReform != null)
            {
                uiEquipReform.PlayEquipmentReformSuccess();
            }
        }
    }
    #endregion

    #region ReviveItem
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.RequestReviveItem)]
    public void RequestReviveItem(string requestor, int requestId)
    {
        Dictionary<string, string> param = new Dictionary<string, string>();
        param.Add("requestor", requestor);
        UIManager.OpenYesNoDialog(GUILocalizationRepo.GetLocalizedString("reviveItem_RequestRevive", param), 
            () => { RPCFactory.NonCombatRPC.AcceptReviveItem(requestId); }, () => { RPCFactory.NonCombatRPC.RejectReviveItem(requestId); });
    }
    #endregion

    #region Quest
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_DeleteQuest)]
    public void Ret_DeleteQuest(bool result, int questid)
    {
        UI_Quest quest = UIManager.GetWindowGameObject(WindowType.Quest).GetComponent<UI_Quest>();
        if (quest != null)
        {
            quest.OnDeleteQuest(result, questid);
        }
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_ResetQuest)]
    public void Ret_ResetQuest(bool result, int questid)
    {
        UI_Quest quest = UIManager.GetWindowGameObject(WindowType.Quest).GetComponent<UI_Quest>();
        if (quest != null)
        {
            quest.OnResetQuest(result, questid);
        }
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_CompleteQuest)]
    public void Ret_CompleteQuest(bool result, int questid)
    {
        UIManager.StopHourglass();
        if (GameInfo.gLocalPlayer != null)
        {
            GameInfo.gLocalPlayer.QuestController.QuestCompleted(questid, result);
        }
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_InteractAction)]
    public void Ret_InteractAction()
    {
        if (UIManager.IsWidgetActived(HUDWidgetType.QuestAction))
        {
            Hud_QuestAction questAction = UIManager.GetWidget(HUDWidgetType.QuestAction).GetComponent<Hud_QuestAction>();
            questAction.SetButtonStatus(true);
        }
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_TriggerQuest)]
    public void Ret_TriggerQuest(int questid, bool result)
    {
        if (GameInfo.gLocalPlayer != null)
        {
            GameInfo.gLocalPlayer.QuestController.QuestTriggered(questid, result);
        }
    }
    #endregion

    #region CharacterInfo
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_CharacterInfoSpendStatsPoints)]
    public void Ret_CharacterInfoSpendStatsPoints(int retVal)
    {
        GameObject obj = UIManager.GetWindowGameObject(WindowType.CharacterInfo);
        UI_CharacterInfo ci = obj.GetComponent<UI_CharacterInfo>();

        ci.mTabTwo.OnConfirmStatsAllocation_ServerFeedback(retVal);
    }
    #endregion

    #region NPCStore
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_NPCStoreInit)]
    public void Ret_NPCStoreInit(string scString)
    {
        JsonSerializerSettings jsonSetting = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
        var store = JsonConvert.DeserializeObject<NPCStoreInfo>(scString, jsonSetting);

        if (GameInfo.gUIShop != null)
        {
            GameInfo.gUIShop.init(store);
        }
    }
    
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_NPCStoreGetPlayerTransactions)]
    public void Ret_NPCStoreGetPlayerTransactions(string scString)
    {
        JsonSerializerSettings jsonSetting = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
        var trans = JsonConvert.DeserializeObject<Dictionary<string, NPCStoreInfo.Transaction>>(scString, jsonSetting);

        if (GameInfo.gUIShop != null)
        {
            GameInfo.gUIShop.UpdateTransactions(trans);
        }
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_NPCStoreBuy)]
    public void Ret_NPCStoreBuy(string scString)
    {
        if (GameInfo.gUIShop != null)
        {
            GameInfo.gUIShop.SignalTransactionStatus(scString);
        }
    }
    #endregion

    #region Skill
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_AddToSkillInventory)]
    public void Ret_AddToSkillInventory(byte result, int skillid, int skillpoint, int money)
    {
        GameObject obj = UIManager.GetWindowGameObject(WindowType.Skill);
        UI_SkillTree ui = obj.GetComponent<UI_SkillTree>();
        ui.OnEventSkillLevelUp(result, skillid, skillpoint, money);
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_SkillInventory)]
    public void Ret_SkillInventory(string skills)
    {
        JsonSerializerSettings jsonSetting = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
        Dictionary<int, int> result = JsonConvert.DeserializeObject<Dictionary<int, int>>(skills, jsonSetting);
        GameInfo.gLocalPlayer.mSkillInventory = result;
    }
    #endregion

    #region Destiny Clue
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_CollectClueReward)]
    public void Ret_CollectClueReward(int clueid, bool result)
    {
        UIManager.StopHourglass();
        if (result)
        {
            if (UIManager.IsWindowOpen(WindowType.DialogClaimReward))
            {
                UIManager.CloseDialog(WindowType.DialogClaimReward);
            }
            UIManager.ShowSystemMessage(GUILocalizationRepo.GetLocalizedSysMsgByName("sys_successcollectcluereward"));
        }
        else
        {
            UIManager.ShowSystemMessage(GUILocalizationRepo.GetLocalizedSysMsgByName("ret_ItemBagFull"));
        }
    }
    #endregion

    #region Donate
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_DonateItem)]
    public void Ret_DonateItem(string guid, int result)
    {
        UIManager.StopHourglass();
        if (result == 0)
        {
            UIManager.ShowSystemMessage(GUILocalizationRepo.GetLocalizedSysMsgByName("ret_donatenoitem"));
        }
        else if (result == 1)
        {
            UIManager.ShowSystemMessage(GUILocalizationRepo.GetLocalizedSysMsgByName("ret_ItemBagFull"));
        }
        else
        {
            if (UIManager.IsWindowOpen(WindowType.DailyQuest))
            {
                UIManager.GetWindowGameObject(WindowType.DailyQuest).GetComponent<UI_DailyActivity>().UpdateDonateData(guid, result);
            }
            UIManager.ShowSystemMessage(GUILocalizationRepo.GetLocalizedSysMsgByName("ret_donatesuccess"));
        }
    }
    #endregion

    #region Tooltip
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_Tooltip_DailyWeeklyLimit)]
    public void Ret_Tooltip_DailyWeeklyLimit(int itemID, int dailyGetLimit, int dailyUseLimit, int weeklyGetLimit, int weeklyUseLimit)
    {
        GameObject obj = UIManager.GetWindowGameObject(WindowType.DialogItemDetail);//UIManager.Get
        UI_DialogItemDetailToolTip tt = obj.GetComponent<UI_DialogItemDetailToolTip>();

        tt.SetDailyWeeklyLimit(itemID, dailyGetLimit, dailyUseLimit, weeklyGetLimit, weeklyUseLimit);
    }
    #endregion

    #region Social
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_SocialOnOpenFriendsMenu)]
    public void Ret_SocialOnOpenFriendsMenu(int resultCode)
    {
        SocialTestTool.DoAction(ctrl => ctrl.OpenSocialMenu_Completed());
    }
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_SocialRaiseRequest)]
    public void Ret_SocialRaiseRequest(int resultCode,string friendName)
    {
        var rlt = (Zealot.Common.Entities.Social.SocialResult)resultCode;
        SocialTestTool.DoAction(ctrl => ctrl.SocialRaiseRequest_Completed(rlt));

        if (rlt == Zealot.Common.Entities.Social.SocialResult.PlayerNameNotFound)
        {
            if (!string.IsNullOrEmpty(friendName))
                SocialController.OpenOkDialog("ret_social_PlayerNameNotFound", "name;" + friendName);
        }

        var selTarget = UIManager.GetWidget(HUDWidgetType.SelectTarget).GetComponent<HUD_SelectTarget>();
        if(selTarget!=null)
        {
            var funcMenu = selTarget.GetComponentInChildren<HUD_PortraitFunctions>(true);
            if(funcMenu!=null)
            {
                funcMenu.SocialUnlock();
            }
        }
    }
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_SocialAcceptRequest)]
    public void Ret_SocialAcceptRequest(int resultCode)
    {
        SocialTestTool.DoAction(ctrl => ctrl.SocialAcceptRequest_Completed((Zealot.Common.Entities.Social.SocialResult)resultCode));
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_SocialAcceptAllRequest)]
    public void Ret_SocialAcceptAllRequest(int resultCode)
    {
        SocialTestTool.DoAction(ctrl => ctrl.SocialAcceptAllRequest_Completed((Zealot.Common.Entities.Social.SocialResult)resultCode));
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_SocialRejectRequest)]
    public void Ret_SocialRejectRequest(int resultCode)
    {
        SocialTestTool.DoAction(ctrl => ctrl.SocialRejectRequest_Completed((Zealot.Common.Entities.Social.SocialResult)resultCode));
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_SocialRejectAllRequest)]
    public void Ret_SocialRejectAllRequest(int resultCode)
    {
        SocialTestTool.DoAction(ctrl => ctrl.SocialRejectAllRequest_Completed((Zealot.Common.Entities.Social.SocialResult)resultCode));
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_SocialAddBlack)]
    public void Ret_SocialAddBlack(int resultCode, string friendName)
    {
        var rlt = (Zealot.Common.Entities.Social.SocialResult)resultCode;
        SocialTestTool.DoAction(ctrl => ctrl.SocialAddBlack_Completed((Zealot.Common.Entities.Social.SocialResult)resultCode));

        if (rlt == Zealot.Common.Entities.Social.SocialResult.PlayerNameNotFound)
        {
            if (!string.IsNullOrEmpty(friendName))
                SocialController.OpenOkDialog("ret_social_PlayerNameNotFound", "name;" + friendName);
        }

        var selTarget = UIManager.GetWidget(HUDWidgetType.SelectTarget).GetComponent<HUD_SelectTarget>();
        if (selTarget != null)
        {
            var funcMenu = selTarget.GetComponentInChildren<HUD_PortraitFunctions>(true);
            if (funcMenu != null)
            {
                funcMenu.SocialUnlock();
            }
        }
    }
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_SocialRemoveBlack)]
    public void Ret_SocialRemoveBlack(int resultCode)
    {
        SocialTestTool.DoAction(ctrl => ctrl.SocialRemoveBlack_Completed((Zealot.Common.Entities.Social.SocialResult)resultCode));
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_SocialRemoveGood)]
    public void Ret_SocialRemoveGood(int resultCode)
    {
        SocialTestTool.DoAction(ctrl => ctrl.SocialRemoveGood_Completed((Zealot.Common.Entities.Social.SocialResult)resultCode));
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_SocialRaiseAllTempRequest)]
    public void Ret_SocialRaiseAllTempRequest()
    {
        SocialTestTool.DoAction(ctrl => ctrl.SocialRaiseAllTempRequest_Completed());
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_SocialClearTemp)]
    public void Ret_SocialClearTemp()
    {
        SocialTestTool.DoAction(ctrl => ctrl.SocialClearTemp_Completed());
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_SocialTest_AddTempFriendsSingle)]
    public void Ret_SocialTest_AddTempFriendsSingle(int resultCode)
    {
        SocialTestTool.DoAction(ctrl => ctrl.SocialTest_AddTempFriendsSingle_Completed((Zealot.Common.Entities.Social.SocialAddTempFriends_Result)resultCode));
    }

#if ZEALOT_DEVELOPMENT
    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_DebugFixTool)]
    public void Ret_DebugFixTool(string msg)
    {
        CharacterDebugTool.DoAction(ctrl => ctrl.DebugFixTool_Completed(msg));
    }

    [RPCMethod(RPCCategory.NonCombat, (byte)ServerNonCombatRPCMethods.Ret_DebugSelectTool)]
    public void Ret_DebugSelectTool(bool success,string charname,string result)
    {
        if (success)
        {
            try
            {
                var token = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JToken>(result);
                
                if (token.Type == Newtonsoft.Json.Linq.JTokenType.Object ||
                    token.Type == Newtonsoft.Json.Linq.JTokenType.Array)//在大量資料時json排版
                    result = token.ToString();
                else//使用JToken.ToString(Formatting.None) 避免因為單一字串JToken.ToString() 不會幫加上 " " 的情況
                    result = token.ToString(Formatting.None);
            }
            catch { }
        }
        CharacterDebugTool.DoAction(ctrl => ctrl.DebugSelectTool_Completed(success, charname, result));
    }
#endif

    #endregion
}
