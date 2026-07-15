using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatArenaWaveTrigger : MonoBehaviour
{
    // ========================================================
    // --- SAHNEDEKİ TÜM DALGALI ARENALARI TAKİP EDEN LİSTE ---
    // ========================================================
    private static List<CombatArenaWaveTrigger> allArenas = new List<CombatArenaWaveTrigger>();

    [System.Serializable]
    public class EnemySpawnData
    {
        [Tooltip("Doğacak olan düşmanın Prefab'ı")]
        public GameObject enemyPrefab;
        [Tooltip("Bu düşmanın tam olarak nerede doğacağını belirleyen boş GameObject (Point)")]
        public Transform spawnPoint;
    }

    // === DALGA (WAVE) YAPISI ===
    [System.Serializable]
    public class EnemyWave
    {
        [Tooltip("Dalgaya özel isim (örn: Dalga 1, Boss Dalgası vb.)")]
        public string waveName = "Yeni Dalga";
        [Tooltip("Bu dalgada doğacak düşmanlar ve noktaları")]
        public List<EnemySpawnData> enemiesToSpawn = new List<EnemySpawnData>();
    }

    [Header("--- DÜŞMAN DALGA AYARLARI ---")]
    [Tooltip("Sırasıyla gelecek olan düşman dalgalarının listesi")]
    public List<EnemyWave> waves = new List<EnemyWave>();

    [Header("--- GÖRSEL SPAWN EFEKTİ ---")]
    [Tooltip("Düşmanlar doğarken çıkacak olan toz, duman veya büyü vfx prefab'ı kanka")]
    public GameObject spawnEffectPrefab;
    [Tooltip("Efekt doğduktan kaç saniye sonra sahnede kalabalık yapmasın diye silinsin?")]
    public float effectDestroyDelay = 2f;

    [Header("--- SINIRLANDIRICI DUVARLAR (WALLS) ---")]
    [Tooltip("Alana girildiğinde aktifleşecek, dövüş bitince kapanacak görünmez duvarlar (1'den fazla eklenebilir)")]
    public List<GameObject> arenaWalls = new List<GameObject>();

    [Header("--- OPSİYONEL ÖZELLİKLER ---")]
    [Tooltip("Eğer bu trigger'ın duvarları kapatmasını İSTEMİYORSAN bunu kapatabilirsin kanka")]
    public bool useWalls = true;

    [Header("--- SİNEMATİK AYARLARI ---")]
    [SerializeField] private CutsceneTrigger slimeEscapeCutscene;

    private bool hasTriggered = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private bool arenaActive = false;
    private int currentWaveIndex = 0;

    private Dictionary<GameObject, int> originalWallLayers = new Dictionary<GameObject, int>();

    // ========================================================
    // --- LİSTEYE KAYIT VE HAFIZA YÖNETİMİ ---
    // ========================================================
    void OnEnable()
    {
        if (!allArenas.Contains(this)) allArenas.Add(this);
    }

    void OnDisable()
    {
        if (allArenas.Contains(this)) allArenas.Remove(this);
    }

    void Start()
    {
        foreach (GameObject wall in arenaWalls)
        {
            if (wall != null && !originalWallLayers.ContainsKey(wall))
            {
                originalWallLayers.Add(wall, wall.layer);
            }
        }
        ToggleWalls(false);
    }

    void Update()
    {
        if (arenaActive)
        {
            CheckEnemyStatus();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            StartCombatArena();
        }
    }

    private void StartCombatArena()
    {
        if (waves.Count == 0)
        {
            Debug.LogError($"🚨 KANKA! {gameObject.name} arenasında hiç dalga tanımlanmamış!");
            return;
        }

        hasTriggered = true;
        arenaActive = true;
        currentWaveIndex = 0; // Her zaman ilk dalgadan başla
        
        Debug.Log("<color=red>⚔️ DÖVÜŞ ALANINA GİRİLDİ! Dalgalı Arena Başlatılıyor... ⚔️</color>");

        if (useWalls)
        {
            ToggleWalls(true);
        }

        SpawnWave(currentWaveIndex);
    }

    private void SpawnWave(int waveIndex)
    {
        if (waveIndex >= waves.Count) return;

        EnemyWave currentWave = waves[waveIndex];
        Debug.Log($"<color=orange>🌊 {currentWave.waveName} BAŞLADI! ({waveIndex + 1}/{waves.Count})</color>");

        foreach (var spawnData in currentWave.enemiesToSpawn)
        {
            if (spawnData.enemyPrefab != null && spawnData.spawnPoint != null)
            {
                if (spawnEffectPrefab != null)
                {
                    GameObject effect = Instantiate(spawnEffectPrefab, spawnData.spawnPoint.position, spawnData.spawnPoint.rotation);
                    Destroy(effect, effectDestroyDelay);
                }

                GameObject enemy = Instantiate(spawnData.enemyPrefab, spawnData.spawnPoint.position, spawnData.spawnPoint.rotation);
                spawnedEnemies.Add(enemy);
            }
            else
            {
                Debug.LogWarning($"🚨 KANKA! {currentWave.waveName} içinde prefab veya spawn point boş bırakılmış, kontrol et!");
            }
        }
    }

    private void CheckEnemyStatus()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] == null)
            {
                spawnedEnemies.RemoveAt(i);
            }
        }

        // Eğer mevcut dalgadaki tüm düşmanlar öldüyse
        if (spawnedEnemies.Count == 0)
        {
            // Sırada bekleyen başka bir dalga var mı bak
            if (currentWaveIndex + 1 < waves.Count)
            {
                currentWaveIndex++;
                SpawnWave(currentWaveIndex);
            }
            else
            {
                // Tüm dalgalar bittiyse arenayı sonlandır kanka
                EndCombatArena();
            }
        }
    }

    private void EndCombatArena()
    {
        arenaActive = false;
        Debug.Log("<color=green>✅ TÜM DALGALAR TEMİZLENDİ! Düşman kalmadı, duvarlar açılıyor!</color>");
        ToggleWalls(false);

        if (slimeEscapeCutscene != null)
        {
            slimeEscapeCutscene.TriggerFromExternalScript(); 
        }
    }

    private void ToggleWalls(bool isActive)
    {
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        foreach (GameObject wall in arenaWalls)
        {
            if (wall != null)
            {
                wall.SetActive(isActive);

                if (isActive)
                {
                    wall.layer = ignoreRaycastLayer;
                }
                else
                {
                    if (originalWallLayers.ContainsKey(wall))
                    {
                        wall.layer = originalWallLayers[wall];
                    }
                }
            }
        }
    }

    // ==============================================================================
    // === OYUNCU ÖLDÜĞÜNDE TÜM DALGALI ARENALARI SIFIRLAYAN SİHRİBAZ FONKSİYON ===
    // ==============================================================================
    public static void ResetAllCombatArenas()
    {
        Debug.Log("<color=yellow>🔄 Oyuncu öldü, tüm aktif dalgalı kapışma arenaları ve düşmanları sıfırlanıyor...</color>");
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ToggleBossUI(false);
        }

        foreach (CombatArenaWaveTrigger arena in allArenas)
        {
            if (arena != null)
            {
                if (arena.hasTriggered)
                {
                    // Doğmuş olan ve hayatta kalan tüm düşmanları temizle
                    for (int i = arena.spawnedEnemies.Count - 1; i >= 0; i--)
                    {
                        if (arena.spawnedEnemies[i] != null)
                        {
                            Destroy(arena.spawnedEnemies[i]);
                        }
                    }
                    arena.spawnedEnemies.Clear();

                    // Değişkenleri sıfırla ve dalga index'ini en başa al
                    arena.hasTriggered = false;
                    arena.arenaActive = false;
                    arena.currentWaveIndex = 0; 

                    // Kilitli kalan arena duvarlarını aç kanka
                    arena.ToggleWalls(false);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f); // Farklılık olsun diye rengi maviye çektim
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(box.center, box.size);
        }

        // Tüm dalgaların spawn pointlerini gizmos ile sahne ekranında göster
        foreach (var wave in waves)
        {
            foreach (var spawnData in wave.enemiesToSpawn)
            {
                if (spawnData.spawnPoint != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(spawnData.spawnPoint.position, 0.4f);
                    Gizmos.color = Color.grey;
                    Gizmos.DrawLine(transform.position, spawnData.spawnPoint.position);
                }
            }
        }
    }
}