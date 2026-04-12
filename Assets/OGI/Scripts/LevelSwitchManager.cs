using UnityEngine;
using Cinemachine;

public class LevelSwitchManager : MonoBehaviour
{
    [Header("Sahnedeki Karakterler")]
    public DonMovement donMovement;
    public SanchoMovement sanchoMovement;

    [Header("Kameralar")]
    public CinemachineFreeLook donCamera;
    public CinemachineFreeLook sanchoCamera;

    [Header("Ayarlar")]
    public bool startWithDon = true;

    void Start()
    {
        // Bölüm baþladýðýnda kontrolü ayarla
        SwitchCharacter(startWithDon);
    }

    void Update()
    {
        // TAB tuþuna basýldýðýnda kontrolü diðerine devret
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchCharacter(!donMovement.isControlled);
        }
    }

    private void SwitchCharacter(bool switchToDon)
    {
        if (switchToDon)
        {
            donMovement.isControlled = true;
            sanchoMovement.isControlled = false;

            // Kamera Don Kiþot'a uçar
            if (donCamera != null) donCamera.Priority = 10;
            if (sanchoCamera != null) sanchoCamera.Priority = 0;
        }
        else
        {
            sanchoMovement.isControlled = true;
            donMovement.isControlled = false;

            // Kamera Sancho'ya uçar
            if (sanchoCamera != null) sanchoCamera.Priority = 10;
            if (donCamera != null) donCamera.Priority = 0;
        }
    }
}