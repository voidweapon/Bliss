using System;
using UnityEngine;

namespace Bliss.ViewModel.Debugger
{
    public class GameDebugStatesGUI : MonoBehaviour
    {
        private void OnGUI()
        {
            GameDebugStatesView.Instance?.Draw();
        }
    }
}