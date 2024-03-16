using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Anim.RuntimeImage;
using Animancer;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Anim.Editor
{
    public class BakerPrefabDataWindow : OdinEditorWindow
    {
        [MenuItem("Tools/Anim/BakerPrefabDataWindow")]
        private static void OpenWindow()
        {
            GetWindow<BakerPrefabDataWindow>().Show();
        }

        private void OnBecameInvisible()
        {
            if (SkeletonPrefabInstance != null)
            {
                GameObject.DestroyImmediate(SkeletonPrefabInstance);
            }
        }

        // 骨架预制体
        [AssetsOnly] public GameObject SkeletonPrefab;
        public List<AnimationClip> Animations;
        [ReadOnly] public GameObject SkeletonPrefabInstance;

        // 获取spriteRender数据
        // 这里会产生z-fighting
        public List<SpriteRenderer> GetSpriteRenderers(GameObject skeletonPrefabInstance)
        {
            var res = skeletonPrefabInstance.GetComponentsInChildren<SpriteRenderer>()
                .OrderBy(render => render.sortingLayerID)
                .ThenBy(render => render.sortingOrder)
                .ToList();
            return res;
        }


        public (Texture2D, Texture2D, List<ClipInfo>) BakerAnimation(List<AnimationClip> clips, List<SpriteRenderer> renderers, AnimancerComponent animancerComponent)
        {
            var resSize = new int2(512, 512);
            var widthMulOne = resSize.x ;

            var resTexPosScale = new Texture2D(resSize.x, resSize.y, TextureFormat.RGBAHalf, false); // 存位置缩放
            var resTexRot = new Texture2D(resSize.x, resSize.y, TextureFormat.RHalf, false); // 存旋转

            resTexRot.anisoLevel = 0;
            resTexPosScale.anisoLevel = 0;
            resTexPosScale.filterMode = FilterMode.Point;
            resTexRot.filterMode = FilterMode.Point;
            
            var clipAllPiexelIndex = 0;
            var clipInfoList = new List<ClipInfo>();

            var renderCount = renderers.Count;

            // 写入默认数据
            for (int i = 0; i < renderers.Count; i++)
            {
                var (pos, scale, rot) = GetSpriteRenderData(SkeletonPrefabInstance, renderers[i]);
                // 写入图片 
                int2 picPos = new int2(clipAllPiexelIndex % widthMulOne, clipAllPiexelIndex / widthMulOne * renderCount + i);
                resTexPosScale.SetPixel(picPos.x, picPos.y, new Color(pos.x, pos.y, scale.x, scale.y));
                resTexRot.SetPixel(picPos.x, picPos.y, new Color(rot, 0, 0, 0));
            }

            clipAllPiexelIndex++;
            clipInfoList.Add(new ClipInfo()
            {
                Name = "Default",
                StartFrameTexIndex = 0,
                FrameCount = 0,
                Duration = 0
            });

            // 写入动画数据
            for (int clipIndex = 0; clipIndex < clips.Count; clipIndex++)
            {
                var lastClipIndex = clipAllPiexelIndex;
                var clip = clips[clipIndex];
                animancerComponent.Play(clip);
                var state = animancerComponent.States.Current;
                state.IsPlaying = false;
                var frameCount = (int)(clip.length * clip.frameRate);
                Debug.Log($"{clip.name}  frameCount:{frameCount}");
                
                // state.Time = state.Duration;
                // animancerComponent.Evaluate();
                // // TODO 写入第最后帧 但是好像依然会闪烁
                // for (int i = 0; i < renderers.Count; i++)
                // {
                //     var (pos, scale, rot) = GetSpriteRenderData(SkeletonPrefabInstance, renderers[i]);
                //     int2 picPos = new int2(clipAllPiexelIndex % widthMulOne, clipAllPiexelIndex / widthMulOne * renderCount + i);
                //     resTexPosScale.SetPixel(picPos.x, picPos.y, new Color(pos.x, pos.y, scale.x, scale.y));
                //     resTexRot.SetPixel(picPos.x, picPos.y, new Color(rot, 0, 0, 0));
                // }
                // clipAllPiexelIndex++;
                
                for (int frameIndex = 0; frameIndex < frameCount + 1; frameIndex++)
                {
                    var time = frameIndex * 1f / clip.frameRate;
                    state.Time = time;
                    animancerComponent.Evaluate();
                    for (int i = 0; i < renderers.Count; i++)
                    {
                        var (pos, scale, rot) = GetSpriteRenderData(SkeletonPrefabInstance, renderers[i]);
                        // 写入图片 注意需要双线性插值 所以最后一个像素写入下一列的第一个像素
                        int2 picPos = new int2(clipAllPiexelIndex % widthMulOne, clipAllPiexelIndex / widthMulOne * renderCount + i);
                        resTexPosScale.SetPixel(picPos.x, picPos.y, new Color(pos.x, pos.y, scale.x, scale.y));
                        resTexRot.SetPixel(picPos.x, picPos.y, new Color(rot, 0, 0, 0));
                    }
                    
                    clipAllPiexelIndex++;
                }
                
                // state.Time = 0;
                // animancerComponent.Evaluate();
                // // TODO 写入第一帧 但是好像依然会闪烁
                // for (int i = 0; i < renderers.Count; i++)
                // {
                //     var (pos, scale, rot) = GetSpriteRenderData(SkeletonPrefabInstance, renderers[i]);
                //     int2 picPos = new int2(clipAllPiexelIndex % widthMulOne, clipAllPiexelIndex / widthMulOne * renderCount + i);
                //     resTexPosScale.SetPixel(picPos.x, picPos.y, new Color(pos.x, pos.y, scale.x, scale.y));
                //     resTexRot.SetPixel(picPos.x, picPos.y, new Color(rot, 0, 0, 0));
                // }
                // clipAllPiexelIndex++;

                clipInfoList.Add(new ClipInfo()
                {
                    Name = clip.name,
                    StartFrameTexIndex = lastClipIndex ,
                    FrameCount = clipAllPiexelIndex - lastClipIndex - 1,
                    Duration = clip.length
                });
            }

            // 将后面一行的第一个像素写入最后一个像素
            // for (int i = 1; i < resSize.y; i++)
            // {
            //     resTexPosScale.SetPixel(resSize.x - 1, i, resTexPosScale.GetPixel(0, i));
            //     resTexRot.SetPixel(resSize.x - 1, i, resTexRot.GetPixel(0, i));
            // }

            resTexPosScale.Apply();
            resTexRot.Apply();

            return (resTexPosScale, resTexRot, clipInfoList);
        }


        private (float2, float2, float) GetSpriteRenderData(GameObject gameObjectInstance, SpriteRenderer spriteRenderer)
        {
            var transform = spriteRenderer.transform;
            var relativePos = transform.position - gameObjectInstance.transform.position;
            var lossyScale = transform.lossyScale;
            Quaternion relativeRotation = Quaternion.Inverse(gameObjectInstance.transform.rotation) * Quaternion.Euler(spriteRenderer.transform.eulerAngles);
            return (new float2(relativePos.x, relativePos.y), new float2(lossyScale.x, lossyScale.y), relativeRotation.eulerAngles.z * Mathf.Deg2Rad);
        }

        private Mesh GenMesh(List<SpriteRenderer> spriteRenderers)
        {
            // 生成mesh
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();

            for (int i = 0; i < spriteRenderers.Count; i++)
            {
                vertices.Add(new Vector3(-0.5f, -0.5f, 0));
                vertices.Add(new Vector3(0.5f, -0.5f, 0));
                vertices.Add(new Vector3(0.5f, 0.5f, 0));
                vertices.Add(new Vector3(-0.5f, 0.5f, 0));
                var index = i * 4;

                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 1);
                triangles.Add(index);
                triangles.Add(index + 3);
                triangles.Add(index + 2);


                uv.Add(new Vector2(0, 0));
                uv.Add(new Vector2(1, 0));
                uv.Add(new Vector2(1, 1));
                uv.Add(new Vector2(0, 1));
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uv.ToArray();
            return mesh;
        }

        [Button]
        public void Reset()
        {
            if (SkeletonPrefabInstance != null)
            {
                GameObject.DestroyImmediate(SkeletonPrefabInstance);
            }
        }

        [Button]
        public void GenMeshBySpriteRender()
        {
            // 准备数据 不能在源prefab上操作
            // localScale < 0 存在问题 简单的解决方案是双面渲染

            if (SkeletonPrefabInstance != null)
            {
                DestroyImmediate(SkeletonPrefabInstance);
            }

            SkeletonPrefabInstance = GameObject.Instantiate(SkeletonPrefab);
            SkeletonPrefabInstance.transform.position = Vector3.zero;
            SkeletonPrefabInstance.transform.rotation = Quaternion.identity;
            SkeletonPrefabInstance.transform.localScale = Vector3.one;
            SkeletonPrefabInstance.hideFlags = HideFlags.HideAndDontSave;
            var animator = SkeletonPrefabInstance.GetComponentInChildren<Animator>();
            AnimancerComponent animancerComponent = animator.gameObject.AddComponent<AnimancerComponent>();
            var spriteRenderers = GetSpriteRenderers(SkeletonPrefabInstance);
            var saveName = SkeletonPrefab.name;
            var savePathName = Path.GetDirectoryName( AssetDatabase.GetAssetPath(SkeletonPrefab)) + "/" + saveName;
            Debug.Log(savePathName);
            var mesh = GenMesh(spriteRenderers);

            var bakedCharacterAsset = ScriptableObject.CreateInstance<BakedCharacterAsset>();
            bakedCharacterAsset.mesh = mesh;
            bakedCharacterAsset.SpriteRenderCount = spriteRenderers.Count;
            bakedCharacterAsset.DefaultSprites = new List<Sprite>();

            var (resTexPosScale, resTexRot, clipCount) = BakerAnimation(Animations, spriteRenderers, animancerComponent);
            bakedCharacterAsset.PosScaleTex = resTexPosScale;
            bakedCharacterAsset.RotTex = resTexRot;
            bakedCharacterAsset.ClipInfo = clipCount;

            for (int i = 0; i < spriteRenderers.Count; i++)
            {
                bakedCharacterAsset.DefaultSprites.Add(spriteRenderers[i].sprite);
            }

            var material = new Material(UnityEngine.Shader.Find("Unlit/Character"));
            material.SetTexture("_PosAndScaleBuffer", resTexPosScale);
            material.SetTexture("_RotationRadiusBuffer", resTexRot);
            material.SetInt("_BufferSizeX", resTexPosScale.width);
            material.SetInt("_BufferSizeY", resTexPosScale.height);
            material.SetInt("_SpriteCount", spriteRenderers.Count);

            bakedCharacterAsset.CharacterMaterial = material;
            resTexPosScale.name = saveName + "_pos_scale";
            resTexRot.name = saveName + "_rot";
            mesh.name = saveName + "_mesh";
            material.name = saveName + "_material";
            AssetDatabase.CreateAsset(bakedCharacterAsset, savePathName + "_baked_character.asset");
            AssetDatabase.AddObjectToAsset(resTexPosScale, bakedCharacterAsset);
            AssetDatabase.AddObjectToAsset(resTexRot, bakedCharacterAsset);
            AssetDatabase.AddObjectToAsset(mesh, bakedCharacterAsset);
            AssetDatabase.AddObjectToAsset(material, bakedCharacterAsset);


            AssetDatabase.SaveAssets();
        }
    }
}