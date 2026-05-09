using UnityEngine;

public class DualRealityDecor : MonoBehaviour
{
    [Header("--- Ýllüzyon Objeleri ---")]
    [Tooltip("Don Kiþot aktifken nerede ve ne þekilde görüneceðini ayarladýðýn obje")]
    public GameObject donView;

    [Tooltip("Sancho aktifken nerede ve ne þekilde görüneceðini ayarladýðýn obje")]
    public GameObject sanchoView;

    // Arka planda karakterin deðiþip deðiþmediðini takip eden hafýza
    private bool lastState;

    void Start()
    {
        // Oyun baþlarken kim aktifse ona göre objeleri aç/kapat
        if (DualRealityManager.Instance != null)
        {
            lastState = DualRealityManager.Instance.isDonActive;
            UpdatePerception(lastState);
        }
    }

    void Update()
    {
        // Her karede karakterin deðiþip deðiþmediðini kontrol et
        // (DualRealityManager'a kod eklememek için bu taktiði kullanýyoruz)
        if (DualRealityManager.Instance != null)
        {
            bool currentState = DualRealityManager.Instance.isDonActive;

            // Eðer karakter deðiþtiyse (TAB tuþuna basýldýysa) durumu güncelle
            if (currentState != lastState)
            {
                lastState = currentState;
                UpdatePerception(lastState);
            }
        }
    }

    public void UpdatePerception(bool isDon)
    {
        // Don aktifse Don'un objesi açýlsýn (Sancho'nunki kapansýn)
        if (donView != null) donView.SetActive(isDon);

        // Sancho aktifse Sancho'nun objesi açýlsýn (Don'unki kapansýn)
        if (sanchoView != null) sanchoView.SetActive(!isDon);
    }
}