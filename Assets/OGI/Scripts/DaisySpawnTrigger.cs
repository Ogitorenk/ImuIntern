using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Reflection; // isControlled kilidi için

public class DaisySpawnTrigger : MonoBehaviour
{
    [System.Serializable]
    public class DialogueData
    {
        [TextArea(3, 10)]
        [Tooltip("Ms. Daisy'nin bu tetiklenmede söyleyeceði söz kanka")]
        public string speechMessage = "Merhaba Don Quixote! Sana yardým etmeye geldim.";
    }

    [Header("--- MS. DAISY SPAWN AYARLARI ---")]
    [Tooltip("Ms. Daisy'nin Prefab'ý")]
    public GameObject daisyPrefab;
    [Tooltip("Ms. Daisy'nin tam olarak nerede belireceðini gösteren boþ obje (Point)")]
    public Transform spawnPoint;
    [Tooltip("Spawn olurken patlayacak olan sis/ýþýk Particle System Prefab'ý")]
    public GameObject spawnParticlePrefab;

    [Header("--- SES AYARLARI ---")]
    [Tooltip("Ms. Daisy belirdiðinde çalacak olan spawn sesi (AudioClip)")]
    public AudioClip spawnSound;
    [Tooltip("Konuþma balonu açýldýðýnda çalacak olan 'bloop/yazý' sesi")]
    public AudioClip speechSound;
    private AudioSource audioSource;

    [Header("--- UI CANVAS BAÐLANTI AYARLARI ---")]
    [Tooltip("Ekrana gelecek olan o hazýr konuþma balonu / Canvas / Panel objen")]
    public GameObject speechPanel;
    [Tooltip("Panelin içindeki TextMeshPro text bileþeni")]
    public TextMeshProUGUI speechText;

    [Header("--- DÝYALOG LÝSTESÝ ---")]
    [Tooltip("Buraya + butonuna basarak sýrasýyla konuþmalarý ekle kanka. Ýlk geçiþte 1, sahne deðiþtirip gelince 2. çalýþýr.")]
    public List<DialogueData> dialogueList = new List<DialogueData>();

    [Header("--- SÜRE AYARLARI ---")]
    [Tooltip("Ms. Daisy konuþmayý bitirdikten kaç saniye sonra pýt diye yok olsun?")]
    public float displayDuration = 4f;

    [Tooltip("Konuþmayý kapatmak için basýlacak tuþ (Ýsterse süreyi beklemeden geçebilir)")]
    public KeyCode skipKey = KeyCode.F;

    // --- PRO STATÝK HAFIZA SÝSTEMÝ (SAHNE DEÐÝÞTÝRÝNCE SIFIRLANMAZ) ---
    // Her trigger'ýn kendine özel bir ismi (ID) olacak ve kaçýncý konuþmada kaldýðýný sahne deðiþse bile RAM'de saklayacak!
    private static Dictionary<string, int> triggerMemory = new Dictionary<string, int>();

    private string uniqueTriggerID;
    private bool isPlayerInside = false;
    private bool isCutsceneActive = false;
    private GameObject activeDaisyInstance = null;
    private MonoBehaviour currentPlayerScript = null;

    void Start()
    {
        // 1. Bu trigger'a sahne içinde benzersiz bir kimlik veriyoruz ki RAM'de karýþmasýn kanka
        uniqueTriggerID = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_" + gameObject.name + "_" + transform.position.ToString();

        // Eðer bu trigger hafýzada yoksa sýfýrdan ekle
        if (!triggerMemory.ContainsKey(uniqueTriggerID))
        {
            triggerMemory.Add(uniqueTriggerID, 0);
        }

        // 2. AudioSource kontrolü
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Oyun baþýnda paneli gizle kanka
        if (speechPanel != null) speechPanel.SetActive(false);
    }

    void Update()
    {
        // Eðer diyalog aktifse ve oyuncu atlama tuþuna bastýysa süreyi beklemeden kapat kanka
        if (isCutsceneActive && Input.GetKeyDown(skipKey))
        {
            StopAllCoroutines(); // Devam eden sayaçlarý durdur
            EndDaisySpeech();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCutsceneActive)
        {
            int currentIndex = triggerMemory[uniqueTriggerID];

            // Eðer oyuncu bu alandaki tüm konuþma hakkýný doldurduysa bir daha tetiklenme kanka!
            if (currentIndex >= dialogueList.Count)
            {
                return;
            }

            // Ýçeri giren karakteri yakala (Don mu Sancho mu)
            var don = other.GetComponent<DonMovement>();
            if (don != null) currentPlayerScript = don;
            else
            {
                var sancho = other.GetComponent<SanchoMovement>();
                if (sancho != null) currentPlayerScript = sancho;
            }

            // Sahnede Ms. Daisy'i doður ve muhabbeti baþlat!
            StartCoroutine(SpawnAndSpeakRoutine(currentIndex));
        }
    }

    private IEnumerator SpawnAndSpeakRoutine(int dialogueIndex)
    {
        isCutsceneActive = true;

        // 1. Karakteri zýnk diye dondur, animasyonunu sýfýrla
        SetPlayerControl(false);

        // 2. PARTÝCLE EFEKTÝ PATLATMA
        if (spawnParticlePrefab != null && spawnPoint != null)
        {
            GameObject particle = Instantiate(spawnParticlePrefab, spawnPoint.position, Quaternion.identity);
            Destroy(particle, 3f); // Efekt iþi bitince temizlensin
        }

        // 3. SPAWN SESÝ ÇALMA
        if (spawnSound != null)
        {
            audioSource.PlayOneShot(spawnSound);
        }

        yield return new WaitForSeconds(0.2f); // Partikül dumaný çýksýn, çýtýr bi gecikme

        // 4. MS. DAISY'I SPAWN ETME
        if (daisyPrefab != null && spawnPoint != null)
        {
            activeDaisyInstance = Instantiate(daisyPrefab, spawnPoint.position, spawnPoint.rotation);

            // Spawn olurken hareket etmesin demiþtin kanka, eðer üzerinde bir Yapay Zeka (AI) veya Movement varsa kapatýyoruz:
            var daisyRb = activeDaisyInstance.GetComponent<Rigidbody>();
            if (daisyRb != null) daisyRb.velocity = Vector3.zero;
        }

        // 5. CANVAS BALONCUÐUNU AÇMA VE TEXT BASMA
        if (speechPanel != null && speechText != null)
        {
            // Hafýzadaki index'e göre diyalog listesinden doðru yazýyý çekiyoruz kanka
            speechText.text = dialogueList[dialogueIndex].speechMessage;
            speechPanel.SetActive(true);

            if (speechSound != null)
            {
                audioSource.PlayOneShot(speechSound);
            }
        }

        // 6. SÜRE SAYACI BAÞLASIN
        yield return new WaitForSeconds(displayDuration);

        // Süre bitince her þeyi kapat kanka
        EndDaisySpeech();
    }

    private void EndDaisySpeech()
    {
        // 1. Paneli kapat
        if (speechPanel != null) speechPanel.SetActive(false);

        // 2. Ms. Daisy'yi sahnede pýt diye yok et (Ýstersen buraya da gitme partikülü koyabilirsin kanka)
        if (activeDaisyInstance != null)
        {
            Destroy(activeDaisyInstance);
        }

        // 3. KRÝTÝK ADIM: Bu trigger'ýn hafýzadaki konuþma sýrasýný 1 arttýr!
        // Böylece sahneden baþka sahneye gidip geri geldiðinde otomatikman bir sonraki baloncuk çalýþacak!
        triggerMemory[uniqueTriggerID]++;

        // 4. Karakteri serbest býrak, koþmaya devam etsin kanka
        SetPlayerControl(true);

        isCutsceneActive = false;
    }

    private void SetPlayerControl(bool canControl)
    {
        if (currentPlayerScript == null) return;

        // Reflection sayesinde Don ve Sancho'yu sýfýr hata ile donduruyoruz
        try
        {
            System.Type type = currentPlayerScript.GetType();
            FieldInfo field = type.GetField("isControlled");

            if (field != null)
            {
                field.SetValue(currentPlayerScript, canControl);
            }

            Animator anim = currentPlayerScript.GetComponentInChildren<Animator>();
            if (anim != null && !canControl)
            {
                anim.SetFloat("Speed", 0f);
            }
        }
        catch { }
    }

    // Editörde level design yaparken tetikleyici alanlarý mor renk görelim kanka, rahatlýk olur
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(box.center, box.size);
        }

        if (spawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
    }
}