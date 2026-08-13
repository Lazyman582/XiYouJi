using System.Collections.Generic;
using UnityEngine;

namespace DiscoDialoguePrototype
{
    public class DialogueUI : MonoBehaviour
    {
        public float charactersPerSecond = 48f;
        public float panelWidth = 410f;
        public float panelMargin = 24f;

        private DialogueRunner runner;
        private DialogueNode renderedNode;
        private int visibleCharacters;
        private float typeTimer;
        private Vector2 historyScroll;
        private bool followLatest = true;

        private GUIStyle panelStyle;
        private GUIStyle headerStyle;
        private GUIStyle speakerStyle;
        private GUIStyle playerSpeakerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle playerBodyStyle;
        private GUIStyle choiceStyle;
        private GUIStyle smallStyle;
        private GUIStyle hintStyle;
        private GUIStyle dividerStyle;
        private Texture2D panelTexture;
        private Texture2D choiceTexture;
        private Texture2D dividerTexture;

        private void Awake() { runner = DialogueRunner.Instance; }

        private void Update()
        {
            if (runner == null) runner = DialogueRunner.Instance;
            if (runner == null || !runner.IsRunning)
            {
                renderedNode = null;
                historyScroll = Vector2.zero;
                followLatest = true;
                return;
            }

            if (renderedNode != runner.CurrentNode)
            {
                renderedNode = runner.CurrentNode;
                visibleCharacters = 0;
                typeTimer = 0f;
                followLatest = true;
            }

            if (renderedNode != null && visibleCharacters < renderedNode.text.Length)
            {
                typeTimer += Time.unscaledDeltaTime * charactersPerSecond;
                visibleCharacters = Mathf.Min(renderedNode.text.Length, Mathf.FloorToInt(typeTimer));
            }

            if (Input.GetKeyDown(KeyCode.Escape)) runner.EndDialogue();
            else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (renderedNode != null && visibleCharacters < renderedNode.text.Length) visibleCharacters = renderedNode.text.Length;
                else if (runner.GetAvailableChoices().Count == 0) runner.Advance();
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.PageUp))
            {
                historyScroll.y = Mathf.Max(0f, historyScroll.y - 160f);
                followLatest = false;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.PageDown))
            {
                historyScroll.y += 160f;
                followLatest = false;
            }

            List<DialogueChoice> choices = runner.GetAvailableChoices();
            if (renderedNode != null && visibleCharacters >= renderedNode.text.Length && choices.Count > 0)
            {
                for (int i = 0; i < choices.Count && i < 9; i++)
                    if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) runner.Choose(i);
            }
        }

        private void OnGUI()
        {
            if (panelStyle == null) CreateStyles();
            if (runner == null) runner = DialogueRunner.Instance;
            if (runner != null && runner.IsRunning) DrawDialoguePanel();
            else
            {
                DrawControlHint();
                DrawInteractionPrompt();
            }
        }

        private void DrawDialoguePanel()
        {
            if (runner == null || renderedNode == null) return;

            float width = Mathf.Clamp(panelWidth, 330f, 500f);
            width = Mathf.Min(width, Screen.width - panelMargin * 2f);
            float panelHeight = Mathf.Max(300f, Screen.height - panelMargin * 2f);
            Rect panel = new Rect(Screen.width - width - panelMargin, panelMargin, width, panelHeight);
            GUI.Box(panel, GUIContent.none, panelStyle);

            GUI.Label(new Rect(panel.x + 22f, panel.y + 18f, panel.width - 44f, 28f), "CONVERSATION", headerStyle);
            string sourceName = runner.CurrentSource == null ? "THE DISTRICT" : runner.CurrentSource.displayName.ToUpperInvariant();
            GUI.Label(new Rect(panel.x + 22f, panel.y + 48f, panel.width - 44f, 20f), sourceName, smallStyle);
            GUI.DrawTexture(new Rect(panel.x + 22f, panel.y + 76f, panel.width - 44f, 2f), dividerTexture);

            List<DialogueChoice> choices = runner.GetAvailableChoices();
            float choiceHeight = choices.Count > 0 && visibleCharacters >= renderedNode.text.Length
                ? Mathf.Min(235f, 74f + choices.Count * 43f)
                : 52f;
            float historyTop = panel.y + 92f;
            float historyBottom = panel.y + panel.height - choiceHeight - 16f;
            Rect historyRect = new Rect(panel.x + 14f, historyTop, panel.width - 28f, Mathf.Max(100f, historyBottom - historyTop));

            if (Event.current.type == EventType.ScrollWheel && historyRect.Contains(Event.current.mousePosition))
                followLatest = false;

            DrawHistory(historyRect);
            DrawChoices(panel, choiceHeight, choices);
        }

        private void DrawHistory(Rect historyRect)
        {
            IReadOnlyList<DialogueHistoryEntry> entries = runner.History;
            float contentWidth = historyRect.width - 30f;
            float contentHeight = 12f;
            for (int i = 0; i < entries.Count; i++)
            {
                DialogueHistoryEntry entry = entries[i];
                string text = GetVisibleText(entry, i == entries.Count - 1);
                GUIStyle textStyle = entry.isPlayer ? playerBodyStyle : bodyStyle;
                contentHeight += 28f + textStyle.CalcHeight(new GUIContent(text), contentWidth) + 24f;
            }

            Rect contentRect = new Rect(0f, 0f, contentWidth, Mathf.Max(contentHeight, historyRect.height));
            if (followLatest) historyScroll.y = contentRect.height;
            historyScroll = GUI.BeginScrollView(historyRect, historyScroll, contentRect, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar);

            float y = 10f;
            for (int i = 0; i < entries.Count; i++)
            {
                DialogueHistoryEntry entry = entries[i];
                bool isCurrent = i == entries.Count - 1;
                GUIStyle currentSpeakerStyle = entry.isPlayer ? playerSpeakerStyle : speakerStyle;
                GUIStyle currentBodyStyle = entry.isPlayer ? playerBodyStyle : bodyStyle;
                string text = GetVisibleText(entry, isCurrent);
                float bodyHeight = currentBodyStyle.CalcHeight(new GUIContent(text), contentWidth);

                GUI.Label(new Rect(2f, y, contentWidth - 4f, 24f), entry.speaker + "  //  " + entry.mood, currentSpeakerStyle);
                GUI.Label(new Rect(2f, y + 27f, contentWidth - 4f, bodyHeight), text, currentBodyStyle);
                y += 27f + bodyHeight + 24f;
            }

            GUI.EndScrollView();
        }

        private string GetVisibleText(DialogueHistoryEntry entry, bool isCurrent)
        {
            if (!isCurrent || renderedNode == null || visibleCharacters >= renderedNode.text.Length) return entry.text;
            return renderedNode.text.Substring(0, Mathf.Clamp(visibleCharacters, 0, renderedNode.text.Length));
        }

        private void DrawChoices(Rect panel, float choiceHeight, List<DialogueChoice> choices)
        {
            float top = panel.y + panel.height - choiceHeight - 4f;
            GUI.DrawTexture(new Rect(panel.x + 22f, top, panel.width - 44f, 2f), dividerTexture);
            bool lineComplete = renderedNode != null && visibleCharacters >= renderedNode.text.Length;
            if (!lineComplete)
            {
                GUI.Label(new Rect(panel.x + 22f, top + 14f, panel.width - 44f, 24f), "SPACE / ENTER  REVEAL LINE", smallStyle);
                return;
            }

            if (choices.Count == 0)
            {
                GUI.Label(new Rect(panel.x + 22f, top + 14f, panel.width - 44f, 24f), "SPACE / ENTER  CONTINUE     ESC  CLOSE", smallStyle);
                return;
            }

            GUI.Label(new Rect(panel.x + 22f, top + 12f, panel.width - 44f, 22f), "WHAT DO YOU SAY?", smallStyle);
            for (int i = 0; i < choices.Count; i++)
            {
                Rect buttonRect = new Rect(panel.x + 22f, top + 39f + i * 42f, panel.width - 44f, 35f);
                if (GUI.Button(buttonRect, (i + 1) + ".  " + choices[i].label, choiceStyle)) runner.Choose(i);
            }
        }

        private void DrawControlHint()
        {
            Rect hint = new Rect(18f, 18f, 300f, 92f);
            GUI.Box(hint, "WASD  MOVE\nLEFT CLICK  MOVE / WALK TO NPC\nE  TALK    SPACE / ENTER  DIALOGUE\nMOUSE WHEEL / UP-DOWN  REVIEW", hintStyle);
        }

        private void DrawInteractionPrompt()
        {
            PrototypeInteractable focused = PrototypeInteractable.Focused;
            if (focused == null) return;
            float width = 340f;
            Rect prompt = new Rect((Screen.width - width) * 0.5f, Screen.height - 88f, width, 44f);
            GUI.Box(prompt, "E  " + focused.interactionHint + "  -  " + focused.displayName, choiceStyle);
        }

        private void CreateStyles()
        {
            panelTexture = MakeTexture(new Color(0.025f, 0.038f, 0.052f, 0.97f));
            choiceTexture = MakeTexture(new Color(0.11f, 0.17f, 0.18f, 0.98f));
            dividerTexture = MakeTexture(new Color(0.90f, 0.58f, 0.18f, 0.75f));

            panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = panelTexture } };
            headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            headerStyle.normal.textColor = new Color(0.98f, 0.72f, 0.27f);
            speakerStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            speakerStyle.normal.textColor = new Color(0.98f, 0.72f, 0.27f);
            playerSpeakerStyle = new GUIStyle(speakerStyle);
            playerSpeakerStyle.normal.textColor = new Color(0.35f, 0.83f, 0.76f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            bodyStyle.normal.textColor = new Color(0.91f, 0.94f, 0.90f);
            playerBodyStyle = new GUIStyle(bodyStyle);
            playerBodyStyle.normal.textColor = new Color(0.70f, 0.86f, 0.82f);
            choiceStyle = new GUIStyle(GUI.skin.button) { normal = { background = choiceTexture }, fontSize = 14, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            choiceStyle.normal.textColor = new Color(0.90f, 0.94f, 0.90f);
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft };
            smallStyle.normal.textColor = new Color(0.55f, 0.68f, 0.64f);
            hintStyle = new GUIStyle(GUI.skin.box) { fontSize = 12, alignment = TextAnchor.UpperLeft, wordWrap = true };
            hintStyle.padding = new RectOffset(12, 10, 10, 8);
            hintStyle.normal.textColor = new Color(0.78f, 0.86f, 0.83f);
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }
    }
}
