using UnityEngine;
using System.Collections;

public class SpawnOverride : MonoBehaviour
{
    IEnumerator Start()
    {
        // CheckpointManager'ın ApplyDataToSceneObjects metodu Start fonksiyonunun sonunda 
        // (WaitForEndOfFrame ile) çalıştığı için, biz bir tık daha bekleyip onun yerleştirmesini eziyoruz.
        yield return new WaitForSeconds(0.05f);

        // Eğer SceneChanger'dan gelen özel bir ışınlanma emri varsa
        if (SceneChanger.ozelIsinlanmaAktif)
        {
            Vector3 hedefPos = SceneChanger.transferKoordinat;

            // DonQuixote'u ışınla
            DonMovement don = FindObjectOfType<DonMovement>();
            if (don != null)
            {
                don.transform.position = hedefPos;
            }

            // Sancho'yu hemen yanına ışınla
            SanchoMovement sancho = FindObjectOfType<SanchoMovement>();
            if (sancho != null)
            {
                sancho.transform.position = hedefPos + new Vector3(1f, 0f, 0f);
            }

            Debug.Log($"<color=green>⚡ [Spawn Override] Karakterler başarıyla {hedefPos} koordinatına zorla yerleştirildi!</color>");

            // İşimiz bittiği için sistemi sıfırlıyoruz ki bir sonraki normal ölümlerde/geçişlerde düzgünce checkpointe dönebilsinler.
            SceneChanger.ozelIsinlanmaAktif = false;
        }
    }
}