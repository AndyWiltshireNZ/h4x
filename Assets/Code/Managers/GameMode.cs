using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GameMode : MonoBehaviour
{
	public static GameMode Instance;
	
	[SerializeField] private GameModeDefinition GameModeDefinition;

	public UIManager UIManager = null;
	public LevelManager LevelManager = null;
	public CameraController MainCamera = null;

	private void Awake ()
    {
        Instance = this;

		init();
    }

	private void OnDestroy()
	{
		Instance = null;
	}

	private async void init()
	{
		AsyncOperationHandle<GameObject> asyncSpawnUIManager = GameModeDefinition.UIManagerAssetReference.InstantiateAsync();
		AsyncOperationHandle<GameObject> asyncSpawnLevelManager = GameModeDefinition.LevelManagerAssetReference.InstantiateAsync();

		await Task.WhenAll( asyncSpawnUIManager.Task, asyncSpawnLevelManager.Task );

		if ( asyncSpawnUIManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject uimanagerObj = asyncSpawnUIManager.Result;

			UIManager = uimanagerObj.GetComponent<UIManager>();
		}

		if ( asyncSpawnLevelManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject levelmanagerObj = asyncSpawnLevelManager.Result;

			LevelManager = levelmanagerObj.GetComponent<LevelManager>();
		}
	}

    private void Update()
    {
        
    }
}
