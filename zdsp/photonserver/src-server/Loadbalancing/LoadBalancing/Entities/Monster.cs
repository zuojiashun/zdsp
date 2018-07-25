﻿
namespace Zealot.Server.Entities
{
    using UnityEngine;
    using System.Collections.Generic;
    using Zealot.Common;
    using Zealot.Common.RPC;
    using Zealot.Common.Actions;
    using Zealot.Common.Entities;
    using Zealot.Server.Actions;
    using Photon.LoadBalancing.GameServer;
    using Kopio.JsonContracts;
    using Zealot.Server.AI;
    using System.Linq;
    using Rules;
    using System.Text;
    using Repository;

    public class BigBossScoreRecord
    {
        public int score;
        public uint tick; //4800 ticks to clear this record;
    }

    public class Monster : Actor
    {
        private long elapsedDT;
        private long regenDT;
        public MonsterSpawnerBase mSp;
        public CombatNPCJson mArchetype;        
        private Vector3 mSpawnPos;
        private GameTimer livetimer;
        private GameTimer deadtimer;
        private Player killer;
        protected BaseAIBehaviour mAIController;
        private bool mIsBigBossLoot;
        private bool mIsBoss;
        private long mBossNoDmgCountdown = 0;
        private long mBossNoDmgCountdownConst = 0;

        private Dictionary<string, int> mPlayerDamages; //Track damages caused by players
        public List<KeyValuePair<string, int>> mPlayerDamageRank; //player damage rank for boss
        private Dictionary<string, BigBossScoreRecord> mPlayerScore; //Track players score for bigboss
        public List<KeyValuePair<string, long>> mPartyScoreRank; //party score rank for bigboss, key is leader name or player self.
        private uint mOnAttackedTick = 0;

        public bool LogAI { get
            {
                return true;
                //bool logflag = mArchetype.monsterclass == MonsterClass.Boss;
                //if (mSp != null)
                //    return mSp.LogAI && logflag;
                //else
                //    return false;
            }
        }
        public Monster() : base()
        {
            this.EntityType = EntityType.Monster;           
            elapsedDT = 0;
            mPlayerDamages = new Dictionary<string, int>();
            mPlayerScore = new Dictionary<string, BigBossScoreRecord>();
        }

        #region Implement abstract methods
        public override void SpawnAtClient(GameClientPeer peer)
        {            
            peer.ZRPC.CombatRPC.SpawnMonsterEntity(mnPersistentID, mArchetype.id, Position.ToRPCPosition(), Forward.ToRPCDirection(), GetHealth(), peer);
        }
        #endregion

        public bool HasEvasion()
        {
            //normalmonster which can be knockback will not dodge.
            return !(/*mArchetype.canbeknockback && */mArchetype.monsterclass == MonsterClass.Normal);
        }

        public override void Update(long dt)
        {            
            base.Update(dt);

            elapsedDT += dt;
            if (!bAIDisabled && elapsedDT >= 500)
            {
                mAIController.OnUpdate(elapsedDT);                        
                elapsedDT = 0;                
            }

            RegenHealth(dt);

            if (mIsBoss)
            {
                mBossNoDmgCountdown -= dt;
                if (mBossNoDmgCountdown < 0)
                {
                    mBossNoDmgCountdown = mBossNoDmgCountdownConst;
                    ((SpecialBossSpawner)mSp).RandomPosition();
                    Position = mSp.GetPos();
                    mAIController.GotoState("Goback");
                }
            }
        }

        protected bool bAIDisabled = false;
        public void StopAI()
        {
            bAIDisabled = true;
        }

        public delegate bool OverwriteStatsCallback();
        public void Init(MonsterSpawnerBase spawner, OverwriteStatsCallback callback = null, long liveDuration=0)
        {
            mSp = spawner;
            mArchetype = spawner.mArchetype;
            SetInstance(spawner.mInstance);
            this.Name = mArchetype.localizedname;
            var sp = spawner.mPropertyInfos;            
            PlayerStats.Alive = true;
            PlayerStats.Team = -100; //default not same as pc
            PlayerStats.MoveSpeed = mArchetype.movespeed;
            PlayerStats.Level = mArchetype.level;
            mSpawnPos = sp.position;

            PlayerCombatStats monstercombatstats = new PlayerCombatStats();
            monstercombatstats.SetPlayerLocalAndSyncStats(null, PlayerStats, this);
            
            CombatStats = monstercombatstats;
            SkillPassiveStats = new SkillPassiveCombatStats(EntitySystem.Timers, this);

            bool overwriteStats = false;
            if (callback != null)
                overwriteStats = callback();
            if (!overwriteStats)
            {
                monstercombatstats.SuppressComputeAll = true;
                CombatStats.SetField(FieldName.AttackBase, mArchetype.attack);
                CombatStats.SetField(FieldName.ArmorBase, mArchetype.armor);
                CombatStats.SetField(FieldName.AccuracyBase, mArchetype.accuracy);
                CombatStats.SetField(FieldName.EvasionBase, mArchetype.evasion);
                // Monster stats needs to be changed
                CombatStats.SetField(FieldName.PierceDamage, 50);
                CombatStats.SetField(FieldName.SliceDamage, 10);
                CombatStats.SetField(FieldName.SmashDamage, 10);
                CombatStats.SetField(FieldName.PierceDefense, 20);
                CombatStats.SetField(FieldName.SliceDefense, 20);
                CombatStats.SetField(FieldName.SmashDefense, 20);
                CombatStats.SetField(FieldName.MetalDefense, 20);
                CombatStats.SetField(FieldName.WoodDefense, 20);
                CombatStats.SetField(FieldName.EarthDefense, 20);
                CombatStats.SetField(FieldName.WaterDefense, 20);
                CombatStats.SetField(FieldName.FireDefense, 20);
                CombatStats.SetField(FieldName.IgnoreArmorBase, 10);
                CombatStats.SetField(FieldName.WeaponAttackBase, 10);
                CombatStats.SetField(FieldName.StrengthBonus, 12);
                CombatStats.SetField(FieldName.IntelligenceBase, 80);
                //CombatStats.SetField(FieldName.VSNullDamage, 100);
                CombatStats.SetField(FieldName.VSHumanDefenseBonus, 10);
                CombatStats.SetField(FieldName.DecreaseFinalDamage, 10);
                CombatStats.SetField(FieldName.BlockRate, 30);
                CombatStats.SetField(FieldName.BlockValueBonus, 80);
                //CombatStats.SetField(FieldName.CriticalDamageBase, mArchetype.criticaldamage);
                //CombatStats.SetField(FieldName.CocriticalBase, mArchetype.cocritical);
                //CombatStats.SetField(FieldName.CriticalBase, mArchetype.critical);
                //CombatStats.SetField(FieldName.CoCriticalDamageBase, mArchetype.cocriticaldamage);
                //CombatStats.SetField(FieldName.TalentPointCloth, mArchetype.talentcloth);
                //CombatStats.SetField(FieldName.TalentPointScissors, mArchetype.talentscissors);
                //CombatStats.SetField(FieldName.TalentPointStone, mArchetype.talentstone);
                SetHealthMax(mArchetype.healthmax); // Init max health first
                SetHealth(mArchetype.healthmax); // Health now uses combatstats
                monstercombatstats.SuppressComputeAll = false;
                monstercombatstats.ComputeAll();//TODO:check the above stats is initialized properly.
            }
            Idle();
            if (liveDuration > 0)
                livetimer = mInstance.SetTimer(liveDuration, OnLiveTimeUp, null);
            mIsBoss = spawner is SpecialBossSpawner;
            mBossNoDmgCountdownConst = SpecialBossRepo.BossNoDmgRandomPos * 1000;
            mBossNoDmgCountdown = mBossNoDmgCountdownConst;
        }

        public override float GetExDamage()
        {
            //return mArchetype.exdamage;
            return 0;
        }

        public override void SetHealth(int val)
        {
            base.SetHealth(val);
            float newhp = (float)val / GetHealthMax();            
            PlayerStats.DisplayHp = newhp;
        }

        private void RegenHealth(long dt)
        {
            int healthMax = GetHealthMax();
            if (mArchetype.hpregenamt > 0 && IsAlive() && GetHealth() < healthMax)
            {
                regenDT += dt;
                if (regenDT > mArchetype.healthregeninterval * 1000)
                {
                    int regenAmt = (int)(mArchetype.hpregenamtbypercent / 100.0f * healthMax) + mArchetype.hpregenamt;
                    OnRecoverHealth(regenAmt);
                    regenDT = 0;
                }
            }
            else
                regenDT = 0;
        }

        public void SetAIBehaviour(BaseAIBehaviour behaviour)
        {
            mAIController = behaviour;
            mAIController.StartMonitoring();
        }

        public bool IsAggressive()
        {
            return mSp.IsAggressive();         
        }

        public override bool IsInvalidTarget()
        {
            return !IsAlive();
        }

        public override bool IsInSafeZone()
        {
            return false;
        }
        
        public Actor QueryForThreat()
        {
            float aggroRadius = mSp.GetAggroRadius();
            if (aggroRadius == 0)
                return null;
            if (!string.IsNullOrWhiteSpace(mSummoner))
            {
                var peer = GameApplication.Instance.GetCharPeer(mSummoner);
                if (peer != null)
                {
                    var summoner = peer.mPlayer;
                    if (summoner != null && summoner.mInstance == mInstance && Vector3.SqrMagnitude(summoner.Position - Position) <= aggroRadius * aggroRadius)
                    return summoner;
                }
            }
            else
                return EntitySystem.QueryForClosestEntityInSphere(this.Position, aggroRadius, (queriedEntity) =>
                {                        
                        IActor target = queriedEntity as IActor;
                        return (target != null && CombatUtils.IsValidEnemyTarget(this, target));
                }) as Actor;
            return null;
        }

        private void OnLiveTimeUp(object arg)
        {
            livetimer = null;
            mSp.OnChildDead(this, null);
            mSp = null;
            CleanUp();
        }

        private void OnDeadTimeUp(object arg)
        {
            deadtimer = null;
            CleanUp();
        }

        public void CleanUp()
        {
            if (deadtimer != null)
            {
                mInstance.StopTimer(deadtimer);
                deadtimer = null;
            }
            if (livetimer != null)
            {
                mInstance.StopTimer(livetimer);
                livetimer = null;
            }
            mInstance.mEntitySystem.RemoveAlwaysShow(this);
            mInstance.mEntitySystem.RemoveEntityByPID(GetPersistentID(), mArchetype.monsterclass == MonsterClass.Boss);                         
        }

        public override void OnKilled(IActor attacker)
        {          
            base.OnKilled(attacker);
            mAIController.OnKilled();

            if (mIsBigBossLoot)
            {
                uint ticknow = EntitySystem.Timers.GetTick();
                mPlayerScore = mPlayerScore.Where(kvp => ticknow - kvp.Value.tick <= 4800).ToDictionary(pair => pair.Key, pair => pair.Value);

                Dictionary<string, long> _partyScore = new Dictionary<string, long>();
                foreach (var kvp in mPlayerScore)
                {
                    string _playername = kvp.Key;
                    int score = kvp.Value.score;
                    int _partyid = PartyRules.GetPartyIdByPlayerName(_playername);
                    if (_partyid != 0)
                    {
                        string leader = PartyRules.GetPartyById(_partyid).leader;
                        if (_partyScore.ContainsKey(leader))
                            _partyScore[leader] += score;
                        else
                            _partyScore.Add(leader, score);
                    }
                    else
                        _partyScore[_playername] = score;
                }

                var _peers = GameApplication.Instance.GetAllCharPeer();
                string _bossinfo = mArchetype.id + ";";
                foreach (var kvp in _partyScore)
                {
                    PartyStatsServer _party = PartyRules.GetMyParty(kvp.Key);
                    if (_party != null && _party.GetPartyMemberList().Count > 1)
                    {
                        StringBuilder _sb = new StringBuilder();
                        _sb.Append(_bossinfo);
                        _sb.Append(kvp.Value + ";");
                        foreach (string _member in _party.GetPartyMemberList().Keys)
                        {
                            if (mPlayerScore.ContainsKey(_member))
                                _sb.AppendFormat("{0}: {1};", _member, mPlayerScore[_member].score);
                        }
                        GameApplication.Instance.BroadcastMessage_Party(BroadcastMessageType.BossKilledMyScore, _sb.ToString(), _party);
                    }
                    else
                    {
                        GameClientPeer _peer;
                        if (_peers.TryGetValue(kvp.Key, out _peer))
                            _peer.ZRPC.CombatRPC.BroadcastMessageToClient((byte)BroadcastMessageType.BossKilledMyScore, _bossinfo + kvp.Value, _peer);
                    }
                }

                mPartyScoreRank = _partyScore.ToList().OrderByDescending(x => x.Value).Take(10).ToList();
                List<string> _lootPlayers = new List<string>();
                if (mPartyScoreRank.Count >= 1)
                {
                    string _name = mPartyScoreRank[0].Key;
                    PartyStatsServer _party = PartyRules.GetMyParty(_name);
                    if (_party != null)
                    {
                        foreach (var _member in _party.GetPartyMemberList())
                        {
                            if (!_member.Value.IsHero() && mPlayerScore.ContainsKey(_member.Key))
                                _lootPlayers.Add(_member.Key);
                        }
                    }
                    else
                        _lootPlayers.Add(_name);
                }
            }
            else //todo check boss loot
            {
                var _peers = GameApplication.Instance.GetAllCharPeer();
                string _bossinfo = mArchetype.id + ";";
                foreach (var kvp in mPlayerDamages)
                {
                    GameClientPeer _peer;
                    if (_peers.TryGetValue(kvp.Key, out _peer))
                        _peer.ZRPC.CombatRPC.BroadcastMessageToClient((byte)BroadcastMessageType.BossKilledMyDmg, _bossinfo + kvp.Value, _peer);
                }
                mPlayerDamageRank = mPlayerDamages.ToList().OrderByDescending(x => x.Value).Take(10).ToList();
                Dictionary<string, float> _lootRatio = new Dictionary<string, float>();
                var _lootlist = mPlayerDamageRank.Take(2).ToList();
                if (_lootlist.Count == 2)
                {
                    float _dmgRatio = 1.0f * _lootlist[1].Value / _lootlist[0].Value;
                    if (_dmgRatio < 0.2f)
                        _lootRatio.Add(_lootlist[0].Key, 1);
                    else
                    {
                        float _ratioTop1 = 1 / (1 + _dmgRatio);
                        _lootRatio.Add(_lootlist[0].Key, _ratioTop1);
                        _lootRatio.Add(_lootlist[1].Key, 1 - _ratioTop1);
                    }
                }
                //todo: distribute loot base on _lootRatio
            }

            PerformAction(new ServerAuthoASDead(this, new DeadActionCommand()));
            deadtimer = mInstance.SetTimer(CombatUtils.DYING_TIME, OnDeadTimeUp, null);//give  seconds for client
            NetEntity ne = (NetEntity)attacker;                 
            if (ne.IsPlayer())
            {
                Player killer_player = attacker as Player;
                killer = killer_player;// GetKiller(killer_player);

                //if (killer != null)
                    killer.OnNPCKilled(mArchetype);
                //else
                //    Console.Write("Monster.cs OnKilled() could not find killer");                          
            }
            else if (ne.IsHero())
            {
                HeroEntity killerHero = attacker as HeroEntity;
                killer = killerHero.Owner;  //set the hero's owner as the killer
                killer.OnNPCKilled(mArchetype);
            }
            mSp.OnChildDead(this, killer);
            mSp = null;
        }

        #region DamageRecord        
        public void ResetDamageRecords()
        {
            mPlayerDamages.Clear();
            mPlayerScore.Clear();
        }

        private void AddDamageRecord(string playerName, int damage)
        {
            if (mIsBigBossLoot)
            {
                uint ticknow = EntitySystem.Timers.GetTick();
                BigBossScoreRecord record;
                if (mPlayerScore.TryGetValue(playerName, out record))
                {
                    if (ticknow - record.tick > 4800)
                        record.score = damage;
                    else
                        record.score += damage;
                }
                else
                {
                    record = new BigBossScoreRecord { score = damage };
                    mPlayerScore.Add(playerName, record);
                }
                record.tick = ticknow;
            }
            else
            {
                if (mPlayerDamages.ContainsKey(playerName))
                    mPlayerDamages[playerName] += damage;
                else
                    mPlayerDamages.Add(playerName, damage);
            }
        }

        //public Player GetKiller(Player attacker)
        //{
        //    if (mArchetype.lootrule == NPCLootRule.LootByLasthit)
        //    {
        //        return attacker;
        //    }
        //    else
        //    {
        //        return GetHighestDamagePlayer();//if highest damage player is not present when the monster is killed, the highest damage player will be the next highest damage player
        //    }
        //}
        public Player GetHighestDamagePlayer()
        {
            int highestAmt = 0;
            Player myplayer = null;
            foreach (KeyValuePair<string, int> entry in mPlayerDamages)
            {
                Player _myplayer = mInstance.mEntitySystem.GetPlayerByName(entry.Key);
                if (_myplayer != null && entry.Value > highestAmt)
                {
                    highestAmt = entry.Value;
                    myplayer = _myplayer;
                }
            }
            return myplayer;
        }

        public void AddDamageToPlayer(string playerName, int damage)
        {
            if (mIsBigBossLoot)
            {
                int score = damage / 2;
                uint ticknow = EntitySystem.Timers.GetTick();
                BigBossScoreRecord record;
                if (mPlayerScore.TryGetValue(playerName, out record))
                {
                    if (ticknow - record.tick > 4800)
                        record.score = score;
                    else
                        record.score += score;
                }
                else
                {
                    record = new BigBossScoreRecord { score = score };
                    mPlayerScore.Add(playerName, record);
                }
                record.tick = ticknow;
            }
        }
        #endregion

        public override void OnDamage(IActor attacker, AttackResult res, bool pbasicattack)
        {
            if (mArchetype.dmgbyhitcount)
            {
                //res.AttackType = AttackResultType.Physical;
                res.RealDamage = 1;
            }

            if (res.RealDamage > 0)
            {
                Player player = attacker as Player;
                if (player != null)
                {
                    //{
                    //    string logstr = string.Format("[{0}][{1}][{2}][{3}][{4}][{5}]", 
                    //        DateTime.Now, Name, " was attacked by", attacker.Name, res.SkillID, res.RealDamage);
                    //}
                    //if (mArchetype.lootrule == NPCLootRule.LootByDamage)
                        AddDamageRecord(player.Name, res.RealDamage); //actual damage caused is less if health is lower than damage

                    if (mInstance.mRealmController != null)
                        mInstance.mRealmController.OnDealtDamage(player, this, res.RealDamage);
                }
            }
            if (mSp!=null)
                mSp.OnChildDamaged(attacker);
             
            base.OnDamage(attacker, res, pbasicattack);
        }

        public override void OnAttacked(IActor attacker, int aggro)
        {
            mAIController.OnAttacked(attacker, aggro);
            if (mIsBigBossLoot)
            {
                uint ticknow = EntitySystem.Timers.GetTick();
                if (mOnAttackedTick == 0 || ticknow - mOnAttackedTick > 75)
                {
                    mOnAttackedTick = ticknow;
                    List<Entity> qr = new List<Entity>();
                    EntitySystem.QueryNetEntitiesInCircle(this.Position, 15, (queriedEntity) =>
                    {
                        return (queriedEntity as Player != null);
                    }, qr);
                    foreach(var entity in qr)
                    {
                        string playerName = ((Player)entity).Name;
                        if (!mPlayerScore.ContainsKey(playerName))
                            mPlayerScore.Add(playerName, new BigBossScoreRecord { score = 1, tick = ticknow });
                        else
                            mPlayerScore[playerName].tick = ticknow;
                    }
                }
            }
            if (mIsBoss)
                mBossNoDmgCountdown = mBossNoDmgCountdownConst;
        }

        public void OnGroupAggro(int pid, IActor attacker)
        {
            if (GetPersistentID() != pid)
            {
                mAIController.OnGroupAggro(attacker, 1);
            }
        }

        public void OnKnockedBack(Vector3 targetpos)
        { 
            //if (!mArchetype.canbeknockback)
            //    return;
            
            mAIController.GotoState("RecoverFromKnockedBack");
            KnockedBackCommand cmd = new KnockedBackCommand();
            cmd.targetpos = targetpos;
            ServerAuthoKnockedBack kbAction = new ServerAuthoKnockedBack(this, cmd);
            kbAction.SetCompleteCallback(() => {
                Idle();
            }); 
            PerformAction(kbAction);//the monster may be in any AIBehaviour State when perform knockedBack aciton. 
        }
         
        public void OnKnockedUp(float dur)
        {
            mAIController.GotoState("RecoverFromKnockedBack");
            KnockedUpCommand cmd = new KnockedUpCommand();
            cmd.dur = dur;
            ServerAuthoKnockedUp action = new ServerAuthoKnockedUp(this, cmd);
            action.SetCompleteCallback(() => {
                Idle(); 
            });
            PerformAction(action);
        }

        public override void onDragged(Vector3 pos, float dur, float speed)
        {             
            DraggedActionCommand cmd = new DraggedActionCommand();
            cmd.pos = pos;
            cmd.dur = dur;
            cmd.speed = speed;
            ASDragged action = new ASDragged(this, cmd);
            action.SetCompleteCallback(() => {
                Idle();
            });
            PerformAction(action);
        }

        public bool HasMoved { get; set; }

        public override void OnStun()
        {
            base.OnStun();
            if (mArchetype.monsterclass == MonsterClass.Normal)
                mAIController.GotoState("Stun");
        }

        public override void OnRoot()
        {
            base.OnRoot();
            if (IsMoving())
            {
                Idle();
            }
        }
                     
        ///////////////////////////////////////////////////////////////
        //Available actions performable by monster:        
        public void Idle()
        {
            ServerAuthoASIdle idleAction = new ServerAuthoASIdle(this, new IdleActionCommand());
            PerformAction(idleAction);
        }

        public void MoveTo(Vector3 pos, bool roam = false)
        {
            WalkActionCommand cmd = new WalkActionCommand();
            cmd.targetPos = pos;
            cmd.speed = roam ? PlayerStats.MoveSpeed / 2 : 0;
            ServerAuthoASWalk walkAction = new ServerAuthoASWalk(this, cmd);
            walkAction.SetCompleteCallback(Idle);
            PerformAction(walkAction);
        }

        public void CastSkill(int skillid, int targetPID)
        {            
            CastSkillCommand cmd = new CastSkillCommand();
            cmd.skillid = skillid;
            cmd.targetpid = targetPID;
            ServerAuthoCastSkill action = new ServerAuthoCastSkill(this, cmd);            
            action.SetCompleteCallback(Idle);
            PerformAction(action);
        }

        public void ApproachTarget(int targetPID, float range)
        {
            ApproachCommand cmd = new ApproachCommand();
            cmd.targetpid = targetPID;
            cmd.range = range;            
            ServerAuthoASApproach approachAction = new ServerAuthoASApproach(this, cmd);
            approachAction.SetCompleteCallback(Idle);
            PerformAction(approachAction);
        }

        public void ApproachTargetWithPathFind(int targetPID, Vector3? pos, float range, bool targetposSafe, bool movedirectonpathfound)
        {
            ApproachWithPathFindCommand cmd = new ApproachWithPathFindCommand();
            cmd.targetpid = targetPID;
            cmd.targetpos = pos;
            cmd.range = range;
            cmd.targetposSafe = targetposSafe;
            cmd.movedirectonpathfound = movedirectonpathfound;
            ASApproachWithPathFind approachAction = new ASApproachWithPathFind(this, cmd);
            approachAction.SetCompleteCallback(Idle);
            PerformAction(approachAction);
        }

        ///////////////////////////////////////////////////////////////
    }
}