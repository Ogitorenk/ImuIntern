using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
//using System.Collections.Collections.Empty; // Gerekirse
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    private PostProcessVolume volume;
    private Vignette vignette;
    private ColorGrading colorGrading;

    public float fadeDuration = 1.0f; // Kararma süresi (saniye)

    void Start()
    {
        volume = GetComponent<PostProcessVolume>();
        
        // Profil içindeki efektlere ulaşıyoruz
        volume.profile.TryGetSettings(out vignette);
        volume.profile.TryGetSettings(out colorGrading);

        // Başlangıçta ekran normal olsun
        if (vignette != null) vignette.intensity.value = 0f;
        if (colorGrading != null) colorGrading.postExposure.value = 0f;
    }

    public void StartFadeToBlack()
    {
        StartCoroutine(FadeRoutine(1f, -10f)); // Hedef Vignette: 1, Hedef Exposure: -10
    }

    public void StartFadeToNormal()
    {
        StartCoroutine(FadeRoutine(0f, 0f));
    }

    private IEnumerator FadeRoutine(float targetVignette, float targetExposure)
    {
        float elapsedTime = 0f;
        float startVignette = vignette != null ? vignette.intensity.value : 0f;
        float startExposure = colorGrading != null ? colorGrading.postExposure.value : 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;

            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(startVignette, targetVignette, t);

            if (colorGrading != null)
                colorGrading.postExposure.value = Mathf.Lerp(startExposure, targetExposure, t);

            yield return null;
        }

        // Değerleri tam eşitleyelim
        if (vignette != null) vignette.intensity.value = targetVignette;
        if (colorGrading != null) colorGrading.postExposure.value = targetExposure;
    }
}