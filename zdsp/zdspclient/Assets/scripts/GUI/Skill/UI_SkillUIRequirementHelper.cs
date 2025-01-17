﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_SkillUIRequirementHelper : MonoBehaviour {

    public Text m_ValueName;
    public Text m_Colon;
    public Text m_Value;

    public void SetData(string name, string value)
    {
        m_ValueName.text = name;
        m_Value.text = value;
    }

    public void SetData(string name)
    {
        m_ValueName.text = "Skill";
        m_Value.text = name;
    }

    public void SetColor(Color color)
    {

        m_Value.color = color;
        m_ValueName.color = color;
        m_Colon.color = color;

    }
}
