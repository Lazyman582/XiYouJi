using UnityEngine;

namespace DiscoDialoguePrototype
{
    public class PrototypeInteractable : MonoBehaviour
    {
        public static PrototypeInteractable Focused { get; private set; }
        public string displayName = "Stranger";
        public string interactionHint = "Talk";
        public DialogueGraph dialogue;
        public float interactDistance = 2.4f;

        public bool CanInteract(GameObject player)
        {
            if (player == null || dialogue == null || !isActiveAndEnabled) return false;
            Vector3 playerPosition = player.transform.position;
            Vector3 targetPosition = transform.position;
            playerPosition.y = 0f;
            targetPosition.y = 0f;
            return Vector3.Distance(playerPosition, targetPosition) <= interactDistance;
        }

        public void Interact(GameObject player)
        {
            if (CanInteract(player) && DialogueRunner.Instance != null)
                DialogueRunner.Instance.StartDialogue(dialogue, this);
        }

        public void SetFocused(bool isFocused)
        {
            if (isFocused) Focused = this;
            else if (Focused == this) Focused = null;
        }

        private void OnDisable() { SetFocused(false); }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, interactDistance);
        }
    }
}
