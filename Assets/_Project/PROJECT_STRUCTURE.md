# Project Structure

`Assets/_Project` contains game-specific assets for the brainrot template.

- `Art/Brainrots/Prefabs` - ready-to-spawn brainrot prefabs with `BrainrotDefinition`.
- `Art/Brainrots/Models` - source brainrot meshes, textures, and imported model files.
- `Art/Brainrots/Icons` - index/shop icons generated for brainrots.
- `Art/Environment` - map models, lego/checker materials, ground materials, and textures.
- `Art/Characters` - player model, animations, animator controller, and destructible character parts.
- `Art/UI` - UI sprites, frames, buttons, fonts, and UI shaders.
- `Data` - ScriptableObject configs and catalogs.
- `Editor` - project editor tools.
- `Input` - input action assets.
- `Prefabs/UI` - shared UI prefabs.
- `Scenes` - playable scenes.

Runtime scripts stay in `Assets/Scripts` and are grouped by feature: `Base`, `Brainrot`, `Player`, `Progression`, `UI`, `Core`, and `Utility`.
