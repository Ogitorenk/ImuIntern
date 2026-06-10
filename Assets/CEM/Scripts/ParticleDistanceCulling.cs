using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleDistanceCulling : MonoBehaviour
{
    [SerializeField] private float activeDistance = 80f;
    [SerializeField] private float checkInterval = 0.25f;

    private ParticleSystem particleSystem;
    private Transform playerCamera;
    private bool isActive = true;

    private void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();

        if (Camera.main != null)
            playerCamera = Camera.main.transform;

        StartCoroutine(CheckDistanceRoutine());
    }

    private IEnumerator CheckDistanceRoutine()
    {
        float sqrActiveDistance = activeDistance * activeDistance;

        while (true)
        {
            if (playerCamera != null)
            {
                float sqrDistance =
                    (playerCamera.position - transform.position).sqrMagnitude;

                if (sqrDistance > sqrActiveDistance)
                {
                    if (isActive)
                    {
                        particleSystem.Stop(
                            true,
                            ParticleSystemStopBehavior.StopEmittingAndClear);

                        isActive = false;
                    }
                }
                else
                {
                    if (!isActive)
                    {
                        particleSystem.Play();
                        isActive = true;
                    }
                }
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }
}