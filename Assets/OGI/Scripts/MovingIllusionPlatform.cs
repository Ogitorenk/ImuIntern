using UnityEngine;

public class MovingIllusionPlatform : MonoBehaviour
{
    public enum PlatformType { Continuous, PressureSensitive }

    [Header("--- Platform Temel Ayarlarý ---")]
    public PlatformType platformType = PlatformType.Continuous;
    public float speed = 3f;
    public float waitTime = 1f;

    [Header("--- Dual Reality (Ýllüzyon) Ayarlarý ---")]
    public bool invertedPerception = false;

    [Header("--- Modeller ve Hareketli Gövde ---")]
    public Transform movingBody;
    public GameObject solidGroup;
    public GameObject illusionGroup;

    [Header("--- Rota (Waypoints) ---")]
    public Transform[] waypoints;

    // --- YENÝ EKLENDÝ: Noktalarýn dünyadaki sabit yerlerini tutacak liste ---
    private Vector3[] globalWaypoints;

    private int currentTargetIndex = 0;
    private bool movingForward = true;
    private Vector3 lastPosition;

    private bool isPlayerOnPlatform = false;
    private bool isWaiting = false;
    private float currentWaitTimer = 0f;
    private bool isSolidForCurrentChar = true;

    void Start()
    {
        if (movingBody == null) movingBody = transform;

        // --- KRÝTÝK DÜZELTME: Noktalarý hafýzaya alýyoruz ---
        if (waypoints.Length > 0)
        {
            globalWaypoints = new Vector3[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                // Noktalarýn dünyadaki (World Space) pozisyonunu kaydediyoruz.
                // Artýk noktalar platformla birlikte hareket etse bile bu deðerler deðiþmez.
                globalWaypoints[i] = waypoints[i].position;
            }

            movingBody.position = globalWaypoints[0];
        }

        lastPosition = movingBody.position;

        if (DualRealityManager.Instance != null)
        {
            UpdatePerception(DualRealityManager.Instance.isDonActive);
        }
    }

    void FixedUpdate()
    {
        // globalWaypoints boþsa veya nokta sayýsý azsa çalýþma
        if (globalWaypoints == null || globalWaypoints.Length < 2) return;

        bool shouldMove = (platformType == PlatformType.Continuous) ||
                          (platformType == PlatformType.PressureSensitive && isPlayerOnPlatform && isSolidForCurrentChar);

        if (shouldMove)
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
                // Artik transform.position veya waypoints[i].position deðil, 
                // hafýzaya aldýðýmýz globalWaypoints[currentTargetIndex] kullanýlýyor.
                Vector3 targetPosition = globalWaypoints[currentTargetIndex];
                movingBody.position = Vector3.MoveTowards(movingBody.position, targetPosition, speed * Time.fixedDeltaTime);

                // Hassas mesafe kontrolü (0.01f Unity için en saðlýklýsýdýr)
                if (Vector3.Distance(movingBody.position, targetPosition) < 0.01f)
                {
                    movingBody.position = targetPosition; // Tam üstüne oturt
                    isWaiting = true;
                }
            }
        }

        Vector3 deltaMovement = movingBody.position - lastPosition;
        if (isPlayerOnPlatform && isSolidForCurrentChar && deltaMovement.magnitude > 0.00001f)
        {
            MoveActiveCharacter(deltaMovement);
        }

        lastPosition = movingBody.position;
    }

    // Ping-pong rota mantýðý (Burasý ayný kaldý)
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

    // Perception, Trigger ve MoveActiveCharacter kýsýmlarý ayný kalacak...
    // (Kodun devamýný kýsalýk adýna yazmýyorum, sendeki mevcut Perception ve Trigger fonksiyonlarýný aynen kullanmaya devam et kanka)

    public void UpdatePerception(bool isDonActive)
    {
        if (!invertedPerception)
            isSolidForCurrentChar = isDonActive;
        else
            isSolidForCurrentChar = !isDonActive;

        int targetLayer = isDonActive ? LayerMask.NameToLayer("World_Don") : LayerMask.NameToLayer("World_Sancho");
        gameObject.layer = targetLayer;

        if (isSolidForCurrentChar)
        {
            if (solidGroup != null)
            {
                solidGroup.SetActive(true);
                SetLayerRecursively(solidGroup, targetLayer);
            }
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
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerOnPlatform = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerOnPlatform = false;
    }

    private void MoveActiveCharacter(Vector3 movement)
    {
        if (DualRealityManager.Instance == null) return;
        GameObject activeChar = DualRealityManager.Instance.isDonActive ?
                                DualRealityManager.Instance.donQuixote :
                                DualRealityManager.Instance.sancho;

        if (activeChar != null)
        {
            CharacterController cc = activeChar.GetComponent<CharacterController>();
            if (cc != null && cc.enabled) cc.Move(movement);
        }
    }
}