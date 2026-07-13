using UnityEngine;

public class FootIK : MonoBehaviour
{
    private Animator animator;

    [Header("IK Ayarlarý")]
    public bool enableFootIK = true;
    [Range(0, 1)] public float weight = 1.0f;
    public LayerMask groundLayer;
    public float footOffset = 0.1f; // Ayaðýn taban hizasý ayarý kanka

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Unity animasyon motoru çalýþýrken ayak kemiklerini burasý büker kanka
    void OnAnimatorIK(int layerIndex)
    {
        if (!animator || !enableFootIK) return;

        // --- HATA DÜZELTÝLDÝ: Aðýrlýklar pozisyon ve rotasyon için ayrý ayrý set edilir kanka ---
        // Sol Ayak Aðýrlýklarý
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, weight);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, weight);

        // Sað Ayak Aðýrlýklarý
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, weight);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, weight);

        // --- SOL AYAK RAYCAST VE HÝZALAMA KONTROLÜ ---
        RaycastHit hit;
        Vector3 leftFootPos = animator.GetIKPosition(AvatarIKGoal.LeftFoot);
        if (Physics.Raycast(leftFootPos + Vector3.up, Vector3.down, out hit, 2f, groundLayer))
        {
            Vector3 targetPos = hit.point;
            targetPos.y += footOffset;
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, targetPos);

            // Ayaðýn rotasyonunu basamaðýn eðimine göre eþitler kanka:
            animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(transform.forward, hit.normal));
        }

        // --- SAÐ AYAK RAYCAST VE HÝZALAMA KONTROLÜ ---
        Vector3 rightFootPos = animator.GetIKPosition(AvatarIKGoal.RightFoot);
        if (Physics.Raycast(rightFootPos + Vector3.up, Vector3.down, out hit, 2f, groundLayer))
        {
            Vector3 targetPos = hit.point;
            targetPos.y += footOffset;
            animator.SetIKPosition(AvatarIKGoal.RightFoot, targetPos);
            animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(transform.forward, hit.normal));
        }
    }
}