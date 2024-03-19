using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anim.RuntimeImage
{
    [Serializable]
    public struct ClipInfo
    {
        public string Name;
        public int StartFrameTexIndex;
        public int FrameCount;
        public float Duration;
    }

    public enum EquipType
    {
        [LabelText("左手武器")] WeaponLeft = 1,
        [LabelText("左手盾牌")] ShieldLeft = 2,
        [LabelText("右手武器")] WeaponRight = 3,
        [LabelText("右手盾牌")] ShieldRight = 4,
        [LabelText("身体")] Body = 5, 
        [LabelText("眼睛")] Eye = 6,
        [LabelText("头发")] Hair =7,
        [LabelText("胡子")] FaceHair = 8,
        [LabelText("衣服")] Cloth = 9,
        [LabelText("裤子")] Pants = 10,
        [LabelText("背部装备")] Back = 11,
        [LabelText("头盔")] Helmet1 = 12,
        [LabelText("头盔2")] Helmet2 = 13,
        [LabelText("肩甲")] Armor = 14,
    }

    
    [Serializable]
    
    public class EquipCellInfo
    {
        public string Name;
        public int index;
        public Sprite DefaultSprite;
    }
    
    [Serializable]
    public class EquipInfo
    {
        public EquipType EquipType;
        public List<EquipCellInfo> EquipCellInfos;
    }

    public class BakedCharacterAsset : ScriptableObject
    {
        [ReadOnly] public Mesh mesh;
        [ReadOnly] public int SpriteRenderCount;
        [ReadOnly] public Texture2D PosScaleTex;
        [ReadOnly] public Texture2D RotTex;
        [ReadOnly] public List<ClipInfo> ClipInfo;
        [ReadOnly] public Material CharacterMaterial;
        public ComputeShader WriteEquipArrayIndexComputeShader;
        public List<EquipInfo> EquipInfos;
    }
}