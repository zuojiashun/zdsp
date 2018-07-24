﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class UI_YesNoDialog : BaseWindowBehaviour
{
    public Text Content;
    public GameObject YesButton;
    public GameObject NoButton;
    public GameObject OkButton;

    private Action mOnClickYesCallBack;
    private Action mOnClickNoCallBack;
    private Action mOnClickOkCallBack;
    private Action mCountdownCallBack;

    private string mMesssage = "";
    private int mCountDown = 0;
    private Coroutine countdownTimer;

    public void InitYesNoDialog(string content, Action yes_callback, Action no_callback, int countdown = 0, Action cd_callback = null)
    {
        YesButton.SetActive(true);
        NoButton.SetActive(true);
        OkButton.SetActive(false);

        Content.text = content;

        mOnClickYesCallBack = yes_callback;
        mOnClickNoCallBack = no_callback;
        mCountdownCallBack = cd_callback;

        if (countdown > 0)
        {
            mMesssage = content;
            mCountDown = countdown;
            countdownTimer = StartCoroutine(Countdown(1));
        }
    }

    public void InitOkDialog(string content, Action ok_callback)
    {
        YesButton.SetActive(false);
        NoButton.SetActive(false);
        OkButton.SetActive(true);

        Content.text = content;
        mOnClickOkCallBack = ok_callback;
    }

    public void OnClicked_Yes()
    {
        if (mOnClickYesCallBack != null)
            mOnClickYesCallBack();
        GetComponent<UIDialog>().OnClosing();
    }

    public void OnClicked_No()
    {
        if (mOnClickNoCallBack != null)
            mOnClickNoCallBack();
        GetComponent<UIDialog>().OnClosing();
    }

    public void OnClicked_Ok()
    {
        if (mOnClickOkCallBack != null)
            mOnClickOkCallBack();
        GetComponent<UIDialog>().OnClosing();
    }

    void OnDisable()
    {
        mOnClickYesCallBack = null;
        mOnClickNoCallBack = null;
        mOnClickOkCallBack = null;
        mCountdownCallBack = null;

        if (countdownTimer != null)
        {
            StopCoroutine(countdownTimer);
            countdownTimer = null;
        }
        mCountDown = 0;
    }

    IEnumerator Countdown(int intervalSeconds)
    {
        Content.text = mMesssage.Replace("{countdown}", mCountDown.ToString());

        yield return new WaitForSeconds(intervalSeconds);

        mCountDown -= intervalSeconds;
        if (mCountDown > 0)
            countdownTimer = StartCoroutine(Countdown(intervalSeconds));
        else
        {
            if (mCountdownCallBack != null)
            {
                mCountdownCallBack();
                GetComponent<UIDialog>().OnClosing();
            }
            else
                OnClicked_No();
        }
    }
}
