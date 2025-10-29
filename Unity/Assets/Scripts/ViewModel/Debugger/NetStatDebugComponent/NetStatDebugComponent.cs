using Bliss;
using TMPro;
using UnityEngine;

namespace Bliss.ViewModel.Debugger
{
    /// <summary>
    /// 显示ping，乱序率，丢包率，抖动
    /// </summary>
    public class NetStatDebugComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public GameObject UI;
        public TextMeshProUGUI TextPing;
        public TextMeshProUGUI TextMiss;
        public TextMeshProUGUI TextLoss;
        public TextMeshProUGUI TextJitter;
        public FixMax_DataUIGraphic Graphic_Jitter;

        public int Simpled_Ping;
        public int Simpled_Miss;
        public int Simpled_Loss;
        public int Simpled_Jitter;
    }
}