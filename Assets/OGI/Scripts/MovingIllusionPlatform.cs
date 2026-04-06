using UnityEngine;
using System.Collections.Generic;

public class MovingIllusionPlatform : MonoBehaviour
{
    public enum PlatformType { Continuous, PressureSensitive, ExternalTrigger }
    public enum VisibilityMode { Both, DonOnly, SanchoOnly }

    [Header("--- Platform Temel Ayarlarý ---")]
    public PlatformType platformType = PlatformType.Continuous;
    public float speed = 3f;
    [Tooltip("Waypoint noktalarýna varýnca ne kadar beklesin?")]
    public float waitTime = 1f;

    [Header("--- Yeni: Kalkýþ Gecikmesi (Delay) ---")]
    [Tooltip("Pressure Sensitive modundayken, üzerine binildiðinde hareket etmeden önce kaç saniye beklesin?")]
    public float detectionDelay = 0.5f;
    private float currentDetectionTimer = 0f; // Arka planda sayan kronometre

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

    // --- PLATFORMA BÝNENLERÝN LÝSTESÝ ---
    private List<Collider> activeObjectsOnPlatform = new List<Collider>();

    void Start()
    {
        if (movingBody == null) movingBody = transform;

        // Waypoint pozisyonlarýný dünya koordinatýna çevir
        if (waypoints != null && waypoints.Length > 0)
        {
            globalWaypoints = new Vector3[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                globalWaypoints[i] = waypoints[i].position;
            }

            // YENÝ: Iþýnlanmayý sildik! 
            // Platform Editörde nerede býrakýldýysa orada baþlar.
            // Ýlk hedefi 0. nokta (Point A) olarak ayarlýyoruz.
            currentTargetIndex = 0;
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
        // --- LÝSTE TEMÝZLÝÐÝ VE KONTROLÜ ---
        activeObjectsOnPlatform.RemoveAll(col => col == null || !col.gameObject.activeInHierarchy || !col.enabled);

        bool hasPassengers = activeObjectsOnPlatform.Count > 0;

        // --- YENÝ: DELAY (GECÝKME) SÝSTEMÝ ---
        if (hasPassengers)
        {
            if (currentDetectionTimer < detectionDelay)
            {
                currentDetectionTimer += Time.fixedDeltaTime;
                isPlayerOnPlatform = false; // Süre dolana kadar "üstünde kimse yok" gibi davran
            }
            else
            {
                isPlayerOnPlatform = true; // Süre doldu, hareket izni verildi
            }
        }
        else
        {
            currentDetectionTimer = 0f; // Biri inerse sayacý sýfýrla
            isPlayerOnPlatform = false;
        }

        if (globalWaypoints == null || globalWaypoints.Length == 0) return;

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

        // Karakteri/Kutuyu taþýma
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

    // --- LÝSTE MANTIÐINA GEÇÝÞ YAPAN TRIGGER'LAR ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Pushable"))
        {
            if (!activeObjectsOnPlatform.Contains(other))
            {
                activeObjectsOnPlatform.Add(other);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Pushable"))
        {
            if (activeObjectsOnPlatform.Contains(other))
            {
                activeObjectsOnPlatform.Remove(other);
            }
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