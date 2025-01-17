﻿#define LOG_BOSS_AI
using Kopio.JsonContracts;
using System.Collections.Generic;
using UnityEngine;
using Zealot.Common;
using Zealot.Common.Actions;
using Zealot.Common.Entities;
using Zealot.Server.Entities;
using Zealot.Repository;

namespace Zealot.Server.AI
{
    public class BossAIBehaviour : BaseAIBehaviour
    {
        public const int ROAM_COOLDOWN_TIME = 8000; //in msec

        protected int mLastRoam;
        protected bool mCanRoam;
        protected int mCurrentRoamCoolDown;
        protected bool mCanPathFind;
        protected bool mGroupAggro;
        protected Monster mMonster;
        protected MonsterSpawnerBase mSpawner;
        protected Actor mTarget;
        protected Actor mHighestThreatAttacker;

        private int mThreatScanCount;
        //protected Vector3 mLastResolveOverlappedPos;
        protected float mSkillRange;
        protected SkillData mSkillToExecute;
        //protected List<NPCSkillCondition> mSkillConditions;//this is the normal skills setup in NPCSkillLink table.
        protected Dictionary<int, Threat> mThreats;
        protected bool mGoingBack;

        protected long mBasicAttackStartTime;
        protected long mBasicAttackCooldown;
        protected long mSkillGCDEnd;
        //protected long[] mSkillCDEnd;
        protected Dictionary<int, long> mSkillCDEndbyID; // <skillid, cooldown>

        //protected bool mHasMoved; 
        public BossAIBehaviour(Monster monster) : base(monster)
        {
            AddState("Roam", OnRoamEnter, OnRoamLeave, OnRoamUpdate);

            mMonster = monster;
            mSpawner = monster.mSp;
            mCanRoam = mSpawner.CanRoam(); //monsters in world can roam and roam y position is based off spawner y position        
            mCanPathFind = mSpawner.CanPathFind(); //Only certain monsters can path find
            mGroupAggro = mSpawner.IsGroupAggro();
            //mLastResolveOverlappedPos = new Vector3(0, -300, 0);
            mTarget = null;
            mHighestThreatAttacker = null;
            mSkillToExecute = null;
            mSkillCDEndbyID = new Dictionary<int, long>();
            mThreats = new Dictionary<int, Threat>();

            mBasicAttackStartTime = 0;
            mBasicAttackCooldown = 1000;

            //mHasMoved = false;
        }

        protected override void InitStates()
        {
            AddState("Idle", OnIdleEnter, OnIdleLeave, OnIdleUpdate);
            AddState("CombatApproach", OnCombatApproachEnter, OnCombatApproachLeave, OnCombatApproachUpdate);
            AddState("CombatExecute", OnCombatExecuteEnter, OnCombatExecuteLeave, OnCombatExecuteUpdate);
            AddState("Goback", OnGobackEnter, OnGobackLeave, OnGobackUpdate);
        }

        public override void StartMonitoring()
        {
            InitAIConditions();
            base.StartMonitoring();
        }

        public bool IsInCombat()
        {
            string curr = GetCurrentStateName();
            return (curr == "CombatApproach" || curr == "CombatExecute");
        }

        protected void SwitchTarget(Actor target)
        {
            if (mTarget != null)
            {
                mTarget.RemoveNPCAttacker(mMonster); //monster is no longer seeking this target
                mMonster.PlayerStats.TargetPID = 0;
            }

            mTarget = target;
            if (mTarget != null)
            {
                mTarget.AddNPCAttacker(mMonster);
                mMonster.PlayerStats.TargetPID = mTarget.GetPersistentID();
            }
        }

        public void AddSkillToWaitForCast(int skillid)
        {
            mQueueSkills.Add(skillid);
        }

        #region Idle State
        protected override void OnIdleEnter(string prevstate)
        {
            base.OnIdleEnter(prevstate);
            mLastRoam = 0;
            //reset the aiskillconditions and happend ones.
            foreach (BaseSkillCondition cond in mAISkillConditions)
                cond.Reset();

            mSkillCDEndbyID = new Dictionary<int, long>();
            //mHasMoved = false;
            //handle recover from knockedback
            if (mTarget != null && currentState.Name != "Stun") //TODO: handle Root status
            {
                SwitchTarget(mTarget);
                ResetSkillToExecute();
                GotoState("CombatApproach");
                return;
            }
        }

        protected override void OnIdleUpdate(long dt)
        {
            if (mMonster.IsAggressive())
            {
                Actor threat = ThreatScan(2); //scan once every 2 updates
                if (threat != null)
                {
                    AddThreat(threat.GetPersistentID(), threat, 0);
                    SwitchTarget(threat);
                    ResetSkillToExecute();
                    GotoState("CombatApproach");
                    return;
                }
            }

            if (mCanRoam)
            {
                mLastRoam += (int)dt;
                if (mLastRoam > mCurrentRoamCoolDown)
                {
                    int pid = mMonster.GetPersistentID();
                    if (mMonster.mInstance.CanMonsterRoam(pid)) //every 20 ticks (of 50msec each) allow monsters of out of 10 groups to roam. If level has 200 monsters, then each group will be about 20 monsters.
                    {
                        if (mMonster.mInstance.HasCPUResourceToRoam())
                        {
                            //System.Diagnostics.Debug.WriteLine(pid + " roam " + mMonster.mInstance.mCurrentLevelID);
                            GotoState("Roam");
                        }
                    }
                }
            }
        }
        #endregion

        #region Roam State
        protected virtual void OnRoamEnter(string prevstate)
        {
#if LOG_BOSS_AI
            if (LogAI) log.Info("OnRoamEnter");
#endif
            //Determine roam point          
            Vector3 randomPos = GameUtils.RandomPos(mSpawner.GetPos(), mSpawner.GetRoamRadius()); //assume height within combat radius of spawn position is "safe"
            //log.InfoFormat("monster pid {0} roaming to {1}", mMonster.GetPersistentID(), randomPos.ToString());

            mMonster.MoveTo(randomPos, true);
        }

        protected virtual void OnRoamLeave()
        {
            mLastRoam = 0;
            mCurrentRoamCoolDown = ROAM_COOLDOWN_TIME + GameUtils.RandomInt(0, 6000);
        }

        protected virtual void OnRoamUpdate(long dt)
        {
            if (!mMonster.IsMoving())
            {
                GotoState("Idle");
            }
        }
        #endregion

        protected Actor ThreatScan(int tickcount)
        {
            Actor threat = null;
            if (mThreatScanCount % tickcount == 0)
            {
                threat = mMonster.QueryForThreat();
                mThreatScanCount = 0;
            }
            ++mThreatScanCount;
            return threat;
        }

        protected bool IsTargetInRange()
        {
            return GameUtils.InRange(mMonster.Position, mTarget.Position, mSkillRange, mTarget.Radius);
        }

        protected bool IsInCombatRadius(Vector3 pos)
        {
            return GameUtils.InRange(mSpawner.GetPos(), pos, mSpawner.GetCombatRadius() + 3.0f);
        }

        protected void ResetSkillToExecute()
        {
            mSkillToExecute = null;
        }

        protected void DetermineSkillToExecute()
        {
            bool hasSkill = false;
            long now = mMonster.EntitySystem.Timers.GetSynchronizedTime();

            if (mQueueSkills.Count > 0)
            {
                if (now >= mSkillGCDEnd) //global cooldown
                {
                    int skillid = mQueueSkills[0];
                    if (!IsSkillInCooldown(skillid, now))
                    {
                        mQueueSkills.RemoveAt(0);
                        hasSkill = true;
                        mSkillToExecute = SkillRepo.GetSkill(skillid);
                    }
                }
            }

            if (mCondistions.Count > 0 && !hasSkill)
            {
                if (now >= mSkillGCDEnd) //global cooldown
                {
                    foreach (KeyValuePair<int, int> keyValue in mCondistions)
                    {
                        int skillid = keyValue.Value;
                        if (IsSkillInCooldown(skillid, now)) //still cooling down
                            continue;

                        hasSkill = true;
                        mSkillToExecute = SkillRepo.GetSkill(skillid);
                        break;
                    }
                }
            }

            if (!hasSkill || mMonster.HasControlStatus(ControlSEType.Silence))
            {
                mSkillToExecute = NextBasicAttack();

                //Peter, TODO: basic attack also have cooldown and cooldown can differ for different monster    
                //We will not check basic attack cooldown here to allow at least 1 skill and its range to refer to e.g. when approaching
            }
            
            SkillGroupJson skillgroupJson = mSkillToExecute.skillgroupJson;

            //Check if skill is friendly, it is assumed monsters/boss would not have any friendly skills
            if (skillgroupJson.targettype != TargetType.Enemy)
            {
                //Revert to basic attack
                log.Info("Warning!! Monster archetype id: " + mMonster.mArchetype.id + " has unsupported friendly skill: " + mSkillToExecute.skillJson.id);
            }

            if (skillgroupJson.threatzone == Threatzone.LongStream)
                mSkillRange = mSkillToExecute.skillJson.range;
            else if (skillgroupJson.threatzone == Threatzone.Single)
                mSkillRange = 2.0f;
            else
                mSkillRange = mSkillToExecute.skillJson.radius; //120,360,single                                     
        }

        protected SkillData NextBasicAttack()
        {
            //if (!mMonster.mArchetype.overwritebasicattack)
            //    return SkillRepo.mMonsterBasicAttack;
            int num = 0;
            if (mMonster.mArchetype.basicattack > 0)
                ++num;
            if (mMonster.mArchetype.basicattack2 > 0)
                ++num;
            if (num == 1)
            {
                int id = mMonster.mArchetype.basicattack > 0 ? mMonster.mArchetype.basicattack :
                mMonster.mArchetype.basicattack2;
                return SkillRepo.GetSkill(id);
            }
            if (GameUtils.GetRandomGenerator().NextDouble() < 0.5)
                return SkillRepo.GetSkill(mMonster.mArchetype.basicattack);
            else
                return SkillRepo.GetSkill(mMonster.mArchetype.basicattack2);
        }

        protected void ApproachTarget()
        {
            //mHasMoved = true;

            PositionSlots slots = mTarget.PositionSlots;
            slots.DeallocateSlot(mMonster);

            //Approach action is supposed to be a black box and will help the monster get nearer to its target  
            //Note: preferredRange should be at least 0.5m bigger than attacker radius      
            if (mCanPathFind)
                mMonster.ApproachTargetWithPathFind(mTarget.GetPersistentID(), null, mSkillRange - 0.5f, true, false); //-0.5f so that it does not toggle at bordercase
            else
                mMonster.ApproachTarget(mTarget.GetPersistentID(), mSkillRange - 0.5f); //-0.5f so that it does not toggle at bordercase
        }

        #region Combat Approach State
        protected override void OnCombatApproachEnter(string prevstate)
        {
            base.OnCombatApproachEnter(prevstate);
            if (mSkillToExecute == null)
                DetermineSkillToExecute(); //At the least, there will always be a basic attack available to use
        }

        //protected override void OnCombatApproachLeave() {}

        protected override void OnCombatApproachUpdate(long dt)
        {
            //Determine if target still valid
            if (!CheckTargetValid())
                return;

            if (!IsInCombatRadius(mMonster.Position))
            {
                if (mMonster.mInstance.HasCPUResourceToGoBack())
                {
                    GotoState("Goback");
                }
                return;
            }


            Zealot.Common.Actions.Action action = mMonster.GetAction();
            if (IsTargetInRange())
            {
                GotoState("CombatExecute");
            }
            else //Out of range, either idling or still approaching
            {
                if (action.mdbCommand.GetActionType() == ACTIONTYPE.IDLE && !mMonster.IsPerformingApproach() &&
                    !mMonster.HasControlStatus(ControlSEType.Root))
                {
                    ApproachTarget();
                }
            }
        }

        #endregion

        #region CombatExecute State
        protected override void OnCombatExecuteEnter(string prevstate)
        {
            //Normal monsters only have 1 normal attack. While boss can have additional skills of various range       
            base.OnCombatExecuteEnter(prevstate);
        }

        //protected override void OnCombatExecuteLeave() { }

        protected override void OnCombatExecuteUpdate(long dt)
        {
            if (mMonster.GetActionCmd().GetActionType() != ACTIONTYPE.CASTSKILL) //if has stopped attacking
            {
                ResetSkillToExecute(); //prepare to decide next skill

                if (!CheckTargetValid())
                    return;

                //just finished last execution
                DetermineSkillToExecute();

                if (IsTargetInRange())
                {
                    CastSkill();
                }
                else
                {
                    if (!mMonster.HasControlStatus(ControlSEType.Root))
                    {
                        GotoState("CombatApproach");
                        ApproachTarget();
                    }
                }
            }
        }

        protected override void OnCombatExecuteLeave()
        {
            mCondistions.Clear();
            mQueueSkills.Clear();
        }

        private bool IsSkillInCooldown(int skillid, long now)
        {
            if (mSkillCDEndbyID.ContainsKey(skillid) && now < mSkillCDEndbyID[skillid])
                return true;
            return false;
        }

        protected void CastSkill()
        {
            bool canCast = true;
            //Basic Attack might still be in cooldown here
            long now = mMonster.EntitySystem.Timers.GetSynchronizedTime();
            if (mSkillToExecute.skillgroupJson.skilltype == SkillType.BasicAttack)
            {
                if (now - mBasicAttackStartTime < mBasicAttackCooldown)
                {
                    return;
                }
                mBasicAttackCooldown = (long)(1000 * mSkillToExecute.skillJson.cooldown);
                mBasicAttackCooldown = mBasicAttackCooldown < 500 ? 500 : mBasicAttackCooldown;
                mBasicAttackStartTime = now;
                canCast = /*!mMonster.HasControlStatus(ControlSEType.Disarmed) &&*/ !mMonster.HasControlStatus(ControlSEType.Stun) && !mMonster.IsGettingHit();
                if (canCast)
                {
                    mMonster.CastSkill(mSkillToExecute.skillJson.id, mTarget.GetPersistentID(), mTarget.Position);
                }
                return;
            }
            else
            {
                canCast = /*!mMonster.HasControlStatus(ControlSEType.Disarmed) &&*/ !mMonster.HasControlStatus(ControlSEType.Stun) && !mMonster.HasControlStatus(ControlSEType.Silence)
                     && !mMonster.IsGettingHit();
            }

            if (canCast && !IsSkillInCooldown(mSkillToExecute.skillJson.id, now))
            {
                mMonster.CastSkill(mSkillToExecute.skillJson.id, mTarget.GetPersistentID(), mTarget.Position);
                mSkillCDEndbyID[mSkillToExecute.skillJson.id] = now + (long)(mSkillToExecute.skillJson.cooldown * 1000);
                mSkillGCDEnd = now + (long)(mSkillToExecute.skillJson.globalcd * 1000);
            }
#if LOG_BOSS_AI
            if (LogAI) log.Info("cast skill" + mSkillToExecute.skillJson.id);
#endif
        }
        #endregion

        protected bool IsTargetInvalid(Actor target)
        {
            return CombatUtils.IsInvalidTarget(target) || !IsInCombatRadius(target.Position);
        }

        protected virtual bool CheckTargetValid()
        {
            if (mMonster.mArchetype.monstertype == MonsterType.Boss)
            {
                if (mHighestThreatAttacker != null)//boss attack the highestThreat attacker
                {
                    SwitchTarget(mHighestThreatAttacker);
                    ResetSkillToExecute();
                    DetermineSkillToExecute();
                }
            }

            if (IsTargetInvalid(mTarget))
            {
                if (mTarget != null)
                {
                    mThreats.Remove(mTarget.GetPersistentID());
                    if (mHighestThreatAttacker == mTarget)
                        mHighestThreatAttacker = null;
                }
                SwitchTarget(null);

                List<int> removeList = new List<int>();
                foreach (KeyValuePair<int, Threat> entry in mThreats)
                {
                    int pid = entry.Key;
                    Actor potentialTarget = entry.Value.actor;
                    if (IsTargetInvalid(potentialTarget)) //Take this opportunity to remove all invalid targets
                    {
                        removeList.Add(pid);
                        if (mHighestThreatAttacker != null && potentialTarget == mHighestThreatAttacker)
                            mHighestThreatAttacker = null;
                    }
                    else if (mTarget == null)
                    {
                        SwitchTarget(potentialTarget);
                        ResetSkillToExecute();
                    }
                }

                foreach (int pid in removeList)
                    mThreats.Remove(pid);

                if (mTarget == null)
                    GotoState("Goback");
                else
                    GotoState("CombatApproach");
                return false;
            }
            return true;
        }

        #region Goback State
        protected void GoBackToSafePoint()
        {
            mGoingBack = true;
            Vector3 randomPos = mSpawner.GetPos();
            if ((randomPos - mMonster.Position).sqrMagnitude > 4)
            {
                if (mCanPathFind)
                    mMonster.ApproachTargetWithPathFind(-1, randomPos, 0, true, false);
                else
                    mMonster.MoveTo(randomPos);
            }
        }

        protected override void OnGobackEnter(string prevstate)
        {
            base.OnGobackEnter(prevstate);
            mHighestThreatAttacker = null;
            mThreats.Clear();
            SwitchTarget(null);
            mGoingBack = false;
            if (mMonster.mInstance.HasCPUResourceToGoBack())
                GoBackToSafePoint();
        }

        protected override void OnGobackUpdate(long dt)
        {
            //TODO: might want to check for aggro while going back... 

            if (!mGoingBack)
            {
                if (mMonster.mInstance.HasCPUResourceToGoBack())
                    GoBackToSafePoint();
                return;
            }

            if (!mMonster.IsPerformingApproach())
            {
                if (mMonster.mArchetype.recoveronreturn)
                    mMonster.SetHealth(mMonster.GetHealthMax());

                //mMonster.StopAllSideEffects();
                mMonster.ResetDamageRecords();
                GotoState("Idle");
            }
        }
        #endregion

        #region Stun state
        protected override void OnStunEnter(string prevstate)
        {
            base.OnStunEnter(prevstate);
            mMonster.Idle();
        }

        protected override void OnStunUpdate(long dt)
        {
            if (!mActor.HasControlStatus(ControlSEType.Stun))
            {
                GotoState("CombatApproach");
            }
        }

        protected override void OnStunLeave()
        {
            base.OnStunLeave();
        }
        #endregion

        #region Frozen state
        protected override void OnFrozenEnter(string prevstate)
        {
            base.OnFrozenEnter(prevstate);
            mMonster.Idle();
        }

        protected override void OnFrozenUpdate(long dt)
        {
            if (!mActor.HasControlStatus(ControlSEType.Freeze))
            {
                GotoState("CombatApproach");
            }
        }

        protected override void OnFrozenLeave()
        {
            base.OnFrozenLeave();
        }
        #endregion

        public void AddThreat(int attackerPID, Actor attackerActor, int aggro)
        {
            int accAggro;
            if (mThreats.ContainsKey(attackerPID))
            {
                mThreats[attackerPID].aggro += aggro;
                accAggro = mThreats[attackerPID].aggro;
            }
            else
            {
                mThreats.Add(attackerPID, new Threat(attackerActor, aggro));
                accAggro = aggro;
            }

            if (mHighestThreatAttacker != null)
            {
                int highestAggro = mThreats[mHighestThreatAttacker.GetPersistentID()].aggro;
                if (accAggro >= highestAggro && attackerActor != mHighestThreatAttacker)
                    mHighestThreatAttacker = attackerActor;
            }
            else
            {
                mHighestThreatAttacker = attackerActor;
            }
        }

        private bool OnNormalAIAttacked(IActor attacker, int aggro)
        {
            //We are not having an aggro system right now. Monster will attack the very first target only.            
            Actor attackerActor = attacker as Actor;//Currently, we handle only for actor. Maybe, there could be other forms of IActor in the future e.g. StaticActor?
            if (attackerActor == null)
                return false;

            int attackerPID = attackerActor.GetPersistentID();
            AddThreat(attackerPID, attackerActor, aggro);


            string currentStateName = GetCurrentStateName();
            if ((currentStateName == "CombatApproach" || currentStateName == "CombatExecute" || currentStateName == "Goback"))
                return false;

            SwitchTarget(attackerActor); //The first attacker will be targeted
            ResetSkillToExecute();

            if (!mMonster.HasControlStatus(ControlSEType.Stun)) //if not in stun state
                GotoState("CombatApproach");
            return true;
        }

        public override void OnAttacked(IActor attacker, int aggro) //Only boss will attack target that is top of its aggro list
        {
            if (!OnNormalAIAttacked(attacker, aggro))
                return;
        }

        public override void OnGroupAggro(IActor attacker, int aggro)
        {
            //boss is not to have group aggro. unless there is a design. it may be done another way also.
        }

        public override void OnKilled()
        {
            SwitchTarget(null);
            mHighestThreatAttacker = null;
            mThreats.Clear();
        }

        public override void OnUpdate(long dt)
        {
            base.OnUpdate(dt);
            
            if (mActor.IsAlive() && IsInCombat())
            {
                foreach (BaseSkillCondition cond in mAISkillConditions)
                {
                    if (cond.Update(dt))
                    {
                        if (mCondistions.ContainsKey(cond.ID))
                            continue;

                        mCondistions.Add(cond.ID, cond.skillid);
                    }
                    else
                    {
                        if (!mCondistions.ContainsKey(cond.ID))
                            continue;

                        mCondistions.Remove(cond.ID);
                    }
                }
            }
        }

        private List<BaseSkillCondition> mAISkillConditions;
        private Dictionary<int, int> mCondistions; // <Boss AI ID, Skill ID>
        private List<int> mQueueSkills;

        private void InitAIConditions()
        {
            mAISkillConditions = new List<BaseSkillCondition>();
            mCondistions = new Dictionary<int, int>();
            mQueueSkills = new List<int>();

            if (mMonster.mArchetype.bossai1 > 0)
            {
                StoreConditions(mMonster.mArchetype.bossai1);
            }

            if (mMonster.mArchetype.bossai2 > 0)
            {
                StoreConditions(mMonster.mArchetype.bossai2);
            }
        }

        private void StoreConditions(int bossAINum)
        {
            BossAIJson data = CombatNPCRepo.GetBossAIByID(bossAINum);
            BaseSkillCondition inst = CreateInstanceByData(data);
            if (inst != null)
                mAISkillConditions.Add(inst);
        }

        private BaseSkillCondition CreateInstanceByData(BossAIJson data)
        {
            BaseSkillCondition res = null;
            switch (data.condition)
            {
                case AISkillCondition.SelfHpDown:
                    res = new HealthMonitorDown(data, this);
                    break;
                case AISkillCondition.SelfHpUp:
                    res = new HealthMonitorUp(data, this);
                    break;
                case AISkillCondition.SelfHpInterval:
                    res = new AccumulatedHealthLoss(data, this);
                    break;
                case AISkillCondition.Engage:
                    res = new EngageTimerMonitor(data, this);
                    break;
                case AISkillCondition.TargetHpUp:
                    res = new TargetHealthMonitor(data, false, this);
                    break;
                case AISkillCondition.TargetHpDown:
                    res = new TargetHealthMonitor(data, true, this);
                    break;
                case AISkillCondition.InrangePlayer:
                    res = new InRangePlayerMonitor(data, this);
                    break;
                case AISkillCondition.TargetNegSE:
                    res = new TargetSEMonitor(data, false, this);
                    break;
                case AISkillCondition.TargetNegSkill:
                    res = new TargetSEMonitor(data, true, this);
                    break;
                case AISkillCondition.None:
                    res = new NoneSpecifiedCondition(data, this);
                    break;
                default:
                    break;
            }
            return res;
        }

        private abstract class BaseSkillCondition
        {
            protected int mID;
            protected int mSkillId;
            protected BossAIBehaviour self;

            public int ID { get { return mID; } }
            public int skillid { get { return mSkillId; } }

            public BaseSkillCondition(BossAIJson data, BossAIBehaviour target)
            {
                mID = data.id;
                mSkillId = data.skillid;
                self = target;
            }

            public virtual void Reset() { }
            public virtual bool Update(long dt) { return false; }
        }

        private class NoneSpecifiedCondition : BaseSkillCondition
        {
            protected bool mTriggered = false;
            public NoneSpecifiedCondition(BossAIJson data, BossAIBehaviour target) : base(data, target) { }

            public override bool Update(long dt)
            {
                mTriggered = true;
                return mTriggered;
            }

            public override void Reset()
            {
                mTriggered = false;
            }
        }

        private class HealthMonitorDown : BaseSkillCondition
        {
            private float mThreshold;

            public HealthMonitorDown(BossAIJson data, BossAIBehaviour target) : base(data, target)
            {
                mThreshold = data.conditiondata * 0.01f;
            }

            public override bool Update(long dt)
            {
                return self.mActor.PlayerStats.DisplayHp < mThreshold;
            }
        }

        private class HealthMonitorUp : BaseSkillCondition
        {
            private float mThreshold;

            public HealthMonitorUp(BossAIJson data, BossAIBehaviour target) : base(data, target)
            {
                mThreshold = data.conditiondata * 0.01f; // Convert data value format to percentage. Range: 0 ~ 1
            }

            public override bool Update(long dt)
            {
                return self.mActor.PlayerStats.DisplayHp > mThreshold;
            }
        }

        private class EngageTimerMonitor : BaseSkillCondition
        {
            private long mEngageTime;
            private long mInitTime;
            private bool bEngaged;

            public EngageTimerMonitor(BossAIJson data, BossAIBehaviour target) : base(data, target)
            {
                mInitTime = (long)(data.conditiondata * 1000);
                mEngageTime = mInitTime;
            }

            public override void Reset()
            {
                bEngaged = false;
                mEngageTime = mInitTime;
            }

            public override bool Update(long dt)
            {
                if (!bEngaged)
                {
                    mEngageTime -= dt;
                    if (mEngageTime <= 0)
                    {
                        bEngaged = true;
                        return true;
                    }
                    return false;
                }
                else
                    return true;
            }
        }

        private class AccumulatedHealthLoss : BaseSkillCondition
        {
            private float mThreshold;
            private float mLastValue;

            public AccumulatedHealthLoss(BossAIJson data, BossAIBehaviour target) : base(data, target)
            {
                mThreshold = data.conditiondata * 0.01f;
                mLastValue = self.mActor.PlayerStats.DisplayHp - mThreshold;
            }

            public override void Reset()
            {
                mLastValue = self.mActor.PlayerStats.DisplayHp - mThreshold;
            }

            public override bool Update(long dt)
            {
                if (self.mActor.PlayerStats.DisplayHp <= mLastValue)
                {
                    mLastValue = self.mActor.PlayerStats.DisplayHp - mThreshold;
                    self.AddSkillToWaitForCast(skillid);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private class TargetHealthMonitor : BaseSkillCondition
        {
            private float mThreshold;
            private float mLastValue;
            private Actor mLastTarget;
            private bool bIsDown = false;

            public TargetHealthMonitor(BossAIJson data, bool isDown, BossAIBehaviour target) : base(data, target)
            {
                mThreshold = data.conditiondata * 0.01f;
                bIsDown = isDown;
            }

            public override bool Update(long dt)
            {
                mLastTarget = self.mTarget;

                if (mLastTarget != null)
                {
                    mLastValue = mLastTarget.PlayerStats.DisplayHp;

                    if (bIsDown && mLastValue < mThreshold) // Target Hp Down
                        return true;
                    else if (!bIsDown && mLastValue > mThreshold) // Target Hp Up
                        return true;
                }

                mLastValue = 0;
                return false;
            }
        }

        private class InRangePlayerMonitor : BaseSkillCondition
        {
            private int mCount = 0;
            private float mRadius;

            public InRangePlayerMonitor(BossAIJson data, BossAIBehaviour target) : base(data, target)
            {
                mCount = Mathf.FloorToInt(data.conditiondata);
                mRadius = SkillRepo.GetSkill(mSkillId).skillJson.radius;
            }
            
            public override bool Update(long dt)
            {
                if (self.mMonster == null)
                    return false;
                
                int num = CombatUtils.QueryNumberOfPlayersInSphere(self.mMonster, mRadius);
                if (num >= mCount)
                    return true;

                return false;
            }
        }

        private class TargetSEMonitor : BaseSkillCondition
        {
            private bool mIsSkillID = false;
            private int id;

            public TargetSEMonitor(BossAIJson data, bool isSKill, BossAIBehaviour target) : base(data, target)
            {
                mIsSkillID = isSKill;
                id = Mathf.FloorToInt(data.conditiondata);
            }

            public override bool Update(long dt)
            {
                if(self.mTarget != null)
                {
                    Player player = self.mTarget as Player;
                    if (player == null)
                        return false;

                    if (mIsSkillID)
                        return player.HasSkill(id);
                    else
                        return player.HasSideEffect(id);
                }

                return false;
            }
        }
    }
}