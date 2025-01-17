﻿namespace Zealot.Server.Entities
{
    using ExitGames.Logging;
    using Kopio.JsonContracts;
    using Newtonsoft.Json;
    using Photon.LoadBalancing.Entities;
    using Photon.LoadBalancing.GameServer;
    using Photon.LoadBalancing.GameServer.Lottery;
    using Photon.LoadBalancing.GameServer.CombatFormula;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using Zealot.Common;
    using Zealot.Common.Datablock;
    using Zealot.Common.Entities;
    using Zealot.Common.RPC;
    using Zealot.Entities;
    using Zealot.Repository;
    using Zealot.Server.Rules;
    using Zealot.Server.SideEffects;

    public class Player : ComboSkillCaster
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private GameTimer mRespawnTimer;
        private GameTimer mSaveCharacterTimer;
        private GameTimer mManaRegenTimer;
        private List<AttackResult> mDamageResults;

        #region Stats Local Objects

        public Dictionary<LOTYPE, LocalObject> LOStats { get; private set; }

        public PlayerSynStats PlayerSynStats { get; set; }
        public SecondaryStats SecondaryStats { get; private set; }
        public InventoryStats[] InventoryStats { get; private set; }
        public EquipmentStats EquipmentStats { get; set; }
        public EquipmentCraftStats EquipmentCraftStats { get; private set; }
        public EquipFusionStats EquipFusionStats { get; private set; }
        public ItemHotbarStats ItemHotbarStats { get; set; }
        public QuestSynStatsServer QuestStats { get; private set; }
        public SkillSynStats SkillStats { get; private set; }
        public RealmStats RealmStats { get; private set; }
        public LocalCombatStats LocalCombatStats { get; private set; }
        public LocalSkillPassiveStats LocalSkillPassiveStats { get; private set; }
        public BuffTimeStats BuffTimeStats { get; private set; }
        //public ChatStats ChatStats { get; private set; }
        public SocialStats SocialStats { get; private set; }
        public WelfareStats WelfareStats { get; private set; }
        public SevenDaysStats SevenDaysStats { get; private set; }
        public QuestExtraRewardsStats QuestExtraRewardsStats { get; private set; }
        public DNAStats DNAStats { get; private set; }
        public LotteryInfoStats LotteryInfoStats { get; private set; } 
        public ExchangeShopSynStats ExchangeShopSynStats { get; private set; }
        public PortraitDataStats PortraitDataStats { get; private set; }
        public PartyStatsServer PartyStats { get; set; }
        public HeroStatsServer HeroStats { get; private set; }
        public DestinyClueSynStats DestinyClueStats { get; private set; }
        public DonateSynStats DonateStats { get; private set; }
        public AchievementStatsServer AchievementStats { get; private set; }

        //public BattleTimeStats BattleTimeStats { get; private set; }

        public PowerUpStats PowerUpStats { get; private set; }
        public MeridianStats MeridianStats { get; private set; }

        #endregion Stats Local Objects

        public RespawnJson mRespawnInfo;
        private Dictionary<byte, Queue<ChatMessage>> mMessageQueues;
        private Dictionary<byte, int> mChannelMsgDispatchLimit;

        private long[] mSkillCDEnd;

        // Side Effects
        private Dictionary<int, List<IPassiveSideEffect>> mTongbaoCostBuffPassiveSEs; //TongbaoCostBuff to SE

        private LotteryController mLotteryInvController;

        //Donate
        public DonateController DonateController;

        // Friends
        private StringBuilder friendSB;
        private StringBuilder friendRemoveSB;
        private Dictionary<string, string> friendsRecDict;
        private DateTime friendRecommendedCD;

        private long mTargetHealthLastUpdate;
        private static readonly long HEALTH_UPDATE_INTERVAL = 750; //in msec

        public GameClientPeer Slot { get; set; }

        public WardrobeController WardrobeController { get; private set; }
        public bool InspectMode = false;
        public bool IsInWorld = false;

        public WeaponType WeaponTypeUsed = WeaponType.Any;
        public MainWeaponAttribute WeaponAttr;

        public Dictionary<int, int> mSkillRecord;

        public Player() : base()
        {
            this.EntityType = EntityType.Player;
            mDamageResults = new List<AttackResult>();

            BuffTimeStats = new BuffTimeStats();

            mMessageQueues = new Dictionary<byte, Queue<ChatMessage>>();
            mMessageQueues.Add((byte)MessageType.World, new Queue<ChatMessage>());
            //mMessageQueues.Add((byte)MessageType.Faction, new Queue<ChatMessage>());
            mMessageQueues.Add((byte)MessageType.Guild, new Queue<ChatMessage>());
            mMessageQueues.Add((byte)MessageType.Party, new Queue<ChatMessage>());
            mMessageQueues.Add((byte)MessageType.Whisper, new Queue<ChatMessage>());
            mMessageQueues.Add((byte)MessageType.System, new Queue<ChatMessage>());
            mMessageQueues.Add((byte)MessageType.BroadcastMessage, new Queue<ChatMessage>());

            mChannelMsgDispatchLimit = new Dictionary<byte, int>();
            mChannelMsgDispatchLimit.Add((byte)MessageType.World, 10);
            //mChannelMsgDispatchLimit.Add((byte)MessageType.Faction, 10);
            mChannelMsgDispatchLimit.Add((byte)MessageType.Guild, 10);
            mChannelMsgDispatchLimit.Add((byte)MessageType.Party, 10);
            mChannelMsgDispatchLimit.Add((byte)MessageType.Whisper, 10);
            mChannelMsgDispatchLimit.Add((byte)MessageType.System, 10);
            mChannelMsgDispatchLimit.Add((byte)MessageType.BroadcastMessage, 10);

            mMonsterExpBonus = 0.0f;

            mSkillCDEnd = new long[5]; //5 skills

            mTongbaoCostBuffPassiveSEs = new Dictionary<int, List<IPassiveSideEffect>>();

            mTargetHealthLastUpdate = 0;

            mExperienceNeeded = 0;

            friendSB = new StringBuilder();
            friendRemoveSB = new StringBuilder();
            friendsRecDict = new Dictionary<string, string>();
            friendRecommendedCD = DateTime.Now;
            
            WardrobeController = new WardrobeController(this);
            
            DonateController = new DonateController(this);

            mSkillRecord = new Dictionary<int, int>();
        }

        public override int GetAccuracy()
        {
            return LocalSkillPassiveStats.Accuracy;
        }

        public override int GetAttack()
        {
            return LocalSkillPassiveStats.Attack;
        }

        public override int GetCritical()
        {
            return LocalSkillPassiveStats.Critical;
        }

        public override int GetCriticalDamage()
        {
            return LocalSkillPassiveStats.CriticalDamage;
        }

        public override int GetArmor()
        {
            return LocalSkillPassiveStats.Armor;
        }

        public override int GetEvasion()
        {
            return LocalSkillPassiveStats.Evasion;
        }

        public override int GetCocritical()
        {
            return LocalSkillPassiveStats.CoCritical;
        }

        public override int GetCocriticalDamage()
        {
            return LocalSkillPassiveStats.CoCriticalDamage;
        }

        public override void UpdateLocalSkillPassiveStats()
        {
            if (LocalSkillPassiveStats != null)
            {
                //PlayerCombatStats combatStats = (PlayerCombatStats)CombatStats;
                LocalSkillPassiveStats.HealthMax = GetHealthMax(); //this field is the healthMax with skilleffect.
                LocalSkillPassiveStats.Accuracy = CombatFormula.GetFinalAccuracy(this);
                LocalSkillPassiveStats.Armor = CombatFormula.GetFinalArmor(this);
                LocalSkillPassiveStats.Attack = CombatFormula.GetFinalAttack(this);
                LocalSkillPassiveStats.Evasion = CombatFormula.GetFinalEvasion(this);
                LocalSkillPassiveStats.Critical = CombatFormula.GetFinalCritical(this);
                LocalSkillPassiveStats.CoCritical = CombatFormula.GetFinalCoCritical(this);
                LocalSkillPassiveStats.CriticalDamage = CombatFormula.GetFinalCriticalDamage(this);
                LocalSkillPassiveStats.CoCriticalDamage = CombatFormula.GetFinalCoCriticalDamage(this);

                if (Slot != null)
                {
                    //Slot.mSevenDaysController.UpdateTask(Zealot.Common.NewServerActivityType.Playerfighting, LocalCombatStats.CombatScore);
                }
            }
        }

        public override void OnComputeCombatStats()
        {
            UpdateAchievement(AchievementObjectiveType.Strength, "-1", true, LocalCombatStats.Strength, false);
            UpdateAchievement(AchievementObjectiveType.Agility, "-1", true, LocalCombatStats.Agility, false);
            UpdateAchievement(AchievementObjectiveType.Dexterity, "-1", true, LocalCombatStats.Dexterity, false);
            UpdateAchievement(AchievementObjectiveType.Constitution, "-1", true, LocalCombatStats.Constitution, false);
            UpdateAchievement(AchievementObjectiveType.Intelligence, "-1", true, LocalCombatStats.Intelligence, false);
        }

        /// <summary>
        /// Inits player Local Objects
        /// </summary>
        /// <remarks>
        /// Only add LocalPlayerStats type local objects into collection.
        /// EquipmentStats should not be added to collection
        /// </remarks>
        public void InitLOStats()
        {
            LOStats = new Dictionary<LOTYPE, LocalObject>();

            SecondaryStats = new SecondaryStats();
            LOStats.Add(LOTYPE.SecondaryStats, SecondaryStats);

            InitInvStats();
            EquipmentStats = new EquipmentStats();

            ItemHotbarStats = new ItemHotbarStats();
            LOStats.Add(LOTYPE.ItemHotbarStats, ItemHotbarStats);

            DestinyClueStats = new DestinyClueSynStats();
            LOStats.Add(LOTYPE.DestinyClueSynStats, DestinyClueStats);

            QuestStats = new QuestSynStatsServer();
            LOStats.Add(LOTYPE.QuestSynStats, QuestStats);

            SkillStats = new SkillSynStats();
            LOStats.Add(LOTYPE.SkillStats, SkillStats);

            RealmStats = new RealmStats();
            LOStats.Add(LOTYPE.RealmStats, RealmStats);

            LocalCombatStats = new LocalCombatStats();
            LOStats.Add(LOTYPE.LocalCombatStats, LocalCombatStats);

            LocalSkillPassiveStats = new LocalSkillPassiveStats();
            LOStats.Add(LOTYPE.LocalSkillPassiveStats, LocalSkillPassiveStats);
            //localSkillPassiveStats = LocalSkillPassiveStats; // Set the Reference in Actor

            HeroStats = new HeroStatsServer();
            LOStats.Add(LOTYPE.HeroStats, HeroStats);

            LOStats.Add(LOTYPE.BuffTimeStats, BuffTimeStats);

            //ChatStats = new ChatStats();
            //LOStats.Add(LOTYPE.ChatStats, ChatStats);

            SocialStats = new SocialStats(true);
            LOStats.Add(LOTYPE.SocialStats, SocialStats);

            WelfareStats = new WelfareStats();
            //LOStats.Add(LOTYPE.WelfareStats, WelfareStats);

            SevenDaysStats = new SevenDaysStats();
            //LOStats.Add(LOTYPE.SevenDaysStats, SevenDaysStats);

            QuestExtraRewardsStats = new QuestExtraRewardsStats();
            //LOStats.Add(LOTYPE.QuestExtraRewardsStats, QuestExtraRewardsStats);

            DNAStats = new DNAStats();
            //LOStats.Add(LOTYPE.DNAStats, DNAStats);

            LotteryInfoStats = new LotteryInfoStats();
            //LOStats.Add(LOTYPE.LotteryShopItemsTabStats, LotteryInfoStats);

            ExchangeShopSynStats = new ExchangeShopSynStats();
            //LOStats.Add(LOTYPE.ExchangeShopStats, ExchangeShopSynStats);

            PortraitDataStats = new PortraitDataStats();
            //LOStats.Add(LOTYPE.PortraitDataStats, PortraitDataStats);
            //BattleTimeStats = new BattleTimeStats();

            PowerUpStats = new PowerUpStats();
            LOStats.Add(LOTYPE.PowerUpStats, PowerUpStats);

            MeridianStats = new MeridianStats();
            LOStats.Add(LOTYPE.MeridianStats, MeridianStats);

            EquipmentCraftStats = new EquipmentCraftStats();
            LOStats.Add(LOTYPE.EquipmentCraftStats, EquipmentCraftStats);

            EquipFusionStats = new EquipFusionStats();
            LOStats.Add(LOTYPE.EquipFusionStats, EquipFusionStats);

            DonateStats = new DonateSynStats();
            LOStats.Add(LOTYPE.DonateSynStats, DonateStats);

            AchievementStats = new AchievementStatsServer();
            LOStats.Add(LOTYPE.AchievementStats, AchievementStats);
        }

        private void InitInvStats()
        {
            int maxcount = (int)InventorySlot.MAXSLOTS;
            int totalcount = maxcount / (int)InventorySlot.COLLECTION_SIZE;
            if (maxcount % (int)InventorySlot.COLLECTION_SIZE != 0) // If got remainder
                ++totalcount;
            InventoryStats = new InventoryStats[totalcount];
            for (int i = 0; i < totalcount; ++i)
            {
                LOTYPE type = (LOTYPE)((int)LOTYPE.InventoryStats + i);
                InventoryStats[i] = new InventoryStats(type);
                LOStats.Add(type, InventoryStats[i]);
            }
        }

        public override void SetInstance(GameLogic instance)
        {
            base.SetInstance(instance);
            IsInWorld = instance.mIsWorld;
            RealmController realmController = instance.mRealmController;
            if (realmController != null)
            {
                RealmPVPType pvpType = realmController.mRealmInfo.pvptype;
                switch (pvpType)
                {
                    case RealmPVPType.Peace:
                        PlayerSynStats.Team = -1;
                        break;
                    case RealmPVPType.FreeForAll:
                        PlayerSynStats.Team = 0;
                        break;
                    case RealmPVPType.Guild:
                        PlayerSynStats.Team = SecondaryStats.guildId;
                        break;
                    case RealmPVPType.Faction:
                        PlayerSynStats.Team = PlayerSynStats.faction;
                        break;
                }
            }
            Slot.mPortalExit = null;
        }

        public void InitSaveCharacterTimer()
        {
            DateTime now = DateTime.Now;
            long elapsedTotalMiliseconds = (long)(now - Slot.mLastSaveCharacterDT).TotalMilliseconds;
            long time_to_save = 120000 - elapsedTotalMiliseconds;
            if (time_to_save <= 0)
                OnSaveCharacterTimerUp(false);
            else
                mSaveCharacterTimer = mInstance.SetTimer(time_to_save, OnSaveCharacterTimerUp, true);
        }

        private void OnSaveCharacterTimerUp(object arg)
        {
            mSaveCharacterTimer = mInstance.SetTimer(120000, OnSaveCharacterTimerUp, true);
            if ((bool)arg)
                SaveToCharacterData(false);
            Slot.SaveCharacter(true);
        }

        public void ForceSaveCharacter()
        {
            if (mSaveCharacterTimer != null)
            {
                mInstance.StopTimer(mSaveCharacterTimer);
                OnSaveCharacterTimerUp(true);
            }
        }

        #region Implement abstract methods

        public override void SpawnAtClient(GameClientPeer peer)
        {
            bool isLocal = peer == Slot;
            peer.ZRPC.CombatRPC.SpawnPlayerEntity(isLocal, mnOwnerID, PlayerSynStats.name, mnPersistentID,
                PlayerSynStats.Gender, Position.ToRPCPosition(), Forward.ToRPCDirection(), peer);
        }

        #endregion Implement abstract methods

        public override void ResetSyncStats()
        {
            base.ResetSyncStats();
            if (EquipmentStats.IsDirty())
                EquipmentStats.Reset();
            foreach (var stat in LOStats.Values)
            {
                if (stat.IsDirty())
                    stat.Reset();
            }
        }

        public override void AddEntitySyncStats(GameClientPeer peer)
        {
            base.AddEntitySyncStats(peer);
            peer.ZRPC.LocalObjectRPC.AddLocalObject((byte)LOCATEGORY.EntitySyncStats, GetPersistentID(), EquipmentStats, peer);
        }

        public override void UpdateEntitySyncStats(GameClientPeer peer)
        {
            base.UpdateEntitySyncStats(peer);
            if (EquipmentStats.IsDirty())
                peer.ZRPC.LocalObjectRPC.UpdateLocalObject((byte)LOCATEGORY.EntitySyncStats, GetPersistentID(), EquipmentStats, peer);
        }

        public override void UpdateLocalObject(GameClientPeer peer)
        {
            //Check for target health/healthmax changes
            long now = EntitySystem.Timers.GetSynchronizedTime();
            if (now - mTargetHealthLastUpdate > HEALTH_UPDATE_INTERVAL)
            {
                mTargetHealthLastUpdate = now;

                int currHealth = GetHealth();
                if (LocalCombatStats.Health != currHealth) //Player's own health (healthmax is immediate via combatstats)
                {
                    LocalCombatStats.Health = currHealth;
                }
            }

            foreach (var stat in LOStats.Values)
            {
                if (stat.IsDirty())
                    peer.ZRPC.LocalObjectRPC.UpdateLocalObject((byte)LOCATEGORY.LocalPlayerStats, GetPersistentID(), stat, peer);
            }
        }

        public override void AddLocalObject(GameClientPeer peer)
        {
            foreach (var stat in LOStats.Values)
            {
                peer.ZRPC.LocalObjectRPC.AddLocalObject((byte)LOCATEGORY.LocalPlayerStats, GetPersistentID(), stat, peer);
            }

            AddPartyLocalObject(peer);
            //AddGuildLocalObject(peer);
            //peer.ZRPC.LocalObjectRPC.AddLocalObject((byte)LOCATEGORY.SharedStats, GetPersistentID(), TongbaoCostBuff.CostBuffData, peer);
        }

        public void AddGuildLocalObject(GameClientPeer peer)
        {
            if (SecondaryStats.guildId != 0)
            {
                peer.ZRPC.LocalObjectRPC.AddLocalObject((byte)LOCATEGORY.SharedStats, GetPersistentID(), GuildRules.GuildList[SecondaryStats.guildId], peer);
            }
        }

        public void AddPartyLocalObject(GameClientPeer peer)
        {
            if (PlayerSynStats.Party > 0)
            {
                PartyStatsServer partyStats = PartyRules.GetPartyById(PlayerSynStats.Party);
                PartyStats = partyStats;
                peer.ZRPC.LocalObjectRPC.AddLocalObject((byte)LOCATEGORY.SharedStats, GetPersistentID(), partyStats, peer);
            }
        }

        public void AddToChatMessageQueue(ChatMessage msg)
        {
            mMessageQueues[msg.mMsgType].Enqueue(msg);
        }

        public void DispatchChatMessages(GameClientPeer peer)
        {
            int msgcount = 0;
            int channelmsgcount = 0;
            foreach (KeyValuePair<byte, Queue<ChatMessage>> kvp in mMessageQueues)
            {
                Queue<ChatMessage> qMsgs = kvp.Value;
                while (qMsgs.Count > 0 && (channelmsgcount < mChannelMsgDispatchLimit[kvp.Key]))
                {
                    ChatMessage msg = qMsgs.Dequeue();
                    MessageType messagetype = (MessageType)msg.mMsgType;
                    if (messagetype == MessageType.BroadcastMessage)
                        peer.ZRPC.CombatRPC.BroadcastMessageToClient(msg.mBroadcastMsgType, msg.mMessage, peer);
                    else
                        peer.ZRPC.CombatRPC.ServerSendChatMessage(kvp.Key, msg.mMessage, msg.mSender, msg.mWhisperTo,
                                                                  msg.mPortraitId, msg.mJobsect, msg.mVipLvl,
                                                                  msg.mFaction, msg.mIsVoiceChat, peer);
                    ++channelmsgcount;
                    ++msgcount;
                }
            }
        }

        public long GetSynchronizedTime()
        {
            return EntitySystem.Timers.GetSynchronizedTime();
        }

        public DateTime GetSynchronizedServerDT()
        {
            return EntitySystem.Timers.GetSynchronizedServerDT();
        }

        public override PlayerSynStats GetPlayerStats()
        {
            return PlayerSynStats;
        }

        private void LogStr(string message, Player attacker)
        {
            GameClientPeer peer = Slot;
            Zealot.Logging.Client.LogClasses.PlayerKillPlayer log = new Zealot.Logging.Client.LogClasses.PlayerKillPlayer();
            log.userId = peer.mUserId;
            log.charId = peer.GetCharId();
            log.message = message;
            log.player = peer.mChar;
            log.killer = attacker.Slot.mChar;
            log.realmID = mInstance.mRealmController.mRealmInfo.id;

            log.realmName = mInstance.mRealmController.mRealmInfo.excelname;
            var ignoreAwait = Zealot.Logging.Client.LoggingAgent.Instance.LogAsync(log);
        }

        public override void OnKilled(IActor attacker)
        {
            StopAllSideEffects();
            //TODO: save logstr
            //string logstr = string.Format("[{0}][{1}][{2}][{3}][{4}]", DateTime.Now.ToString(), Name, "Was Killed by", ((Actor)attacker).IsPlayer() ? attacker.Name : ((Actor)attacker).GetPersistentID().ToString(), mInstance.mCurrentLevelID);
            Actor attackerActor = (Actor)attacker;
            if (attackerActor.IsPlayer())
            {
                string logstr2 = string.Format("[{0}][{1}][{2}][{3}][{4}]", DateTime.Now.ToString(), attacker.Name, "Killed", Name, mInstance.mCurrentLevelId);
                LogStr(logstr2, attacker as Player);
            }

            if (mInstance.IsDoingTutorialRealm())
            {
                SetHealth(GetHealthMax()); // Restore health if player killed in first guide realm.
                return;
            }
            //log.InfoFormat("[{0}]: onkilled {1} ({2}) {3} killer {4} ({5})", mInstance.ID, Name, GetPersistentID(), ((Actor)attacker).IsPlayer() ? "Player": "", attacker.Name, ((Actor)attacker).GetPersistentID());

            base.OnKilled(attacker);
            RealmController realmController = mInstance.mRealmController;
            if(realmController != null)
            {
                int respawnId = realmController.mRealmInfo.respawn;
                mRespawnInfo = RespawnRepo.GetRespawnDataByID(respawnId);
                string attackername = attacker.Name == null ? "" : attacker.Name;
                Slot.ZRPC.CombatRPC.OnPlayerDead(attackername, (byte)respawnId, Slot);

                if(mRespawnInfo.respawntype == RespawnType.City)
                {
                    if(mRespawnInfo.countdown != -1)
                    {
                        int realCD = mRespawnInfo.countdown * 1000;
                        mRespawnTimer = mInstance.SetTimer(realCD, (arg) =>
                        {
                            RespawnAtCity();
                            int mapId = mInstance.mCurrentLevelId;
                            DeathRules.LogDeathRespawnType("Timer", mapId, Slot);
                        }, null);
                    }
                }
                else if(mRespawnInfo.respawntype == RespawnType.SafeZone)
                {
                    if(mRespawnInfo.countdown != -1)
                    {
                        int realCD = mRespawnInfo.countdown * 1000;
                        mRespawnTimer = mInstance.SetTimer(realCD, (arg) =>
                        {
                            RespawnAtSafezone();
                            int mapId = mInstance.mCurrentLevelId;
                            DeathRules.LogDeathRespawnType("Timer", mapId, Slot);
                        }, null);
                    }
                }
                realmController.OnPlayerDead(this, attacker as Actor);
            }
            if (attackerActor.IsPlayer())
            {
                Player killer = attacker as Player;
                if (killer.PlayerSynStats.faction != PlayerSynStats.faction)
                {
                    killer.Slot.CharacterData.FactionKill++;
                    Slot.CharacterData.FactionDeath++;
                }
                killer.UpdateAchievement(AchievementObjectiveType.PVPKill);
                UpdateAchievement(AchievementObjectiveType.PVPDie);
            }
            UpdateAchievement(AchievementObjectiveType.DieNumber);
        }

        private long lastComboTime = 0;

        public void HandleHitCombo()
        {
            long curr = EntitySystem.Timers.GetSynchronizedTime();
            if (curr - lastComboTime < CombatUtils.COMBOTHIT_TIMEOUT)
            {
                if(LocalCombatStats.ComboHit < 999)
                {
                    LocalCombatStats.ComboHit++;
                }
                else
                {
                    LocalCombatStats.ComboHit = 999;
                }
            }
            else
            {
                LocalCombatStats.ComboHit = 1;
            }
            //Slot.ZRPC.CombatRPC.OnHitComboChange(hitComboCount, Slot);
            lastComboTime = curr;
        }

        public void RespawnAtCity()
        {
            if (mRespawnTimer != null)
            {
                mInstance.StopTimer(mRespawnTimer);
                mRespawnTimer = null;
            };
            if (Slot.TransferToLastWorld(PlayerSynStats.Level))
                ResetStatsOnRespawn();
            else
            {
                mInstance.SetPlayerPosToSpawnPos(this);
                Respawn();
                HeroStats.SetSpawnedHeroPosition(this);
            }
        }

        public void RespawnAtSafezone()
        {
            if (mRespawnTimer != null)
            {
                mInstance.StopTimer(mRespawnTimer);
                mRespawnTimer = null;
            }
            if (mRespawnInfo.respawntype == RespawnType.SafeZone)
            {
                StartInvincible(1);
            }
            mInstance.SetPlayerPosToSpawnPos(this);
            Respawn();
            HeroStats.SetSpawnedHeroPosition(this);
        }

        public void RespawnOnSpot()
        {
            if (mRespawnTimer != null)
            {
                mInstance.StopTimer(mRespawnTimer);
                mRespawnTimer = null;
            }
            StartInvincible(1); // Set invincible for 1 second
            Respawn();
        }

        public void ResetStatsOnRespawn()
        {
            log.InfoFormat("[{0}]: Respawn ({1})", mInstance.ID, GetPersistentID());
            SetHealth(GetHealthMax() / 5);
            SetMana(GetManaMax());
        }

        public void Respawn()
        {
            ResetStatsOnRespawn();
            Slot.ZRPC.CombatRPC.RespawnPlayer(Position.ToRPCPosition(), Position.ToRPCDirection(), Slot);
        }

        public override void QueueDmgResult(AttackResult res)
        {
            //attacker res handled in client
            if (mDamageResults.Count < 100)
                mDamageResults.Add(res);
        }

        public void UnreliableLocalEntityUpdate(GameClientPeer peer)
        {
            int count = mDamageResults.Count;
            for (int i = 0; i < count; ++i)
            {
                AttackResult res = mDamageResults[i];
                peer.ZRPC.UnreliableCombatRPC.EntityOnDamage(res.TargetPID, 0, res.AttackInfo, res.RealDamage, res.LabelNum, res.Skillid, peer);
            }
            mDamageResults.Clear();
        }

        public void CheckSafeZoneSphere(bool enter, int id)
        {
            //LocalCombatStats.isInSafeZone = false;
            SafeZoneData mydata;
            if (SafeZoneInfo.mySafeZoneData.TryGetValue(id, out mydata) == true)
            {
                //Vector3 offsetPos;
                //if (enter == true)
                //    offsetPos = Position + (SafeZoneInfo.mySafeZoneData[id].pos - Position).normalized * 2;
                //else
                //    offsetPos = Position + (Position - SafeZoneInfo.mySafeZoneData[id].pos).normalized * 2;

                //float mydist = Vector3.Distance(SafeZoneInfo.mySafeZoneData[id].pos, offsetPos);
                //if (mydist <= SafeZoneInfo.mySafeZoneData[id].radius + Radius)
                //{
                //    LocalCombatStats.isInSafeZone = true;
                //}
                //else
                //{
                //    LocalCombatStats.isInSafeZone = false;
                //}
                LocalCombatStats.IsInSafeZone = enter;
            }
            else
            {
                LocalCombatStats.IsInSafeZone = false;
            }
        }

        public void CheckSafeZoneBox(bool enter, int id)//bool enter,string safeZoneName
        {
            SafeZoneData mydata;
            if (SafeZoneInfo.mySafeZoneData.TryGetValue(id, out mydata) == true)
            {
                //float dist = 0;
                //Vector3 offsetPos;

                //if (enter == true)
                //    offsetPos = Position + (SafeZoneInfo.mySafeZoneData[id].pos - Position).normalized * 2;
                //else
                //    offsetPos = Position + (Position - SafeZoneInfo.mySafeZoneData[id].pos).normalized * 2;

                //Vector3 bmin = SafeZoneInfo.mySafeZoneData[id].pos - (SafeZoneInfo.mySafeZoneData[id].size) / 2;
                //Vector3 bmax = SafeZoneInfo.mySafeZoneData[id].pos + (SafeZoneInfo.mySafeZoneData[id].size) / 2;

                //if (offsetPos.x < bmin.x)
                //{
                //    dist += Mathf.Pow(offsetPos.x - bmin.x, 2);
                //}
                //else if (offsetPos.x > bmax.x)
                //{
                //    dist += Mathf.Pow(offsetPos.x - bmax.x, 2);
                //}

                //if (offsetPos.z < bmin.z)
                //{
                //    dist += Mathf.Pow(offsetPos.z - bmin.z, 2);
                //}
                //else if (offsetPos.z > bmax.z)
                //{
                //    dist += Mathf.Pow(offsetPos.z - bmax.z, 2);
                //}

                //if (dist <= Mathf.Pow(Radius, 2))
                //{
                //    LocalCombatStats.isInSafeZone = true;
                //}
                //else
                //{
                //    LocalCombatStats.isInSafeZone = false;
                //}
                LocalCombatStats.IsInSafeZone = enter;
            }
            else
            {
                LocalCombatStats.IsInSafeZone = false;
            }
        }

        public override void OnAttacked(IActor attacker, int aggro)
        {
            base.OnAttacked(attacker, aggro);
            CombatStarted();
        }

        public override void OnDamage(IActor attacker, AttackResult res, bool pbasicattack)
        {
            Monster boss = attacker as Monster;
            if (boss != null && boss.mArchetype.monstertype == MonsterType.Boss)
                boss.AddDamageToPlayer(Name, res.RealDamage);
            base.OnDamage(attacker, res, pbasicattack);
        }

        private long InCombatTime = 0;

        public void CombatStarted()
        {
            InCombatTime = 6000;
            if (!LocalCombatStats.IsInCombat)
                LocalCombatStats.IsInCombat = true;
        }

        private long mBattleTimeCountdown = 10000;

        public override void Update(long dt)
        {
            base.Update(dt);

            if (InCombatTime > 0)
            {
                InCombatTime -= dt;
                if (InCombatTime <= 0)
                    LocalCombatStats.IsInCombat = false;
                if (IsInWorld && PlayerSynStats.Party == 0)
                {
                    mBattleTimeCountdown -= dt;
                    if (mBattleTimeCountdown <= 0)
                        DeductBattleTime();
                }
            }

            StartManaRegen();
        }

        public void DeductBattleTime()
        {
            mBattleTimeCountdown = 10000;
            Slot.CharacterData.BattleTime -= 10;
            SecondaryStats.BattleTime = Slot.CharacterData.BattleTime;
        }

        public void DeductBattleTime(int timeInSec)
        {
            Slot.CharacterData.BattleTime -= timeInSec;
            SecondaryStats.BattleTime = Slot.CharacterData.BattleTime;
        }

        public void NewDay()
        {
            Slot.CharacterData.ResetOnNewDay();
            SecondaryStats.ResetOnNewDay(Slot.CharacterData);
            RealmStats.ResetOnNewDay();
            Slot.mInventory.NewDayReset();

            Slot.CharacterData.GuildQuests.ResetOnNewDay();
            UpdateGuildQuestDailyTimes();

            //Slot.mWelfareCtrlr.ResetOnNewDay();
            //mLotteryInvController.ResetOnNewDay();
            //Slot.CharacterData.ExchangeShopInv.NewDayReset();
            //Slot.mQuestExtraRewardsCtrler.ResetOnNewDay();
            Slot.QuestController.ResetOnNewDay();
        }

        public void StartManaRegen()
        {
            if (mManaRegenTimer != null || GetMana() == GetManaMax())
                return;

            int amt = (int)CombatStats.GetField(FieldName.ManaRegen);
            if (amt > 0)  // ensure player can regen mana before starting timer
            {
                mManaRegenTimer = mInstance.SetTimer(10000, OnManaRegenTimerUp, null);
                mManaRegenTimer.AutoReset = true;
            }
        }

        private void OnManaRegenTimerUp(object arg)
        {
            if (mManaRegenTimer == null) return;

            // get regen value here again in case value changed after starting timer
            int amt = (int)CombatStats.GetField(FieldName.ManaRegen);
            AddToMana(amt);

            if (GetMana() == GetManaMax())
            {
                mInstance.QueueDestoryTimer(mManaRegenTimer);
                mManaRegenTimer = null;
            }
        }

        public void SaveToCharacterData(bool exitroom)
        {
            if (mInstance.IsDoingTutorialRealm())
                return;
            log.InfoFormat("[{0}]: save ({1}) {2}", mInstance.ID, GetPersistentID(), Name);

            CharacterData characterData = Slot.CharacterData;
            characterData.ProgressLevel = PlayerSynStats.Level;
            characterData.portraitID = PlayerSynStats.PortraitID;
            characterData.CharInfoData.StatPoint = LocalCombatStats.StatsPoint;
            characterData.CharInfoData.Str = (int)CombatStats.GetField(FieldName.StrengthBase);
            characterData.CharInfoData.Agi = (int)CombatStats.GetField(FieldName.AgilityBase);
            characterData.CharInfoData.Con = (int)CombatStats.GetField(FieldName.ConstitutionBase);
            characterData.CharInfoData.Int = (int)CombatStats.GetField(FieldName.IntelligenceBase);
            characterData.CharInfoData.Dex = (int)CombatStats.GetField(FieldName.DexterityBase);

            int lastlevelid = mInstance.mCurrentLevelId;
            string roomguid = mInstance.mRoom.Guid;
            if (exitroom)
            {
                if (IsInWorld)
                {
                    characterData.LastWorldId = mInstance.mCurrentLevelId;
                    characterData.LastWorldPos[0] = Position.x;
                    characterData.LastWorldPos[1] = Position.y;
                    characterData.LastWorldPos[2] = Position.z;
                }
                /*else
                {
                    bool kickToCity = false;
                    if (InspectMode)
                        kickToCity = true;
                    else if (!IsAlive())
                    {
                        ResetStatsOnRespawn();
                        if (mRespawnInfo.respawntype == RespawnType.SafeZone)
                            mInstance.SetPlayerPosToSpawnPos(this);
                        else
                            kickToCity = true;
                    }
                    if (kickToCity)
                    {
                        lastlevelid = LevelRepo.GetInfoByName(RealmRepo.GetCity(PlayerSynStats.Level)).id;
                        Position = Vector3.zero;
                        roomguid = "";
                    }
                }*/
            }

            //characterData.EquipScore = LocalCombatStats.CombatScore;
            characterData.Health = GetHealth();
            characterData.Mana = GetMana();
            characterData.Experience = SecondaryStats.experience;
            characterData.RoomGuid = roomguid;
            characterData.LastLevelId = lastlevelid;           
            characterData.LastLevelPos[0] = Position.x;
            characterData.LastLevelPos[1] = Position.y;
            characterData.LastLevelPos[2] = Position.z;
            characterData.LastDirection[0] = Forward.x;
            characterData.LastDirection[1] = Forward.y;
            characterData.LastDirection[2] = Forward.z;

            characterData.ItemHotBarData = ItemHotbarStats.ToString();
            characterData.InspectCombatStats.Update(LocalCombatStats);
            characterData.CurrencyExchangeTime = SecondaryStats.CurrencyExchangeTime;
            characterData.GuildId = SecondaryStats.guildId;
            characterData.GuildRank = SecondaryStats.guildRank;
            characterData.GuildShopBuyCount = SecondaryStats.guildShopBuyCount;
            characterData.GuildSMBossEntry = SecondaryStats.GuildSMBossEntry;
            characterData.GuildSMBossExtraEntry = SecondaryStats.GuildSMBossExtraEntry;
            characterData.LeaveGuildCDEndTick = SecondaryStats.guildLeaveGuildCDEnd;
            characterData.GuildDreamHouseUsed = SecondaryStats.GuildDreamHouseUsed;
            characterData.GuildDreamHouseCollected = SecondaryStats.GuildDreamHouseCollected;
            characterData.LastFreeLotteryRoll = SecondaryStats.lastFreeLotteryRoll;

            characterData.RandomBoxTimeTick = SecondaryStats.RandomBoxTimeTick;
            characterData.costbuffid = SecondaryStats.costbuffid;
            characterData.costbuffgold = SecondaryStats.costbuffgold;
            characterData.tutorialreddot = SecondaryStats.tutorialreddot;

            characterData.JobSect = PlayerSynStats.jobsect;

            //if (exitroom) // Logout
            //{
            //    //Bag
            //    TimeSpan duration = new TimeSpan(DateTime.Now.Ticks - SecondaryStats.DTSlotOpenTime);
            //    int minutes = (int)duration.TotalMinutes; //drop off the seconds
            //    characterData.InvSlotsData.OpenSlotTimePassed = SecondaryStats.OpenSlotTimePassed + minutes;
            //    characterData.InvSlotsData.DTSlotOpenTime = DateTime.Now.Ticks; //reset
            //}
            //else
            //{
            //    characterData.InvSlotsData.OpenSlotTimePassed = SecondaryStats.OpenSlotTimePassed;
            //}

            // Currency
            characterData.CurrencyInventory.SaveToInventoryData(SecondaryStats, PlayerSynStats);

            SkillInventoryData skillInventory = characterData.SkillInventory;
            skillInventory.basicAttack1SId = SkillStats.basicAttack1SId;
            //skillInventory.SkillInvCount = SkillStats.SkillInvCount;
            skillInventory.equipGroup = SkillStats.EquipGroup;
            skillInventory.autoGroup = SkillStats.AutoGroup;
            skillInventory.EquipSize = SkillStats.EquipSize;

            int count = SkillStats.EquippedSkill.Count;
            for (int i = 0; i < count; ++i)
            {
                skillInventory.EquippedSkill[i] = (int)SkillStats.EquippedSkill[i];
                skillInventory.AutoSkill[i] = (int)SkillStats.AutoSkill[i];
            }

            foreach(var item in mSkillRecord)
            {
                if (skillInventory.m_SkillInventory.ContainsKey(item.Key))
                {
                    skillInventory.m_SkillInventory[item.Key] = item.Value;
                }
                else
                    skillInventory.AddSkillToBag(item.Key, item.Value);
            }

            SideEffectInventoryData sideeffectInv = characterData.SideEffectInventory;
            sideeffectInv.SEList.Clear();
            foreach (SpecialSE se in mPersistentSideEffects)
            {
                if (se != null /*&& se.mSideeffectData.persistentonlogout*/)
                    sideeffectInv.SEList.Add(new SideEffectDBInfo(se.mSideeffectData.id, se.mTotalElapsedTime));
            }

            // Realm
            characterData.RealmInventory.SaveToInventoryData(RealmStats);

            // Quest
            Slot.QuestController.SaveQuestInventory(characterData.QuestInventory);
            Slot.DestinyClueController.SaveDestinyClueInventory(characterData.ClueInventory);

            //serialise the m_SideEffectList balyat
            //format -> seid, number of stacks
            sideeffectInv.SERecords.Clear();
            foreach (var iter in m_SideEffectList)
            {
                sideeffectInv.SERecords.Add(iter.Key);
                sideeffectInv.SERecords.Add(iter.Value);
            }

            // Hero
            HeroStats.SaveToInventory(characterData.HeroInventory);

            // Achievement
            AchievementStats.SaveToInventory(Slot.AchievementInvData);

            // Chatroom
            //characterData.ChatInventory.SaveToInventoryData(ChatStats);

            // Social
            //SocialStats.SaveToInventoryData(Slot.mSocialInventory);

            //SocialInventoryData socialInv = Slot.mSocialInventory;
            //IList<string> socialInvFriendList = socialInv.friendList;
            //socialInvFriendList.Clear();
            //foreach (object friend in SocialStats.friendList)
            //{
            //    if (friend == null)
            //        continue;
            //    socialInvFriendList.Add(friend as string);
            //}
            //socialInvFriendList = socialInv.friendRequestList;
            //socialInvFriendList.Clear();
            //foreach (object friendRequest in SocialStats.friendRequestList)
            //{
            //    if (friendRequest == null)
            //        continue;
            //    socialInvFriendList.Add(friendRequest as string);
            //}

            //characterData.ItemKindInv = Slot.mInventory.GetItemKindData();
            //characterData.StoreData.list_store.Clear();
            //for (int i = 0; i < StoreSynStats.list_store.Count; i++)
            //{
            //    //if (i >= characterData.StoreData.list_store.Count)
            //    //    break;

            //    if (StoreSynStats.list_store[i] != null)
            //    {
            //        StoreCategory sc = JsonConvert.DeserializeObject<StoreCategory>((string)StoreSynStats.list_store[i]);
            //        //characterData.StoreData.list_store[i] = sc;
            //        characterData.StoreData.list_store.Add(sc);
            //    }
            //    else
            //    {
            //        characterData.StoreData.list_store.Add(null);
            //    }
            //}

            // Welfare
            //characterData.WelfareInventory.SaveToInventory(WelfareStats);
            //WelfareRules.SaveToDB();
            //WelfareStats.onlineDuration += Slot.mWelfareCtrlr.GetOnlineDuration();
            //characterData.WelfareInventory.OnlineDuration += Slot.mWelfareCtrlr.GetOnlineDuration();

            // SevenDays
            //characterData.SevenDaysInventory.SaveToInventory(SevenDaysStats);
            //characterData.QuestExtraRewardsInventory.SaveToInventory(QuestExtraRewardsStats);
            //WardrobeController.Save();

            // DNA
            characterData.DNAInventory.SaveToInventory(DNAStats);

            //ExchangeShop
            //characterData.ExchangeShopInv.SaveToExchangeShopInventory(ExchangeShopSynStats.exchangeLeftMapJsonString);

            //PortraitData
            //characterData.PortraitData.SaveToPortraitData(PortraitDataStats.portraitDataInfoString);

            // PowerUp
            characterData.PowerUpInventory.SaveToPowerUpInventoryData(PowerUpStats);
            characterData.PowerUpInventory.SaveToMeridianInventoryData(MeridianStats);

            //EquipmentCraft
            characterData.EquipmentCraftInventory.SaveToInventoryData(EquipmentCraftStats);

            //EquipFushion
            characterData.EquipFusionInventory.SaveToInventoryData(EquipFusionStats);

            Slot.mCanSaveDB = true;
        }

        public override bool AddSpecialSideEffect(SpecialSE se)
        {
            bool added = base.AddSpecialSideEffect(se);
            if (added)
            {
                for (int i = 0; i < BuffTimeStats.MAX_EFFECTS; ++i)
                {
                    if ((int)BuffTimeStats.Persistents[i] == 0)
                    {
                        BuffTimeStats.Persistents[i] = se.mSideeffectData.id;
                        BuffTimeStats.PersistentsDur[i] = (int)(se.GetTimeRemaining() * 0.001);
                        break;
                    }
                }
            }
            return added;
        }

        public override void RemoveSpecialSideEffect(SpecialSE se)
        {
            base.RemoveSpecialSideEffect(se);
            for (int i = 0; i < BuffTimeStats.MAX_EFFECTS; ++i)
            {
                //no need to care about the index of the same id if multiply.
                if ((int)BuffTimeStats.Persistents[i] == se.mSideeffectData.id)
                {
                    BuffTimeStats.Persistents[i] = 0;
                    BuffTimeStats.PersistentsDur[i] = 0;
                    break;
                }
            }
        }

        public void OnNPCKilled(CombatNPCJson npc, float exp)
        {
            if (exp > 0)
            {
                int monsterLvl = npc.level;
                if (IsInParty())
                {
                    List<Player> partyPlayers = PartyStats.GetSameInstancePartyMembers(Name, mInstance);
                    int count = partyPlayers.Count;
                    int totalValidPlayers = count + 1;
                    if (totalValidPlayers == 1)
                        AddMonsterExperience(exp, monsterLvl);
                    else
                    {
                        float expEach = exp / totalValidPlayers;
                        AddMonsterExperience(expEach, monsterLvl);
                        for (int index = 0; index < count; ++index)
                            partyPlayers[index].AddMonsterExperience(expEach, monsterLvl);
                    }
                }
                else
                    AddMonsterExperience(exp, monsterLvl);
            }

            Slot.QuestController.KillCheck(npc.id, 1);
        }

        #region PlayerStats Methods

        private int mExperienceNeeded;
        private int mJobExperienceNeeded;
        private int mMaxLevel;
        private int mMaxJobLevel;
        private float mMonsterExpBonus;

        public void AddMonsterExpBonus(float bonus)
        {
            mMonsterExpBonus += bonus;
            if (mMonsterExpBonus < 0)
                mMonsterExpBonus = 0;
        }

        public void InitLevelUpCost()
        {
            mMaxLevel = CharacterLevelRepo.GetMaxLevel();
            GetLevelUpCost(PlayerSynStats.Level);
        }

        private void GetLevelUpCost(int level)
        {
            if (level < mMaxLevel)
                mExperienceNeeded = CharacterLevelRepo.GetExpByLevel(level + 1);
            else
                mExperienceNeeded = 0;
        }

        public void InitJobLevelUpCost()
        {
            mMaxJobLevel = CharacterLevelRepo.GetJobMaxLevel();
            GetJobLevelUpCost(PlayerSynStats.progressJobLevel);
        }

        private void GetJobLevelUpCost(int joblevel)
        {
            if (joblevel < mMaxJobLevel)
                mJobExperienceNeeded = CharacterLevelRepo.GetExpBySkillPt(joblevel);
            else
                mJobExperienceNeeded = 0;
        }

        public string GetLocalizedProgressLevelMin(int accumulatedlvl)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("level", accumulatedlvl.ToString());
            return GameUtils.FormatString(GUILocalizationRepo.GetLocalizedString("inst_LevelMinRank0"), parameters);
        }

        public void AddMonsterExperience(float exp, int monsterLvl)
        {
            if (SecondaryStats.BattleTime > 0)
            {
                float lvldiffPenalty = CharacterLevelRepo.GetShareExpByLevelDiff(PlayerSynStats.Level, monsterLvl) * 0.01f; //floating point inaccuracy
                exp *= lvldiffPenalty;
                if (exp < 1)
                    AddExperience(1);
                else
                    AddExperience(Mathf.FloorToInt(exp));
            }
        }

        public void AddExperience(int exp)
        {
            if (PlayerSynStats.Level < mMaxLevel)
            {
                SecondaryStats.experience += exp;

                // Broadcast Exp msg with all the defaults less the BroadcastMessageType being GainExperience
                AddToChatMessageQueue(new ChatMessage(MessageType.BroadcastMessage, exp.ToString(),
                                      "", "", 0, 1, 0, (byte)FactionType.Dragon, false,
                                      (byte)BroadcastMessageType.GainExperience));

                if (SecondaryStats.experience >= mExperienceNeeded)
                {
                    LevelUp();
                }
            }
        }

        public void AddJobExperience(int exp)
        {
            InitJobLevelUpCost();

            if (PlayerSynStats.progressJobLevel < mMaxJobLevel)
            {
                SecondaryStats.jobexperience += (int)exp;

                if (SecondaryStats.jobexperience >= mJobExperienceNeeded)
                {
                    JobLevelUp();
                }
            }
        }

        public static void SetPlayerStats(byte jobsect, int currentlevel, ICombatStats combatstats, CharacterInfoData charainfo)
        {
            combatstats.AddToField(FieldName.StrengthBase, charainfo.Str);
            combatstats.AddToField(FieldName.AgilityBase, charainfo.Agi);
            combatstats.AddToField(FieldName.DexterityBase, charainfo.Dex);
            combatstats.AddToField(FieldName.ConstitutionBase, charainfo.Con);
            combatstats.AddToField(FieldName.IntelligenceBase, charainfo.Int);

            JobsectJson job = JobSectRepo.GetJobByType((JobType)jobsect);
            combatstats.AddToField(FieldName.HealthBase, job.hpbase);
            combatstats.AddToField(FieldName.ManaBase, job.manabase);
            return;
        }

        public void LevelUp()
        {
            int oriLevel = PlayerSynStats.Level;
            int currentlevel = PlayerSynStats.Level;
            int currentExp = SecondaryStats.experience;
            while (currentlevel < mMaxLevel && currentExp >= mExperienceNeeded)
            {
                currentExp -= mExperienceNeeded;
                ++currentlevel;

                GetLevelUpCost(currentlevel);
            }

            //Increase level
            PlayerSynStats.Level = currentlevel;
            SecondaryStats.experience = currentExp;

            //Add stats point
            LocalCombatStats.StatsPoint += CharacterLevelRepo.GetStatsByLevel(PlayerSynStats.Level);

            if (IsInParty())
                PartyStats.SetMemberLevel(Name, PlayerSynStats.Level);

            Slot.QuestController.ConditionCheck(QuestRequirementType.Level);
            UpdateAchievement(AchievementObjectiveType.Level, "-1", true, PlayerSynStats.Level, false);
            Slot.DestinyClueController.Unlock(PlayerSynStats.Level);

            //PlayerSynStats.progressLevel = currentlevel;
            //SecondaryStats.experience = currentExp;
            //string changetype = "lvl";
            //int before = oriLevel;
            //int after = PlayerSynStats.progressLevel;
            //int gold = 0;
            //int crystal = 0;

            //string message = string.Format("changetype: {0} | before: {1} | after: {2} | goldDeducted: {3}  | crystalDeducted: {4}",
            //    changetype,
            //    before,
            //    after,
            //    gold,
            //    crystal);

            //Logging.Client.LogClasses.XpLevelRank xpLevelRankLog = new Logging.Client.LogClasses.XpLevelRank();
            //xpLevelRankLog.userId = Slot.mUserId;
            //xpLevelRankLog.charId = Slot.GetCharId();
            //xpLevelRankLog.message = message;
            //xpLevelRankLog.changeType = changetype;
            //xpLevelRankLog.before = before;
            //xpLevelRankLog.after = after;
            //xpLevelRankLog.goldDeducted = 0;
            //xpLevelRankLog.crystalDeducted = 0;
            //var ignoreAwait = Logging.Client.LoggingAgent.Instance.LogAsync(xpLevelRankLog);

            //JobCombatStatsJson jobpara = ExperienceRepo.GetJobParameterByJobType((JobType)PlayerSynStats.jobsect);
            //LevelUpJson levelUpJsonAfter = ExperienceRepo.GetLevelInfo(after);
            //LevelUpJson levelUpJsonBefore = ExperienceRepo.GetLevelInfo(before);

            //CombatStats.AddToField(FieldName.HealthBase, Mathf.FloorToInt(jobpara.hp * (levelUpJsonAfter.hp - levelUpJsonBefore.hp)));
            //CombatStats.AddToField(FieldName.AttackBase, Mathf.FloorToInt(jobpara.attack * (levelUpJsonAfter.attack - levelUpJsonBefore.attack)));
            //CombatStats.AddToField(FieldName.ArmorBase, Mathf.FloorToInt(jobpara.defense * (levelUpJsonAfter.defense - levelUpJsonBefore.defense)));
            //CombatStats.AddToField(FieldName.CriticalBase, Mathf.FloorToInt(jobpara.critical * (levelUpJsonAfter.critical - levelUpJsonBefore.critical)));
            //CombatStats.AddToField(FieldName.CocriticalBase, Mathf.FloorToInt(jobpara.cocritical * (levelUpJsonAfter.cocritical - levelUpJsonBefore.cocritical)));
            //CombatStats.AddToField(FieldName.CriticalDamageBase, Mathf.FloorToInt(jobpara.criticaldamage * (levelUpJsonAfter.criticaldamage - levelUpJsonBefore.criticaldamage)));
            //CombatStats.AddToField(FieldName.CoCriticalDamageBase, Mathf.FloorToInt(jobpara.cocriticaldamage * (levelUpJsonAfter.cocriticaldamage - levelUpJsonBefore.cocriticaldamage)));
            //CombatStats.AddToField(FieldName.AccuracyBase, Mathf.FloorToInt(jobpara.accuracy * (levelUpJsonAfter.accuracy - levelUpJsonBefore.accuracy)));
            //CombatStats.AddToField(FieldName.EvasionBase, Mathf.FloorToInt(jobpara.evasion * (levelUpJsonAfter.evasion - levelUpJsonBefore.evasion)));

            //SetHealth(GetHealthMax());
            //CombatStats.ComputeAll();
        }

        public void JobLevelUp()
        {
            int oriLevel = PlayerSynStats.progressJobLevel;
            int currentlevel = PlayerSynStats.progressJobLevel;
            int currentExp = SecondaryStats.jobexperience;
            while (currentlevel < mMaxJobLevel && currentExp >= mJobExperienceNeeded)
            {
                currentExp -= mJobExperienceNeeded;
                ++currentlevel;

                GetJobLevelUpCost(currentlevel);
            }

            //Increase level
            PlayerSynStats.progressJobLevel = currentlevel;
            SecondaryStats.jobexperience = currentExp;

            LocalCombatStats.SkillPoints += 1;
        }

        public void UpdateJobSect(byte jobsect)
        {
            PlayerSynStats.jobsect = jobsect;

            if (IsInParty())
            {
                PartyStats.SetMemberAvatar(Name, Slot.CharacterData.EquipmentInventory, (JobType)jobsect);
                PartyStats.OnDirty(Name);
            }
        }

        public bool LevelUpTo(int level)
        {
            if (PlayerSynStats.Level + level > 100)
                return false;

            for (int i = 0; i < level; ++i)
            {
                PlayerSynStats.Level += 1;
                //SecondaryStats.pointstoadd += (int)mLvlUpCost["Stats"];
                GetLevelUpCost(PlayerSynStats.Level);
            }
            return true;
        }

        public void SetLevelTo(int level)
        {
            GetLevelUpCost(level - 1);
            SecondaryStats.experience = mExperienceNeeded;
            PlayerSynStats.Level = level;
        }

        public int GetAccumulatedLevel()
        {
            return PlayerSynStats.Level;
        }

        public override void SetHealth(int val)
        {
            base.SetHealth(val);
            PlayerStats.DisplayHp = (float)val / GetHealthMax();

            // Update health to party members
            if (IsInParty())
                PartyStats.SetMemberHP(Name, PlayerStats.DisplayHp);
        }

        public override void SetMana(int val)
        {
            base.SetMana(val);
            LocalCombatStats.Mana = val;

            // Update mana to party members
            if (IsInParty())
                PartyStats.SetMemberMP(Name, GetManaNormalized());
        }

        // InventoryStats
        public void InitInventoryStats(ItemInventoryData itemInvData)
        {
            int collectionSize = (int)InventorySlot.COLLECTION_SIZE;
            int slotCount = itemInvData.Slots.Count;
            for (int idx = 0; idx < slotCount; ++idx)
            {
                IInventoryItem invItem = itemInvData.Slots[idx];
                if (invItem != null)
                {
                    invItem.EncodeItem();
                    int containerIdx = idx / collectionSize;
                    int collectionIdx = idx % collectionSize;
                    InventoryStats[containerIdx].ItemInventory[collectionIdx] = invItem.GetItemCodeForLocalObj();
                }
            }
            SecondaryStats.UnlockedSlotCount = itemInvData.UnlockedSlotCount;
        }

        public void UpdateInventoryStats(int idx, IInventoryItem item)
        {
            int collectionSize = (int)InventorySlot.COLLECTION_SIZE;
            int containerIdx = idx / collectionSize;
            int collectionIdx = idx % collectionSize;

            if (item != null)
            {
                item.EncodeItem();
                InventoryStats[containerIdx].ItemInventory[collectionIdx] = item.GetItemCodeForLocalObj();
                item.ResetNewItem();
            }
            else
                InventoryStats[containerIdx].ItemInventory[collectionIdx] = null;
        }

        public void RemoveItem(int idx)
        {
            int collectionSize = (int)InventorySlot.COLLECTION_SIZE;
            int containerIdx = idx / collectionSize;
            int collectionIdx = idx % collectionSize;

            InventoryStats[containerIdx].ItemInventory[collectionIdx] = null;
        }

        // EquipmentStats

        #region EquipmentStats

        public void InitEquipmentStats(EquipmentInventoryData eqInvData)
        {
            WeaponTypeUsed = WeaponType.Any;
            int weaponSlotIndex = (int)EquipmentSlot.Weapon;
            for (int i = 0; i < (int)EquipmentSlot.MAXSLOTS; ++i)
            {
                Equipment equipment = eqInvData.Slots[i] as Equipment;
                if (equipment != null)
                {
                    equipment.EncodeItem();
                    EquipmentStats.EquipInventory[i] = equipment.GetItemCodeForLocalObj();
                    if (i == weaponSlotIndex)
                    {
                        WeaponTypeUsed = EquipmentRepo.GetWeaponTypeByPartType(equipment.EquipmentJson.partstype);
                        UpdateBasicAttack(equipment.EquipmentJson);
                    }
                }
            }
            for (int i = 0; i < (int)FashionSlot.MAXSLOTS; ++i)
            {
                Equipment equipment = eqInvData.FashionSlots[i] as Equipment;
                if (equipment != null)
                {
                    equipment.EncodeItem();
                    EquipmentStats.FashionInventory[i] = equipment.GetItemCodeForLocalObj();
                }
            }
            for (int i = 0; i < (int)AppearanceSlot.MAXSLOTS; ++i)
            {
                EquipmentStats.AppearanceInventory[i] = eqInvData.AppearanceSlots[i];
            }
            EquipmentStats.HideHelm = eqInvData.HideHelm;
        }

        public void UpdateEquipmentStats(int idx, Equipment equipment)
        {
            if (equipment != null)
            {
                equipment.EncodeItem();
                EquipmentStats.EquipInventory[idx] = equipment.GetItemCodeForLocalObj();
            }
            else
                EquipmentStats.EquipInventory[idx] = null;
            if (idx == (int)EquipmentSlot.Weapon)
                WeaponTypeUsed = equipment == null ? WeaponType.Any : EquipmentRepo.GetWeaponTypeByPartType(equipment.EquipmentJson.partstype);
        }

        public void UpdateFashionStats(int idx, Equipment equipment)
        {
            if (equipment != null)
            {
                equipment.EncodeItem();
                EquipmentStats.FashionInventory[idx] = equipment.GetItemCodeForLocalObj();
            }
            else
                EquipmentStats.FashionInventory[idx] = null;
        }

        //public void UpdateEquipmentCombatStats(EquipItem item, bool added)
        //{
        //    (CombatStats as PlayerCombatStats).ComputeEquippedCombatStats(item, EquipmentStats.selectedGemGroup, EquipmentStats.selectedGemGroup, added);
        //    CombatStats.ComputeAll();
        //}

        #endregion EquipmentStats

        public void UpdateItemHotbar(byte index, int itemIdToAdd)
        {
            CollectionHandler<object> itemHotbarCollection = ItemHotbarStats.ItemHotbar;
            if (itemIdToAdd > 0)
            {
                int itemHotbarSlotCnt = itemHotbarCollection.Count, idxToSet = -1;
                for (int i = 0; i < itemHotbarSlotCnt; ++i)
                {
                    int itemId = (int)itemHotbarCollection[i];
                    if (itemId == itemIdToAdd)
                    {
                        idxToSet = -1;
                        break;
                    }
                    else if (itemId == 0 && idxToSet == -1)
                        idxToSet = i;
                }
                if (idxToSet != -1)
                {
                    idxToSet = ((int)itemHotbarCollection[index] == 0) ? index : idxToSet;
                    itemHotbarCollection[idxToSet] = itemIdToAdd;
                }
            }
            else
            {
                if ((int)itemHotbarCollection[index] != 0)
                    itemHotbarCollection[index] = 0;
            }
        }

        public void InitExchangeShopStats(ExchangeShopInventory exInv)
        {
            exInv.UpdateInventoryList();

            ExchangeShopSynStats.exchangeLeftMapJsonString = JsonConvert.SerializeObject(exInv.map_exchangeLeft);
        }

        public void InitPortraitDataStats(PortraitData portData)
        {
            //portData.mInfoDic = null;   //HACK
            if (portData.mInfoDic == null)
                portData.InitDefault((JobType)PlayerSynStats.jobsect);

            //Update character data if there is new portrait
            //Update any setting that has been missed
            portData.UpdateClassPortrait((JobType)PlayerSynStats.jobsect);

            //Load character data into stats
            PortraitDataStats.LoadCharacterPortraitData(portData);
        }

        // WelfareStats

        #region WelfareStats

        public void InitWelfareStats(WelfareInventoryData welfareInv)
        {
            int serverid = GameApplication.Instance.GetMyServerId();

            // Total Credit
            if (WelfareRules.GetTotalCreditEventId() != welfareInv.TotalCreditCurrEventId)
            {
                welfareInv.TotalCreditRewards = WelfareRules.GetTotalCreditRewardDataByEvent(welfareInv.TotalCreditCurrEventId);

                // Send unclaimed rewards to player via mail
                Slot.mWelfareCtrlr.SendTotalGoldUncollectedRewardsToPlayer(true);

                welfareInv.TotalCreditCurrEventId = WelfareRules.GetTotalCreditEventId();
                welfareInv.TotalCreditRewards = WelfareRules.GetTotalCreditRewardData();
                welfareInv.TotalCreditClaims = Slot.mWelfareCtrlr.InitTotalGoldClaims(welfareInv.TotalCreditCurrEventId, WelfareRules.GetTotalCreditRewardData());
                welfareInv.TotalGoldCredited = 0;
            }
            else
            {
                welfareInv.TotalCreditRewards = WelfareRules.GetTotalCreditRewardDataByEvent(welfareInv.TotalCreditCurrEventId);
            }

            // Total Spend
            if (WelfareRules.GetTotalSpendEventId() != welfareInv.TotalSpendCurrEventId)
            {
                welfareInv.TotalSpendRewards = WelfareRules.GetTotalSpendRewardDataByEvent(welfareInv.TotalSpendCurrEventId);

                // Send unclaimed rewards to player via mail
                Slot.mWelfareCtrlr.SendTotalGoldUncollectedRewardsToPlayer(false);

                welfareInv.TotalSpendCurrEventId = WelfareRules.GetTotalSpendEventId();
                welfareInv.TotalSpendRewards = WelfareRules.GetTotalSpendRewardData();
                welfareInv.TotalSpendClaims = Slot.mWelfareCtrlr.InitTotalGoldClaims(welfareInv.TotalSpendCurrEventId, WelfareRules.GetTotalSpendRewardData());
                welfareInv.TotalGoldSpent = 0;
            }
            else
            {
                welfareInv.TotalSpendRewards = WelfareRules.GetTotalSpendRewardDataByEvent(welfareInv.TotalSpendCurrEventId);
            }

            // Gold Jackpot
            if (WelfareRules.GetGoldJackpotCurrEventId() != welfareInv.GoldJackpotCurrEventId)
            {
                welfareInv.GoldJackpotCurrEventId = WelfareRules.GetGoldJackpotCurrEventId();
                Slot.mWelfareCtrlr.ResetGoldJackpotTier();
            }

            Slot.mWelfareCtrlr.SetGoldJackpotTierData(WelfareRules.GetGoldJackpotTierData());
            welfareInv.GoldJackpotHighestTier = WelfareRules.GetGoldJackpotHighestTier();

            DateTime serverOpenDT = NewServerEventRules.serverOpenDT;
            welfareInv.InitServerStartDT((ushort)serverOpenDT.Year, (ushort)serverOpenDT.Month, (ushort)serverOpenDT.Day);
            welfareInv.InitFromInventory(WelfareStats);

            if (WelfareRules.GetContLoginEventId() != welfareInv.ContLoginCurrEventId)
            {
                welfareInv.ContLoginRewards = WelfareRules.GetContLoginRewardDataByEvent(welfareInv.ContLoginCurrEventId);

                welfareInv.ContLoginCurrEventId = WelfareRules.GetContLoginEventId();
                welfareInv.ContLoginRewards = WelfareRules.GetContLoginRewardData();
                welfareInv.ResetContLoginClaimed();
            }
            else
            {
                welfareInv.ContLoginRewards = WelfareRules.GetContLoginRewardDataByEvent(welfareInv.ContLoginCurrEventId);
            }
            Slot.mWelfareCtrlr.InitContLoginRewards();

            //if(Slot != null)
            //{
            //    Slot.OnWelfareClaimSignInPrize(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);
            //}
        }

        #endregion WelfareStats

        public void InitPowerUpStats(PowerUpInventoryData powerupInv)
        {
            powerupInv.InitFromPowerUpInventory(PowerUpStats);
            powerupInv.InitFromMeridianInventory(MeridianStats);
        }

        public void InitEquipmentCraftStats(EquipmentCraftInventoryData equipmentCraftInv)
        {
            equipmentCraftInv.InitFromInventory(EquipmentCraftStats);
        }

        public void InitEquipFusionStats(EquipFusionInventoryData equipFusionInv)
        {
            equipFusionInv.InitFromInventory(EquipFusionStats);
        }

        // LotteryShopStats

        #region LotteryShopStats

        public void InitLotteryStats(LotteryInventoryData lotteryInv, GameClientPeer peer)
        {
            mLotteryInvController = new LotteryController(peer);
            mLotteryInvController.RestToNewData();
        }

        public bool UpdateLotteryStats()
        {
            return mLotteryInvController.RestToNewData();
        }

        public void UpdateLotteryStat(int lottery_id)
        {
            mLotteryInvController.UpdateLotteryStat(lottery_id);
        }

        public void AddLotteryFreeTicket(int lottery_id, int count)
        {
            mLotteryInvController.AddFreeTicket(lottery_id, count);
        }

        public int GetLotteryFreeTicket(int lottery_id)
        {
            return mLotteryInvController.GetFreeTicketCount(lottery_id);
        }

        public int DeductLotteryFreeTicket(int lottery_id, int used_free_tickets)
        {
            return mLotteryInvController.DeductFreeTicket(lottery_id, used_free_tickets);
        }

        public int GetLotteryPoint(int lottery_id)
        {
            return mLotteryInvController.GetPoint(lottery_id);
        }

        public int AddLotteryPoint(int lottery_id, int point)
        {
            return mLotteryInvController.AddPoint(lottery_id, point);
        }

        public int GetLotteryCount(int lottery_id)
        {
            return mLotteryInvController.GetLotteryCount(lottery_id);
        }

        public bool AddLotteryCountAndPoint(int lottery_id, int count, int point)
        {
            return mLotteryInvController.AddCountAndPoint(lottery_id, count, point);
        }

        public List<int> GetLotteryRewardedPoints(int lottery_id)
        {
            return mLotteryInvController.GetRewardedPoints(lottery_id);
        }

        public bool AddLotteryRewarderPoint(int lottery_id, int point)
        {
            return mLotteryInvController.AddRewarderPoint(lottery_id, point);
        }

        #endregion LotteryShopStats

        // SevenDaysStats

        #region SevenDaysStats

        public void InitSevenDaysStats(SevenDaysInvData sevenDaysInv)
        {
            Slot.mSevenDaysController.InitTaskProgress();

            sevenDaysInv.InitFromInventory(SevenDaysStats);

            if (!Slot.mSevenDaysController.IsEventPeriod() && !Slot.mSevenDaysController.IsCollectionPeriod())
            {
                Slot.mSevenDaysController.SendUncollectedRewardsToPlayer();
            }
        }

        #endregion SevenDaysStats

        #region QuestExtraRewardsStats

        public void InitQuestExtraRewardsStats(QuestExtraRewardsInvData questExtraRewardsInv)
        {
            Slot.mQuestExtraRewardsCtrler.InitTaskProgress();
            Slot.mQuestExtraRewardsCtrler.InitBoxRewards();

            questExtraRewardsInv.InitFromInventory(QuestExtraRewardsStats);
        }

        #endregion QuestExtraRewardsStats

        #region DNA
        public void InitDNAStats(DNAInvData dnaInv)
        {
            dnaInv.InitFromInventory(DNAStats);
        }
        #endregion

        //public void InitBattleTimeStats(BattleTimeInventoryData battleTimeInv)
        //{
        //    battleTimeInv.InitFromInventory(SecondaryStats.BattleTime);
        //}

        #endregion PlayerStats Methods

        #region Currency Methods

        public const int currencyMax = 2100000000;
        private DateTime currentTime;

        public void AddMoney(int amount, string from)
        {
            if (amount <= 0) //in case hacked
                return;

            //log.InfoFormat("[{0}]: addm ({1}) {2} {3}", mInstance.ID, GetPersistentID(), SecondaryStats.money, money);
            SecondaryStats.Money += amount;
            if (SecondaryStats.Money < 0 || SecondaryStats.Money > currencyMax) //in case overflow.
                SecondaryStats.Money = currencyMax;
            if (amount >= 10000)
                LogCurrencyChange(from, CurrencyType.Money, amount, SecondaryStats.Money);
        }

        public bool DeductMoney(int amount, string from)
        {
            if (SecondaryStats.Money >= amount)
            {
                //log.InfoFormat("[{0}]: deductmoney ({1}) {2} {3}", mInstance.ID, GetPersistentID(), SecondaryStats.money, amount);
                SecondaryStats.Money -= amount;
                if (amount >= 10000)
                    LogCurrencyChange(from, CurrencyType.Money, -amount, SecondaryStats.Money);
                return true;
            }
            return false;
        }

        public void AddGold(int gold, string from)
        {
            if (gold <= 0) //in case hacked
                return;

            //log.InfoFormat("[{0}]: addg ({1}) {2} {3}", mInstance.ID, GetPersistentID(), SecondaryStats.gold, gold);
            SecondaryStats.Gold += gold;
            if (SecondaryStats.Gold < 0 || SecondaryStats.Gold > currencyMax) //in case overflow.
                SecondaryStats.Gold = currencyMax;
            LogCurrencyChange(from, CurrencyType.Gold, gold, SecondaryStats.Gold);
        }

        public bool DeductLockGold(int amount, string from)
        {
            if (SecondaryStats.bindgold >= amount)
            {
                //log.InfoFormat("[{0}]: deductlockgold ({1}) {2} {3}", mInstance.ID, GetPersistentID(), SecondaryStats.bindgold, amount);
                SecondaryStats.bindgold -= amount;
                LogCurrencyChange(from, CurrencyType.LockGold, -amount, SecondaryStats.bindgold);
                return true;
            }
            return false;
        }

        public void OnGuildChange()
        {
            if (SecondaryStats.guildId == 0)
                Slot.CharacterData.GuildQuests.InitDefault();
            else
            {
                //int addtimes = VIPRepo.GetVIPPrivilege("GuildQuest", PlayerSynStats.vipLvl);
                //Slot.CharacterData.GuildQuests.ResetOnNewDay(addtimes);
                //Slot.CharacterData.GuildQuests.RefreshQuests(PlayerSynStats.progressLevel, false);
            }
        }

        public void AddGuildQuestRefreshTimes(int count)
        {
            Slot.CharacterData.GuildQuests.additionaltimes += count;
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("num", count.ToString());
            Slot.ZRPC.CombatRPC.Ret_SendSystemMessage("sys_GuildQuestAddTime", GameUtils.FormatString(dic), false, Slot);
        }

        public bool OnGuildQuestOperation(GuildQuestOperation ope, int id, out int error)
        {
            bool resultOK = false;
            error = 0;
            Slot.CharacterData.GuildQuests.ComputeTime();
            if (ope == GuildQuestOperation.Accept)
            {
                resultOK = Slot.CharacterData.GuildQuests.AcceptQuest(id);
                if (!resultOK)
                {
                    error = (int)GuildQuestOperationError.QuestNotFound;
                }
            }
            else if (ope == GuildQuestOperation.Cancel)
            {
                resultOK = Slot.CharacterData.GuildQuests.CancelQuest(id);
                if (!resultOK)
                {
                    error = (int)GuildQuestOperationError.QuestNotFound;
                }
                if (resultOK)
                {
                    //log
                    string message = string.Format("player:{0}|droped quests:{1}", Name, id);
                    Zealot.Logging.Client.LogClasses.GuildQuestDrop syslog = new Zealot.Logging.Client.LogClasses.GuildQuestDrop();
                    if (Slot != null)
                    {
                        syslog.userId = Slot.mUserId;
                        syslog.charId = Slot.GetCharId();
                    }
                    syslog.message = message;
                    syslog.questid1 = id;
                    syslog.dailytimesleft = 3 - Slot.CharacterData.GuildQuests.finishedtimestoday;
                    var ignoreAwait = Zealot.Logging.Client.LoggingAgent.Instance.LogAsync(syslog);
                }
            }
            else if (ope == GuildQuestOperation.Refresh)
            {
                if (Slot.CharacterData.GuildQuests.refreshtimesfree > 0
                    || Slot.CharacterData.GuildQuests.additionaltimes > 0)
                    resultOK = Slot.CharacterData.GuildQuests.RefreshQuests(PlayerSynStats.Level, true);
                else
                {
                    int cost = GameConstantRepo.GetConstantInt("GuildQuest_Refresh_Cost", 5);
                    if (DeductGold(cost, true, true, "GuildQuest_Refresh"))
                        resultOK = Slot.CharacterData.GuildQuests.RefreshQuests(PlayerSynStats.Level, false);
                    else
                    {
                        resultOK = false;
                        error = (int)GuildQuestOperationError.NotEnoughGold;
                    }
                }

                if (resultOK)
                {
                    //log
                    string ids = Slot.CharacterData.GuildQuests.GetIdLists();
                    string message = string.Format("player:{0}|refreshed quests:{1}", Name, ids);
                    Zealot.Logging.Client.LogClasses.GuildQuestRefresh syslog = new Zealot.Logging.Client.LogClasses.GuildQuestRefresh();
                    if (Slot != null)
                    {
                        syslog.userId = Slot.mUserId;
                        syslog.charId = Slot.GetCharId();
                    }
                    syslog.message = message;
                    syslog.questid1 = Slot.CharacterData.GuildQuests.questlist[0].id;
                    syslog.questid2 = Slot.CharacterData.GuildQuests.questlist[1].id;
                    syslog.questid3 = Slot.CharacterData.GuildQuests.questlist[2].id;
                    syslog.freerefreshtimesleft = Slot.CharacterData.GuildQuests.refreshtimesfree;
                    var ignoreAwait = Zealot.Logging.Client.LoggingAgent.Instance.LogAsync(syslog);
                }
            }
            else if (ope == GuildQuestOperation.Finish)
            {
                Zealot.Logging.Client.LogClasses.GuildQuestFinish syslog = new Zealot.Logging.Client.LogClasses.GuildQuestFinish();
                if (Slot != null)
                {
                    syslog.userId = Slot.mUserId;
                    syslog.charId = Slot.GetCharId();
                }
                string message = string.Format("player:{0}|Finish quest:{1}", Name, id);
                syslog.message = message;
                syslog.questid1 = id;
                syslog.dailytimesleft = 3 - Slot.CharacterData.GuildQuests.finishedtimestoday;
                UpdateGuildQuestDailyTimes();
                if (resultOK = Slot.CharacterData.GuildQuests.FinishQuest(id, out error))
                {
                    GuildQuestJson qdj = GuildRepo.GetQuestJson(id);
                    int rewardcontribution = qdj.rewardcontribution;
                    int rewardwealth = qdj.rewardwealth;
                    int itemcount = qdj.itemcount;
                    GMActivityConfigData config = GMActivityConfig.GetConfigInt(GMActivityType.GuildQuest, DateTime.Now);
                    if (config != null)
                    {
                        rewardwealth = Mathf.FloorToInt(rewardwealth * config.mDataList[0] / 100);
                        rewardcontribution = Mathf.FloorToInt(rewardcontribution * config.mDataList[1] / 100);
                        itemcount = Mathf.FloorToInt(itemcount * config.mDataList[2] / 100);
                    }
                    if (rewardcontribution > 0)
                    {
                        AddGuildContribution(rewardcontribution, string.Format("GuildQuest:{0}", id));
                    }
                    if (rewardwealth > 0)
                    {
                        AddGuildGold(rewardwealth, syslog);
                    }
                    if (qdj.itemid != "" && itemcount > 0)
                    {
                        int itemid = 0;
                        if (int.TryParse(qdj.itemid, out itemid) && itemid > 0)
                        {
                            IInventoryItem item = GameRepo.ItemFactory.GetInventoryItem(itemid);
                            item.StackCount = (ushort)itemcount;
                            InvRetval addRes = Slot.mInventory.AddItemsToInventory(item, true, "GuildQuest");
                            if (addRes.retCode != InvReturnCode.AddSuccess)
                            {
                                GameRules.SendMailWithAttachment(Slot.mChar, "Reward_GuildQuestReward", new List<IInventoryItem>() { item }, null);
                            }
                        }
                    }
                    Slot.mQuestExtraRewardsCtrler.UpdateTask(QuestExtraType.GuildQuest);

                    var ignoreAwait = Zealot.Logging.Client.LoggingAgent.Instance.LogAsync(syslog);
                }
            }
            else if (ope == GuildQuestOperation.Fastforwad)
            {
                Slot.CharacterData.GuildQuests.FastForwardQuest();
                resultOK = true;
            }
            return resultOK;
        }

        public bool DeductGold(int amount, bool useBindGold, bool spend, string from)
        {
            if (useBindGold)
            {
                //if (SecondaryStats.gold + SecondaryStats.bindgold >= amount) Overflow if at max currency~
                if (SecondaryStats.Gold >= amount - SecondaryStats.bindgold)  // enough total gold
                {
                    //log.InfoFormat("[{0}]: deductbindg ({1}) {2} {3} {4}", mInstance.ID, GetPersistentID(), SecondaryStats.bindgold, SecondaryStats.gold, amount);
                    if (SecondaryStats.bindgold >= amount)
                    {
                        SecondaryStats.bindgold -= amount;

                        if (spend)
                        {
                            int before = SecondaryStats.Gold + SecondaryStats.bindgold;
                            TongbaoCostBuffAdd(amount);
                            Slot.mWelfareCtrlr.OnDeducted(amount);
                            Slot.mQuestExtraRewardsCtrler.UpdateTask(QuestExtraType.GoldSpent, amount);
                            int after = before - amount;

                            WelfareRules.LogWelfareTotalSpend("Welfare Total Spent Gold", amount, before, after, Slot);
                        }
                        LogCurrencyChange(from, CurrencyType.LockGold, -amount, SecondaryStats.bindgold);
                        return true;
                    }
                    else
                    {
                        int remainAmt = amount;
                        int bindgold = SecondaryStats.bindgold;
                        if (bindgold > 0)
                        {
                            remainAmt -= bindgold;
                            SecondaryStats.bindgold = 0;
                            LogCurrencyChange(from, CurrencyType.LockGold, -bindgold, 0);
                        }
                        SecondaryStats.Gold -= remainAmt;
                        LogCurrencyChange(from, CurrencyType.Gold, -remainAmt, SecondaryStats.Gold);

                        if (spend)
                        {
                            int before = SecondaryStats.Gold + SecondaryStats.bindgold;
                            TongbaoCostBuffAdd(amount);
                            //add to total spent
                            Slot.mWelfareCtrlr.OnDeducted(amount);
                            Slot.mQuestExtraRewardsCtrler.UpdateTask(QuestExtraType.GoldSpent, amount);
                            int after = before - amount;

                            WelfareRules.LogWelfareTotalSpend("Welfare Total Spent Gold", amount, before, after, Slot);
                        }
                        return true;
                    }
                }
            }
            else
            {
                if (SecondaryStats.Gold >= amount)
                {
                    //log.InfoFormat("[{0}]: deductg ({1}) {2} {3}", mInstance.ID, GetPersistentID(), SecondaryStats.gold, amount);
                    SecondaryStats.Gold -= amount;
                    LogCurrencyChange(from, CurrencyType.Gold, -amount, SecondaryStats.Gold);

                    if (spend)
                    {
                        int before = SecondaryStats.Gold + SecondaryStats.bindgold;
                        TongbaoCostBuffAdd(amount);
                        //add to total spent
                        Slot.mWelfareCtrlr.OnDeducted(amount);
                        Slot.mQuestExtraRewardsCtrler.UpdateTask(QuestExtraType.GoldSpent, amount);
                        int after = before - amount;

                        WelfareRules.LogWelfareTotalSpend("Welfare Total Spent Gold", amount, before, after, Slot);
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Just to spend gold without deducting. To deduct gold, call DeductGold
        /// </summary>
        public void SpendGold(int bindgold, int gold)
        {
            TongbaoCostBuffAdd(bindgold + gold);
            //add to total spent
            Slot.mWelfareCtrlr.OnDeducted(bindgold + gold);
        }

        public void AddBindGold(int gold, string from)
        {
            if (gold <= 0)
                return;

            //log.InfoFormat("[{0}]: addbindg ({1}) {2} {3}", mInstance.ID, GetPersistentID(), SecondaryStats.bindgold, gold);
            SecondaryStats.bindgold += gold;
            if (SecondaryStats.bindgold < 0 || SecondaryStats.bindgold > currencyMax) //in case overflow.
                SecondaryStats.bindgold = currencyMax;
            LogCurrencyChange(from, CurrencyType.LockGold, gold, SecondaryStats.bindgold);
        }

        public void AddLotteryPoint(int lotterypoint, string from)
        {
            if (lotterypoint <= 0)
                return;

            //log.InfoFormat("[{0}]: addlotpt ({1}) {2} {3}", mInstance.ID, GetPersistentID(), SecondaryStats.lotterypoints, lotterypoint);
            SecondaryStats.lotterypoints += lotterypoint;
            if (SecondaryStats.lotterypoints < 0 || SecondaryStats.lotterypoints > currencyMax) //in case overflow.
                SecondaryStats.lotterypoints = currencyMax;
            LogCurrencyChange(from, CurrencyType.LotteryTicket, lotterypoint, SecondaryStats.lotterypoints);
        }

        public void AddHonorPoint(int honorpt, string from)
        {
            if (honorpt <= 0)
                return;

            //log.InfoFormat("[{0}]: addhon ({1}) {2} {3}", mInstance.ID, GetPersistentID(), SecondaryStats.honor, honorpt);
            SecondaryStats.honor += honorpt;
            if (SecondaryStats.honor < 0 || SecondaryStats.honor > currencyMax) //in case overflow.
                SecondaryStats.honor = currencyMax;
            LogCurrencyChange(from, CurrencyType.HonorValue, honorpt, SecondaryStats.honor);
        }

        public void AddAchievementExp(int amt)
        {
            if (amt <= 0)
                return;

            if (PlayerSynStats.AchievementLevel < AchievementRepo.ACHIEVEMENT_MAX_LEVEL)
            {
                SecondaryStats.AchievementExp += amt;

                AchievementLevel info = AchievementRepo.GetAchievementLevelInfo(PlayerSynStats.AchievementLevel);
                if (info != null && info.expToNextLv > 0 && SecondaryStats.AchievementExp >= info.expToNextLv)
                {
                    AchievementLevelUp();
                }
            }
        }

        private void AchievementLevelUp()
        {
            AchievementLevel info = AchievementRepo.GetAchievementLevelInfo(PlayerSynStats.AchievementLevel);
            while (PlayerSynStats.AchievementLevel < AchievementRepo.ACHIEVEMENT_MAX_LEVEL
                && info != null && SecondaryStats.AchievementExp >= info.expToNextLv)
            {
                SecondaryStats.AchievementExp -= info.expToNextLv;
                PlayerSynStats.AchievementLevel++;

                // levelled up to this level, give this level's reward
                info = AchievementRepo.GetAchievementLevelInfo(PlayerSynStats.AchievementLevel);
                if (info != null)
                    AchievementStats.GiveLevelUpReward(info);
            }

            if (PlayerSynStats.AchievementLevel == AchievementRepo.ACHIEVEMENT_MAX_LEVEL)
                SecondaryStats.AchievementExp = 0;

            UpdateAchievement(AchievementObjectiveType.AchievementLevel, "-1", true, PlayerSynStats.AchievementLevel, false);
        }

        public void AddGuildGold(int amount, Logging.Client.LogClasses.GuildQuestFinish log = null)
        {
            int guildId = SecondaryStats.guildId;
            if (amount <= 0 && guildId == 0)
                return;
            if (log != null)
            {
                log.wealthBefore = Slot.CharacterData.CurrencyInventory.GuildFundToday;
                log.wealthAmount = amount;
            }
            CurrencyInventoryData currencyInventory = Slot.CharacterData.CurrencyInventory;
            currencyInventory.GuildFundToday += amount;
            currencyInventory.GuildFundTotal += amount;
            GuildRules.AddGuildGold(guildId, amount, Name);
            if (log != null)
                log.wealthAfter = Slot.CharacterData.CurrencyInventory.GuildFundToday;
        }

        public void AddGuildContribution(int amount, string from)
        {
            if (amount <= 0)
                return;
            SecondaryStats.contribute += amount;
            if (SecondaryStats.contribute < 0 || SecondaryStats.contribute > currencyMax)
                SecondaryStats.contribute = currencyMax;
            LogCurrencyChange(from, CurrencyType.GuildContribution, amount, SecondaryStats.contribute);
        }

        public void AddBattleCoin(int amount, string from)
        {
            if (amount <= 0) //in case hacked
                return;
            //log.InfoFormat("[{0}]: addm ({1}) {2} {3}", mInstance.ID, GetPersistentID(), SecondaryStats.battlecoin, amount);
            SecondaryStats.battlecoin += amount;
            if (SecondaryStats.battlecoin < 0 || SecondaryStats.battlecoin > currencyMax) //in case overflow.
                SecondaryStats.battlecoin = currencyMax;
            LogCurrencyChange(from, CurrencyType.BattleCoin, amount, SecondaryStats.battlecoin);
        }

        public bool DeductHonor(int amount, string from)
        {
            if (SecondaryStats.honor >= amount)
            {
                //log.InfoFormat("[{0}]: deducthon ({1}) {2} {3}", mInstance.ID, GetPersistentID(), SecondaryStats.honor, amount);
                SecondaryStats.honor -= amount;
                LogCurrencyChange(from, CurrencyType.HonorValue, -amount, SecondaryStats.honor);
                return true;
            }
            return false;
        }

        public bool DeductLotteryPoint(int amount, string from)
        {
            if (SecondaryStats.lotterypoints >= amount)
            {
                //log.InfoFormat("[{0}]: deductlotpt ({1}) {2} {3}", mInstance.ID, GetPersistentID(), SecondaryStats.lotterypoints, amount);
                SecondaryStats.lotterypoints -= amount;
                LogCurrencyChange(from, CurrencyType.LotteryTicket, -amount, SecondaryStats.lotterypoints);
                return true;
            }
            return false;
        }

        public bool DeductBattleCoin(int amount, string from)
        {
            if (SecondaryStats.battlecoin >= amount)
            {
                //log.InfoFormat("[{0}]: deductmoney ({1}) {2} {3}", mInstance.ID, GetPersistentID(), SecondaryStats.battlecoin, amount);
                SecondaryStats.battlecoin -= amount;
                LogCurrencyChange(from, CurrencyType.BattleCoin, -amount, SecondaryStats.battlecoin);
                return true;
            }
            return false;
        }

        public bool DeductGuildContribution(int amount, string from)
        {
            if (SecondaryStats.contribute >= amount)
            {
                //log.InfoFormat("[{0}]: deductmoney ({1}) {2} {3}", mInstance.ID, GetPersistentID(), SecondaryStats.contribute, amount);
                SecondaryStats.contribute -= amount;
                LogCurrencyChange(from, CurrencyType.GuildContribution, -amount, SecondaryStats.contribute);
                return true;
            }
            return false;
        }

        public void AddCurrency(CurrencyType type, int amount, string from)
        {
            if (amount <= 0)
                return;
            switch (type)
            {
                case CurrencyType.Money:
                    AddMoney(amount, from);
                    break;
                case CurrencyType.GuildContribution:
                    AddGuildContribution(amount, from);
                    break;
                case CurrencyType.GuildGold:
                    AddGuildGold(amount);
                    break;
                case CurrencyType.Gold:
                    AddGold(amount, from);
                    break;
                case CurrencyType.LockGold:
                    AddBindGold(amount, from);
                    break;
                case CurrencyType.LotteryTicket:
                    break;
                case CurrencyType.HonorValue:
                    AddHonorPoint(amount, from);
                    break;
                case CurrencyType.Exp:
                    AddExperience(amount);
                    break;
                case CurrencyType.AExp:
                    AddAchievementExp(amount);
                    break;
                case CurrencyType.BattleCoin:
                    AddBattleCoin(amount, from);
                    break;
                default:
                    break;
            }
        }

        public bool DeductCurrency(CurrencyType type, int amount, bool useBindGold, string from)
        {
            if (amount <= 0)
                return true;
            bool success = false;
            switch (type)
            {
                case CurrencyType.Money:
                    success = DeductMoney(amount, from);
                    break;
                case CurrencyType.GuildContribution:
                    success = DeductGuildContribution(amount, from);
                    break;
                case CurrencyType.Gold:
                    success = DeductGold(amount, useBindGold, true, from);
                    break;
                case CurrencyType.LockGold:
                    //success = DeductLockGold(amount);
                    success = DeductGold(amount, useBindGold, true, from);
                    break;
                case CurrencyType.LotteryTicket:
                    break;
                case CurrencyType.HonorValue:
                    success = DeductHonor(amount, from);
                    break;
                case CurrencyType.BattleCoin:
                    success = DeductBattleCoin(amount, from);
                    break;
                default:
                    break;
            }
            return success;
        }

        public bool IsCurrencySufficient(CurrencyType curType, int amount, bool allowbind = true)
        {
            if (curType == CurrencyType.None) return true;
            int curval = 0;
            switch (curType)
            {
                case CurrencyType.Money:
                    curval = SecondaryStats.Money;
                    break;
                case CurrencyType.Gold:
                    if (allowbind)
                        return SecondaryStats.Gold >= amount - SecondaryStats.bindgold;
                    else
                        return SecondaryStats.Gold >= amount;
                case CurrencyType.LockGold:
                    if (allowbind)
                        return SecondaryStats.bindgold >= amount - SecondaryStats.Gold;
                    else
                        return SecondaryStats.bindgold >= amount;
                case CurrencyType.HonorValue:
                    curval = SecondaryStats.honor;
                    break;
                case CurrencyType.GuildContribution:
                    curval = SecondaryStats.contribute;
                    break;
                case CurrencyType.LotteryTicket:
                    break;
                case CurrencyType.BattleCoin:
                    curval = SecondaryStats.battlecoin;
                    break;
            }
            return curval >= amount;
        }

        public bool IsCurrencyAddable(CurrencyType curType, int amount)
        {
            int curval = 0;
            switch (curType)
            {
                case CurrencyType.Money:
                    curval = SecondaryStats.Money;
                    break;
                case CurrencyType.GuildContribution:
                    curval = SecondaryStats.contribute;
                    break;
                case CurrencyType.Gold:
                    curval = SecondaryStats.Gold;
                    break;
                case CurrencyType.LockGold:
                    curval = SecondaryStats.bindgold;
                    break;
                case CurrencyType.LotteryTicket:
                    curval = SecondaryStats.lotterypoints;
                    break;
                case CurrencyType.HonorValue:
                    curval = SecondaryStats.honor;
                    break;
                case CurrencyType.GuildGold:
                    curval = 0;
                    if (SecondaryStats.guildId > 0)
                    {
                        GuildStatsServer guildStats = GuildRules.GetGuildById(SecondaryStats.guildId);
                        if (guildStats != null)
                        {
                            long max = long.MaxValue;
                            return guildStats.guildGold <= max - amount;
                        }
                    }
                    break;
                default:
                    break;
            }
            return curval <= currencyMax - amount;
        }

        public int GetCurrencyAmt(CurrencyType curType)
        {
            switch (curType)
            {
                case CurrencyType.Money:
                    return SecondaryStats.Money;
                case CurrencyType.GuildContribution:
                    return SecondaryStats.contribute;
                case CurrencyType.Gold:
                    return SecondaryStats.Gold;
                case CurrencyType.LockGold:
                    return SecondaryStats.bindgold;
                case CurrencyType.LotteryTicket:
                    return SecondaryStats.lotterypoints;
                case CurrencyType.HonorValue:
                    return SecondaryStats.honor;
                case CurrencyType.BattleCoin:
                    return SecondaryStats.battlecoin;
                default:
                    return -1;
            }
        }

        public void LogCurrencyChange(string from, CurrencyType type, int count, int after)
        {
            byte currency_byte = (byte)type;
            string message = string.Format("source:{0}|type:{1}|amt:{2}|aft:{3}", from, currency_byte, count, after);
            Zealot.Logging.Client.LogClasses.CurrencyChange currencyChangeLog = new Zealot.Logging.Client.LogClasses.CurrencyChange();
            currencyChangeLog.userId = Slot.mUserId;
            currencyChangeLog.charId = Slot.GetCharId();
            currencyChangeLog.message = message;
            currencyChangeLog.source = from;
            currencyChangeLog.currency = currency_byte;
            currencyChangeLog.amt = count;
            currencyChangeLog.after = after;
            var ignoreAwait = Zealot.Logging.Client.LoggingAgent.Instance.LogAsync(currencyChangeLog);
        }

        #endregion Currency Methods

        #region BuffTimeStats Methods
        public override int AddSideEffect(SideEffect se, bool positiveEffect)
        {
            int slotid = base.AddSideEffect(se, positiveEffect);
            if (slotid < 0)
            {
                //if (slotid == -2)
                //Slot.ZRPC.CombatRPC.Ret_SendSystemMessage("sys_CantOverrideBuff", "", false, Slot);
                return slotid;
            }
            if (slotid == (byte)SideEffectsUtils.EffectHandleType.Buff) //set buff to local player so that client know the sideeffect on iteslef
            {
                // do conditioning
                // check for debuffs
                //find dups first
                long encodehead = 1 << 8;
                encodehead |= (byte)se.mOrigin;

                long encoded = encodehead << 32;
                encoded |= (uint)se.mOriginID;

                for (int i = 0; i < BuffTimeStats.EFFECT_BAG; i += 2)
                {
                    if (Convert.ToInt64(BuffTimeStats.Buffs[i]) == encoded)
                    {
                        BuffTimeStats.Buffs[i + 1] = se.GetTimeRemaining();
                        return slotid;
                    }

                    if (Convert.ToInt64(BuffTimeStats.Buffs[i]) == 0)
                    {
                        // since empty just insert, no checks
                        BuffTimeStats.Buffs[i] = encoded;
                        BuffTimeStats.Buffs[i + 1] = se.GetTimeRemaining();
                        return slotid;
                    }
                }

                for (int i = 0; i < BuffTimeStats.EFFECT_BAG; i += 2)
                {
                    UInt64 header = (UInt64)BuffTimeStats.Buffs[i];
                    if ((header & (1 << 40)) == 1)
                    {
                        //is a buff, can remove
                        if ((i + 2) < BuffTimeStats.EFFECT_BAG)
                        {
                            for (int j = i; j < BuffTimeStats.EFFECT_BAG - 2; ++j)
                            {
                                BuffTimeStats.Buffs[i] = BuffTimeStats.Buffs[i + 2];
                            }
                            break;
                        }
                    }
                }

                BuffTimeStats.Buffs[6] = encoded;
                BuffTimeStats.Buffs[7] = se.GetTimeRemaining();
            }

            else if(slotid == (byte)SideEffectsUtils.EffectHandleType.Debuff)
            {

                long encoded = (byte)se.mOrigin << 32;
                encoded |= (uint)se.mOriginID;

                for (int i = 0; i < BuffTimeStats.EFFECT_BAG; i += 2)
                {
                    if (Convert.ToInt64(BuffTimeStats.Buffs[i]) == encoded)
                    {
                        BuffTimeStats.Buffs[i + 1] = se.GetTimeRemaining();
                        return slotid;
                    }

                    if (Convert.ToInt64(BuffTimeStats.Buffs[i]) == 0)
                    {
                        // since empty just insert, no checks
                        BuffTimeStats.Buffs[i] = encoded;
                        BuffTimeStats.Buffs[i + 1] = se.GetTimeRemaining();
                        return slotid;
                    }
                }

                // find buffs
                for (int i = 0; i < BuffTimeStats.EFFECT_BAG; i += 2)
                {
                    long header = Convert.ToInt64(BuffTimeStats.Buffs[i]);
                    if ((header & (1 << 40)) == 1)
                    {
                        //is a buff, can remove
                        if ((i + 2) < BuffTimeStats.EFFECT_BAG)
                        {
                            for (int j = i; j < BuffTimeStats.EFFECT_BAG - 2; ++j)
                            {
                                BuffTimeStats.Buffs[i] = BuffTimeStats.Buffs[i + 2];
                            }
                            break;
                        }
                    }
                }
                // insert debuffs
                BuffTimeStats.Buffs[6] = encoded;
                BuffTimeStats.Buffs[7] = se.GetTimeRemaining();
            }

            return slotid;
        }

        public override int RemoveSideEffect(SideEffect se, bool positiveEffect)
        {
            int slotid = base.RemoveSideEffect(se, positiveEffect);
            if (slotid < 0)
                return -1;


            long encoded = 0;

            if (slotid == (byte)SideEffectsUtils.EffectHandleType.Buff)
            {
                long encodehead = 1 << 8;
                encodehead |= (uint)se.mOrigin;

                encoded = encodehead << 32;
                encoded |= (uint)se.mOriginID;
            }
            else if (slotid == (byte)SideEffectsUtils.EffectHandleType.Debuff)
            {
                encoded = (uint)se.mOrigin << 32;
                encoded |= (uint)se.mOriginID;
            }

            bool isFound = false;
            int index = 0;

            for (int i = 0; i < BuffTimeStats.EFFECT_BAG; i += 2)
            {
                if (Convert.ToInt64(BuffTimeStats.Buffs[i]) == encoded)
                {
                    BuffTimeStats.Buffs[i] = BuffTimeStats.Buffs[i + 1] = 0;
                    if (i < 5)
                    {
                        isFound = !isFound;
                        index = i;
                    }
                    break;
                }
            }

            if (isFound)
            {
                for (int i = index; i < BuffTimeStats.EFFECT_BAG - 2; ++i)
                {
                    BuffTimeStats.Buffs[i] = BuffTimeStats.Buffs[i + 2];
                }
            }

            return slotid;
        }

        public void ResumeSideEffects(SideEffectInventoryData seInv)
        {
            List<SideEffectDBInfo> selist = seInv.SEList;
            for (int index = 0; index < selist.Count; index++)
            {
                SideEffectJson sej = SideEffectRepo.GetSideEffect(selist[index].SEID);
                if (sej != null && (sej.persistentafterdeath))// || sej.persistentonlogout))
                {
                    SpecialSE se = new SpecialSE(sej);
                    se.mTotalElapsedTime = selist[index].Elapsed;
                    se.Apply(this);
                }
            }
        }

        public void RemoveSideEffect(int seid)
        {
            SideEffect se = base.GetSideEffect(seid);
            if (se != null)
            {
                SideEffectJson sej = SideEffectRepo.GetSideEffect(seid);
                bool positiveEffect = SideEffectsUtils.IsSideEffectPositive(sej);
                RemoveSideEffect(se, positiveEffect);
            }
        }

        #endregion BuffTimeStats Methods

        #region SocialStats Methods
        #region Social:removed future
        //public async Task SocialAcceptFriendRequest(string requestNameList)
        //{
        //    if (string.IsNullOrEmpty(requestNameList))
        //        return;
        //    string[] splitRequestNameList = requestNameList.Split('`');
        //    int requestNameListLen = splitRequestNameList.Length;
        //    if (requestNameListLen == 0)
        //        return;

        //    CollectionHandler<object> myFriendsList = SocialStats.friendList;
        //    Dictionary<string, SocialInfo> myFriendsDict = SocialStats.GetFriendListDict();
        //    int max = SocialInventoryData.MAX_GOOD_FRIENDS;
        //    if (myFriendsDict != null)
        //    {
        //        SocialInfo mySocialInfo = new SocialInfo(Name, (byte)PlayerSynStats.PortraitID, PlayerSynStats.jobsect, 0,
        //                                                 PlayerSynStats.Level, 0, PlayerSynStats.faction,
        //                                                 PlayerSynStats.guildName, true, 0);
        //        string mySocialInfoStr = mySocialInfo.ToString();
        //        friendRemoveSB.Clear();
        //        for (int i = 0; i < requestNameListLen; ++i)
        //        {
        //            string currRequestName = splitRequestNameList[i];
        //            if (myFriendsDict.Count < max && !myFriendsDict.ContainsKey(currRequestName))
        //            {
        //                GameClientPeer requestPeer = GameApplication.Instance.GetCharPeer(currRequestName);
        //                if (requestPeer != null && requestPeer.mPlayer != null) // Current player is online
        //                {
        //                    SocialStats currSocialStats = requestPeer.mPlayer.SocialStats;
        //                    Dictionary<string, SocialInfo> currFriendsDict = currSocialStats.GetFriendListDict();
        //                    if (currFriendsDict.Count >= max || currFriendsDict.ContainsKey(Name)) // Is max or already added me
        //                        continue;

        //                    int slotIdx = currSocialStats.GetAvailableSlotFriends();
        //                    mySocialInfo.localObjIdx = slotIdx;
        //                    currFriendsDict[Name] = mySocialInfo;
        //                    currSocialStats.friendList[slotIdx] = mySocialInfoStr;
        //                    requestPeer.mPlayer.SocialRemoveFriendRequest(Name); // Remove friend request from me if any

        //                    // Add to my friendlist
        //                    int mySlotIdx = SocialStats.GetAvailableSlotFriends();
        //                    PlayerSynStats playerSynStats = requestPeer.mPlayer.PlayerSynStats;
        //                    SocialInfo currSocialInfo = new SocialInfo(currRequestName, (byte)playerSynStats.PortraitID, playerSynStats.jobsect,
        //                                                               0, playerSynStats.Level,
        //                                                               0, playerSynStats.faction,
        //                                                               playerSynStats.guildName, true, mySlotIdx);
        //                    myFriendsDict[currRequestName] = currSocialInfo;
        //                    myFriendsList[mySlotIdx] = currSocialInfo.ToString();
        //                }
        //                else // Current player is offline
        //                {
        //                    Dictionary<string, object> dbInfo = await GameApplication.dbRepository.Character.GetSocialByName(currRequestName);
        //                    GameApplication.Instance.executionFiber.Enqueue(() =>
        //                    {
        //                        if (dbInfo.Count > 0)
        //                        {
        //                            string friendListStr = (string)dbInfo["friends"];
        //                            string[] splitStr = friendListStr.Split('|');
        //                            int splitStrLen = splitStr.Length;
        //                            if (splitStrLen < max && !SocialStrContains(splitStr, Name))
        //                            {
        //                                StringBuilder sb = new StringBuilder(friendListStr);
        //                                if (sb.Length != 0) sb.Append('|');
        //                                sb.Append(mySocialInfoStr);
        //                                Task dbTask = GameApplication.dbRepository.Character.UpdateSocialList(currRequestName, sb.ToString(), false);

        //                                // Add to my friendlist
        //                                string guildName = GuildRules.GetGuildNameById((int)dbInfo["guildid"]);
        //                                int mySlotIdx = SocialStats.GetAvailableSlotFriends();
        //                                SocialInfo currSocialInfo = new SocialInfo(currRequestName, (int)dbInfo["portraitid"],
        //                                                                           (byte)dbInfo["jobsect"], (byte)dbInfo["viplevel"],
        //                                                                           (int)dbInfo["progresslevel"], (int)dbInfo["combatscore"],
        //                                                                           (byte)dbInfo["faction"], guildName, false, mySlotIdx);
        //                                myFriendsDict[currRequestName] = currSocialInfo;
        //                                myFriendsList[mySlotIdx] = currSocialInfo.ToString();
        //                            }
        //                        }
        //                    });
        //                }
        //                if (friendRemoveSB.Length != 0)
        //                    friendRemoveSB.Append('`');
        //                friendRemoveSB.Append(currRequestName);
        //            }
        //        }
        //        SocialRemoveFriendRequest(friendRemoveSB.ToString()); // Remove accepted friend request
        //    }
        //}

        //public void SocialRemoveFriendRequest(string playerList)
        //{
        //    // Check if player is in ur friendrequest list
        //    // If true, remove from ur socialstats friendrequest list
        //    string[] splitPlayerList = playerList.Split('`');
        //    int playerListLen = splitPlayerList.Length;
        //    if (playerListLen == 0)
        //        return;

        //    CollectionHandler<object> myRequestsList = SocialStats.friendRequestList;
        //    Dictionary<string, SocialInfoBase> myRequestsDict = SocialStats.GetFriendRequestListDict();
        //    if (myRequestsDict != null && myRequestsDict.Count > 0 && myRequestsList != null)
        //    {
        //        int max = SocialInventoryData.MAX_GOOD_FRIENDS;
        //        for (int i = 0; i < playerListLen; ++i)
        //        {
        //            string currName = splitPlayerList[i];
        //            if (myRequestsDict.ContainsKey(currName))
        //            {
        //                myRequestsList[myRequestsDict[currName].localObjIdx] = null;
        //                myRequestsDict.Remove(currName);
        //            }
        //        }
        //    }
        //}

        //private bool SocialStrContains(string[] socialStrList, string value)
        //{
        //    int socialStrListLen = socialStrList.Length;
        //    for (int i = 0; i < socialStrListLen; ++i)
        //    {
        //        string currentStr = socialStrList[i];
        //        if (string.IsNullOrEmpty(currentStr))
        //            continue;
        //        int sepIdx = currentStr.IndexOf('`');
        //        if (sepIdx != -1 && currentStr.IndexOf(value, 0, sepIdx) != -1)
        //            return true;
        //    }
        //    return false;
        //}

        //public async Task SocialSendFriendRequest(string sendPlayerList)
        //{
        //    string[] splitSendPlayerList = sendPlayerList.Split('`');
        //    int sendPlayerListLen = splitSendPlayerList.Length;
        //    if (sendPlayerListLen == 0)
        //        return;

        //    Dictionary<string, SocialInfo> myFriendsDict = SocialStats.GetFriendListDict();
        //    int max = SocialInventoryData.MAX_GOOD_FRIENDS;
        //    if (myFriendsDict != null && myFriendsDict.Count < max)
        //    {
        //        StringBuilder friendAddSB = new StringBuilder();
        //        SocialInfoBase mySocialInfo = new SocialInfoBase(Name, (byte)PlayerSynStats.PortraitID, PlayerSynStats.jobsect,
        //                                                         0, PlayerSynStats.Level,
        //                                                         0, 0);
        //        string mySocialInfoStr = mySocialInfo.ToString();
        //        int minLvl = GameConstantRepo.GetConstantInt("Friends_UnlockLvl");
        //        for (int i = 0; i < sendPlayerListLen; ++i)
        //        {
        //            string currSendName = splitSendPlayerList[i];
        //            if (currSendName == Name)  // Is yourself
        //                continue;
        //            if (myFriendsDict.ContainsKey(currSendName)) // Already is your friend
        //            {
        //                Slot.ZRPC.CombatRPC.Ret_SocialReturnResult((byte)SocialReturnCode.Ret_AlreadyAdded, currSendName, Slot);
        //                continue;
        //            }

        //            GameClientPeer sendPeer = GameApplication.Instance.GetCharPeer(currSendName);
        //            if (sendPeer != null && sendPeer.mPlayer != null) // Current player is online
        //            {
        //                Player sendPlayer = sendPeer.mPlayer;
        //                if (sendPlayer.GetAccumulatedLevel() < minLvl)
        //                {
        //                    Slot.ZRPC.CombatRPC.Ret_SocialReturnResult((byte)SocialReturnCode.Ret_LevelNotEnough, minLvl.ToString(), Slot);
        //                    continue;
        //                }
        //                if (!sendPeer.GameSetting.AutoAcceptFriendRequest)
        //                {
        //                    SocialStats currSocialStats = sendPlayer.SocialStats;
        //                    Dictionary<string, SocialInfoBase> currRequestsDict = currSocialStats.GetFriendRequestListDict();
        //                    if (currRequestsDict.Count >= max || currRequestsDict.ContainsKey(Name)) // Is max or already added me
        //                        continue;

        //                    int slotIdx = currSocialStats.GetAvailableSlotRequests();
        //                    mySocialInfo.localObjIdx = slotIdx;
        //                    currRequestsDict[Name] = mySocialInfo;
        //                    currSocialStats.friendRequestList[slotIdx] = mySocialInfoStr;
        //                }
        //                else  // Immediate add as friend
        //                {
        //                    if (friendAddSB.Length != 0)
        //                        friendAddSB.Append('`');
        //                    friendAddSB.Append(currSendName);
        //                }
        //            }
        //            else // Current player is offline
        //            {
        //                Dictionary<string, object> dbInfo = await GameApplication.dbRepository.Character.GetSocialByName(currSendName);
        //                GameApplication.Instance.executionFiber.Enqueue(async () =>
        //                {
        //                    if (dbInfo.Count > 0)
        //                    {
        //                        int currProgressLvl = (int)dbInfo["progresslevel"];
        //                        if (currProgressLvl >= minLvl)
        //                        {
        //                            string gameSettingStr = (string)dbInfo["gamesetting"];
        //                            ServerSettingsData gamesetting = string.IsNullOrEmpty(gameSettingStr)
        //                                ? new ServerSettingsData() : ServerSettingsData.Deserialize(gameSettingStr);
        //                            if (gamesetting != null && gamesetting.AutoAcceptFriendRequest)
        //                            {
        //                                await SocialAcceptFriendRequest(currSendName);
        //                            }
        //                            else
        //                            {
        //                                string friendRequestListStr = (string)dbInfo["friendrequests"];
        //                                string[] splitStr = friendRequestListStr.Split('|');
        //                                int splitStrLen = splitStr.Length;
        //                                if (splitStrLen < max && !SocialStrContains(splitStr, Name))
        //                                {
        //                                    StringBuilder sb = new StringBuilder(friendRequestListStr);
        //                                    if (sb.Length != 0) sb.Append('|');
        //                                    sb.Append(mySocialInfoStr);
        //                                    Task dbTask = GameApplication.dbRepository.Character.UpdateSocialList(currSendName, sb.ToString(), true);
        //                                }
        //                            }
        //                        }
        //                        else
        //                            Slot.ZRPC.CombatRPC.Ret_SocialReturnResult((byte)SocialReturnCode.Ret_LevelNotEnough, minLvl.ToString(), Slot);
        //                    }
        //                    else
        //                        Slot.ZRPC.CombatRPC.Ret_SocialReturnResult((byte)SocialReturnCode.Ret_DoesNotExist, "", Slot);
        //                });
        //            }
        //        }
        //        string friendList = friendAddSB.ToString();
        //        Task task;
        //        if (!string.IsNullOrEmpty(friendList))
        //            task = SocialAcceptFriendRequest(friendList);
        //    }
        //}

        //public async Task SocialRemoveFriend(string playerName)
        //{
        //    CollectionHandler<object> myFriendsList = SocialStats.friendList;
        //    Dictionary<string, SocialInfo> myFriendsDict = SocialStats.GetFriendListDict();
        //    if (myFriendsDict != null && myFriendsDict.Count > 0 && myFriendsList != null)
        //    {
        //        if (myFriendsDict.ContainsKey(playerName))
        //        {
        //            myFriendsList[myFriendsDict[playerName].localObjIdx] = null;
        //            myFriendsDict.Remove(playerName);

        //            // Remove from friend's list
        //            GameClientPeer peer = GameApplication.Instance.GetCharPeer(playerName);
        //            if (peer != null && peer.mPlayer != null) // Current player is online
        //            {
        //                SocialStats currSocialStats = peer.mPlayer.SocialStats;
        //                CollectionHandler<object> currFriendsList = currSocialStats.friendList;
        //                Dictionary<string, SocialInfo> currFriendsDict = currSocialStats.GetFriendListDict();
        //                if (currFriendsDict.ContainsKey(Name))
        //                {
        //                    currFriendsList[currFriendsDict[Name].localObjIdx] = null;
        //                    currFriendsDict.Remove(Name);
        //                }
        //            }
        //            else // Current player is offline
        //            {
        //                var dbInfo = await GameApplication.dbRepository.Character.GetSocialByName(playerName);
        //                GameApplication.Instance.executionFiber.Enqueue(() =>
        //                {
        //                    if (dbInfo.Count > 0)
        //                    {
        //                        string friendListStr = (string)dbInfo["friends"];
        //                        string[] splitStr = friendListStr.Split('|');
        //                        int splitStrLen = splitStr.Length;
        //                        StringBuilder sb = new StringBuilder();
        //                        for (int i = 0; i < splitStrLen; ++i)
        //                        {
        //                            string currentStr = splitStr[i];
        //                            int idx = currentStr.IndexOf('`');
        //                            if (idx != -1 && currentStr.IndexOf(Name, 0, idx) != -1)
        //                                continue;
        //                            if (i != 0) sb.Append('|');
        //                            sb.Append(currentStr);
        //                        }
        //                        Task dbTask = GameApplication.dbRepository.Character.UpdateSocialList(playerName, sb.ToString(), false);
        //                    }
        //                });
        //            }
        //        }
        //    }
        //}

        //public async Task SocialGetRecommendedFriends()
        //{
        //    if ((DateTime.Now - friendRecommendedCD).TotalSeconds <= 3)
        //    {
        //        Slot.ZRPC.CombatRPC.Ret_SocialReturnResult((byte)SocialReturnCode.Ret_OnCooldown, "", Slot);
        //        return;
        //    }

        //    // Get Dict of peers
        //    Dictionary<string, GameClientPeer> charPeerDict = GameApplication.Instance.GetCharPeerDictCopy();
        //    if (charPeerDict == null)
        //        return;

        //    friendRecommendedCD = DateTime.Now;
        //    friendSB.Clear();
        //    friendRemoveSB.Clear();
        //    friendRemoveSB.AppendFormat("|{0}|", Name);
        //    charPeerDict.Remove(Name); // Remove your name from peer dict
        //    Dictionary<string, SocialInfo> myFriendsDict = SocialStats.GetFriendListDict();
        //    var myFriendsDictKeys = myFriendsDict.Keys;
        //    foreach (string friend in myFriendsDictKeys) // Append your friends to filter dict
        //    {
        //        friendRemoveSB.AppendFormat("{0}|", friend);
        //        charPeerDict.Remove(friend); // Remove your friends from dict
        //    }

        //    int peerDictCnt = charPeerDict.Count, foundCnt = 0;
        //    if (peerDictCnt > 0) // Get random friends from online peer list first
        //    {
        //        int min = (peerDictCnt < 100) ? peerDictCnt : 100;
        //        int max = (peerDictCnt < 150) ? peerDictCnt : 150;
        //        // Random sample size
        //        int sampleSize = (min == max) ? peerDictCnt : GameUtils.RandomInt(min, max);

        //        friendsRecDict.Clear();
        //        int minLvl = GameConstantRepo.GetConstantInt("Friends_UnlockLvl");
        //        for (int i = 0; i < sampleSize; ++i)
        //        {
        //            int currIdx = GameUtils.RandomInt(0, sampleSize - 1);
        //            GameClientPeer currPeer = charPeerDict.Values.ElementAt(currIdx);
        //            string currCharName = currPeer.mChar;
        //            if (myFriendsDict.ContainsKey(currCharName) || friendsRecDict.ContainsKey(currCharName))
        //                continue;

        //            PlayerSynStats currPlayerSynStats = currPeer.mPlayer.PlayerSynStats;
        //            if (currPlayerSynStats.Level < minLvl)
        //                continue;

        //            if (foundCnt != 0) friendSB.Append('|');
        //            friendSB.AppendFormat("{0}`{1}`{2}`{3}`{4}`{5}", currCharName, currPlayerSynStats.PortraitID, currPlayerSynStats.jobsect,
        //                                  0, currPlayerSynStats.Level,
        //                                  0);
        //            friendRemoveSB.AppendFormat("{0}|", currCharName);
        //            friendsRecDict[currCharName] = currCharName;
        //            if (++foundCnt >= 5)
        //                break;
        //        }
        //    }

        //    if (foundCnt < 5) // If can't find more than 5 from peer list, search from db
        //    {
        //        var dbInfo = await GameApplication.dbRepository.Character.GetSocialRandom(friendRemoveSB.ToString());
        //        GameApplication.Instance.executionFiber.Enqueue(() =>
        //        {
        //            int dbInfoCnt = dbInfo.Count;
        //            if (dbInfoCnt > 0)
        //            {
        //                int dbIdx = 0;
        //                for (int i = foundCnt; i < 5; ++i)
        //                {
        //                    if (dbIdx >= dbInfoCnt)
        //                        break;
        //                    Dictionary<string, object> infoDict = dbInfo[dbIdx];
        //                    if (i != 0) friendSB.Append('|');
        //                    friendSB.AppendFormat("{0}`{1}`{2}`{3}`{4}`{5}", infoDict["charname"], infoDict["portraitid"], infoDict["jobsect"],
        //                                          infoDict["viplevel"], infoDict["progresslevel"], infoDict["combatscore"]);
        //                    ++dbIdx;
        //                }
        //            }
        //            Slot.ZRPC.CombatRPC.Ret_SocialReturnResult((byte)SocialReturnCode.Ret_RecommendedResult, friendSB.ToString(), Slot);
        //        });
        //    }
        //    else
        //    {
        //        Slot.ZRPC.CombatRPC.Ret_SocialReturnResult((byte)SocialReturnCode.Ret_RecommendedResult, friendSB.ToString(), Slot);
        //    }
        //}

        //public void SocialUpdateFriendsInfo()
        //{
        //    Dictionary<string, SocialInfo> myFriendsDict = SocialStats.GetFriendListDict();
        //    foreach (var kvp in myFriendsDict)
        //    {
        //        SocialInfo socialInfo = kvp.Value;
        //        GameClientPeer peer = GameApplication.Instance.GetCharPeer(kvp.Key);
        //        if (peer != null && peer.mPlayer != null) // Friend is online
        //        {
        //            Player currPlayer = peer.mPlayer;
        //            PlayerSynStats currPlayerStats = currPlayer.PlayerSynStats;
        //            socialInfo.portraitId = currPlayerStats.PortraitID;
        //            socialInfo.vipLvl = 0;
        //            socialInfo.charLvl = currPlayerStats.Level;
        //            //socialInfo.combatScore = currPlayer.LocalCombatStats.CombatScore;
        //            socialInfo.guildName = currPlayerStats.guildName;
        //            socialInfo.isOnline = true;
        //        }
        //        else
        //            socialInfo.isOnline = false;
        //        SocialStats.friendList[socialInfo.localObjIdx] = socialInfo.ToString();
        //    }
        //}
        #endregion
        #endregion SocialStats Methods

        #region Guild Methods

        public void ApplyGuildPassiveSE(GuildStatsServer guildStats)
        {
            UpdateAllGuildTechBonus(guildStats.mGuildTechDict, true);
        }

        public void OnCreateGuild(GuildStatsServer guildStats)
        {
            DeductGold(GuildRepo.GetValue("CreateGuildCost"), true, true, "Guild_Create");  // deduct gold here
            SecondaryStats.guildId = guildStats.guildId;
            SecondaryStats.guildRank = (byte)GuildRankType.Leader;
            PlayerSynStats.guildName = guildStats.name;
            Slot.ZRPC.CombatRPC.Ret_GuildAdd((byte)GuildReturnCode.Success, Slot);
            AddGuildLocalObject(Slot);
            OnGuildChange();
        }

        public void OnJoinGuild(GuildStatsServer guildStats)
        {
            SecondaryStats.guildId = guildStats.guildId;
            SecondaryStats.guildRank = (byte)GuildRankType.Member;
            PlayerSynStats.guildName = guildStats.name;
            Slot.ZRPC.CombatRPC.Ret_GuildJoin((byte)GuildReturnCode.Success, Slot);
            AddGuildLocalObject(Slot);
            OnGuildChange();
            ApplyGuildPassiveSE(guildStats);
        }

        public void OnLeaveGuild(GuildStatsServer guildStats, bool kicked)
        {
            PlayerSynStats.guildName = "";
            Slot.CharacterData.ClearGuild();
            SecondaryStats.guildId = 0;
            OnGuildChange();
            SecondaryStats.guildRank = (byte)GuildRankType.Member;
            SecondaryStats.guildLeaveGuildCDEnd = DateTime.Now.AddSeconds(GuildRepo.GetValue("LeaveGuildCooldownTime")).Ticks;
            Slot.ZRPC.CombatRPC.Ret_GuildLeave(kicked ? (byte)GuildReturnCode.BeKicked : (byte)GuildReturnCode.Success, Slot);
            UpdateAllGuildTechBonus(guildStats.mGuildTechDict, false);
        }

        public void OnGuildTechLevelUp(GuildTechLevelJson techLevelJson)
        {
            float stats = techLevelJson.stats;
            if (techLevelJson.level > 1)
                stats -= GuildRepo.GetGuildTechByTypeAndLevel(techLevelJson.type, techLevelJson.level - 1).stats;
            if (stats <= 0)
                return;
            UpdateGuildTechBonus(techLevelJson.type, stats, true, (PlayerCombatStats)CombatStats);
            StoreRules.UpdateRefreshCategoryFree(StoreRepo.GetStoreOrder(UIStoreLinkType.GuildStore), Slot); //Update store
        }

        public void UpdateGuildQuestDailyTimes()
        {
            Slot.CharacterData.GuildQuests.daymaxtimes = Mathf.FloorToInt(GuildRules.GetGuildTechStats(this, GuildTechType.Quest));
        }

        private void UpdateAllGuildTechBonus(Dictionary<GuildTechType, int> guildTechDict, bool add)
        {
            PlayerCombatStats combatStats = (PlayerCombatStats)CombatStats;
            bool needComputeAll = false;
            foreach (var kvp in guildTechDict)
            {
                GuildTechLevelJson techLevelJson = GuildRepo.GetGuildTechByTypeAndLevel(kvp.Key, kvp.Value);
                if (techLevelJson == null)
                    continue;
                float stats = techLevelJson.stats;
                if (stats > 0)
                {
                    if (UpdateGuildTechBonus(kvp.Key, add ? stats : -stats, false, combatStats))
                        needComputeAll = true;
                }
            }
            if (needComputeAll)
                combatStats.ComputeAll();
        }

        private bool UpdateGuildTechBonus(GuildTechType type, float stats, bool computeAll, PlayerCombatStats combatStats)
        {
            int statsMultiple10 = Mathf.CeilToInt(stats * 10); //support one decimal
            switch (type)
            {
                case GuildTechType.Health:
                    combatStats.AddToField(FieldName.HealthPercBonus, statsMultiple10);
                    break;

                case GuildTechType.Attack:
                    combatStats.AddToField(FieldName.AttackPercBonus, statsMultiple10);
                    break;

                case GuildTechType.Armor:
                    combatStats.AddToField(FieldName.ArmorPercBonus, statsMultiple10);
                    break;

                case GuildTechType.Accuracy:
                    combatStats.AddToField(FieldName.AccuracyPercBonus, statsMultiple10);
                    break;

                case GuildTechType.Evasion:
                    combatStats.AddToField(FieldName.EvasionPercBonus, statsMultiple10);
                    break;

                case GuildTechType.Critical:
                    combatStats.AddToField(FieldName.CriticalPercBonus, statsMultiple10);
                    break;

                case GuildTechType.CoCritical:
                    combatStats.AddToField(FieldName.CocriticalPercBonus, statsMultiple10);
                    break;
                //case GuildTechType.CriticalDamage:
                //    combatStats.AddToField(FieldName.CriticalDamagePercBonus, statsMultiple10);
                //    break;
                //case GuildTechType.CoCriticalDamage:
                //    combatStats.AddToField(FieldName.CoCriticalDamagePercBonus, statsMultiple10);
                //    break;
                default:
                    return false;
            }
            if (computeAll)
                combatStats.ComputeAll();
            return true;
        }

        public void NewGuildWeek()
        {
            CharacterData charData = Slot.CharacterData;
            charData.NewGuildWeekDt = GuildRules.bossPrevResetDT;
            charData.GuildBossRewardRealm = 0;
        }

#endregion Guild Methods

#region Party Methods

        public bool IsInParty()
        {
            return PlayerSynStats.Party > 0 && PartyStats != null;
        }

        public void OnCreateParty(int partyId)
        {
            PlayerSynStats.Party = partyId;
            AddPartyLocalObject(Slot);
        }

        public void OnJoinParty(int partyId)
        {
            PlayerSynStats.Party = partyId;
            AddPartyLocalObject(Slot);
            Slot.ZRPC.CombatRPC.Ret_SendSystemMessage("sys_party_JoinPartySuccess", "", false, Slot);
        }

        public void OnLeaveParty(LeavePartyReason reason)
        {
            PlayerSynStats.Party = 0;
            PartyStats = null;
            switch (reason)
            {
                case LeavePartyReason.Self:
                case LeavePartyReason.Disband:
                    Slot.ZRPC.CombatRPC.Ret_SendSystemMessage("sys_party_LeavePartySuccess", "", false, Slot);
                    break;

                case LeavePartyReason.Kick:
                    Slot.ZRPC.CombatRPC.Ret_SendSystemMessage("sys_party_KickedFromParty", "", false, Slot);
                    break;
            }

            // check for any hero that is temporarily unsummoned, if have, need to summon back
            // have summoned heroid in record but no spawned entity
            if (HeroStats.SummonedHeroId > 0 && !HeroStats.IsHeroSummoned(HeroStats.SummonedHeroId))
                HeroStats.SummonHero(HeroStats.SummonedHeroId);
        }

#endregion Party Methods

#region TongbaoCostBuff Methods

        public void ApplyTongbaoCostBuffPassiveSE()
        {
            PlayerCombatStats combatStats = (PlayerCombatStats)CombatStats;
            foreach (KeyValuePair<int, List<IPassiveSideEffect>> entry in mTongbaoCostBuffPassiveSEs)
            {
                for (int i = 0; i < entry.Value.Count; i++)
                {
                    IPassiveSideEffect se = entry.Value[i];
                    se.RemovePassive();
                }
                entry.Value.Clear();
            }
            mTongbaoCostBuffPassiveSEs.Clear();

            if (TongbaoCostBuff.IsReach(SecondaryStats.costbuffid, SecondaryStats.costbuffgold))
            {
                AddTongbaoCostBuffPassiveSE();
            }

            combatStats.ComputeAll();
        }

        public void AddTongbaoCostBuffPassiveSE()
        {
            int skillid = TongbaoCostBuff.CostBuffData.skillid;
            List<SideEffectJson> sideEffects = SkillRepo.GetSkillSideEffects(skillid).mTarget;
            if (sideEffects == null)
                return;

            for (int i = 0; i < sideEffects.Count; i++)
            {
                SideEffect se = SideEffectFactory.CreateSideEffect(sideEffects[i], SEORIGINID.NONE, -1, true);
                IPassiveSideEffect pse = se as IPassiveSideEffect;
                if (pse == null)
                {
                    System.Diagnostics.Debug.WriteLine("Warning! Invalid sideeffect added as passive seid: " + sideEffects[i].id + " TongbaoCostBuff.CostBuffData.skillid: " + skillid);
                    continue;
                }
                pse.AddPassive(this);
                if (mTongbaoCostBuffPassiveSEs.ContainsKey(skillid))
                    mTongbaoCostBuffPassiveSEs[skillid].Add(pse);
                else
                    mTongbaoCostBuffPassiveSEs.Add(skillid, new List<IPassiveSideEffect>() { pse });
            }
        }

#endregion TongbaoCostBuff Methods

        public void SetPortraitID(int portraitID)
        {
            PlayerSynStats.PortraitID = portraitID;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (mRespawnTimer != null)
            {
                mInstance.StopTimer(mRespawnTimer);
                mRespawnTimer = null;
            }
            if (mSaveCharacterTimer != null)
            {
                mInstance.StopTimer(mSaveCharacterTimer);
                mSaveCharacterTimer = null;
            }
            if (mManaRegenTimer != null)
            {
                mInstance.StopTimer(mManaRegenTimer);
                mManaRegenTimer = null;
            }

            ((ServerEntitySystem)EntitySystem).UnregisterPlayerName(Name);
        }

        public override bool IsInvalidTarget()
        {
            return !IsAlive() || IsInSafeZone();
        }

        public override bool IsInSafeZone()
        {
            return LocalCombatStats.IsInSafeZone || InspectMode; //inspect mode should not be targetd.
        }

        /// <summary>
        /// this function is just for  setting localobject status, it is called after ControlStats is updated,
        /// </summary>
        public override void OnControlChanged()
        {
            base.OnControlChanged();
            //LocalCombatStats.Stun = ControlStats.Stuned;
            //LocalCombatStats.Root = ControlStats.Rooted;
            //LocalCombatStats.Silence = ControlStats.Silenced;
            //LocalCombatStats.Disarmed = ControlStats.Disarmed;
        }

        public void SetArenaRecord(ArenaPlayerRecord record)
        {
            //TODO: remove all sideeffects and passive sideeffects.
            StopAllSideEffects();
            ResetPassiveSkills();
            //SkillPassiveStats.ResetAll();
            CharacterData charData = Slot.CharacterData;
            CharacterCreationData CharacterCreationData = record.CharacterCreationData;
            CharacterCreationData.Name = Name;
            CharacterCreationData.JobSect = PlayerSynStats.jobsect;
            CharacterCreationData.ProgressLevel = PlayerSynStats.Level;
            CharacterCreationData.EquipmentInventory = ObjectClone.CloneJson(charData.EquipmentInventory);
            //CharacterCreationData.EquipScore = LocalCombatStats.CombatScore;

            BonusCombatStats BonusCombatStats = record.BonusCombatStats;
            BonusCombatStats.HealthBonus = (int)CombatStats.GetField(FieldName.HealthBonus);
            BonusCombatStats.HealthPercentBonus = (int)CombatStats.GetField(FieldName.HealthPercBonus);
            BonusCombatStats.AttackBonus = (int)CombatStats.GetField(FieldName.AttackBonus);
            BonusCombatStats.AttackPercentBonus = (int)CombatStats.GetField(FieldName.AttackPercBonus);
            BonusCombatStats.ArmorBonus = (int)CombatStats.GetField(FieldName.ArmorBonus);
            BonusCombatStats.ArmorPercentBonus = (int)CombatStats.GetField(FieldName.ArmorPercBonus);
            BonusCombatStats.AccuracyBonus = (int)CombatStats.GetField(FieldName.AccuracyBonus);
            BonusCombatStats.AccuracyPercentBonus = (int)CombatStats.GetField(FieldName.AccuracyPercBonus);
            BonusCombatStats.EvasionBonus = (int)CombatStats.GetField(FieldName.EvasionBonus);
            BonusCombatStats.EvasionPercentBonus = (int)CombatStats.GetField(FieldName.EvasionPercBonus);

            BonusCombatStats.CriticalBonus = (int)CombatStats.GetField(FieldName.CriticalBonus);
            BonusCombatStats.CriticalPercentBonus = (int)CombatStats.GetField(FieldName.CriticalPercBonus);
            BonusCombatStats.CoCriticalBonus = (int)CombatStats.GetField(FieldName.CocriticalBonus);
            BonusCombatStats.CoCriticalPercentBonus = (int)CombatStats.GetField(FieldName.CocriticalPercBonus);
            BonusCombatStats.CriticalDmgBonus = (int)CombatStats.GetField(FieldName.CriticalDamageBonus);
            //BonusCombatStats.CriticalDmgPercentBonus = (int)CombatStats.GetField(FieldName.CriticalDamagePercBonus);
            //BonusCombatStats.CoCriticalDmgBonus = (int)CombatStats.GetField(FieldName.CoCriticalDamageBonus);
            //BonusCombatStats.CoCriticalDmgPercentBonus = (int)CombatStats.GetField(FieldName.CoCriticalDamagePercBonus);

            BonusCombatStats.AbsorbDmgBonus = (int)CombatStats.GetField(FieldName.AbsorbDamageBonus);

            record.SkillInventory = ObjectClone.CloneJson<SkillInventoryData>(charData.SkillInventory);
            //ArenaSkillLevel ArenaSkillLevel = record.ArenaSkillLevel;
            //NewSkillComboData skillComboData = charData.NewHeroInvData.comboSkill;
            //ArenaSkillLevel.RedLvl = mNewHeroController.GetHeroSkillLevel(skillComboData.hero_main_red);
            //ArenaSkillLevel.RedSubLvl = mNewHeroController.GetHeroSkillLevel(skillComboData.hero_sub_red);
            //ArenaSkillLevel.GreenLvl = mNewHeroController.GetHeroSkillLevel(skillComboData.hero_main_green);
            //ArenaSkillLevel.GreenSubLvl = mNewHeroController.GetHeroSkillLevel(skillComboData.hero_sub_green);
            //ArenaSkillLevel.BlueLvl = mNewHeroController.GetHeroSkillLevel(skillComboData.hero_main_blue);
            //ArenaSkillLevel.BlueSubLvl = mNewHeroController.GetHeroSkillLevel(skillComboData.hero_sub_blue);

            ArenaTalentStats ArenaTalentStats = record.ArenaTalentStats;
        }

        public void DebugSendCombatStats(GameClientPeer peer)
        {
            peer.ZRPC.CombatRPC.SendMessageToConsoleCmd("*** CombatStats of " + Name + " Start ***", peer);

            //for (int i = 0; i < (int) FieldName.LastField; i++)
            //{
            //    FieldName currFieldName = (FieldName)i;
            //    object val = CombatStats.GetField(currFieldName);
            //    string desc = currFieldName.ToString() + " = " + val.ToString();
            //    peer.ZRPC.CombatRPC.SendMessageToConsoleCmd(desc, peer);
            //}

            List<FieldName>[] field = CombatStats.GetAllFields();
            for (int i = 0; i < field.Length; i++)
            {
                List<FieldName> currentTierNames = field[i];
                foreach (FieldName name in currentTierNames)
                {
                    object val = CombatStats.GetField(name);
                    string desc = name.ToString() + " = " + val.ToString();
                    peer.ZRPC.CombatRPC.SendMessageToConsoleCmd(desc, peer);
                }
            }

            peer.ZRPC.CombatRPC.SendMessageToConsoleCmd("MoveSpeed = " + PlayerStats.MoveSpeed, peer);

            peer.ZRPC.CombatRPC.SendMessageToConsoleCmd("***CombatStats of " + Name + " End ***", peer);
        }

        public void DebugSendSideEffectsInfo(GameClientPeer peer)
        {
            peer.ZRPC.CombatRPC.SendMessageToConsoleCmd("*** Positive Sideeffects of " + Name + " Start ***", peer);
            int count = 0;
            for (int i = 0; i < mSideEffectsPos.Length; i++)
            {
                SideEffect se = mSideEffectsPos[i];
                if (se != null)
                {
                    peer.ZRPC.CombatRPC.SendMessageToConsoleCmd((++count) + ") " + se.mSideeffectData.id + " : " + se.mSideeffectData.name + " " + (int)(se.GetTimeRemaining() / 1000) + "sec", peer);
                }
            }
            peer.ZRPC.CombatRPC.SendMessageToConsoleCmd("*** Positive Sideeffects of " + Name + " End ***", peer);

            count = 0;
            peer.ZRPC.CombatRPC.SendMessageToConsoleCmd("*** Negative Sideeffects of " + Name + " Start ***", peer);
            for (int i = 0; i < mSideEffectsNeg.Length; i++)
            {
                SideEffect se = mSideEffectsNeg[i];
                if (se != null)
                {
                    peer.ZRPC.CombatRPC.SendMessageToConsoleCmd((++count) + ") " + se.mSideeffectData.id + " : " + se.mSideeffectData.name + " " + (int)(se.GetTimeRemaining() / 1000) + "sec", peer);
                }
            }
            peer.ZRPC.CombatRPC.SendMessageToConsoleCmd("*** Negative Sideeffects of " + Name + " End ***", peer);
        }

        public void SetSkillCDEnd(int skillindex, long endtime)
        {
            mSkillCDEnd[skillindex] = endtime;
        }

        public void RemoveMyBuff(int sideID)
        {
            for (int i = 0; i < this.mSideEffectsPos.Length; ++i)
            {
                if (this.mSideEffectsPos[i] != null)
                {
                    if (this.mSideEffectsPos[i].mSideeffectData.id == sideID)
                    {
                        this.mSideEffectsPos[i].Stop();
                        return;
                    }
                }
            }
        }

        public override void onDragged(Vector3 pos, float dur, float speed)
        {
            Slot.ZRPC.CombatRPC.OnPlayerDragged(pos.ToRPCPosition(), dur, speed, Slot);
        }

        public void TestComboSkill(int mainskillid, SideEffectJson mainsej, SideEffectJson subsej, int lvl = 1, float dur = 1.0f)
        {
        }

        public void TongbaoCostBuffAdd(int amount)
        {
            //SystemSwitch Check
            if (!SystemSwitch.mSysSwitch.IsOpen(SysSwitchType.DialogTongbaoCostBuff))
                return;

            bool is_in = TongbaoCostBuff.CostBuffData.CheckInTime();
            if (is_in == true)
            {
                int cbid = TongbaoCostBuff.CostBuffData.id;
                int need_cost = TongbaoCostBuff.CostBuffData.costamount;
                if (cbid == SecondaryStats.costbuffid)
                {
                    int pre = SecondaryStats.costbuffgold;
                    SecondaryStats.costbuffgold += amount;
                    if (pre < need_cost && SecondaryStats.costbuffgold >= need_cost)
                    {
                        // record over costamount
                        log.InfoFormat("[{0}]: {1}:{2}", "TongbaoCostBuffAdd(Over)", GetPersistentID(), Name);
                        // give reward
                        ApplyTongbaoCostBuffPassiveSE();
                    }
                }
                else
                {
                    SecondaryStats.costbuffid = cbid;
                    SecondaryStats.costbuffgold = amount;
                }

                Zealot.Logging.Client.LogClasses.TongbaoBuffCost slog = new Zealot.Logging.Client.LogClasses.TongbaoBuffCost();
                if (Slot != null)
                {
                    slog.userId = Slot.mUserId;
                    slog.charId = Slot.GetCharId();
                }
                bool isreach = (SecondaryStats.costbuffgold >= need_cost ? true : false);
                string message = string.Format("player:{0}|tbid:{1}|addGold:{2}|totalGold:{3}|isArrived:{4}",
                    Name,
                    SecondaryStats.costbuffid,
                    amount,
                    SecondaryStats.costbuffgold,
                    isreach);
                slog.message = message;
                slog.tbid = SecondaryStats.costbuffid;
                slog.addGold = amount;
                slog.totalGold = SecondaryStats.costbuffgold;
                slog.isArrived = isreach;
                var ignoreAwait = Zealot.Logging.Client.LoggingAgent.Instance.LogAsync(slog);

                //log.InfoFormat("[{0}]: {1}:{2}:{3}:{4}:{5}", "TongbaoCostBuffAdd", GetPersistentID(), Name, cbid, SecondaryStats.costbuffgold, amount);
            }
        }

        public bool CheckInvitePvpAskInCity(GameClientPeer askerpeer, GameClientPeer targetpeer, bool fromasker)
        {
            if (askerpeer.mPlayer.mInstance.mIsCity && targetpeer.mPlayer.mInstance.mIsCity)
                return true;
            else
            {
                string askername = askerpeer.mPlayer.Name;
                string targetname = targetpeer.mPlayer.Name;
                askerpeer.ZRPC.CombatRPC.Ret_InvitePvpResult((byte)InvitePvpReturnCode.Ret_NotInCity, targetname, askerpeer);
                if (!fromasker)
                    targetpeer.ZRPC.CombatRPC.Ret_InvitePvpResult((byte)InvitePvpReturnCode.Ret_NotInCity, askername, targetpeer);
                return false;
            }
        }

        public bool CheckInvitePvpAskInRealm(GameClientPeer askerpeer, GameClientPeer targetpeer, bool fromasker)
        {
            string askername = askerpeer.mPlayer.Name;
            string targetname = targetpeer.mPlayer.Name;
            if (RealmRules.GetInvitePVPData(askername) != null || RealmRules.GetInvitePVPData(targetname) != null)
            {
                askerpeer.ZRPC.CombatRPC.Ret_InvitePvpResult((byte)InvitePvpReturnCode.Ret_InRealm, targetname, askerpeer);
                if (!fromasker)
                    targetpeer.ZRPC.CombatRPC.Ret_InvitePvpResult((byte)InvitePvpReturnCode.Ret_InRealm, askername, targetpeer);
                return true;
            }
            return false;
        }

        public void InvitePvpAsk(string targetname)
        {
            GameClientPeer targetpeer = GameApplication.Instance.GetCharPeer(targetname);
            if (targetpeer != null && targetpeer.mPlayer != null)
            {
                //target online, check is in realm
                if (CheckInvitePvpAskInRealm(Slot, targetpeer, true) == true)
                    return;

                //check is in city
                if (CheckInvitePvpAskInCity(Slot, targetpeer, true) == true)
                    targetpeer.ZRPC.CombatRPC.Ret_InvitePvpResult((byte)InvitePvpReturnCode.Ret_AskToTarget, Name, targetpeer);
            }
            else
            {
                Slot.ZRPC.CombatRPC.Ret_InvitePvpResult((byte)InvitePvpReturnCode.Ret_NotOnline, targetname, Slot);
            }
        }

        public void InvitePvpReply(string askername, int reply)
        {
            InvitePvpReturnCode mereply = (InvitePvpReturnCode)reply;
            GameClientPeer askerpeer = GameApplication.Instance.GetCharPeer(askername);
            if (askerpeer != null && askerpeer.mPlayer != null)
            {
                //asker online
                if (mereply == InvitePvpReturnCode.Ret_IngToAsker)
                    askerpeer.ZRPC.CombatRPC.Ret_InvitePvpResult((byte)InvitePvpReturnCode.Ret_IngToAsker, Name, askerpeer);
                else if (mereply == InvitePvpReturnCode.Ret_NoToAsker)
                    askerpeer.ZRPC.CombatRPC.Ret_InvitePvpResult((byte)InvitePvpReturnCode.Ret_NoToAsker, Name, askerpeer);
                else if (mereply == InvitePvpReturnCode.Ret_YesToAsker)
                {
                    //check is in realm
                    if (CheckInvitePvpAskInRealm(askerpeer, Slot, false) == true)
                        return;

                    //check is in city
                    if (CheckInvitePvpAskInCity(askerpeer, Slot, false) == true)
                    {
                        RealmRules.PareInvitePVP(askername, Name);
                        RealmRules.EnterRealmById(46, askerpeer);
                        RealmRules.EnterRealmById(46, Slot);
                        askerpeer.mQuestExtraRewardsCtrler.UpdateTask(QuestExtraType.PvpTimes);
                        Slot.mQuestExtraRewardsCtrler.UpdateTask(QuestExtraType.PvpTimes);
                    }
                }
            }
        }

        public void UpdateTutorialList(int bitpos)
        {
            int pre_val = SecondaryStats.tutorialreddot;
            int after_val = 1 << bitpos | pre_val;
            SecondaryStats.tutorialreddot = after_val;
        }

        public override int GetParty()
        {
            if (PlayerSynStats != null)
                return PlayerSynStats.Party;
            return -1;
        }

        public void UpdateBasicAttack(EquipmentJson equipment)
        {
            PartsType weaponType = equipment.partstype;
            string genderStr = (PlayerSynStats.Gender == 0) ? "M" : "F";
            SkillData skill = SkillRepo.GetGenderWeaponBasicAttackData(weaponType, 1, genderStr);
            SkillStats.basicAttack1SId = skill.skillJson.id;
        }

        public void UpdateAchievement(AchievementObjectiveType objType, string target = "-1", bool isNonTarget = true, int count = 1, bool increment = true)
        {
            AchievementStats.UpdateAchievement(objType, target, isNonTarget, count, increment);
        }

        public void SetQuestRealmId(int questid)
        {
            if (questid == -1)
            {
                Slot.QuestController.FailQuest(Slot.CharacterData.QuestInventory.RealmQuestId);
            }
            Slot.CharacterData.QuestInventory.RealmQuestId = questid;
        }

        public bool HasSkill(int id)
        {
            foreach (int skillid in SkillStats.AutoSkill)
            {
                if (skillid != 0 && skillid == id)
                    return true;
            }

            foreach (int skillid in SkillStats.EquippedSkill)
            {
                if (skillid != 0 && skillid == id)
                    return true;
            }

            return false;
        }
    }
}
