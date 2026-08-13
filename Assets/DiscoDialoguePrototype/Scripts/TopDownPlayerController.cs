using UnityEngine;

namespace DiscoDialoguePrototype
{
    [RequireComponent(typeof(CharacterController))]
    public class TopDownPlayerController : MonoBehaviour
    {
        public float moveSpeed = 4.2f;
        public float rotationSpeed = 12f;
        public float focusRange = 3.1f;
        public Camera gameplayCamera;
        private CharacterController characterController;
        private Vector3? clickTarget;
        private PrototypeInteractable focusedInteractable;
        private PrototypeInteractable pendingInteractable;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (gameplayCamera == null) gameplayCamera = Camera.main;
        }

        private void Update()
        {
            if (DialogueRunner.IsActive) { ClearFocus(); return; }
            HandleMouseDestination();
            HandleMovement();
            UpdateFocus();
            if (Input.GetKeyDown(KeyCode.E) && focusedInteractable != null) focusedInteractable.Interact(gameObject);
            TryPendingInteraction();
        }

        private void HandleMouseDestination()
        {
            if (!Input.GetMouseButtonDown(0) || gameplayCamera == null) return;
            Ray ray = gameplayCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 100f)) return;
            PrototypeInteractable interactable = hit.collider.GetComponentInParent<PrototypeInteractable>();
            if (interactable != null)
            {
                if (interactable.CanInteract(gameObject))
                {
                    interactable.Interact(gameObject);
                }
                else
                {
                    pendingInteractable = interactable;
                    Vector3 toNpc = transform.position - interactable.transform.position;
                    toNpc.y = 0f;
                    Vector3 destination = interactable.transform.position + (toNpc.sqrMagnitude > 0.01f ? toNpc.normalized : Vector3.back) * (interactable.interactDistance * 0.72f);
                    destination.y = transform.position.y;
                    clickTarget = destination;
                }
                return;
            }

            pendingInteractable = null;
            Vector3 groundDestination = hit.point;
            groundDestination.y = transform.position.y;
            clickTarget = groundDestination;
        }

        private void HandleMovement()
        {
            float horizontal = 0f;
            float vertical = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
            Vector3 keyboard = new Vector3(horizontal, 0f, vertical);
            Vector3 movement = Vector3.zero;
            if (keyboard.sqrMagnitude > 0.01f)
            {
                clickTarget = null;
                movement = keyboard.normalized;
            }
            else if (clickTarget.HasValue)
            {
                Vector3 toTarget = clickTarget.Value - transform.position;
                toTarget.y = 0f;
                if (toTarget.magnitude < 0.12f) clickTarget = null;
                else movement = toTarget.normalized;
            }

            if (movement.sqrMagnitude > 0.01f)
            {
                characterController.SimpleMove(movement * moveSpeed);
                Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            else characterController.SimpleMove(Vector3.zero);
        }

        private void TryPendingInteraction()
        {
            if (pendingInteractable == null) return;
            if (pendingInteractable.CanInteract(gameObject))
            {
                PrototypeInteractable target = pendingInteractable;
                pendingInteractable = null;
                clickTarget = null;
                target.Interact(gameObject);
            }
        }

        private void UpdateFocus()
        {
            PrototypeInteractable closest = null;
            float closestDistance = focusRange;
            PrototypeInteractable[] interactables = FindObjectsOfType<PrototypeInteractable>();
            for (int i = 0; i < interactables.Length; i++)
            {
                PrototypeInteractable candidate = interactables[i];
                if (!candidate.CanInteract(gameObject)) continue;
                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance < closestDistance) { closest = candidate; closestDistance = distance; }
            }

            if (focusedInteractable == closest) return;
            if (focusedInteractable != null) focusedInteractable.SetFocused(false);
            focusedInteractable = closest;
            if (focusedInteractable != null) focusedInteractable.SetFocused(true);
        }

        private void ClearFocus()
        {
            if (focusedInteractable != null) focusedInteractable.SetFocused(false);
            focusedInteractable = null;
            pendingInteractable = null;
        }
    }
}
