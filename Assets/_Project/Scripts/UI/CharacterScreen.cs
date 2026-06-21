using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AvatarStudio
{
    /// <summary>
    /// Entry point for the 形象 (character) page. Builds camera, light, UI at runtime, loads the
    /// VRoid VRM from StreamingAssets/Avatars, and connects the 捏脸 / 换装 / 换肤 systems.
    /// Just put this component on one GameObject in the scene and press Play.
    /// </summary>
    public class CharacterScreen : MonoBehaviour
    {
        AvatarContext _avatar;
        FaceCustomizer _face;
        SkinColorChanger _skin;
        OutfitManager _outfit;
        TurntableController _turntable;
        MotionController _motion;
        SpringBoneTuner _spring;

        Camera _cam;
        Text _hint;
        Text _hud;
        readonly Dictionary<string, GameObject> _tabBodies = new();
        readonly Dictionary<string, Button> _tabButtons = new();
        string _activeTab = "face";
        Color _skinTint = Color.white;

        static readonly Color TabActive = new(0.26f, 0.60f, 1.00f);
        static readonly Color TabIdle = new(0.16f, 0.19f, 0.27f);

        async void Start()
        {
            EnsureEventSystem();
            EnsureCameraAndLight();
            BuildShell();

            Directory.CreateDirectory(VrmAvatarLoader.AvatarsDirectory);
            string path = VrmAvatarLoader.FindFirstVrm();
            if (path == null)
            {
                ShowHint($"未找到角色模型。\n请用 VRoid Studio 导出 .vrm，放到：\n{VrmAvatarLoader.AvatarsDirectory}\n然后重新运行。");
                return;
            }

            try
            {
                _avatar = await VrmAvatarLoader.LoadAsync(path);
                if (_avatar == null) { ShowHint("加载 VRM 失败。"); return; }

                SetupAvatar();
                PopulateTabs();
                ShowTab("face");
                if (_hint) _hint.gameObject.SetActive(false);
            }
            catch (System.Exception e)
            {
                // async void swallows exceptions silently — surface them on screen so we are never blind.
                ShowHint($"初始化出错：\n{e.GetType().Name}: {e.Message}");
                Debug.LogException(e);
            }
        }

        void Update()
        {
            if (_hud == null) return;
            var es = EventSystem.current;
            _hud.text =
                $"诊断 HUD  Clicks:{UIFactory.Clicks}  OverUI:{UiPointer.IsOverUI()}  " +
                $"ES:{(es ? es.name : "NULL")}  Tab:{_activeTab}\n" +
                (_avatar == null ? "avatar: (loading…)" :
                    $"morphs:{(_face!=null?_face.Morphs.Count:0)}  skinMats:{(_skin!=null?_skin.SkinMaterialCount:0)}  outfit:{(_outfit!=null?_outfit.Items.Count:0)}");
        }

        // ---------- scene plumbing ----------

        void EnsureEventSystem()
        {
            var es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem));
                go.AddComponent<StandaloneInputModule>();
                es = go.GetComponent<EventSystem>();
            }
            else
            {
                es.enabled = true;
                var module = es.GetComponent<BaseInputModule>();
                if (module == null)
                {
                    // EventSystem exists but has no input module → no clicks are delivered at all.
                    es.gameObject.AddComponent<StandaloneInputModule>();
                }
                else if (!module.enabled)
                {
                    // A disabled module silently swallows every click — re-enable it.
                    module.enabled = true;
                }
            }
            EventSystem.current = es;
        }

        void EnsureCameraAndLight()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera));
                go.tag = "MainCamera";
                _cam = go.GetComponent<Camera>();
            }
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.16f, 0.18f, 0.24f);
            _cam.transform.position = new Vector3(0, 1.0f, 3.0f);
            _cam.transform.rotation = Quaternion.identity;

            if (FindFirstObjectByType<Light>() == null)
            {
                var lgo = new GameObject("Key Light", typeof(Light));
                var l = lgo.GetComponent<Light>();
                l.type = LightType.Directional;
                l.color = new Color(1f, 0.97f, 0.92f);
                l.intensity = 1.1f;
                lgo.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
            }
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);
        }

        void SetupAvatar()
        {
            _face = new FaceCustomizer(_avatar);
            _skin = new SkinColorChanger(_avatar);
            _outfit = new OutfitManager(_avatar);

            _turntable = _avatar.Root.AddComponent<TurntableController>();

            var procedural = _avatar.Root.AddComponent<ProceduralMotion>();
            procedural.Bind(_avatar.Animator);
            _motion = _avatar.Root.AddComponent<MotionController>();
            _motion.Bind(_avatar.Animator, procedural);
            _spring = _avatar.Root.AddComponent<SpringBoneTuner>();
            _spring.Bind(_avatar.Root, _avatar.Animator);

            FrameCamera();
        }

        void FrameCamera()
        {
            var bounds = new Bounds(_avatar.Root.transform.position, Vector3.one);
            bool has = false;
            foreach (var r in _avatar.AllRenderers)
            {
                if (!has) { bounds = r.bounds; has = true; }
                else bounds.Encapsulate(r.bounds);
            }
            float height = Mathf.Max(bounds.size.y, 0.5f);
            float dist = height * 1.5f + 0.6f;
            Vector3 center = bounds.center;
            _cam.transform.position = new Vector3(center.x, center.y + height * 0.15f, center.z + dist);
            _cam.transform.LookAt(new Vector3(center.x, center.y, center.z));
            _cam.fieldOfView = 35f;
        }

        // ---------- UI ----------

        void BuildShell()
        {
            var canvas = UIFactory.CreateCanvas("UI");

            // Title
            var title = UIFactory.Label(canvas.transform, "形象 · 角色定制", 30, TextAnchor.UpperLeft, UIFactory.TextColor);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(0, 1);
            trt.pivot = new Vector2(0, 1);
            trt.anchoredPosition = new Vector2(28, -22);
            trt.sizeDelta = new Vector2(600, 44);

            // Right control panel
            var panel = UIFactory.Panel(canvas.transform, "ControlPanel", UIFactory.PanelColor);
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(1, 0); prt.anchorMax = new Vector2(1, 1);
            prt.pivot = new Vector2(1, 0.5f);
            prt.sizeDelta = new Vector2(470, 0);
            prt.anchoredPosition = Vector2.zero;

            // Tab buttons
            var tabRow = UIFactory.Panel(panel.transform, "Tabs", new Color(0, 0, 0, 0), raycast: false);
            var trt2 = tabRow.rectTransform;
            trt2.anchorMin = new Vector2(0, 1); trt2.anchorMax = new Vector2(1, 1);
            trt2.pivot = new Vector2(0.5f, 1);
            trt2.sizeDelta = new Vector2(0, 64);
            trt2.anchoredPosition = new Vector2(0, -10);
            var hl = tabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            hl.spacing = 8; hl.padding = new RectOffset(12, 12, 6, 6);

            _tabButtons["face"] = UIFactory.Button(tabRow.transform, "捏脸", TabIdle, () => ShowTab("face"));
            _tabButtons["outfit"] = UIFactory.Button(tabRow.transform, "换装", TabIdle, () => ShowTab("outfit"));
            _tabButtons["skin"] = UIFactory.Button(tabRow.transform, "换肤", TabIdle, () => ShowTab("skin"));
            _tabButtons["motion"] = UIFactory.Button(tabRow.transform, "动作", TabIdle, () => ShowTab("motion"));

            // Tab body host
            foreach (var key in new[] { "face", "outfit", "skin", "motion" })
            {
                var body = UIFactory.Panel(panel.transform, $"Body_{key}", new Color(0, 0, 0, 0), raycast: false);
                var b = body.rectTransform;
                UIFactory.Stretch(b, 10, 70, 10, 84);
                _tabBodies[key] = body.gameObject;
                body.gameObject.SetActive(false);
            }

            // Bottom bar: auto-spin + reset
            var bar = UIFactory.Panel(canvas.transform, "BottomBar", new Color(0, 0, 0, 0), raycast: false);
            var brt = bar.rectTransform;
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(0, 0);
            brt.pivot = new Vector2(0, 0);
            brt.anchoredPosition = new Vector2(28, 24);
            brt.sizeDelta = new Vector2(360, 56);
            var hl2 = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl2.childControlWidth = true; hl2.childControlHeight = true;
            hl2.childForceExpandWidth = true; hl2.childForceExpandHeight = true;
            hl2.spacing = 10;
            UIFactory.Button(bar.transform, "自动旋转", new Color(0.2f, 0.28f, 0.4f),
                () => { if (_turntable) _turntable.AutoSpin = !_turntable.AutoSpin; });
            UIFactory.Button(bar.transform, "重置脸型", new Color(0.4f, 0.25f, 0.28f),
                () => { _face?.ResetAll(); });

            // Diagnostic HUD (top-center) — proves whether clicks reach the UI.
            _hud = UIFactory.Label(canvas.transform, "诊断 HUD", 18, TextAnchor.UpperCenter, new Color(1f, 0.9f, 0.4f, 1f));
            var hud = _hud.rectTransform;
            hud.anchorMin = new Vector2(0.5f, 1); hud.anchorMax = new Vector2(0.5f, 1);
            hud.pivot = new Vector2(0.5f, 1);
            hud.anchoredPosition = new Vector2(0, -16);
            hud.sizeDelta = new Vector2(1100, 60);

            // Hint (center)
            _hint = UIFactory.Label(canvas.transform, "", 22, TextAnchor.MiddleCenter, UIFactory.TextColor);
            var hrt = _hint.rectTransform;
            hrt.anchorMin = new Vector2(0.5f, 0.5f); hrt.anchorMax = new Vector2(0.5f, 0.5f);
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.sizeDelta = new Vector2(900, 300);
            _hint.gameObject.SetActive(false);

            // Highlight the default tab from frame 1 — before the (possibly slow) VRM load — so the
            // tabs visibly respond to clicks even while the model is still loading.
            ShowTab("face");
        }

        void ShowHint(string msg)
        {
            if (_hint == null) return;
            _hint.gameObject.SetActive(true);
            _hint.text = msg;
        }

        void ShowTab(string key)
        {
            _activeTab = key;
            foreach (var kv in _tabBodies) kv.Value.SetActive(kv.Key == key);
            foreach (var kv in _tabButtons)
                UIFactory.SetBaseColor(kv.Value, kv.Key == key ? TabActive : TabIdle);
        }

        void PopulateTabs()
        {
            PopulateFaceTab(_tabBodies["face"].transform);
            PopulateOutfitTab(_tabBodies["outfit"].transform);
            PopulateSkinTab(_tabBodies["skin"].transform);
            PopulateMotionTab(_tabBodies["motion"].transform);
        }

        void PopulateMotionTab(Transform parent)
        {
            var content = UIFactory.ScrollList(parent);

            UIFactory.SectionHeader(content, "程序化动作（内置，无需素材）");
            MotionButton(content, "待机", MotionRoutine.Idle);
            MotionButton(content, "摇摆扭胯", MotionRoutine.Sway);
            MotionButton(content, "鞠躬", MotionRoutine.Bow);
            MotionButton(content, "点头致意", MotionRoutine.Nod);
            MotionButton(content, "挥手", MotionRoutine.Wave);

            UIFactory.SectionHeader(content, $"Mixamo 动作（共 {_motion.Clips.Count} 个）");
            if (_motion.Clips.Count == 0)
            {
                UIFactory.Label(content,
                    "把 Mixamo 的 .fbx 拖到 Assets/_Project/Resources/Motions/，运行时这里会自动列出并重定向到当前角色。",
                    15, TextAnchor.UpperLeft, UIFactory.TextColor);
            }
            else
            {
                foreach (var clip in _motion.Clips)
                {
                    var captured = clip;
                    var b = UIFactory.Button(content, captured.name, new Color(0.18f, 0.30f, 0.24f),
                        () => _motion.PlayClip(captured));
                    var le = b.gameObject.AddComponent<LayoutElement>();
                    le.minHeight = 44; le.preferredHeight = 44;
                }
            }

            UIFactory.SectionHeader(content, $"布料物理（{_spring.Count} 条弹簧骨）");
            UIFactory.SliderRow(content, "刚度", Mathf.Clamp01(_spring.Stiffness / 4f), v => _spring.SetStiffness(v * 4f));
            UIFactory.SliderRow(content, "重力", Mathf.Clamp01(_spring.Gravity / 2f), v => _spring.SetGravity(v * 2f));
            UIFactory.SliderRow(content, "阻尼", Mathf.Clamp01(_spring.Drag), v => _spring.SetDrag(v));

            bool wind = false;
            Button windBtn = null;
            windBtn = UIFactory.Button(content, "风：关", new Color(0.22f, 0.24f, 0.30f), () =>
            {
                wind = !wind;
                _spring.SetWind(wind);
                var tx = windBtn.GetComponentInChildren<Text>();
                if (tx) tx.text = wind ? "风：开" : "风：关";
            });
            var wle = windBtn.gameObject.AddComponent<LayoutElement>();
            wle.minHeight = 44; wle.preferredHeight = 44;

            Button colBtn = null;
            colBtn = UIFactory.Button(content, "加防穿模碰撞", new Color(0.22f, 0.24f, 0.30f), () =>
            {
                _spring.AddBodyColliders();
                var tx = colBtn.GetComponentInChildren<Text>();
                if (tx) tx.text = "已加碰撞 ✓";
                colBtn.interactable = false;
            });
            var cle = colBtn.gameObject.AddComponent<LayoutElement>();
            cle.minHeight = 44; cle.preferredHeight = 44;
        }

        void MotionButton(Transform parent, string label, MotionRoutine routine)
        {
            var b = UIFactory.Button(parent, label, new Color(0.20f, 0.28f, 0.40f),
                () => _motion.PlayProcedural(routine, label));
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 44; le.preferredHeight = 44;
        }

        void PopulateFaceTab(Transform parent)
        {
            var content = UIFactory.ScrollList(parent);
            if (_face.Morphs.Count == 0)
            {
                UIFactory.SectionHeader(content, "该模型没有可调形态键。");
                UIFactory.Label(content, "用 Tools/blender_add_face_morphs.py 给模型加捏脸形态键。", 15,
                    TextAnchor.UpperLeft, UIFactory.TextColor);
                return;
            }

            string lastGroup = null;
            for (int i = 0; i < _face.Morphs.Count; i++)
            {
                var m = _face.Morphs[i];
                if (m.Group != lastGroup)
                {
                    UIFactory.SectionHeader(content, m.Group);
                    lastGroup = m.Group;
                }
                int idx = i; // capture
                UIFactory.SliderRow(content, m.DisplayName, _face.GetWeight01(idx),
                    v => _face.SetWeight01(idx, v));
            }
        }

        void PopulateOutfitTab(Transform parent)
        {
            var content = UIFactory.ScrollList(parent);
            if (_outfit.Items.Count == 0)
            {
                UIFactory.SectionHeader(content, "没有可切换的服装部件。");
                UIFactory.Label(content, "在 VRoid 里多穿几件，或导出多套 VRM 来整套切换。", 15,
                    TextAnchor.UpperLeft, UIFactory.TextColor);
                return;
            }

            string lastCat = null;
            foreach (var item in _outfit.Items)
            {
                if (item.Category != lastCat)
                {
                    UIFactory.SectionHeader(content, item.Category);
                    lastCat = item.Category;
                }
                var captured = item;
                Button btn = null;
                btn = UIFactory.Button(content, ItemLabel(captured),
                    new Color(0.18f, 0.3f, 0.24f), () =>
                    {
                        _outfit.Toggle(captured);
                        var t = btn.GetComponentInChildren<Text>();
                        if (t) t.text = ItemLabel(captured);
                    });
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 44; le.preferredHeight = 44;
            }
        }

        static string ItemLabel(OutfitManager.Item item) =>
            (item.On ? "● " : "○ ") + item.DisplayName;

        void PopulateSkinTab(Transform parent)
        {
            var content = UIFactory.ScrollList(parent);
            UIFactory.SectionHeader(content, $"肤色预设（共 {_skin.SkinMaterialCount} 个皮肤材质）");

            var presetRow = new GameObject("Presets", typeof(RectTransform));
            presetRow.transform.SetParent(content, false);
            var hl = presetRow.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            hl.spacing = 6;
            var ple = presetRow.AddComponent<LayoutElement>();
            ple.minHeight = 46; ple.preferredHeight = 46;

            foreach (var preset in SkinColorChanger.Presets)
            {
                var tint = preset.tint;
                UIFactory.Button(presetRow.transform, preset.label, new Color(0.22f, 0.24f, 0.3f),
                    () => { _skinTint = tint; _skin.SetTint(tint); RefreshSkinSliders(); });
            }

            UIFactory.SectionHeader(content, "微调 RGB");
            _skinR = UIFactory.SliderRow(content, "红 R", _skinTint.r, v => { _skinTint.r = v; _skin.SetTint(_skinTint); });
            _skinG = UIFactory.SliderRow(content, "绿 G", _skinTint.g, v => { _skinTint.g = v; _skin.SetTint(_skinTint); });
            _skinB = UIFactory.SliderRow(content, "蓝 B", _skinTint.b, v => { _skinTint.b = v; _skin.SetTint(_skinTint); });
        }

        Slider _skinR, _skinG, _skinB;
        void RefreshSkinSliders()
        {
            if (_skinR) _skinR.SetValueWithoutNotify(_skinTint.r);
            if (_skinG) _skinG.SetValueWithoutNotify(_skinTint.g);
            if (_skinB) _skinB.SetValueWithoutNotify(_skinTint.b);
        }
    }
}
