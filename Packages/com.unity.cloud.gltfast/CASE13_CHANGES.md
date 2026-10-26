# Project customization of glTFast 6.20.0

This is an embedded copy of Unity's installed glTFast package, retaining its
licenses, original asset GUIDs, and runtime implementation.

Only the editor import path changes:

- GltfImporter version 2 declares its build target dependency through
  `AssetImportContext.selectedBuildTarget`.
- Android embedded RGB/RGBA images use ASTC 4x4 at best encoder quality.
- Desktop imports, external TextureImporter assets, HDR/compressed textures,
  and images smaller than 16 pixels on either axis retain their original format.
- Resolution, alpha, mipmaps, color space, model geometry, animations, materials,
  and subasset identifiers retain their existing settings.

ASTC is lossy GPU compression. Full-resolution 4x4 blocks prioritize visual
quality over the smaller 6x6/8x8 formats. Source GLB files remain byte-identical.
The project already targets ASTC on Android; this extends that choice to images
that the stock glTF importer otherwise serializes without GPU compression.

When upgrading glTFast, reapply these changes to the new editor importer and
rerun the import/compression checks. Removing the embedded package restores the
registry implementation, including its uncompressed embedded images.
