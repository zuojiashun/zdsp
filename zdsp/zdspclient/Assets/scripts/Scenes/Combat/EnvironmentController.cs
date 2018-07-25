﻿using UnityEngine;

public class EnvironmentController
{
    private Color mAmbientLight;
    private float mAmbientIntensity;
    private FogSettingZoomInMode mFogSettingZoomInMode;

    public EnvironmentController()
    {
        mAmbientLight = RenderSettings.ambientLight;
        mAmbientIntensity = RenderSettings.ambientIntensity;
        mFogSettingZoomInMode = GameObject.FindObjectOfType<FogSettingZoomInMode>();
    }

    public void SetUIAmbientLight(bool enable)
    {
        if (enable)
        {
            RenderSettings.ambientLight = new Color(0.314f, 0.314f, 0.314f);
            RenderSettings.ambientIntensity = 1f;
        }
        else
        {
            RenderSettings.ambientLight = mAmbientLight;
            RenderSettings.ambientIntensity = mAmbientIntensity;
        }
    }

    public void EnableZoomInMode(bool enable)
    {
        if (mFogSettingZoomInMode)
            mFogSettingZoomInMode.EnableZoomInMode(enable);
    }
}