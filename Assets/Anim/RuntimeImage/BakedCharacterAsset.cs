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

    public class BakedCharacterAsset : ScriptableObject
    {
        [ReadOnly] public Mesh mesh;
        [ReadOnly] public int SpriteRenderCount;
        [ReadOnly] public Texture2D PosScaleTex;
        [ReadOnly] public Texture2D RotTex;
        [ReadOnly] public List<ClipInfo> ClipInfo;
        [ReadOnly] public Material CharacterMaterial;
        public ComputeShader WriteEquipArrayIndexComputeShader;
        public List<Sprite> DefaultSprites;
    }
}