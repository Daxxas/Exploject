using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    const float fpsMeasurePeriod = 0.1f;
    private int m_FpsAccumulator = 0;
    private float m_FpsNextPeriod = 0;
    private int m_CurrentFps;
    const string display = "{0} FPS";
    private TextMeshProUGUI tmp;
    
    private void Start()
    {
        m_FpsNextPeriod = Time.realtimeSinceStartup + fpsMeasurePeriod;
        tmp = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        // measure average frames per second
        m_FpsAccumulator++;
        if (Time.realtimeSinceStartup > m_FpsNextPeriod)
        {
            m_CurrentFps = (int) (m_FpsAccumulator/fpsMeasurePeriod);
            m_FpsAccumulator = 0;
            m_FpsNextPeriod += fpsMeasurePeriod;
            tmp.text = string.Format(display, m_CurrentFps);
        }    
    }
}
