using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages universal assets and game-wide services across all scenes.
/// This acts as the primary singleton service for the entire monolithic arcade application.
/// </summary>
public class AssetManager : MonoBehaviour
{
    public static AssetManager Instance { get; private set; }
    
    private Dictionary<string, object> loadedAssets = new Dictionary<string, object>();

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Ensure persistence across scene loads
            Debug.Log("AssetManager initialized. Shared library ready.");
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    /// <summary>
    /// Loads a specific asset resource by key.
    /// </summary>
    /// <param name="assetKey">A unique string identifier for the asset (e.g., "TETRIS_BLOCK_ASSET").</param>
    /// <returns>The loaded asset object, or null if not found.</returns>
    public object LoadAsset(string assetKey)
    {
        if (loadedAssets.ContainsKey(assetKey))
        {
            return loadedAssets[assetKey];
        }

        // --- Placeholder for actual asset loading logic (e.g., Resources.Load<T>()) ---
        Debug.LogWarning($"Attempting to load asset: {assetKey}. (Simulated load)");
        
        // Simulate loading an object
        object loadedObject = new GameObject(assetKey); 
        loadedAssets.Add(assetKey, loadedObject);
        return loadedObject;
    }

    /// <summary>
    /// Retrieves a pre-loaded asset.
    /// </summary>
    public object GetAsset(string assetKey)
    {
        if (loadedAssets.ContainsKey(assetKey))
        {
            return loadedAssets[assetKey];
        }
        Debug.LogError($"Asset not found: {assetKey}");
        return null;
    }
}