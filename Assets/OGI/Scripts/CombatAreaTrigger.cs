using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatAreaTrigger : MonoBehaviour
{
    // ========================================================
    // --- YENİ EKLENDİ: SAHNEDEKİ TÜM ARENALARI TAKİP EDEN LİSTE ---
    // ========================================================
    private static List<CombatAreaTrigger> allArenas = new List<CombatAreaTrigger>();

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

    private bool hasTriggered = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private bool arenaActive = false;

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
        hasTriggered = true;
        arenaActive = true;
        Debug.Log("<color=red>⚔️ DÖVÜŞ ALANINA GİRİLDİ! Arena Başlatılıyor... ⚔️</color>");

        if (useWalls)
        {
            ToggleWalls(true);
        }

        SpawnAllEnemies();
    }

    private void SpawnAllEnemies()
    {
        foreach (var spawnData in enemiesToSpawn)
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
                Debug.LogWarning("🚨 KANKA! CombatAreaTrigger içinde prefab veya spawn point boş bırakılmış, kontrol et!");
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

        if (spawnedEnemies.Count == 0)
        {
            EndCombatArena();
        }
    }

    private void EndCombatArena()
    {
        arenaActive = false;
        Debug.Log("<color=green>✅ DÖVÜŞ BİTTİ! Tüm düşmanlar temizlendi, duvarlar açılıyor!</color>");
        ToggleWalls(false);
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
    // === YENİ EKLENDİ: OYUNCU ÖLDÜĞÜNDE TÜM ARENALARI SIFIRLAYAN SİHRİBBAZ FONKSİYON ===
    // ==============================================================================
    public static void ResetAllCombatArenas()
    {
        Debug.Log("<color=yellow>🔄 Oyuncu öldü, tüm aktif kapışma arenaları ve düşmanları sıfırlanıyor...</color>");

        foreach (CombatAreaTrigger arena in allArenas)
        {
            if (arena != null)
            {
                // 1. Eğer kapışma bitmediyse ve aktifse (veya hasTriggered olduysa)
                if (arena.hasTriggered)
                {
                    // Doğmuş olan ve hayatta kalan tüm düşmanları sahneden kazı kanka
                    for (int i = arena.spawnedEnemies.Count - 1; i >= 0; i--)
                    {
                        if (arena.spawnedEnemies[i] != null)
                        {
                            Destroy(arena.spawnedEnemies[i]);
                        }
                    }
                    arena.spawnedEnemies.Clear();

                    // 2. Değişkenleri fabrikadan çıkmış haline geri çek kanka
                    arena.hasTriggered = false;
                    arena.arenaActive = false;

                    // 3. Kilitli kalan arena duvarlarını oyuncu rahat geçsin diye indir
                    arena.ToggleWalls(false);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(box.center, box.size);
        }

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