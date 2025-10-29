using Bliss.Core;

namespace Bliss
{
    /// <summary>
    /// 将Session关联到LogicScene，使得LogicScene中的组件可以获取到Session
    /// </summary>
    public class SessionComponent : Entity
    {
        private EntityRef<Session>  _sessionRef;

        public Session Session
        {
            get
            {
                return (Session)_sessionRef;
            }
            set
            {
                _sessionRef = value;
            }
        }
        
    }

}