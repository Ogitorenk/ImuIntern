using UnityEngine;
using UnityEditor;
using System.IO;

public class UnusedAssetFinder : EditorWindow
{
    [MenuItem("Tools/Kullanılmayan Assetleri Bul")]
    public static void FindUnused()
    {
        // Projedeki tüm asset yollarını al
        string[] allAssets = AssetDatabase.GetAllAssetPaths();
        
        Debug.Log("--- Kullanılmayan Asset Taraması Başladı ---");
        foreach (string assetPath in allAssets)
        {
            // Sadece Assets klasörü altındaki dosyalara bak (Paketleri ve dahili dosyaları ele)
            if (assetPath.StartsWith("Assets") && !Directory.Exists(assetPath))
            {
                // Assetin bağımlılıklarını kontrol et
                string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);
                
                // Eğer bu asete hiçbir şey bağımlı değilse ve kendisi kritik bir dosya değilse listele
                if (dependencies.Length == 0 && !assetPath.EndsWith(".cs") && !assetPath.EndsWith(".unity"))
                {
                    Debug.LogWarning("Kullanılmıyor olabilir: " + assetPath);
                }
            }
        }
        Debug.Log("--- Tarama Bitti ---");
    }
}