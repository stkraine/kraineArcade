using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for the main Home Screen UI. This acts as the central menu hub.
/// It uses the AssetManager to access shared functionality like asset databases.
/// </summary>
public class HomeScreenController : MonoBehaviour
{
    [Header("UI Elements")]
    public Text searchInputText;
    public Button selectGameButton;

    void Start()
    {
        // --- Integration Test Hook ---
        // We test here that the shared manager is accessible.
        if (AssetManager.Instance == null)
        {
            Debug.LogError("HomeScreenController failed integration test: AssetManager is null!");
            return;
        }
        
        Debug.Log("HomeScreenController initialized successfully. Integration Test Passed: Shared Library Linked.");
        
        // Initial setup logic for the home screen
        SetupSearchFunction();
    }

    private void SetupSearchFunction()
    {
        // Connect UI elements to logic
        selectGameButton.onClick.AddListener(OnSelectGame);
        // Add input listeners here...
    }

    public void OnSelectGame()
    {
        // Example: Fetching a game asset via the shared manager
        object gameAsset = AssetManager.Instance.LoadAsset("TETRIS_GAME_MODULE");
        
        if (gameAsset != null)
        {
            Debug.Log("Navigating to game module using AssetManager.");
            // Logic to load the game scene/module would go here
        }
        else
        {
            Debug.LogError("Could not find the game module asset!");
        }
    }
}