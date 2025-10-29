using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FixMax_DataUIGraphic : BaseUIGraphic
{
    public float Max;
    public TextMeshProUGUI MiddleText;
    public string MiddleTextFormat;
    /// <summary>
    /// 由外部数据源设置
    /// </summary>
    [HideInInspector]
    public float Value;

    protected override void OnInit()
    {
        if (MiddleText != null)
        {
            if (string.IsNullOrEmpty(MiddleTextFormat))
            {
                MiddleText.text = $"{Max / 2}";
            }
            else
            {
                MiddleText.text = string.Format(MiddleTextFormat, Max / 2);
            }
        }
    }

    protected override float CollectData(int categoryIndex)
    {
        return Mathf.Min(Value, Max);
    }

    protected override float GetMax(float[] buffer)
    {
        return Max;
    }

    protected override float GetSum(float[] buffer)
    {
        return Max;
    }
}
