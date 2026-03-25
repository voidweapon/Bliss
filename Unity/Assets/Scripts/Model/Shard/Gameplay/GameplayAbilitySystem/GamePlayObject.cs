using Bliss.Base;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    public class GamePlayObject : BObject
    {
        private SubClassOf<GamePlayObject> _subClassOf;
        
        public object GetDefaultObject()
        {
            if (_subClassOf == null)
            {
                _subClassOf = new SubClassOf<GamePlayObject>(GetType());
            }
            
            return AbilitySystemGlobals.Instance.GetDefaultObject(_subClassOf);
        }
    }
}