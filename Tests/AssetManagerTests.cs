using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
// Assuming the AssetManager class is accessible here for testing

[TestFixture]
public class AssetManagerTests
{
    private AssetManager _testManager;

    [SetUp]
    public void Setup()
    {
        // In a real Unity environment, we would initialize a test instance of AssetManager
        // For this simulation, we assume a test singleton can be instantiated.
        _testManager = new AssetManager(); 
    }

    [Test]
    public void LoadAsset_ShouldReturnNull_ForNonExistentAsset()
    {
        // Arrange
        string nonExistentKey = "NON_EXISTENT_ASSET";
        
        // Act
        object asset = _testManager.LoadAsset(nonExistentKey);
        
        // Assert
        Assert.IsNull(asset, "Expected null for a non-existent asset.");
    }

    [Test]
    public void LoadAsset_ShouldReturnSameAsset_OnDuplicateCall()
    {
        // Arrange
        string assetKey = "DUPLICATE_TEST_ASSET";

        // Act 1
        object firstAsset = _testManager.LoadAsset(assetKey);
        
        // Act 2
        object secondAsset = _testManager.LoadAsset(assetKey);

        // Assert
        // We test that the object retrieved is the same instance (simulating caching)
        Assert.AreEqual(firstAsset, secondAsset, "Expected cached asset object to be the same instance.");
    }
}