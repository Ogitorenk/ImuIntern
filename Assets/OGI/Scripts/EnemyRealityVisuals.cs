using UnityEngine;

public class EnemyRealityVisuals : MonoBehaviour
{
    [Header("Gerçeklik Modelleri")]
    [Tooltip("Don Kiþot'un gördüðü düþman")]
    public GameObject banditModel;

    [Tooltip("Sancho'nun gördüðü düþman")]
    public GameObject skeletonModel;

    private bool isDonReality = true;
    private EnemyMelee enemyMelee; // YENÝ: Düþman scriptine eriþim

    void Start()
    {
        enemyMelee = GetComponent<EnemyMelee>();
        UpdateVisuals();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isDonReality = !isDonReality;
            UpdateVisuals();
        }
    }

    void UpdateVisuals()
    {
        if (isDonReality)
        {
            if (banditModel != null) banditModel.SetActive(true);
            if (skeletonModel != null) skeletonModel.SetActive(false);
        }
        else
        {
            if (banditModel != null) banditModel.SetActive(false);
            if (skeletonModel != null) skeletonModel.SetActive(true);
        }

        // YENÝ: Gerçeklik deðiþtiðinde, aktif olan çocuðun Animatörünü bul ve Melee scriptine ver!
        if (enemyMelee != null)
        {
            enemyMelee.animator = GetComponentInChildren<Animator>();
        }
    }
}