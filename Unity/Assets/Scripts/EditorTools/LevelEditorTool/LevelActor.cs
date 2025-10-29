using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bliss;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;


namespace Bliss.EditorTool.LevelEditorTool
{
    public class LevelActor : MonoBehaviour
    {
        [ReadOnly]
        public long NetworkIdentity;
    
        [ValueDropdown(nameof(GetFilteredEntityTypeList))]
        public string ActorType;
    
        [ValueDropdown(nameof(GetFilteredComponentTypeList))]
        public List<string> ComponentTypes = new List<string>();

        public IEnumerable<string> GetFilteredComponentTypeList()
        {
            var modelCmp =  typeof(Entry).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && t.IsSubclassOf(typeof(Entity)))
                .Select(t => t.FullName);
        
            var viewModelCmp =  typeof(Bliss.ViewModel.PlaceHolder).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && t.IsSubclassOf(typeof(Entity)))
                .Select(t => t.FullName);
       
            return modelCmp.AppendWith(viewModelCmp);
        }
    
        public IEnumerable<string> GetFilteredEntityTypeList()
        {
            var modelCmp =  typeof(Entry).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && t.IsSubclassOf(typeof(Entity)))
                .Select(t => t.FullName);
        
            var viewModelCmp =  typeof(Bliss.ViewModel.PlaceHolder).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && t.IsSubclassOf(typeof(Entity)))
                .Select(t => t.FullName);
       
            return modelCmp.AppendWith(viewModelCmp);
        }
    }
}

