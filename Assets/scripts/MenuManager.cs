using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Canvases")]
    public GameObject mainMenuCanvas;
    public GameObject optionsCanvas;
    public GameObject creditsCanvas;
    public GameObject soundOptionsCanvas;
    public GameObject controlsCanvas;

    private GameObject currentCanvas;
    private GameObject previousCanvas;

    [Header("Scene")]
    public string gameSceneName = "Level-1";

    [SerializeField] private AudioClip menuMusic;

    private void Start()
    {
        ShowMainMenu();

        if (AudioManager.Instance != null && menuMusic != null)
        {
            AudioManager.Instance.PlayMusic(menuMusic);
        }
    }

    public void OnStartButton()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnOptionsButton()
    {
        OpenCanvas(optionsCanvas);
    }

    public void OnCreditsButton()
    {
        OpenCanvas(creditsCanvas);
    }

    public void OnSoundButton()
    {
        OpenCanvas(soundOptionsCanvas);
    }

    public void OnControlsButton()
    {
        OpenCanvas(controlsCanvas);
    }

    public void OnExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnBackButton()
    {
        if (previousCanvas == null)
        {
            ShowMainMenu();
            return;
        }

        currentCanvas.SetActive(false);
        previousCanvas.SetActive(true);

        currentCanvas = previousCanvas;
        previousCanvas = mainMenuCanvas;
    }

    private void OpenCanvas(GameObject newCanvas)
    {
        previousCanvas = currentCanvas;

        if (currentCanvas != null)
        {
            currentCanvas.SetActive(false);
        }

        currentCanvas = newCanvas;
        currentCanvas.SetActive(true);
    }

    private void ShowMainMenu()
    {
        mainMenuCanvas.SetActive(true);
        optionsCanvas.SetActive(false);
        creditsCanvas.SetActive(false);
        soundOptionsCanvas.SetActive(false);
        if (controlsCanvas != null) controlsCanvas.SetActive(false);

        currentCanvas = mainMenuCanvas;
        previousCanvas = null;
    }
}