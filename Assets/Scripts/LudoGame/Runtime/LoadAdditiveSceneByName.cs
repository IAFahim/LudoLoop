using Events;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadAdditiveSceneByName : MonoBehaviour
{
    public string sceneName;
    public EventBusString eventBusString;

    public void Load()
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        eventBusString.Publish(sceneName);
    }
}