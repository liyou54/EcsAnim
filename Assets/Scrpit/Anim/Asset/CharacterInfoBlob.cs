using Anim.RuntimeImage;
using Unity.Collections;
using Unity.Entities;

namespace Scrpit.Anim.Asset
{
    public struct CharacterInfoBlob
    {
        public BlobArray<FixedString32Bytes> AnimBlob;
    }
}