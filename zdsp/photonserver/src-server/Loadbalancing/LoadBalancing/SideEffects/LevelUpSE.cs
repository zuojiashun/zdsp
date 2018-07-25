﻿namespace Zealot.Server.SideEffects
{
    using System;
    using Zealot.Common;
    using Zealot.Common.Entities;
    using Zealot.Server.Entities;
    using Zealot.Repository;
    using Kopio.JsonContracts;

    public class LevelUpSE : SideEffect
    {
        public LevelUpSE(SideEffectJson sideeffectData)
            : base(sideeffectData)
        {
            mNeedCaster = false;
        }

        protected override void InitKopioData()
        {
            base.InitKopioData();
            mDuration = 0;
        }

        protected override bool OnApply()
        {
            if (!mTarget.IsPlayer())
                return false;

            //LevelUp sideeffect is non durational. It's always once off.
            
            Player player = (Player)mTarget;
            //player.LevelUp(); //Kelvin, TODO: make player level up exactly 1 level here

            return base.OnApply();
        }
    }

    
}