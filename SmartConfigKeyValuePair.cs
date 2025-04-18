using System;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [Serializable]
    public class SmartConfigKeyValuePair<TK, TV>
    {
        public TK Key
        {
            get => key;
            set => key = value;
        }

        public TV Value
        {
            get => value;
            set => this.value = value;
        }

        [SerializeField] private TK key;
        [SerializeField] private TV value;

        public SmartConfigKeyValuePair(TK key, TV value)
        {
            this.key = key;
            this.value = value;
        }
    }
}