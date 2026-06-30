using System;
using UnityEngine;

namespace Bliss
{
    public class ViewEntity : MonoBehaviour
    {
        private void Awake()
        {

        }

        private void OnDestroy()
        {
            
        }
    }
    
    public struct Event_MapViewEntity
    {
        public GameObject go;
    }
    
    public struct Event_UnMapViewEntity
    {
        public GameObject go;
    }
}