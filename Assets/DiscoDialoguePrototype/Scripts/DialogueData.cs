using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiscoDialoguePrototype
{
    [CreateAssetMenu(fileName = "DialogueGraph", menuName = "Disco Prototype/Dialogue Graph")]
    public class DialogueGraph : ScriptableObject
    {
        public string graphId = "dialogue.graph";
        public string startNodeId = "start";
        public List<DialogueNode> nodes = new List<DialogueNode>();

        public DialogueNode FindNode(string id)
        {
            if (nodes == null) return null;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].id == id) return nodes[i];
            }
            return null;
        }
    }

    [Serializable]
    public class DialogueNode
    {
        public string id;
        public string speaker = "Unknown";
        public string portraitGlyph = "?";
        [TextArea(3, 8)] public string text;
        public string nextNodeId;
        public string mood = "NEUTRAL";
        public List<DialogueChoice> choices = new List<DialogueChoice>();

        public DialogueNode() { }

        public DialogueNode(string nodeId, string nodeSpeaker, string line, string glyph, string nodeMood = "NEUTRAL")
        {
            id = nodeId;
            speaker = nodeSpeaker;
            text = line;
            portraitGlyph = glyph;
            mood = nodeMood;
        }
    }

    [Serializable]
    public class DialogueChoice
    {
        public string label;
        public string responseNodeId;
        public string requiredFlag;
        public string requiredFact;
        public string setFlag;
        public string skillTag;
        public string unavailableHint;

        public DialogueChoice() { }

        public DialogueChoice(string choiceLabel, string responseId, string tag = "")
        {
            label = choiceLabel;
            responseNodeId = responseId;
            skillTag = tag;
        }
    }

    [Serializable]
    public class DialogueHistoryEntry
    {
        public string speaker;
        public string portraitGlyph;
        public string text;
        public string mood;
        public bool isPlayer;

        public DialogueHistoryEntry(string entrySpeaker, string entryText, string glyph, string entryMood, bool player = false)
        {
            speaker = entrySpeaker;
            text = entryText;
            portraitGlyph = glyph;
            mood = entryMood;
            isPlayer = player;
        }
    }
}
