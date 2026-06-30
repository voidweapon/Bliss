using TMPro;
using UnityEngine.UI;

namespace Bliss
{
    public class UILoginComponent : Entity, IAwake, IDestroy
    {
        public TMP_InputField Input_User;
        public TMP_InputField Input_Pwd;
        public Button Btn_Login;
    }
}