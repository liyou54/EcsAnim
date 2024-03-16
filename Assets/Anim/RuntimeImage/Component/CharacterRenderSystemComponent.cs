using System.Collections.Generic;
using Unity.Entities;

namespace Anim.RuntimeImage
{
    public class CharacterRenderSystemComponent : IComponentData
    {
        public Dictionary<BakedCharacterAsset, int> CharacterRender = new Dictionary<BakedCharacterAsset, int>();
        public Dictionary<int, CharacterRendererData> CharacterRendererDataDic = new Dictionary<int, CharacterRendererData>();
    }
}


// public class CharacterRendererDataChange
// {
//     public int SpriteCount;
//     public int CurrentInstanceId;
//     public int StartInstanceId;
//     public NativeList<CharacterEquipInstanceIndex> MoveId;
//     public NativeList<CharacterEquipInstanceIndex> AddId;
//     public NativeList<UpdateEquipBufferIndex> ChangeEquipId;
//     public NativeList<CharacterEquipInstanceIndex> UnLoadIndex;
//     public BatchMaterialID MaterialID;
//     public BatchMeshID MeshID;
//
//
//     public readonly List<IComponentData> TemplateList = new List<IComponentData>();
//
// }
//
//
// private Dictionary<int, CharacterRendererDataChange> CharacterRendererDataChangeDic = new Dictionary<int, CharacterRendererDataChange>();
//
//
// public void UpdateCharacterRendererDataChange()
// {
//
//     
//     foreach (var characterRendererData in characterRendererDataDic)
//     {
//         CharacterRendererData data = characterRendererData.Value;
//         if (!CharacterRendererDataChangeDic.TryGetValue(characterRendererData.Key, out var change))
//         {
//             change = new CharacterRendererDataChange();
//             change.AddId = new NativeList<CharacterEquipInstanceIndex>(Allocator.Persistent);
//             change.MoveId = new NativeList<CharacterEquipInstanceIndex>(Allocator.Persistent);
//             change.UnLoadIndex = new NativeList<CharacterEquipInstanceIndex>(Allocator.Persistent);
//             change.ChangeEquipId = new NativeList<UpdateEquipBufferIndex>(Allocator.Persistent);
//             change.SpriteCount = data.SpriteCount;
//             CharacterRendererDataChangeDic.Add(characterRendererData.Key, change);
//         }
//
//         change.MaterialID = data.BatchMaterialID;
//         change.MeshID = data.BatchMeshID;
//         change.CurrentInstanceId = data.EquipTexPosIdBuffer.UsedSize / data.SpriteCount;
//         change.StartInstanceId = change.CurrentInstanceId;
//         change.MoveId.Clear();
//         change.AddId.Clear();
//         change.UnLoadIndex.CopyFrom(data.UnLoadIndex);
//         change.ChangeEquipId.Clear();
//     }
// }
//
//
// public void ApplyCreateAndDel()
// {
//     foreach (var characterRendererData in characterRendererDataDic)
//     {
//         CharacterRendererData data = characterRendererData.Value;
//         if (CharacterRendererDataChangeDic.TryGetValue(characterRendererData.Key, out var change))
//         {
//             data.UnLoadIndex.Clear();
//             data.UnLoadIndex.CopyFrom(change.UnLoadIndex);
//             foreach (var move in change.MoveId)
//             {
//                 data.UnLoadIndex.Add(move);
//             }
//
//             if (change.MoveId.Length > 0)
//             {
//                 data.RemoveUsedInstance(change.MoveId);
//             }
//
//             if (change.AddId.Length > 0)
//             {
//                 var count = (change.CurrentInstanceId - change.StartInstanceId) * data.SpriteCount;
//                 if (count > 0)
//                 {
//                     var temp = new CharacterEquipInstanceIndex[count];
//                     data.EquipTexPosIdBuffer.AddData(temp);
//                 }
//             }
//
//             if (change.ChangeEquipId.Length > 0)
//             {
//             
//                 data.SetEquip( change.ChangeEquipId);
//             }
//             
//         }
//     }
// }