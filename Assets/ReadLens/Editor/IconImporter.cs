using UnityEditor;

namespace ReadLens.EditorTools
{
    // Material Symbols PNGs (white glyphs) become UI sprites, tinted at runtime by UIKit.
    class IconImporter : AssetPostprocessor
    {
        public override uint GetVersion() => 1;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/ReadLens/Icons/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true; // avoids dark fringes from the transparent black pixels
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed; // tiny, and ASTC would blur the edges
        }
    }
}
