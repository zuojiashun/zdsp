﻿using UnityEngine;
using UnityEngine.UI;

public class GameIconCmpt_StackCount : MonoBehaviour
{
    [SerializeField]
    Text txtStackCount = null;

    public void SetStackCount(int count)
    {
        txtStackCount.text = (count > 1) ? ((count > 999) ? "999+" : count.ToString()) : "";
    }

    public void SetStackCountFull(int count, bool uncapped = false)
    {
        txtStackCount.text = (count > 999) ? uncapped ? count.ToString() : "999+" : (count < -999) ? "-999+" : count.ToString();
    }

    public void SetStackCount(int invcount, int reqcount)
    {
        string hexaStr = ClientUtils.GetHexaStringFromColor(ClientUtils.ColorRed);
        string stackCountTxt = invcount < reqcount ? string.Format(ClientUtils.FormatStringColor("{0}", "#" + hexaStr) + "/{1}", invcount, reqcount) : string.Format("{0}/{1}", invcount, reqcount);
        txtStackCount.text = stackCountTxt;
    }
}
