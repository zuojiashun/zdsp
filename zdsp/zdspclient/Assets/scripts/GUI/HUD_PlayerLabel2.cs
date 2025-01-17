﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Zealot.Common;
using Zealot.Repository;
using Zealot.Client.Entities;
using Zealot.Common.Entities;
using Zealot.Common.Datablock;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This enum should tally with ActorStats's EffectVisualTypes
/// EffectVisualTypes is a flag
/// </summary>
public enum BuffEnum
{
    Stun,
    Slow,
    Silence,
    Root,
    Disarmed,
    Frozen,
    Petrify,
    NUM
};

public enum LabelTypeEnum
{
    Invalid = 0,
    Player,
    PartyMember,
    OtherPlayer,
    SelectedOtherPlayer,
    NPC,
    HurtNPC,
    Monster,            //No hp bar
    HurtMonster,      //Hp bar
    BossMonster,
    Battleground_Player,
    Battleground_Enemy,
    Battleground_Ally,
};

/// <summary>
/// Class Objective:
/// 1) Components for display are easily changeable from outside or public
/// 2) Hide convenient variables
/// 3) Provide convenient functions that work on (1)
/// 4) Not for reuse for other games
/// </summary>
public class HUD_PlayerLabel2 : MonoBehaviour
{
    private class BuffDebuff
    {
        public int controlstat = -1;
        public int skill = -1;
        public Image img = null;

        public void SetAsControlStatus(int ctrlIdx, Sprite spr)
        {
            controlstat = ctrlIdx;
            img.sprite = spr;
            skill = -1;
            img.gameObject.SetActive(true);
        }

        public void SetAsSkillDebuff(int skillIdx, Sprite spr)
        {
            controlstat = -1;
            img.sprite = spr;
            skill = skillIdx;
            img.gameObject.SetActive(true);
        }

        public void Turnoff()
        {
            controlstat = -1;
            skill = -1;
            img.gameObject.SetActive(false);
            img.sprite = null;
        }
    };

    public delegate Vector2 GetCanvasPosDelegate(Vector3 worldOffset);

    [Header("Inspector Linked GameObject")]
    #region Link gameobject
    [SerializeField]
    GameObject mAffinityLine;
    [SerializeField]
    GameObject mBuffParent;
    [SerializeField]
    GameObject mGuildParent;
    [SerializeField]
    GameObject mGuild;
    [SerializeField]
    GameObject mGuildIcon;
    [SerializeField]
    GameObject mTitleAndNameParent;
    [SerializeField]
    GameObject mTitle;
    [SerializeField]
    GameObject mName;
    [SerializeField]
    GameObject mHPShieldParent;    //Contains shield and HP bar
    [SerializeField]
    GameObject mHpBar;          //Contains HP bar
    [SerializeField]
    GameObject mShield;         //Contains value of shield
    #endregion

    #region shortcut var
    Gradient2 mAffLineGradient;
    Image mGuildIconImg;
    Text mGuildName;
    Text mTitleName;
    Text mPlayerName;
    UI_ProgressBarC mHpBarImg;         //Color portion
    Gradient2 mHpBarGradient;
    Text mShieldValue;
    LabelTypeEnum mLabelType = LabelTypeEnum.Invalid;
    GetCanvasPosDelegate mCanvasPosFunc = null;
    int mNumActiveBuff = 0;
    const int MAX_BUFF = 4;
    List<BuffDebuff> mActiveBuffList = new List<BuffDebuff>();
    #endregion

    #region properties
    //Properties
    public string GuildName
    {
        get { return mGuildName.text; }
        set
        {
            mGuildName.text = value;
            mGuildName.color = mGuildNameColor;
            CheckEmptyTextAutoOff(mGuildName, mGuild);
        }
    }
    public string Title
    {
        get { return mTitleName.text; }
        set
        {
            mTitleName.text = value;
            CheckEmptyTextAutoOff(mTitleName, mTitle);
        }
    }
    public string Name
    {
        get { return mPlayerName.text; }
        set
        {
            mPlayerName.text = value;
            CheckEmptyTextAutoOff(mPlayerName, mName);
        }
    }
    public float HPf
    {
        get { return mHpBarImg.BarImage.fillAmount; }
        set
        {
            //Do not cap value if max allowed to exceed
            if (!mHpBarImg.CanExceedMax)
                value = Mathf.Min(1f, value);
            value = Mathf.Max(0f, value);

            mHpBarImg.Value = (long)(value * mHpBarImg.Max);
            //Need to change color upon value change
        }
    }
    public long HP
    {
        get { return (long)mHpBarImg.Value; }
        set
        {
            value = Math.Max(0, value);
            mHpBarImg.Value = value;
        }
    }
    public long MaxHP
    {
        get { return (long)mHpBarImg.Max; }
        set
        {
            value = Math.Max(0, value);
            mHpBarImg.Max = value;
        }
    }
    public int Shield
    {
#if _ENABLE_GET_PROPERTY_
        get
        {
            int res;
            if (int.TryParse(mShieldValue.text, out res))
            {
                Debug.Log("HUD_PlayerLabel2 shield value get property: parse failed.");
                return -1;
            }
            return res;
        }
#endif
        set
        {
            value = Mathf.Max(0, value);
            mShieldValue.text = value.ToString();
            mShield.SetActive(value > 0);
        }
    }
    public Sprite GuildIcon
    {
        get { return mGuildIconImg.sprite; }
        set
        {
            mGuildIconImg.sprite = value;
        }
    }
    public LabelTypeEnum LabelType
    {
        get { return mLabelType; }
    }
    public Vector3 WorldSpaceOffset
    {
        set { mOffset_WorldSpace = value; }
    }
    public GetCanvasPosDelegate CanvasPosFunc
    {
        set { mCanvasPosFunc = value; }
    }
    private int NumBuff
    {
        get { return mNumActiveBuff; }
        set
        {
            mNumActiveBuff = value;
            CheckBuffAutoOff();
        }
    }
    #endregion

    [Header("Buff related")]
    [SerializeField]
    GameObject mBuffPrefab;
    [SerializeField]
    List<Sprite> mBuffPathList;


    [Header("HP bar gradient")]
    [SerializeField]
    UnityEngine.Gradient mPlayerGradient;
    [SerializeField]
    UnityEngine.Gradient mFriendlyGradient;
    [SerializeField]
    UnityEngine.Gradient mHostileGradient;
    [SerializeField]
    UnityEngine.Gradient mNPCGradient;
    [SerializeField]
    UnityEngine.Gradient mOtherPlayerGradient;

    [Header("Text Color")]
    [SerializeField]
    UnityEngine.Color mPlayerNameColor;
    [SerializeField]
    UnityEngine.Color mOtherNameColor;
    [SerializeField]
    UnityEngine.Color mEnemyNameColor;
    [SerializeField]
    UnityEngine.Color mGuildNameColor;

    RectTransform mRectTrans;
    Vector3 mOffset_WorldSpace = Vector3.zero;

    public void Awake()
    {
        mAffLineGradient = mAffinityLine.GetComponent<Gradient2>();
        mGuildIconImg = mGuild.GetComponentInChildren<Image>();
        mGuildName = mGuild.GetComponent<Text>();
        mTitleName = mTitle.GetComponent<Text>();
        mPlayerName = mName.GetComponent<Text>();
        mHpBarImg = mHpBar.GetComponent<UI_ProgressBarC>();
        mHpBarGradient = mHpBar.GetComponentInChildren<Gradient2>();
        mShieldValue = mShield.GetComponent<Text>();
        mRectTrans = gameObject.GetComponent<RectTransform>();

        mPlayerName.text = "";
        mTitleName.text = "";
        mGuildName.text = "";
        mShieldValue.text = "";

        //Create all buff gameobject icons and hide them
        //Create dict to quick access buff game object icon
        for (int i = 0; i < MAX_BUFF; ++i)
        {
            GameObject obj = Instantiate(mBuffPrefab, Vector3.zero, Quaternion.identity) as GameObject;
            obj.transform.SetParent(mBuffParent.transform, false);
            obj.SetActive(false);

            Image img = obj.GetComponent<Image>();
            img.sprite = null;

            BuffDebuff bd = new BuffDebuff();
            bd.img = img;

            mActiveBuffList.Add(bd);
        }
    }
    void OnDestroy()
    {
        GameObject playerlabelParent = UIManager.GetWidget(HUDWidgetType.PlayerLabel);
        if (playerlabelParent == null)
        {
            //When changing scene
            return;
        }

        HUD_LabelController lc = playerlabelParent.GetComponent<HUD_LabelController>();
        lc.DecrementLabelOrder(mLabelType);
    }
    public void UpdateAchorPos()
    {
        if (mCanvasPosFunc == null)
            return;

        mRectTrans.anchoredPosition = mCanvasPosFunc(mOffset_WorldSpace);
    }
    private void UpdateLabelController(LabelTypeEnum oldE, LabelTypeEnum newE)
    {
        GameObject playerlabelParent = UIManager.GetWidget(HUDWidgetType.PlayerLabel);
        if (playerlabelParent == null)
        {
            Debug.LogError("HUD_PlayerLabel2: This PlayerLabel has no parent");
            return;
        }

        //assign the new enum
        mLabelType = newE;

        //Assign label order
        HUD_LabelController lc = playerlabelParent.GetComponent<HUD_LabelController>();
        if (oldE != LabelTypeEnum.Invalid)
        {
            lc.ReassignPlayerLabel(oldE, gameObject, gameObject.transform.localPosition);
        }
        else
        {
            lc.AssignPlayerLabel(gameObject, gameObject.transform.localPosition);
        }

    }

    /// <summary>
    /// Set control debuff's icons, has higher priority than skill buff/debuff icons
    /// overwrites any skill buff/debuff icons
    /// </summary>
    /// <param name="e"></param>
    /// <param name="onoff"></param>
    public void SetUnsetBuffDebuff(EffectVisualTypes e, int onoff)
    {
        if (e >= EffectVisualTypes.NUM)
            return;
        
        //Cast bitwise flag into index for array
        //(Math: the power of the value of the flag is the index to our array)
        int ee = (int)Mathf.Log((int)e, 2);
        int emptyIdx = -1;
        int skilldebuff = -1;

        //Loop check, if buff exists, set according to onoff
        for (int i = 0; i < MAX_BUFF; ++i)
        {
            if (mActiveBuffList[i].controlstat == ee)
            {
                if (onoff <= 0)
                {
                    mActiveBuffList[i].Turnoff();
                    NumBuff--;
                }
                //return; //buff found, exit
            }

            else if (mActiveBuffList[i].controlstat < 0)
            {
                if (mActiveBuffList[i].skill < 0)
                    emptyIdx = i;
                else
                    skilldebuff = i;
            } 
        }

        //If buff dont exist, and buff turn off, exit
        if (onoff <= 0)
            return;

        //TODO: Allocate better later
        //Find a empty buff slot and initialize
        if (emptyIdx > 0)
        {
            mActiveBuffList[emptyIdx].SetAsControlStatus(ee, mBuffPathList[ee]);
            NumBuff++;
        }
        else if (skilldebuff > 0)
        {
            mActiveBuffList[skilldebuff].SetAsControlStatus(ee, mBuffPathList[ee]);
            NumBuff++;
        }
    }
    public void SetUnsetBuffDebuff(CollectionHandler<object> buffs)
    {
        if (buffs == null)
            return;

        //CollectionHandler<object> buffs - contain only 4 debuffs or buff
        bool isBuff = false;
        SEORIGINID origin = SEORIGINID.NONE;
        int originid = -1;
        int buffIdx = 0;

        for (int j = 0; j < MAX_BUFF; ++j, buffIdx += 2)
        {
            //Skip if this is a controlstatus debuff
            if (mActiveBuffList[j].controlstat >= 0)
                continue;

            //Grab 1 buff
            long t = Convert.ToInt64(buffs[buffIdx]);
            long ticks = Convert.ToInt64(buffs[buffIdx+1]);
            //if buff is no more, disable it
            if (t == 0)
            {
                if (mActiveBuffList[j].skill >= 0)
                {
                    mActiveBuffList[j].Turnoff();
                    NumBuff--;
                }
                continue;
            }

            //Decode buff/debuff
            isBuff = (t & (((long)1) << 40)) == (((long)1) << 40) ? true : false;
            origin = (SEORIGINID)((t >> 32) & ~(1 << 8));
            originid = (int)(t & ~1);

            Sprite spr = null;
            switch (origin)
            {
                case SEORIGINID.EITEM:
                    IInventoryItem item = GameRepo.ItemFactory.GetInventoryItem(originid);
                    spr = ClientUtils.LoadIcon(item.JsonObject.iconspritepath);
                    break;
                case SEORIGINID.ESKILL:
                    SkillData sd = SkillRepo.GetSkill(originid);
                    spr = ClientUtils.LoadIcon(sd.skillgroupJson.icon);
                    break;
            }

            //Check if its a new skill buff
            if (mActiveBuffList[j].skill < 0)
                NumBuff++;
            //Initialize buff icon with skill buff/debuff info;
            mActiveBuffList[j].SetAsSkillDebuff(originid, spr);

        }
    }

    public void SetBuffDebuff(ActorGhost ghost)
    {
        if (ghost == null)
            return;

        //ghost.PlayerStats.VisualEffectTypes;
        //SetUnsetBuffDebuff(BuffEnum.ArrowUp, ghost.PlayerStats.havebuff);
        //SetUnsetBuffDebuff(BuffEnum.ArrowDown, ghost.PlayerStats.havedebuff);
        //SetUnsetBuffDebuff(BuffEnum.GreenWaterdrop, ghost.PlayerStats.havedot);
        //SetUnsetBuffDebuff(BuffEnum.Chain, ghost.PlayerStats.havecontrol);
        //SetUnsetBuffDebuff(BuffEnum.HeartBroken, ghost.PlayerStats.havehot);
    }
    public void SetScale(Vector3 scale)
    {
        transform.localScale = scale;
    }

    //Quick set functions
    public void SetPlayer()
    {
        if (CheckSameType(LabelTypeEnum.Player))
            return;

        mAffinityLine.SetActive(true);
        mBuffParent.SetActive(true);
        mGuildParent.SetActive(true);
        mGuild.SetActive(true);
        mGuildIcon.SetActive(true);
        mTitleAndNameParent.SetActive(true);
        mTitle.SetActive(true);
        mName.SetActive(true);
        mHPShieldParent.SetActive(true);
        mHpBar.SetActive(true);
        mShield.SetActive(true);

        SetHPGradient(LabelTypeEnum.Player);
        SetNameColor(LabelTypeEnum.Player);

        CheckBuffAutoOff();
        CheckHaveGuildAutoOff();
        CheckHaveShieldAutoOff();
        CheckEmptyTextAutoOff(mTitleName, mTitle);

        UpdateLabelController(mLabelType, LabelTypeEnum.Player);
    }
    public void SetPartyMember()
    {
        if (CheckSameType(LabelTypeEnum.PartyMember))
            return;

        mAffinityLine.SetActive(true);
        mBuffParent.SetActive(false);
        mGuildParent.SetActive(true);
        mGuild.SetActive(true);
        mGuildIcon.SetActive(true);
        mTitleAndNameParent.SetActive(true);
        mTitle.SetActive(true);
        mName.SetActive(true);
        mHPShieldParent.SetActive(true);
        mHpBar.SetActive(true);
        mShield.SetActive(true);

        SetHPGradient(LabelTypeEnum.PartyMember);
        SetNameColor(LabelTypeEnum.PartyMember);

        CheckBuffAutoOff();
        CheckHaveGuildAutoOff();
        CheckHaveShieldAutoOff();
        CheckEmptyTextAutoOff(mTitleName, mTitle);

        UpdateLabelController(mLabelType, LabelTypeEnum.PartyMember);
    }
    public void SetMonster()
    {
        if (CheckSameType(LabelTypeEnum.Monster))
            return;

        //Does not show hp bar
        mAffinityLine.SetActive(false);
        mBuffParent.SetActive(true);
        mGuildParent.SetActive(false);
        mGuild.SetActive(false);
        mGuildIcon.SetActive(false);
        mTitleAndNameParent.SetActive(false);
        mTitle.SetActive(false);
        mName.SetActive(false);
        mHPShieldParent.SetActive(false);
        mHpBar.SetActive(false);
        mShield.SetActive(false);

        SetHPGradient(LabelTypeEnum.Monster);
        SetNameColor(LabelTypeEnum.Monster);

        CheckBuffAutoOff();
        CheckHaveShieldAutoOff();

        UpdateLabelController(mLabelType, LabelTypeEnum.Monster);
    }
    public void SetHurtMonster()
    {
        if (CheckSameType(LabelTypeEnum.HurtMonster))
            return;

        //Shows hp bar
        //Prevent adding more monster hp if exceeding limit
        GameObject playerlabelParent = UIManager.GetWidget(HUDWidgetType.PlayerLabel);
        HUD_LabelController lc = playerlabelParent.GetComponent<HUD_LabelController>();
        if (lc.isMonsterCountExceeded())
            return;

        mAffinityLine.SetActive(true);
        mBuffParent.SetActive(true);
        mGuildParent.SetActive(false);
        mGuild.SetActive(false);
        mGuildIcon.SetActive(false);
        mTitleAndNameParent.SetActive(true);
        mTitle.SetActive(false);
        mName.SetActive(true);
        mHPShieldParent.SetActive(true);
        mHpBar.SetActive(true);
        mShield.SetActive(true);

        SetHPGradient(LabelTypeEnum.HurtMonster);
        SetNameColor(LabelTypeEnum.HurtMonster);

        CheckBuffAutoOff();
        CheckHaveShieldAutoOff();

        UpdateLabelController(mLabelType, LabelTypeEnum.HurtMonster);
    }
    public void SetBoss()
    {
        if (CheckSameType(LabelTypeEnum.BossMonster))
            return;

        mAffinityLine.SetActive(true);
        mBuffParent.SetActive(true);
        mGuildParent.SetActive(false);
        mGuild.SetActive(false);
        mGuildIcon.SetActive(false);
        mTitleAndNameParent.SetActive(true);
        mTitle.SetActive(false);
        mName.SetActive(true);
        mHPShieldParent.SetActive(true);
        mHpBar.SetActive(true);
        mShield.SetActive(true);

        SetHPGradient(LabelTypeEnum.BossMonster);
        SetNameColor(LabelTypeEnum.BossMonster);

        CheckBuffAutoOff();
        CheckHaveShieldAutoOff();

        UpdateLabelController(mLabelType, LabelTypeEnum.BossMonster);
    }
    public void SetFieldPlayer()
    {
        if (CheckSameType(LabelTypeEnum.OtherPlayer))
            return;

        mAffinityLine.SetActive(true);
        mBuffParent.SetActive(false);
        mGuildParent.SetActive(true);
        mGuild.SetActive(true);
        mGuildIcon.SetActive(true);
        mTitleAndNameParent.SetActive(true);
        mTitle.SetActive(true);
        mName.SetActive(true);
        mHPShieldParent.SetActive(false);
        mHpBar.SetActive(false);
        mShield.SetActive(false);

        SetHPGradient(LabelTypeEnum.OtherPlayer);
        SetNameColor(LabelTypeEnum.OtherPlayer);

        CheckHaveGuildAutoOff();
        CheckHaveShieldAutoOff();
        CheckEmptyTextAutoOff(mTitleName, mTitle);

        UpdateLabelController(mLabelType, LabelTypeEnum.OtherPlayer);
    }
    public void SetSelectedFieldPlayer()
    {
        if (CheckSameType(LabelTypeEnum.SelectedOtherPlayer))
            return;

        mAffinityLine.SetActive(true);
        mBuffParent.SetActive(false);
        mGuildParent.SetActive(true);
        mGuild.SetActive(true);
        mGuildIcon.SetActive(true);
        mTitleAndNameParent.SetActive(true);
        mTitle.SetActive(true);
        mName.SetActive(true);
        mHPShieldParent.SetActive(true);
        mHpBar.SetActive(true);
        mShield.SetActive(true);

        SetHPGradient(LabelTypeEnum.SelectedOtherPlayer);
        SetNameColor(LabelTypeEnum.SelectedOtherPlayer);

        CheckHaveGuildAutoOff();
        CheckHaveShieldAutoOff();
        CheckEmptyTextAutoOff(mTitleName, mTitle);

        UpdateLabelController(mLabelType, LabelTypeEnum.SelectedOtherPlayer);
    }
    public void SetNPC()
    {
        if (CheckSameType(LabelTypeEnum.NPC))
            return;

        mAffinityLine.SetActive(true);
        mBuffParent.SetActive(false);
        mGuildParent.SetActive(true);
        mGuild.SetActive(true);
        mGuildIcon.SetActive(true);
        mTitleAndNameParent.SetActive(true);
        mTitle.SetActive(false);
        mName.SetActive(true);
        mHPShieldParent.SetActive(false);
        mHpBar.SetActive(false);
        mShield.SetActive(false);

        SetHPGradient(LabelTypeEnum.NPC);
        SetNameColor(LabelTypeEnum.NPC);

        CheckBuffAutoOff();
        CheckHaveGuildAutoOff();
        CheckHaveShieldAutoOff();

        UpdateLabelController(mLabelType, LabelTypeEnum.NPC);
    }
    public void SetHurtNPC()
    {
        if (CheckSameType(LabelTypeEnum.HurtNPC))
            return;

        mAffinityLine.SetActive(true);
        mBuffParent.SetActive(false);
        mGuildParent.SetActive(true);
        mGuild.SetActive(true);
        mGuildIcon.SetActive(true);
        mTitleAndNameParent.SetActive(true);
        mTitle.SetActive(false);
        mName.SetActive(true);
        mHPShieldParent.SetActive(true);
        mHpBar.SetActive(true);
        mShield.SetActive(true);

        SetHPGradient(LabelTypeEnum.HurtNPC);
        SetNameColor(LabelTypeEnum.HurtNPC);

        CheckBuffAutoOff();
        CheckHaveGuildAutoOff();
        CheckHaveShieldAutoOff();

        UpdateLabelController(mLabelType, LabelTypeEnum.HurtNPC);
    }
    public void SetPVPEnemy()
    {
        if (CheckSameType(LabelTypeEnum.Battleground_Enemy))
            return;

        //Does not show hp bar
        mAffinityLine.SetActive(true);
        mBuffParent.SetActive(true);
        mGuildParent.SetActive(true);
        mGuild.SetActive(true);
        mGuildIcon.SetActive(true);
        mTitleAndNameParent.SetActive(true);
        mTitle.SetActive(true);
        mName.SetActive(true);
        mHPShieldParent.SetActive(true);
        mHpBar.SetActive(true);
        mShield.SetActive(true);

        SetHPGradient(LabelTypeEnum.Battleground_Enemy);
        SetNameColor(LabelTypeEnum.Battleground_Enemy);

        CheckBuffAutoOff();
        CheckHaveShieldAutoOff();

        UpdateLabelController(mLabelType, LabelTypeEnum.Battleground_Enemy);
    }

    private void CheckEmptyTextAutoOff(Text s, GameObject obj)
    {
        if (s.text.Length > 0 || obj == null)
            return;

        obj.SetActive(false);
    }
    private void CheckBuffAutoOff()
    {
        mBuffParent.SetActive(mNumActiveBuff > 0);
    }
    private void CheckHaveGuildAutoOff()
    {
        CheckEmptyTextAutoOff(mGuildName, mGuild);
        mGuildIcon.SetActive(mGuild.GetActive());
        mGuildParent.SetActive(mGuild.GetActive());
    }
    private void CheckHaveShieldAutoOff()
    {
        int res;
        if (!int.TryParse(mShieldValue.text, out res))
        {
            mShield.SetActive(false);
            return;
        }

        mShield.SetActive(res > 0);
    }
    private bool CheckSameType(LabelTypeEnum e)
    {
        return (mLabelType == e);
    }

    private void SetHPGradient(LabelTypeEnum e)
    {
        switch (e)
        {
            case LabelTypeEnum.Player:
                mAffLineGradient.EffectGradient =
                mHpBarGradient.EffectGradient = mPlayerGradient;
                break;
            case LabelTypeEnum.Battleground_Ally:
            case LabelTypeEnum.PartyMember:
                mAffLineGradient.EffectGradient =
                mHpBarGradient.EffectGradient = mFriendlyGradient;
                break;
            case LabelTypeEnum.OtherPlayer:
            case LabelTypeEnum.SelectedOtherPlayer:
                mAffLineGradient.EffectGradient =
                mHpBarGradient.EffectGradient = mOtherPlayerGradient;
                break;
            case LabelTypeEnum.Monster:
            case LabelTypeEnum.HurtMonster:
            case LabelTypeEnum.BossMonster:
            case LabelTypeEnum.Battleground_Enemy:
                mAffLineGradient.EffectGradient =
                mHpBarGradient.EffectGradient = mHostileGradient;
                break;
            case LabelTypeEnum.NPC:
            case LabelTypeEnum.HurtNPC:
                mAffLineGradient.EffectGradient =
                mHpBarGradient.EffectGradient = mNPCGradient;
                break;
        }
    }
    private void SetNameColor(LabelTypeEnum e)
    {
        switch (e)
        {
            case LabelTypeEnum.Player:
            case LabelTypeEnum.Battleground_Ally:
            case LabelTypeEnum.PartyMember:
                mPlayerName.color = mPlayerNameColor;
                break;
            case LabelTypeEnum.OtherPlayer:
            case LabelTypeEnum.SelectedOtherPlayer:
            case LabelTypeEnum.Monster:
            case LabelTypeEnum.HurtMonster:
            case LabelTypeEnum.BossMonster:
            case LabelTypeEnum.NPC:
            case LabelTypeEnum.HurtNPC:
                mPlayerName.color = mOtherNameColor;
                break;
            case LabelTypeEnum.Battleground_Enemy:
                mPlayerName.color = mEnemyNameColor;
                break;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(HUD_PlayerLabel2))]
public class HUD_PlayerLabel2Editor : Editor
{
    public override void OnInspectorGUI()
    {
        HUD_PlayerLabel2 pl = (HUD_PlayerLabel2)target;
        if (GUILayout.Button("Player"))
        {
            pl.SetPlayer();
        }
        if (GUILayout.Button("Party Member"))
        {
            pl.SetPartyMember();
        }
        if (GUILayout.Button("Monster"))
        {
            pl.SetMonster();
        }
        if (GUILayout.Button("Hurt Monster"))
        {
            pl.SetHurtMonster();
        }
        if (GUILayout.Button("OtherPlayer"))
        {
            pl.SetFieldPlayer();
        }
        if (GUILayout.Button("Selected OtherPlayer"))
        {
            pl.SetSelectedFieldPlayer();
        }
        if (GUILayout.Button("Boss"))
        {
            pl.SetBoss();
        }
        if (GUILayout.Button("NPC"))
        {
            pl.SetNPC();
        }
        if (GUILayout.Button("Hurt NPC"))
        {
            pl.SetHurtNPC();
        }
        //if (GUILayout.Button("Buff01"))
        //{
        //    pl.AddBuffDebuff(BuffEnum.ArrowUp);
        //}
        //if (GUILayout.Button("Buff02"))
        //{
        //    pl.AddBuffDebuff(BuffEnum.ArrowDown);
        //}
        //if (GUILayout.Button("Buff03"))
        //{
        //    pl.AddBuffDebuff(BuffEnum.HeartBroken);
        //}
        //if (GUILayout.Button("Buff04"))
        //{
        //    pl.AddBuffDebuff(BuffEnum.GreenWaterdrop);
        //}
        //if (GUILayout.Button("Buff05"))
        //{
        //    pl.AddBuffDebuff(BuffEnum.Chain);
        //}
        //if (GUILayout.Button("BG Player"))
        //{
        //    pl.SetBattleGroundPlayer();
        //}
        //if (GUILayout.Button("BG Ally"))
        //{
        //    pl.SetBattleGroundAlly();
        //}
        if (GUILayout.Button("BG Enemy"))
        {
            pl.SetPVPEnemy();
        }
        base.OnInspectorGUI();
    }
}
#endif
