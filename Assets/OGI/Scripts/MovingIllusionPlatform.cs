using UnityEngine;

public class MovingIllusionPlatform : MonoBehaviour
{
    public enum PlatformType { Continuous, PressureSensitive, ExternalTrigger }
    public enum VisibilityMode { Both, DonOnly, SanchoOnly }

    [Header("--- Platform Temel Ayarlarý ---")]
    public PlatformType platformType = PlatformType.Continuous;
    public float speed = 3f;
    public float waitTime = 1f;

    [Header("--- Dual Reality (Ýllüzyon) Ayarlarý ---")]
    public VisibilityMode visibilityMode = VisibilityMode.Both;

    [Header("--- Modeller ve Hareketli Gövde ---")]
    public Transform movingBody;
    public GameObject solidGroup;
    public GameObject illusionGroup;

    [Header("--- Rota (Waypoints) ---")]
    public Transform[] waypoints;

    private Vector3[] globalWaypoints;
    private int currentTargetIndex = 0;
    private bool movingForward = true;
    private Vector3 lastPosition;

    [Header("--- Debug Durumu ---")]
    [SerializeField] private bool isPlayerOnPlatform = false;
    private bool isWaiting = false;
    private float currentWaitTimer = 0f;
    private bool isSolidForCurrentChar = true;
    private bool isExternallyActivated = false;

    void Start()
    {
        if (movingBody == null) movingBody = transform;

        // Waypoint pozisyonlarýný dünya koordinatýna çevir ve platformu ilkine koy
        if (waypoints != null && waypoints.Length > 1)
        {
            globalWaypoints = new Vector3[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                globalWaypoints[i] = waypoints[i].position;
            }
            movingBody.position = globalWaypoints[0];
            currentTargetIndex = 1;
        }
        else
        {
            Debug.LogError(gameObject.name + " platformunun waypointleri eksik!");
        }

        lastPosition = movingBody.position;
        if (DualRealityManager.Instance != null) UpdatePerception(DualRealityManager.Instance.isDonActive);
    }

    void FixedUpdate()
    {
        if (globalWaypoints == null || globalWaypoints.Length < 2) return;

        bool shouldMove = false;
        switch (platformType)
        {
            case PlatformType.Continuous: shouldMove = true; break;
            case PlatformType.PressureSensitive: shouldMove = isPlayerOnPlatform && isSolidForCurrentChar; break;
            case PlatformType.ExternalTrigger: shouldMove = isExternallyActivated; break;
        }

        if (shouldMove)
        {
            HandleMovement();
        }

        // Karakteri taþýma
        Vector3 deltaMovement = movingBody.position - lastPosition;
        if (isPlayerOnPlatform && isSolidForCurrentChar && deltaMovement.magnitude > 0.0001f)
        {
            MoveActiveCharacter(deltaMovement);
        }

        lastPosition = movingBody.position;
    }

    private void HandleMovement()
    {
        if (isWaiting)
        {
            currentWaitTimer += Time.fixedDeltaTime;
            if (currentWaitTimer >= waitTime)
            {
                isWaiting = false;
                currentWaitTimer = 0f;
                UpdateTargetIndex();
            }
        }
        else
        {
            Vector3 targetPosition = globalWaypoints[currentTargetIndex];
            movingBody.position = Vector3.MoveTowards(movingBody.position, targetPosition, speed * Time.fixedDeltaTime);

            if (Vector3.Distance(movingBody.position, targetPosition) < 0.05f)
            {
                movingBody.position = targetPosition;
                isWaiting = true;
                Debug.Log($"<color=yellow>[Vardý]</color> {gameObject.name} Waypoint: {currentTargetIndex}");
            }
        }
    }

    private void UpdateTargetIndex()
    {
        if (movingForward)
        {
            if (currentTargetIndex < globalWaypoints.Length - 1) currentTargetIndex++;
            else { movingForward = false; currentTargetIndex--; }
        }
        else
        {
            if (currentTargetIndex > 0) currentTargetIndex--;
            else { movingForward = true; currentTargetIndex++; }
        }
    }

    // --- EN GARANTÝ ALGILAMA SÝSTEMÝ: TRIGGER ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Pushable"))
        {
            isPlayerOnPlatform = true;
            Debug.Log("<color=green>[Giriþ]</color> Platforma biri bindi: " + other.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Pushable"))
        {
            isPlayerOnPlatform = false;
            Debug.Log("<color=red>[Çýkýþ]</color> Platformdan biri indi.");
        }
    }

    public void UpdatePerception(bool isDonActive)
    {
        switch (visibilityMode)
        {
            case VisibilityMode.Both: isSolidForCurrentChar = true; break;
            case VisibilityMode.DonOnly: isSolidForCurrentChar = isDonActive; break;
            case VisibilityMode.SanchoOnly: isSolidForCurrentChar = !isDonActive; break;
        }

        int targetLayer = isDonActive ? LayerMask.NameToLayer("World_Don") : LayerMask.NameToLayer("World_Sancho");
        if (targetLayer == -1) targetLayer = 0;
        gameObject.layer = targetLayer;

        if (isSolidForCurrentChar)
        {
            if (solidGroup != null) { solidGroup.SetActive(true); SetLayerRecursively(solidGroup, targetLayer); }
            if (illusionGroup != null) illusionGroup.SetActive(false);
        }
        else
        {
            if (solidGroup != null) solidGroup.SetActive(false);
            if (illusionGroup != null) illusionGroup.SetActive(true);
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }

    private void MoveActiveCharacter(Vector3 movement)
    {
        if (DualRealityManager.Instance == null) return;
        GameObject activeChar = DualRealityManager.Instance.isDonActive ? DualRealityManager.Instance.donQuixote : DualRealityManager.Instance.sancho;
        if (activeChar != null)
        {
            CharacterController cc = activeChar.GetComponent<CharacterController>();
            if (cc != null && cc.enabled) cc.Move(movement);
        }
    }

    public void ActivatePlatform() { isExternallyActivated = true; }
    public void DeactivatePlatform() { isExternallyActivated = false; }
}