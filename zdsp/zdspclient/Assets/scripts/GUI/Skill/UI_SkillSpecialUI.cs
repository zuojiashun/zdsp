﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zealot.Common;
using Zealot.Repository;
using Kopio.JsonContracts;


public class UI_SkillSpecialUI : MonoBehaviour {

    [Header("Prefabs")]
    public GameObject m_SpecialSkillRow;
    public GameObject m_SkillIconData;

    [Header("Content Rect")]
    public GameObject m_ContentRect;

    [Header("Skill Details Panel")]
    public UI_SkillSpecialExpandUI m_SkillDescriptor;

    public UI_SkillTree m_Parent { get; set; }
    private UI_SkillTree.GameObjectPoolManager m_SpecialRowPool;
    private UI_SkillTree.GameObjectPoolManager m_SkillIconPool;

    private int m_RowCount = 0;
    private GameObject m_CurrentRow;

    private UI_SkillSpecialSelectButton m_BasicAttack;

    [SerializeField]
    private UnityEngine.UI.Button m_Close;

    private UI_SkillButtonBase m_CurrentActive;

    public void Initialise(Transform parent)
    {
        m_SpecialRowPool = new UI_SkillTree.GameObjectPoolManager(3, parent, m_SpecialSkillRow);
        m_SkillIconPool = new UI_SkillTree.GameObjectPoolManager(9, parent, m_SkillIconData);
        m_SkillDescriptor.Initialise(this.transform);
        m_SkillDescriptor.gameObject.SetActive(false);
    }

    private UI_SkillSpecialSelectButton AddSkillToList(SkillData skill)
    {
        UI_SkillSpecialSelectButton button = null;
        if (m_RowCount == 3 || m_RowCount == 0)
        {
            m_RowCount = 0;
            m_CurrentRow = m_SpecialRowPool.RequestObject();
            
            m_CurrentRow.transform.SetParent(m_ContentRect.transform, false);
            m_CurrentRow.transform.localPosition = new Vector3(0, 0, 1);
            m_CurrentRow.transform.localScale = new Vector3(1, 1, 1);
        }
        if (m_RowCount < 3)
        {
            GameObject obj = m_SkillIconPool.RequestObject();
            obj.transform.SetParent(m_CurrentRow.transform, false);
            obj.transform.localPosition = new Vector3(0, 0, 1);
            obj.transform.localScale = new Vector3(1, 1, 1);
            button = obj.GetComponent<UI_SkillSpecialSelectButton>();
            ++m_RowCount;
        }
        return button;
    }

    public void GenerateList(JobType job)
    {
        // Add basic attack as a special skill
        //PartsType weaponType = GameInfo.gLocalPlayer.WeaponTypeUsed;
        //string genderStr = (GameInfo.gLocalPlayer.PlayerSynStats.Gender == 0) ? "M" : "F";
        SkillData bskill = SkillRepo.GetSkill(GameInfo.gLocalPlayer.SkillStats.basicAttack1SId);
        m_BasicAttack = AddSkillToList(bskill);
        m_BasicAttack.Init(bskill);
        m_BasicAttack.AddListener(OnSelectSkill);

        // get list of special skills from repo
        List<int> skills = SkillRepo.GetSpecialSkillGivenJob(job);
        foreach(var skill in skills)
        {
            SkillData skd = SkillRepo.GetSkill(skill);
            UI_SkillSpecialSelectButton button = AddSkillToList(skd);
            button.Init(skd);
            button.AddListener(OnSelectSkill);
        }
    }

    public void OnSelectSkill(UI_SkillButtonBase button)
    {
        if(m_CurrentActive == null)
        {
            m_CurrentActive = button;
            m_SkillDescriptor.gameObject.SetActive(true);
            m_SkillDescriptor.Show(m_CurrentActive);
        }
        else if(m_CurrentActive == button)
        {
            m_CurrentActive = null;
            m_SkillDescriptor.OnClosed();
            m_SkillDescriptor.CloseUI();
        }
        else
        {
            m_CurrentActive.m_Toggle.isOn = false;
            m_CurrentActive = button;
            m_SkillDescriptor.gameObject.SetActive(true);
            m_SkillDescriptor.Show(m_CurrentActive);
        }

        //if (button.m_Toggle.isOn && m_CurrentActive != button && m_CurrentActive != null)
        //    //if(m_CurrentActive != button && m_CurrentActive != null)
        //    m_CurrentActive.m_Toggle.isOn = false;

        //if (button.m_Toggle.isOn)
        //{
        //    m_CurrentActive = button;
        //    m_SkillDescriptor.gameObject.SetActive(true);
        //    m_SkillDescriptor.Show(m_CurrentActive);

        //}
        //else if (!button.m_Toggle.isOn && button == m_CurrentActive)
        //{
        //    m_SkillDescriptor.OnClosed();
        //    //m_SkillDescriptor.CloseUI();
        //}
    }

    public void OnClose()
    {
        if(m_CurrentActive != null)
            m_CurrentActive.m_Toggle.isOn = false;
        m_CurrentActive = null;
    }

    public void CloseUI()
    {
        m_Close.onClick.Invoke();
    }

    public void UpdateBasicAttack(int skid)
    {
        SkillData skd = SkillRepo.GetSkill(skid);
        if(skd != null && m_BasicAttack != null)
        m_BasicAttack.OnValueUpdate(skd);
    }
}
