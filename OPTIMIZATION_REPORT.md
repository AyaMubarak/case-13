# Android size optimization — 2026-10-26

Applied to this Unity project. No new APK was built, so the final APK size is
not yet measured. The previous 700+ MB APK was unavailable for comparison.

## Changes

- Embedded glTFast 6.20.0 with a small editor importer customization. Android
  embedded RGB/RGBA textures now use ASTC 4x4 at best encoder quality, without
  resizing. The stock importer left these textures in uncompressed ARGB32.
  Source GLB files, geometry, animations, materials and subasset IDs are unchanged.
  Desktop imports retain their original texture formats. HDR, compressed and
  tiny textures are skipped. See `Packages/com.unity.cloud.gltfast/CASE13_CHANGES.md`.
- Moved four Photon sample Resources folders under their samples' Editor
  folders, preserving every file and GUID. These samples are not referenced
  by the game's scene/runtime resource dependency graph. This prevents their
  automatic inclusion in the player. To build the optional Photon sample games,
  move their Resources folders and corresponding .meta files back out of Editor.
- Android build profile uses default ZIP packaging instead of nested LZ4.
  This favors APK size; loading performance may differ from LZ4 packaging.
- Android IL2CPP uses Optimize Size code generation. ARM64, the scripting
  backend and managed stripping settings retain their previous values.
- Added `Cases > Build Size > Audit Build Dependencies` and
  `Cases > Build Size > Build Optimized Android APK` editor menu commands.
  The build command creates `Builds/Android/Case13-optimized.apk` as a release
  build, with a packed asset size report in `Logs/BuildSize`.

Game scenes, gameplay scripts, videos, audio, lighting and image dimensions were
not edited. ASTC is lossy compression: preserving resolution does not mean
pixel-identical output. The full-resolution 4x4 setting prioritizes quality.

## Unity 6.3 URP build configuration repair

The project's global settings carried asset version 11 from a newer Unity
release, while the installed URP 17.3.0 requires version 10. The current PC
quality level also referenced a nonexistent pipeline GUID, and the Graphics
default pipeline was empty. Consequently Unity could not resolve the existing
RenderGraph settings during its build validation.

- Matched the global asset version to the installed URP schema.
- Assigned the existing Mobile_RPAsset as the default pipeline fallback.
- Corrected the PC quality reference to the existing PC_RPAsset.
- Preserved the existing RenderGraph setting (compatibility mode disabled).

An isolated Unity 6000.3.9f1 Android-target check invoked URP's actual
`URPBuildDataValidator.IsProjectValidForBuilding`. The original settings
reproduced both reported errors. The repaired settings returned **Valid: True**,
resolved PC_RPAsset and successfully located RenderGraphSettings. This verifies
the reported pre-build failure; it does not establish that a complete APK build
or device play test has passed.

## Verification

- Unity 6000.3.9f1 headless import of an actual game GLB, before and after the
  importer change, both completed successfully.
- Serialized textures: **2,799,856 → 702,764 bytes (74.90% smaller)**.
  This is a texture sample measurement, not a whole-game APK reduction.
- All imported subasset IDs, texture dimensions/mipmap counts and mesh
  vertex/submesh counts matched the baseline.
- Decoded ASTC sample comparison: RGB PSNR 46.76 and 57.05 dB;
  maximum alpha error 0/255 for the sampled images.
- Separate native compression checks passed for readable and non-readable
  textures, including preservation of dimensions and mipmap counts.
- Relocated sample files matched their saved SHA-256 hashes after moving.
- Unity dependency audit identified 21 referenced GLB model files, confirming
  that embedded images affect actual game content.
- The standalone `dotnet build` check could not resolve Unity's newly generated
  glTFast project references (missing generated glTFast.csproj). The embedded
  importer itself compiled and ran in the isolated Unity import validation.

No full APK build, on-device play test, or whole-game visual comparison was
performed. Final size and runtime behavior still require a new Android build
and device verification. Detailed local test outputs are in `Logs/BuildSize`.

## References

- [Unity: reducing build size](https://docs.unity.com/en-us/engine/6000.3/manual/building-and-publishing/reducing-filesize)
- [Unity: build target dependencies for scripted importers](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/assetimporters/assetimportcontext/selectedbuildtarget)
- [Unity: compressed player data and Android packaging](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/buildoptions/compresswithlz4)
