# VRoid Avatar Studio · 二次元角色形象页

[English](#english) | [中文](#中文)

A runtime "character screen" for anime avatars in Unity, built on **UniVRM** and the
**VRoid** pipeline. Drop a `.vrm` in, and sculpt the face, swap outfits, change skin
tone, and preview motions — with hair/skirt spring-bone cloth physics throughout.

一个用 **Unity + UniVRM** 搭的二次元角色「形象页」，模型走 **VRoid** 管线。丢一个
`.vrm` 进去，就能捏脸、换装、换肤、预览动作，全程带头发/裙摆的弹簧骨布料物理。

> The whole UI is built from code at runtime — put one `CharacterScreen` component in a
> scene and press Play. No prefabs to wire.
> 整个 UI 都是运行时纯代码生成——场景里放一个 `CharacterScreen` 组件，按 Play 即可，
> 不用拖任何预制体。

---

## English

### Features
- **Face sculpting (捏脸)** — exposes the VRM blendshapes as sliders.
- **Outfits (换装)** — toggle clothing parts exported in the model.
- **Skin tone (换肤)** — presets plus live RGB tuning.
- **Motion preview (动作)** — built-in procedural motions (idle / sway / bow / nod /
  wave) that need no assets, plus any **Mixamo** Humanoid clip dropped into
  `Resources/Motions` (auto-retargeted onto the VRoid skeleton at runtime via a
  PlayableGraph — no AnimatorController required).
- **Cloth physics** — `VRMSpringBone` for hair/skirt, with runtime stiffness / gravity
  / drag sliders, a wind toggle, and a one-click body-collider pass to stop the skirt
  clipping through the legs.

### Requirements
- Unity 6 (6000.x), Built-in Render Pipeline
- UniVRM 0.131.x (VRM 0.x — `com.vrmc.gltf` + `com.vrmc.univrm`)
- A `.vrm` exported from VRoid Studio — export as **VRM 0.0**

### Getting started
1. Put your `.vrm` in `Assets/StreamingAssets/Avatars/` (the first one is auto-loaded).
2. Run the menu `AvatarStudio ▸ Build Character Scene`, open the generated scene
   `Assets/_Project/Scenes/Character.unity`, and press **Play**.
3. *(Optional)* Add real motions: download Mixamo clips as **FBX Binary, Without Skin,
   In Place**, and drop the `.fbx` into `Assets/_Project/Resources/Motions/`. They are
   auto-configured to Humanoid + looping and show up in the 动作 tab.

### Notes
- VRM models and Mixamo FBX are **not** committed (licensing + size). The drop folders
  and their readme placeholders are kept so the structure is obvious.
- Built-in procedural motions only drive spine/chest/head/hips (rig-axis-safe); big
  arm-driven motion is meant to come from Mixamo.

---

## 中文

### 功能
- **捏脸** —— 把 VRM 形态键做成滑条。
- **换装** —— 切换模型里导出的服装部件。
- **换肤** —— 肤色预设 + 实时 RGB 微调。
- **动作预览** —— 内置程序化动作（待机/摇摆/鞠躬/点头/挥手），无需素材即可用；
  以及拖进 `Resources/Motions` 的任意 **Mixamo** Humanoid 动作（运行时通过
  PlayableGraph 自动重定向到 VRoid 骨架，无需 AnimatorController）。
- **布料物理** —— 头发/裙摆用 `VRMSpringBone`，运行时可调刚度/重力/阻尼，带风开关
  和一键防穿模身体碰撞。

### 环境要求
- Unity 6（6000.x），Built-in 渲染管线
- UniVRM 0.131.x（VRM 0.x —— `com.vrmc.gltf` + `com.vrmc.univrm`）
- VRoid Studio 导出的 `.vrm` —— 导出选 **VRM 0.0**

### 快速开始
1. 把 `.vrm` 放到 `Assets/StreamingAssets/Avatars/`（自动加载第一个）。
2. 跑菜单 `AvatarStudio ▸ Build Character Scene`，打开生成的场景
   `Assets/_Project/Scenes/Character.unity`，按 **Play**。
3. *（可选）* 加真实动作：Mixamo 下载选 **FBX Binary、Without Skin、In Place**，把
   `.fbx` 拖进 `Assets/_Project/Resources/Motions/`，会自动设成 Humanoid + 循环，
   并在「动作」栏里出现。

### 说明
- VRM 模型和 Mixamo FBX **不入库**（授权 + 体积），只保留占位文件夹和说明，方便看清结构。
- 内置程序化动作只驱动 脊柱/胸/头/胯（rig 轴向安全）；大幅度的手臂动作交给 Mixamo。

---

## License
[MIT](LICENSE)
