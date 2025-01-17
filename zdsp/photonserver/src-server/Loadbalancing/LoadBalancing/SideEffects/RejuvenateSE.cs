﻿namespace Zealot.Server.SideEffects
{
    using Zealot.Common;
    using Zealot.Common.Entities;
    using Kopio.JsonContracts;

    public class RejuvenateSE : SideEffect
    {
        private int mAmount;
        private delegate void ComputeFunction();
        private ComputeFunction fComputeFunc;
        public RejuvenateSE(SideEffectJson sideeffectData, SEORIGINID origin, int originID)
            : base(sideeffectData, origin, originID)
        {
            mAmount = 0;
            mNeedCaster = false;
        }
        private float mBonusPerc = 0;
        private float mBonusValue = 0;
        public void SetRejuvenateBonus(float bonusperc, float bonusvalue)
        {
            mBonusPerc = bonusperc;
            mBonusValue = bonusvalue;
        }

        //protected override bool CheckApplyCondition()
        //{
        //    if (mTarget.HasSideEffect(mSideeffectData.id))
        //    {
        //        SideEffect se = mTarget.GetSideEffect(mSideeffectData.id);
        //        long elpsedtime = se.GetTotalTimeElapsed();
        //        se.Prolong(elpsedtime);//refresh the old sideeffect 
        //        return false;
        //    }
        //    return true;
        //}

        public override bool IsHot()
        {
            return ( mSideeffectData.interval > 0);
        }

        protected override bool OnApply(int equipid = -1)
        {
            if (base.OnApply(equipid))
            {
                ComputeRecoverAmount();
                ApplyRecovery();
                return true;
            }
            return false;
        }

        protected override void OnInterval()
        {
            base.OnInterval();

            ApplyRecovery();
        }

        protected void ComputeRecoverAmount()
        {               
            double randomAmt = GameUtils.Random(mSideeffectData.min, mSideeffectData.max);
            
            if (mSideeffectData.isrelative)
            {
                if (mSideeffectData.effecttype == EffectType.Rejuvenate_HealthPotion)
                {
                    mAmount = (int)(randomAmt * 0.01f * (int)mTarget.GetHealthMax()); 
                } 
            }
            else
            {
                mAmount = (int)(randomAmt);
            }

            mAmount = (int)( mAmount * (1 + mBonusPerc * 0.01f) + mBonusValue);
        }

        private void ComputeRecoverAmountHealing()
        {
            float intel = mTarget.CombatStats.GetField(FieldName.Intelligence) * 20;
            float hp = mTarget.CombatStats.GetField(FieldName.HealingPoint) - mTarget.CombatStats.GetField(FieldName.HealingPointDebuff);
            float he = mTarget.CombatStats.GetField(FieldName.HealingEffect) - mTarget.CombatStats.GetField(FieldName.HealingEffectDebuff);

            mAmount = (int)((mSideeffectData.max + intel + hp) * (1 + he * 0.01f));
            mAmount = (int)(mAmount * (1 + mBonusPerc * 0.01f) + mBonusValue);
        }

        private void ComputeRecoverAmountPotion()
        {
            if (mSideeffectData.isrelative)
                mAmount = (int)(mSideeffectData.max * 0.01f * (int)mTarget.GetHealthMax());
            else
                mAmount = (int)(mSideeffectData.max);

            mAmount = (int)(mAmount * (1 + mBonusPerc * 0.01f) + mBonusValue);
        }

        private void ApplyRecovery()
        {            
            if (mAmount <= 0)
                return; 
            if (mSideeffectData.effecttype == EffectType.Rejuvenate_HealthPotion)
                mTarget.OnRecoverHealth(mAmount);
        }
    }
}
