using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace Assets.Scripts
{
    static public class MethodExtensionForMonoBehaviourTransform
    {
        /// <summary>
        /// Gets or add a component. Usage example:
        /// BoxCollider boxCollider = transform.GetOrAddComponent<BoxCollider>();
        /// </summary>
        static public T GetOrAddComponent<T>(this Component child) where T : Component
        {
            T result = child.GetComponent<T>();
            if (result == null)
            {
                result = child.gameObject.AddComponent<T>();
            }
            return result;
        }
    }
}
