using UnityEngine;
using UnityEditor;
using System.IO;

// Post Processing Stack v2 bileşenini kodla okumak için
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

public class SteamScreenshotWindow : EditorWindow
{
    public enum ResolutionOption
    {
        FullHD_1080p, // 1920 x 1080
        QuadHD_2K,    // 2560 x 1440
        UltraHD_4K,   // 3840 x 2160
        SuperHD_8K    // 7680 x 4320
    }

    private ResolutionOption selectedResolution = ResolutionOption.UltraHD_4K;

    [MenuItem("Tools/Steam Screenshot Taker")]
    public static void ShowWindow()
    {
        GetWindow<SteamScreenshotWindow>("Steam SS Taker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Steam Ekran Görüntüsü Aracı (Post-Process Destekli)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        selectedResolution = (ResolutionOption)EditorGUILayout.EnumPopup("Hedef Çözünürlük", selectedResolution);
        EditorGUILayout.HelpBox(GetResolutionText(selectedResolution), MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Oyunu Dondur / Devam Ettir (Pause)", GUILayout.Height(30)))
        {
            Time.timeScale = Time.timeScale == 0f ? 1f : 0f;
        }

        EditorGUILayout.Space();
        GUI.backgroundColor = Color.green;

        if (GUILayout.Button("SCENE AÇISINDAN FOTOĞRAF ÇEK", GUILayout.Height(45)))
        {
            CaptureSceneView();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space();

        if (GUILayout.Button("Ekran Görüntüleri Klasörünü Aç", GUILayout.Height(25)))
        {
            OpenFolder();
        }
    }

    private string GetResolutionText(ResolutionOption res)
    {
        switch (res)
        {
            case ResolutionOption.FullHD_1080p: return "Çıktı: 1920 x 1080 (1080p)";
            case ResolutionOption.QuadHD_2K: return "Çıktı: 2560 x 1440 (2K)";
            case ResolutionOption.UltraHD_4K: return "Çıktı: 3840 x 2160 (4K - Steam İçin İdeal)";
            case ResolutionOption.SuperHD_8K: return "Çıktı: 7680 x 4320 (8K)";
            default: return "";
        }
    }

    private (int width, int height) GetDimensions(ResolutionOption res)
    {
        switch (res)
        {
            case ResolutionOption.FullHD_1080p: return (1920, 1080);
            case ResolutionOption.QuadHD_2K: return (2560, 1440);
            case ResolutionOption.UltraHD_4K: return (3840, 2160);
            case ResolutionOption.SuperHD_8K: return (7680, 4320);
            default: return (3840, 2160);
        }
    }

    private void CaptureSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            Debug.LogError("Lütfen önce Scene penceresine tıklayın!");
            return;
        }

        (int width, int height) = GetDimensions(selectedResolution);

        // 1. Geçici bir fotoğraf kamerası oluştur
        GameObject tempCamObj = new GameObject("TempPhotoCam");
        Camera photoCam = tempCamObj.AddComponent<Camera>();

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            photoCam.CopyFrom(mainCam);

            // Post-Processing Layer bileşenini kopyalama (Built-in RP için)
            var mainPostLayer = mainCam.GetComponent("PostProcessLayer");
            if (mainPostLayer != null)
            {
                // Reflection ile PostProcessLayer bileşenini ve ayarlarını kopyalıyoruz
                System.Type postLayerType = mainPostLayer.GetType();
                var photoPostLayer = tempCamObj.AddComponent(postLayerType);

                // Field kopyalama (volumeLayer, volumeTrigger vb.)
                var fields = postLayerType.GetFields();
                foreach (var field in fields)
                {
                    field.SetValue(photoPostLayer, field.GetValue(mainPostLayer));
                }
            }
        }

        // 2. Kamerayı SceneView konumuna taşı
        photoCam.transform.position = sceneView.camera.transform.position;
        photoCam.transform.rotation = sceneView.camera.transform.rotation;
        photoCam.fieldOfView = sceneView.camera.fieldOfView;

        // 3. Render Texture ile yüksek çözünürlüklü çizim
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        photoCam.targetTexture = rt;
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);

        photoCam.Render();

        RenderTexture.active = rt;
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        // 4. Temizlik
        photoCam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);
        DestroyImmediate(tempCamObj);

        // 5. PNG Kaydet
        byte[] bytes = screenshot.EncodeToPNG();
        string filename = $"Steam_SS_{width}x{height}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string filePath = Path.Combine(Application.dataPath, "..", filename);
        
        File.WriteAllBytes(filePath, bytes);

        Debug.Log($"<color=green><b>[Steam SS]</b> Post-Process Efektleriyle Başarıyla Kaydedildi ({width}x{height}): {filePath}</color>");
        AssetDatabase.Refresh();
    }

    private void OpenFolder()
    {
        string folderPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        EditorUtility.RevealInFinder(folderPath);
    }
}