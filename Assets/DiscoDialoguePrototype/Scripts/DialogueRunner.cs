using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiscoDialoguePrototype
{
    public class DialogueRunner : MonoBehaviour
    {
        public static DialogueRunner Instance { get; private set; }
        public event Action<DialogueNode> NodeChanged;
        public event Action<PrototypeInteractable> DialogueStarted;
        public event Action DialogueEnded;
        public DialogueGraph CurrentGraph { get; private set; }
        public DialogueNode CurrentNode { get; private set; }
        public PrototypeInteractable CurrentSource { get; private set; }
        public bool IsRunning { get; private set; }
        public static bool IsActive => Instance != null && Instance.IsRunning;

        private readonly HashSet<string> flags = new HashSet<string>();
        private readonly HashSet<string> facts = new HashSet<string>();
        private readonly List<DialogueChoice> availableChoices = new List<DialogueChoice>();
        private readonly List<DialogueHistoryEntry> history = new List<DialogueHistoryEntry>();

        public IReadOnlyList<DialogueHistoryEntry> History => history;

        private void Awake() { Instance = this; }

        public void StartDialogue(DialogueGraph graph, PrototypeInteractable source)
        {
            if (graph == null) { Debug.LogWarning("Dialogue start requested without a DialogueGraph."); return; }
            CurrentGraph = graph;
            CurrentSource = source;
            history.Clear();
            IsRunning = true;
            DialogueStarted?.Invoke(source);
            GoTo(graph.startNodeId);
        }

        public void Advance()
        {
            if (!IsRunning || CurrentNode == null || GetAvailableChoices().Count > 0) return;
            if (!string.IsNullOrEmpty(CurrentNode.nextNodeId)) GoTo(CurrentNode.nextNodeId);
            else EndDialogue();
        }

        public void Choose(int visibleIndex)
        {
            if (!IsRunning || CurrentNode == null) return;
            List<DialogueChoice> choices = GetAvailableChoices();
            if (visibleIndex < 0 || visibleIndex >= choices.Count) return;
            DialogueChoice choice = choices[visibleIndex];
            if (!string.IsNullOrEmpty(choice.setFlag)) flags.Add(choice.setFlag);
            if (!string.IsNullOrEmpty(choice.label))
                history.Add(new DialogueHistoryEntry("YOU", choice.label, ">", "RESPONSE", true));
            if (!string.IsNullOrEmpty(choice.responseNodeId)) GoTo(choice.responseNodeId);
            else EndDialogue();
        }

        public List<DialogueChoice> GetAvailableChoices()
        {
            availableChoices.Clear();
            if (!IsRunning || CurrentNode == null || CurrentNode.choices == null) return availableChoices;
            for (int i = 0; i < CurrentNode.choices.Count; i++)
            {
                DialogueChoice choice = CurrentNode.choices[i];
                if (choice == null) continue;
                bool flagAllowed = string.IsNullOrEmpty(choice.requiredFlag) || flags.Contains(choice.requiredFlag);
                bool factAllowed = string.IsNullOrEmpty(choice.requiredFact) || facts.Contains(choice.requiredFact);
                if (flagAllowed && factAllowed) availableChoices.Add(choice);
            }
            return availableChoices;
        }

        public bool HasFlag(string flag) { return !string.IsNullOrEmpty(flag) && flags.Contains(flag); }

        public void SetFact(string fact)
        {
            if (!string.IsNullOrEmpty(fact)) facts.Add(fact);
        }

        public void EndDialogue()
        {
            if (!IsRunning) return;
            IsRunning = false;
            CurrentGraph = null;
            CurrentNode = null;
            CurrentSource = null;
            DialogueEnded?.Invoke();
        }

        private void GoTo(string nodeId)
        {
            DialogueNode next = CurrentGraph == null ? null : CurrentGraph.FindNode(nodeId);
            if (next == null)
            {
                Debug.LogError("Dialogue node not found: " + nodeId);
                EndDialogue();
                return;
            }
            CurrentNode = next;
            history.Add(new DialogueHistoryEntry(next.speaker, next.text, next.portraitGlyph, next.mood));
            if (!string.IsNullOrEmpty(next.id)) facts.Add(CurrentGraph.graphId + ".visited." + next.id);
            NodeChanged?.Invoke(CurrentNode);
        }
    }
}
