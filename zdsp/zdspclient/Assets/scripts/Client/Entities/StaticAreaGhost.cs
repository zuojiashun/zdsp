﻿using Kopio.JsonContracts;
using UnityEngine;
using Zealot.Repository;
using Zealot.Common.Entities;
using System.Collections.Generic;
using Zealot.Common;

namespace Zealot.Client.Entities
{
    public class StaticAreaGhost : StaticClientNPCAlwaysShow
    {
        protected string mArchetype;
        public int mArchetypeID;
        private float mRadius;

        public StaticAreaGhost()
        {
            this.EntityType = EntityType.StaticNPC;
        }

        public string Archetype { get { return mArchetype; } }

        public void Init(string archetype, Vector3 pos, Vector3 forward, float radius)
        {
            staticNPCJson = StaticNPCRepo.GetStaticNPCByName(archetype);
            mArchetype = archetype;
            this.mArchetypeID = staticNPCJson == null ? 0 : staticNPCJson.id;
            this.Name = staticNPCJson == null ? "" : staticNPCJson.localizedname;
            mRadius = radius;
            mActiveQuest = -1;
            mActiveStatus = staticNPCJson.activeonstartup;
            GetQuestList();

            Position = pos;
            Forward = forward;

            base.Init();
            OnAnimObjLoaded(new GameObject());
        }

        public override void InitAnimObj()
        {
            GameInfo.gCombat.SetStaticNPCParent(AnimObj);
            AnimObj.transform.position = Position;
            AnimObj.transform.forward = Forward;
            AnimObj.tag = "NPC";
            AnimObj.name = "";

            base.InitAnimObj();
            GameObject go = new GameObject();
            go.name = "playerDetecter";
            go.transform.SetParent(AnimObj.transform, false);
            NpcPlayerDetect detect = go.AddComponent<NpcPlayerDetect>();
            detect.Init(this, mRadius);
            mShadow.SetActive(false);

            Show(true);
            ShowEffect(true);
        }

        public override bool Interact()
        {
            return false;
        }

        public override void UpdateOngoingQuest(List<int> quests)
        {
            mOngoingQuest = quests;
            UpdateAvtiveQuest();
        }

        public override void RemoveOngoingQuest(int questid)
        {
            if (mOngoingQuest.Contains(questid))
            {
                mOngoingQuest.Remove(questid);
                UpdateAvtiveQuest();
            }
        }

        public override void UpdateAvailableQuestList()
        {
            PlayerGhost player = GameInfo.gLocalPlayer;
            if (player == null)
            {
                return;
            }

            List<int> availablequest = new List<int>();
            foreach (int questid in mQuestList)
            {
                if (player.QuestController.IsQuestAvailable(questid))
                {
                    availablequest.Add(questid);
                }
            }

            if (availablequest != mAvailableQuest)
            {
                mAvailableQuest = availablequest;
                UpdateAvtiveQuest();
            }
        }

        private void UpdateAvtiveQuest()
        {
            if (GameInfo.gLocalPlayer == null)
            {
                return;
            }

            QuestClientController questController = GameInfo.gLocalPlayer.QuestController;

            if (mOngoingQuest.Count > 0)
            {
                mActiveQuest = QuestRepo.GetPriorityQuestId(mOngoingQuest);
            }
            else if (mAvailableQuest.Count > 0)
            {
                mActiveQuest = QuestRepo.GetPriorityQuestId(mAvailableQuest);
            }
            else
            {
                mActiveQuest = -1;
            }
            questController.UpdateTriggerData(this);
        }

        public int GetInteractId()
        {
            if (mActiveQuest != -1 && GameInfo.gLocalPlayer != null)
            {
                if (mOngoingQuest.Contains(mActiveQuest))
                {
                    return GameInfo.gLocalPlayer.QuestController.GetInteractiveId(mActiveQuest, mArchetypeID);
                }
                else if (mAvailableQuest.Contains(mActiveQuest))
                {
                    QuestJson questJson = QuestRepo.GetQuestByID(mActiveQuest);
                    if (questJson != null && questJson.triggertype == QuestTriggerType.Interact)
                    {
                        return questJson.triggercaller;
                    }
                }
            }
            return -1;
        }

        public int GetActivedQuestId()
        {
            return mActiveQuest;
        }

        public override int GetDisplayLevel()
        {
            return 1;
        }

        public void Despawn()
        {
            EntitySystem.RemoveEntityByID(ID);
        }

        public override void Show(bool val)
        {
            base.Show(mActiveStatus);
        }

        private void GetQuestList()
        {
            mQuestList = new List<int>();
            string[] ids = staticNPCJson.questid.Split(';');
            foreach (string id in ids)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    mQuestList.Add(int.Parse(id));
                }
            }
        }

        public override int GetArchetypeID()
        {
            return mArchetypeID;
        }

        public void OnPlayerNear()
        {
            if (GameInfo.gLocalPlayer != null && mActiveQuest != -1)
            {
                GameInfo.gLocalPlayer.QuestController.OnEnterStaticArea(this);
            }
        }

        public void OnPlayerAway()
        {
            if (GameInfo.gLocalPlayer != null)
            {
                GameInfo.gLocalPlayer.QuestController.OnExitStaticArea(this);
            }
        }
    }
}