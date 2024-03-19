using System;
using System.Collections.Generic;
using Anim.RuntimeImage;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anim
{
    [Serializable]
    [HideReferenceObjectPicker]
    [TableList()]
    public class EquipArrayInfoAsset
    {
        public EquipType EquipType;
        [Serializable]
        [HideReferenceObjectPicker]
        public class EquipArrayInfoEditor
        {
            public String Name;
            public SpriteRenderer SpriteRenderer;
        }
        [TableList()]
        public List<EquipArrayInfoEditor> EquipArrayInfoEditors;
        public bool IsSpriteGroup;
    }

    public class EquipArrayInfo : MonoBehaviour
    {
        [HideReferenceObjectPicker]    [TableList()]
        public List<EquipArrayInfoAsset> EquipArrayInfos;
    }
}