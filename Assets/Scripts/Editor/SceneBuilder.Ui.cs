using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TargetRange.Core;
using TargetRange.Feedback;
using TargetRange.UI;

namespace TargetRange.EditorTools
{
    /// <summary>
    /// The world space UI. XR has no screen, so every panel is a Canvas set to "World Space" that hangs in the room:
    /// a small scoreboard and an instruction board on the shooting bench, a big scoreboard on the far wall,
    /// and the end of round panel that is moved in front of the player when time runs out.
    /// </summary>
    public static partial class SceneBuilder
    {
        private const int UiLayer = 5;

        private static void BuildUi(GameManager game, AudioService audio, Camera head)
        {
            Transform ui = Folder("World Space UI");

            // Close scoreboard on the bench (right), instruction board (left). Both tilt up towards the player's eyes.
            Canvas nearBoard = WorldCanvas("Scoreboard Bench", ui, new Vector2(832, 512), 0.52f, head, new Vector3(0.50f, 1.245f, 1.09f), Quaternion.Euler(25f, 0f, 0f), false);
            BuildScoreboard(nearBoard.transform, game, 832, 512);

            Canvas help = WorldCanvas("Instruction Board", ui, new Vector2(832, 512), 0.52f, head, new Vector3(-0.46f, 1.245f, 1.09f), Quaternion.Euler(25f, 0f, 0f), false);
            BuildInstructions(help.transform);

            // Big scoreboard on the far wall, readable from the firing line.
            Canvas wall = WorldCanvas("Scoreboard Wall", ui, new Vector2(1600, 640), 4.32f, head, new Vector3(0f, 4.0f, HallFront - 0.03f), Quaternion.identity, false);
            BuildScoreboard(wall.transform, game, 1600, 640);

            BuildResults(ui, game, audio, head, new[] { nearBoard.gameObject, help.gameObject });

            var popupMgrObj = new GameObject("Score Popup Manager");
            popupMgrObj.transform.SetParent(ui, false);
            var popupMgr = popupMgrObj.AddComponent<ScorePopupManager>();
            Wire.Ref(popupMgr, "popupFont", boldFont);

            SetLayerRecursive(ui.gameObject, UiLayer);
        }

        private static Canvas WorldCanvas(string name, Transform parent, Vector2 pixels, float worldWidth, Camera camera, Vector3 position,
            Quaternion rotation, bool xrInteractive)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = pixels;
            rect.localScale = Vector3.one * (worldWidth / pixels.x);
            rect.SetPositionAndRotation(position, rotation);

            go.AddComponent<GraphicRaycaster>();
            if (xrInteractive) go.AddComponent<TrackedDeviceGraphicRaycaster>(); // lets the controller ray press buttons
            return canvas;
        }

        private static void SetLayerRecursive(GameObject root, int layer)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        // ------------------------------------------------------------------ scoreboard
        /// <summary>One layout for both scoreboards: it is computed from the canvas size, so the big one is just a wider copy.</summary>
        private static void BuildScoreboard(Transform canvas, GameManager game, float w, float h)
        {
            float k = h / 512f; // font and spacing scale
            var center = new Vector2(0.5f, 0.5f);

            Image back = UiFactory.Img("Background", canvas, "ui_round.png", UiFactory.Panel);
            UiFactory.Stretch(back.rectTransform);

            Image accent = UiFactory.Img("Accent", canvas, "ui_pill.png", UiFactory.Amber);
            UiFactory.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f * k), new Vector2(w * 0.86f, 8f * k));

            Image divider = UiFactory.Img("Divider", canvas, null, new Color(1f, 1f, 1f, 0.12f));
            UiFactory.Place(divider.rectTransform, center, new Vector2(0f, h * 0.05f), new Vector2(3f * k, h * 0.52f));

            float colX = w * 0.25f;
            float colW = w * 0.46f;

            Label(canvas, "Score Label", "SCORE", bodyFont, 34f * k, UiFactory.Muted, new Vector2(-colX, h * 0.27f), new Vector2(colW, 50f * k), 6f);
            TextMeshProUGUI score = Label(canvas, "Score", "000", titleFont, 215f * k, UiFactory.Amber, new Vector2(-colX, h * 0.04f), new Vector2(colW, 240f * k));

            Label(canvas, "Time Label", "TIME LEFT", bodyFont, 34f * k, UiFactory.Muted, new Vector2(colX, h * 0.27f), new Vector2(colW, 50f * k), 6f);
            TextMeshProUGUI time = Label(canvas, "Time", "01:00", titleFont, 165f * k, UiFactory.Text, new Vector2(colX, h * 0.04f), new Vector2(colW, 200f * k));

            TextMeshProUGUI stats = Label(canvas, "Stats", "SHOTS 0   HITS 0", bodyFont, 32f * k, UiFactory.Muted, new Vector2(0f, -h * 0.27f), new Vector2(w * 0.9f, 48f * k), 3f);
            TextMeshProUGUI status = Label(canvas, "Status", "PICK UP THE PISTOL TO START", boldFont, 40f * k, UiFactory.Text, new Vector2(0f, -h * 0.385f), new Vector2(w * 0.9f, 56f * k), 4f);

            var view = canvas.gameObject.AddComponent<ScoreboardView>();
            Wire.Ref(view, "game", game);
            Wire.Ref(view, "scoreText", score);
            Wire.Ref(view, "timeText", time);
            Wire.Ref(view, "statsText", stats);
            Wire.Ref(view, "statusText", status);
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color,
            Vector2 position, Vector2 box, float spacing = 0f, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI tmp = UiFactory.Label(name, parent, text, font, size, color, align, spacing);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = size * 0.5f;
            tmp.fontSizeMax = size;
            UiFactory.Place(tmp.rectTransform, new Vector2(0.5f, 0.5f), position, box);
            return tmp;
        }

        // ------------------------------------------------------------------ instruction board
        private static void BuildInstructions(Transform canvas)
        {
            var center = new Vector2(0.5f, 0.5f);
            Image back = UiFactory.Img("Background", canvas, "ui_round.png", UiFactory.Panel);
            UiFactory.Stretch(back.rectTransform);

            Image accent = UiFactory.Img("Accent", canvas, "ui_pill.png", UiFactory.Amber);
            UiFactory.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(715f, 8f));

            Label(canvas, "Title", "HOW TO PLAY", titleFont, 66f, UiFactory.Amber, new Vector2(0f, 200f), new Vector2(700f, 80f), 6f);

            var rows = new (string key, string action)[]
            {
                ("F2", "switch desktop / XR simulator"),
                ("SPACE (HOLD)", "control right XR controller"),
                ("G (HOLD)", "grip the pistol with controller"),
                ("LEFT CLICK", "controller trigger / fire"),
                ("MOUSE", "rotate selected XR device"),
                ("F1", "show / hide simulator help"),
            };

            for (int i = 0; i < rows.Length; i++)
            {
                float y = 112f - i * 54f;
                Image chip = UiFactory.Img($"Key {i + 1}", canvas, "ui_pill.png", UiFactory.Tile);
                UiFactory.Place(chip.rectTransform, center, new Vector2(-240f, y), new Vector2(270f, 46f));
                TextMeshProUGUI key = UiFactory.Label("Label", chip.transform, rows[i].key, titleFont, 31f, UiFactory.Amber, TextAlignmentOptions.Center, 2f);
                key.textWrappingMode = TextWrappingModes.NoWrap;
                UiFactory.Stretch(key.rectTransform);

                Label(canvas, $"Action {i + 1}", rows[i].action, bodyFont, 29f, UiFactory.Text, new Vector2(150f, y), new Vector2(430f, 46f),
                    0f, TextAlignmentOptions.Left);
            }

            Label(canvas, "Scoring", "BULLSEYE +30    NEAR 10    MOVING 20    FAR 30", boldFont, 30f, UiFactory.Amber, new Vector2(0f, -200f), new Vector2(760f, 44f), 3f);
            Label(canvas, "Headset", "Consecutive hits grant streak multipliers! (VR: Grip grabs, Trigger fires)", bodyFont, 24f, UiFactory.Muted, new Vector2(0f, -238f), new Vector2(760f, 38f));
        }

        // ------------------------------------------------------------------ results panel
        private static void BuildResults(Transform parent, GameManager game, AudioService audio, Camera head, GameObject[] boardsToHide)
        {
            // This canvas is moved in front of the player when the round ends, so its start position does not matter.
            Canvas canvas = WorldCanvas("Results Canvas", parent, new Vector2(1100, 800), 1.15f, head, new Vector3(0f, 1.5f, 1.8f), Quaternion.identity, true);

            RectTransform panel = UiFactory.Rect("Panel", canvas.transform);
            UiFactory.Stretch(panel);
            UiFactory.Group(panel.gameObject);
            var fader = panel.gameObject.AddComponent<PanelFader>();
            var center = new Vector2(0.5f, 0.5f);

            Image back = UiFactory.Img("Background", panel, "ui_round.png", new Color(0.06f, 0.065f, 0.08f, 0.96f));
            UiFactory.Stretch(back.rectTransform);

            Image accent = UiFactory.Img("Accent", panel, "ui_pill.png", UiFactory.Amber);
            UiFactory.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(940f, 10f));

            Label(panel, "Title", "TIME UP", titleFont, 120f, UiFactory.Amber, new Vector2(0f, 290f), new Vector2(900f, 130f), 10f);
            Label(panel, "Score Label", "FINAL SCORE", bodyFont, 40f, UiFactory.Muted, new Vector2(0f, 185f), new Vector2(700f, 54f), 8f);
            TextMeshProUGUI score = Label(panel, "Score", "0", titleFont, 270f, UiFactory.Text, new Vector2(0f, 40f), new Vector2(700f, 290f));
            TextMeshProUGUI rank = Label(panel, "Rank", "ROOKIE", boldFont, 62f, UiFactory.Amber, new Vector2(0f, -125f), new Vector2(900f, 80f), 8f);
            TextMeshProUGUI stats = Label(panel, "Stats", "0 targets down   |   0 shots   |   0% accuracy", bodyFont, 34f, UiFactory.Muted,
                new Vector2(0f, -200f), new Vector2(1000f, 50f));

            Button restart = UiFactory.MakeButton("Restart Button", panel, "RESTART", boldFont, UiFactory.Amber, new Color(0.1f, 0.09f, 0.07f),
                center, new Vector2(-235f, -310f), new Vector2(400f, 112f), 50f, audio);
            Button exit = UiFactory.MakeButton("Exit Button", panel, "EXIT", boldFont, UiFactory.Tile, UiFactory.Text,
                center, new Vector2(235f, -310f), new Vector2(400f, 112f), 50f, audio);

            var results = canvas.gameObject.AddComponent<ResultsView>();
            Wire.Ref(results, "game", game);
            Wire.Ref(results, "head", head.transform);
            Wire.Ref(results, "panelRoot", panel.gameObject);
            Wire.Ref(results, "fader", fader);
            Wire.Ref(results, "scoreText", score);
            Wire.Ref(results, "rankText", rank);
            Wire.Ref(results, "statsText", stats);
            Wire.Ref(results, "restartButton", restart);
            Wire.Ref(results, "exitButton", exit);
            Wire.Refs(results, "hideWhileShown", System.Array.ConvertAll(boardsToHide, g => (UnityEngine.Object)g));
        }
    }
}
