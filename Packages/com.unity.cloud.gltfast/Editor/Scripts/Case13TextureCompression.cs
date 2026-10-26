// Project-specific editor import optimization. Source image data remains untouched.
using UnityEditor;
using UnityEngine;

namespace GLTFast.Editor
{
    internal static class Case13TextureCompression
    {
        internal static void Compress(Texture2D texture)
        {
            // Keep HDR, single-channel, already compressed and tiny constant
            // textures unchanged. ASTC 4x4 preserves alpha and costs 8 bits/pixel.
            // Never rescale images or regenerate normal maps/mipmap chains.
            if (texture.width < 16 || texture.height < 16) return;
            switch (texture.format)
            {
                case TextureFormat.RGB24:
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.BGRA32:
                    EditorUtility.CompressTexture(texture, TextureFormat.ASTC_4x4,
                        TextureCompressionQuality.Best);
                    if (texture.format != TextureFormat.ASTC_4x4)
                        Debug.LogWarning("Could not compress embedded Android texture: " + texture.name);
                    break;
            }
            // CompressTexture works on imported non-readable textures. Calling
            // Apply here would throw for those textures and is unnecessary.
        }
    }
}
