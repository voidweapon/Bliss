using QFSW.QC;

namespace Bliss.ViewModel.Debugger
{
    public class Console : Singleton<Console>, ISingletonInit<LogicScene>
    {
        private LogicScene _attachedScene;
        public LogicScene  AttachedScene => _attachedScene;

        public void Init(LogicScene param)
        {
            _attachedScene= param;
            QuantumConsoleProcessor.GenerateCommandTable(true);
        }
    }
}