using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Varaince_DataUIGraphic : BaseUIGraphic
{
    private List<float> Values;

    protected override void OnInit()
    {
        base.OnInit();
        Values =  new List<float>(CategoryCountLimit);
        for (int i = 0; i < CategoryCountLimit; i++)
        {
            Values.Add(0);
        }
    }

    protected override float CollectData(int categoryIndex)
    {
        return Values[categoryIndex];
    }

    public void SetValue(int categoryIndex, float value)
    {
        Values[categoryIndex] = value;
    }
}
