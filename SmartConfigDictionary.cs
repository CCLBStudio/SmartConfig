using System;
using System.Collections.Generic;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [Serializable]
    public class SmartConfigDictionary<TK, TV>
    {
        [SerializeField] public List<SmartConfigKeyValuePair<TK, TV>> pairs = new();
    }
}
