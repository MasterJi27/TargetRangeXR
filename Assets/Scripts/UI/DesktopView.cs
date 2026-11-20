using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TargetRange.Core;

namespace TargetRange.UI
{
    /// <summary>Compact desktop HUD. XR keeps its world-space boards and results canvas.</summary>
    public sealed class DesktopView : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private XRDesktopBridge bridge;
        [SerializeField] private TMP_FontAsset font;
        private GameObject root, menu, hud, released, crosshair;
        private TMP_Text score, timer, hint;
        private Canvas[] worldBoards;
        private bool wasDesktop;
        private static readonly Color Panel = new Color(0.035f, 0.05f, 0.065f, 0.96f);
        private static readonly Color Accent = new Color(1f, 0.73f, 0.28f);
        private void Start()
        {
            worldBoards = FindObjectsByType<Canvas>();
            root = new GameObject("Desktop Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            menu = Box("Start Card", root.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(740, 490));
            Text(menu.transform, "TARGET RANGE / 60 SECOND CHALLENGE", new Vector2(0, 195), new Vector2(660, 36), 20, Accent);
            Text(menu.transform, "Ready on the firing line", new Vector2(0, 135), new Vector2(660, 65), 44, Color.white);
            Text(menu.transform, "Pick up the pistol. Aim at a target. Make every shot count.", new Vector2(0, 78), new Vector2(660, 40), 23, Color.white);
            Text(menu.transform, "WASD  move     Mouse / Arrow keys  aim\nLeft click / Space  fire     E  equip or return gun\nEsc  release cursor (the round keeps running)", new Vector2(0, -5), new Vector2(660, 105), 23, new Color(0.7f, 0.76f, 0.8f));
            Button(menu.transform, "PICK UP & START  [E]", new Vector2(0, -103), new Vector2(650, 66), bridge.EquipDesktopPistol, true);
            Button(menu.transform, "XR SIMULATOR  [F2]", new Vector2(-167, -184), new Vector2(315, 56), () => bridge.SetSimulatorMode(true), false);
            Button(menu.transform, "EXIT", new Vector2(167, -184), new Vector2(315, 56), game.QuitGame, false);
            hud = Box("Live HUD", root.transform, new Vector2(0.5f, 1f), new Vector2(0, -61), new Vector2(450, 78));
            score = Text(hud.transform, "SCORE  000", new Vector2(-105, 0), new Vector2(215, 62), 30, Accent);
            timer = Text(hud.transform, "01:00", new Vector2(120, 0), new Vector2(170, 62), 38, Color.white);
            hint = Text(root.transform, "", new Vector2(0, 35), new Vector2(1350, 38), 20, Color.white);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            crosshair = Box("Reticle", root.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5, 5));
            crosshair.GetComponent<Image>().color = Accent;
            released = Box("Cursor Released", root.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 210));
            Text(released.transform, "CURSOR RELEASED", new Vector2(0, 60), new Vector2(560, 48), 30, Accent);
            Text(released.transform, "The timer continues. Press Esc or click below to aim.", new Vector2(0, 12), new Vector2(570, 40), 21, Color.white);
            Button(released.transform, "RESUME AIMING", new Vector2(0, -55), new Vector2(550, 60), bridge.Resume, true);
            Refresh();
        }
        private void Update() { if (root != null) Refresh(); }
        private void Refresh()
        {
            bool desktop = bridge.IsDesktopMode;
            root.SetActive(desktop);
            if (desktop != wasDesktop)
            {
                foreach (var board in worldBoards)
                    if (board.name == "Scoreboard Bench" || board.name == "Instruction Board" || board.name == "Scoreboard Wall")
                        board.enabled = !desktop;
                wasDesktop = desktop;
            }
            if (!desktop) return;
            bool playing = game.State == GameState.Playing;
            menu.SetActive(game.State == GameState.Ready);
            hud.SetActive(playing);
            released.SetActive(playing && !bridge.IsCaptured);
            crosshair.SetActive(playing && bridge.IsCaptured);
            score.text = $"SCORE  {game.Score.Score:000}";
            int seconds = Mathf.CeilToInt(game.RemainingSeconds);
            timer.text = $"{seconds / 60:00}:{seconds % 60:00}";
            timer.color = seconds <= 10 ? new Color(1f, 0.35f, 0.3f) : Color.white;
            hint.text = game.State == GameState.Ended ? "R / Enter  replay     Q  exit     F2  XR simulator" :
                playing ? "WASD  move     Mouse / Arrows  aim     Click / Space  fire     E  equip / drop     Esc  cursor" : "Desktop mode  |  No headset needed";
        }
        private GameObject Box(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size; rect.anchoredPosition = position;
            var image = go.GetComponent<Image>(); image.color = Panel; image.raycastTarget = false;
            return go;
        }
        private TMP_Text Text(Transform parent, string value, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = fontSize; text.color = color;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.enableAutoSizing = true; text.fontSizeMin = fontSize * 0.8f; text.fontSizeMax = fontSize;
            text.rectTransform.sizeDelta = size; text.rectTransform.anchoredPosition = position;
            return text;
        }
        private void Button(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, bool primary)
        {
            GameObject go = Box(label, parent, new Vector2(0.5f, 0.5f), position, size);
            var image = go.GetComponent<Image>(); image.color = primary ? Accent : new Color(0.13f, 0.17f, 0.20f); image.raycastTarget = true;
            var button = go.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            Text(go.transform, label, Vector2.zero, size - new Vector2(20, 8), 24, primary ? Panel : Color.white);
        }
    }
}
