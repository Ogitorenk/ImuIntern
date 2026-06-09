using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatAreaTrigger : MonoBehaviour
{
    // Müfettiş ekranında her bir düşmanın nereye ve hangi prefabla doğacağını seçmek için alt sınıf
    [System.Serializable]
    public class EnemySpawnData
    {
        [Tooltip("Doğacak olan düşmanın Prefab'ı")]
        public GameObject enemyPrefab;
        [Tooltip("Bu düşmanın tam olarak nerede doğacağını belirleyen boş GameObject (Point)")]
        public Transform spawnPoint;
    }

    [Header("--- DÜŞMAN SPAWN AYARLARI ---")]
    [Tooltip("Bu alana girildiğinde doğacak düşmanların ve noktalarının listesi")]
    public List<EnemySpawnData> enemiesToSpawn = new List<EnemySpawnData>();

    [Header("--- SINIRLANDIRICI DUVARLAR (WALLS) ---")]
    [Tooltip("Alana girildiğinde aktifleşecek, dövüş bitince kapanacak görünmez duvarlar (1'den fazla eklenebilir)")]
    public List<GameObject> arenaWalls = new List<GameObject>();

    [Header("--- OPSİYONEL ÖZELLİKLER ---")]
    [Tooltip("Eğer bu trigger'ın duvarları kapatmasını İSTEMİYORSAN bunu kapatabilirsin kanka")]
    public bool useWalls = true;

    private bool hasTriggered = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private bool arenaActive = false;

    void Start()
    {
        // Oyun başında duvarları ne olur ne olmaz deaktif et kanka, oyuncu özgürce yürüsün
        ToggleWalls(false);
    }

    void Update()
    {
        // Eğer dövüş başladıysa her karede doğan düşmanlardan hayatta olan var mı diye denetle
        if (arenaActive)
        {
            CheckEnemyStatus();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Alana giren kişi Don veya Sancho (yani Player) ise ve dövüş henüz başlamadıysa tetikle
        if (!hasTriggered && other.CompareTag("Player"))
        {
            StartCombatArena();
        }
    }

    private void StartCombatArena()
    {
        hasTriggered = true;
        arenaActive = true;
        Debug.Log("<color=red>⚔️ DÖVÜŞ ALANINA GİRİLDİ! Arena Başlatılıyor... ⚔️</color>");

        // 1. Duvarları Seçeneğe Göre Kilitle
        if (useWalls)
        {
            ToggleWalls(true);
        }

        // 2. Düşmanları Point'lerinde Doğur
        SpawnAllEnemies();
    }

    private void SpawnAllEnemies()
    {
        foreach (var spawnData in enemiesToSpawn)
        {
            if (spawnData.enemyPrefab != null && spawnData.spawnPoint != null)
            {
                // Düşmanı Point'in koordinatlarında ve rotasyonunda klonla
                GameObject enemy = Instantiate(spawnData.enemyPrefab, spawnData.spawnPoint.position, spawnData.spawnPoint.rotation);

                // Takip listemize ekle ki ölüp ölmediklerini bilelim
                spawnedEnemies.Add(enemy);
            }
            else
            {
                Debug.LogWarning("🚨 KANKA! CombatAreaTrigger içinde prefab veya spawn point boş bırakılmış, kontrol et!");
            }
        }
    }

    private void CheckEnemyStatus()
    {
        // Listeyi arkadan öne doğru tarıyoruz ki silerken index kaçmasın
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            // Eğer düşman script yardımıyla Destroy edildiyse listeden çıkar
            if (spawnedEnemies[i] == null)
            {
                spawnedEnemies.RemoveAt(i);
            }
        }

        // Eğer listede hiç düşman kalmadıysa dövüş bitmiştir!
        if (spawnedEnemies.Count == 0)
        {
            EndCombatArena();
        }
    }

    private void EndCombatArena()
    {
        arenaActive = false;
        Debug.Log("<color=green>✅ DÖVÜŞ BİTTİ! Tüm düşmanlar temizlendi, duvarlar açılıyor!</color>");

        // Duvarları indir, oyuncu ilerleyebilsin
        ToggleWalls(false);

        // İleride bu dövüş bitince kapı açılma sesi veya müzik değişimi tetiklenecekse tam buraya yazılır kanka
    }

    private void ToggleWalls(bool isActive)
    {
        foreach (GameObject wall in arenaWalls)
        {
            if (wall != null)
            {
                wall.SetActive(isActive);
            }
        }
    }

    // --- LEVED DESIGNER DOSTU GIZMOS HİLESİ ---
    // Editör ekranında pointlerin nerede olduğunu çizgilerle görmemizi sağlar, aşırı kolaylık kanka!
    private void OnDrawGizmos()
    {
        // Tetikleyici kutunun sınırlarını yeşil göster
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(box.center, box.size);
        }

        // Doğacak düşman noktalarına kırmızı çizgiler çek ve küre koy
        foreach (var spawnData in enemiesToSpawn)
        {
            if (spawnData.spawnPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(spawnData.spawnPoint.position, 0.4f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, spawnData.spawnPoint.position);
            }
        }
    }
}