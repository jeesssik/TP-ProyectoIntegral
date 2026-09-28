using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string pauseCanvasName = "Canvas-Pausa";
    [SerializeField] private string mainMenuSceneName = "Menu";

    [Header("Optional")]
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private GameObject controlsCanvas;
    [SerializeField] private GameObject optionsCanvas;
    [SerializeField] private GameObject controlsMapCanvas;
    [SerializeField] private GameObject audioCanvas;

    private bool isPaused;
    private bool listenersBound;

    private void Awake()
    {
        ResolveReferences();
        HideAllManagedCanvases();
        Time.timeScale = 1f;
    }

    private void Start()
    {
        BindButtons();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) && !Input.GetKeyDown(KeyCode.P))
        {
            return;
        }

        if (IsActive(audioCanvas) || IsActive(controlsMapCanvas))
        {
            ShowControlsCanvas();
            return;
        }

        if (IsActive(controlsCanvas))
        {
            ShowOptionsCanvas();
            return;
        }

        if (IsActive(GetOptionsCanvas()))
        {
            ShowPauseMenu();
            return;
        }

        if (IsActive(pauseCanvas))
        {
            TogglePause();
            return;
        }

        ShowPauseMenu();
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
            return;
        }

        PauseGame();
    }

    public void PauseGame()
    {
        ResolveReferences();

        if (pauseCanvas == null)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;
        HideAllManagedCanvases();
        pauseCanvas.SetActive(true);
    }

    public void ResumeGame()
    {
        ResolveReferences();

        isPaused = false;
        Time.timeScale = 1f;
        HideAllManagedCanvases();
    }

    public void ShowPauseMenu()
    {
        ResolveReferences();

        isPaused = true;
        Time.timeScale = 0f;
        HideAllManagedCanvases();

        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(true);
        }
    }

    public void ShowOptionsCanvas()
    {
        ResolveReferences();

        if (GetOptionsCanvas() == null)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;
        HideAllManagedCanvases();
        SetCanvasActive(GetOptionsCanvas(), true);
    }

    public void ShowControlsCanvas()
    {
        ResolveReferences();

        GameObject canvas = controlsCanvas;
        if (canvas == null)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;
        HideAllManagedCanvases();
        canvas.SetActive(true);
    }

    public void ShowControlsMapCanvas()
    {
        ResolveReferences();

        if (controlsMapCanvas == null)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;
        HideAllManagedCanvases();
        controlsMapCanvas.SetActive(true);
    }

    public void ShowAudioCanvas()
    {
        ResolveReferences();

        if (audioCanvas == null)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;
        HideAllManagedCanvases();
        audioCanvas.SetActive(true);
    }

    public void ReturnToPauseMenu()
    {
        ShowPauseMenu();
    }

    public void ReturnToOptionsMenu()
    {
        ShowOptionsCanvas();
    }

    public void ReturnToControlsMenu()
    {
        ShowControlsCanvas();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResolveReferences()
    {
        if (pauseCanvas == null)
        {
            pauseCanvas = FindSceneObjectByName(pauseCanvasName);
        }

        if (controlsCanvas == null)
        {
            controlsCanvas = FindAnyKnownControlsCanvas();
        }

        if (optionsCanvas == null)
        {
            optionsCanvas = controlsCanvas != null ? controlsCanvas : FindAnyKnownOptionsCanvas();
        }

        if (controlsMapCanvas == null)
        {
            controlsMapCanvas = FindAnyKnownControlsMapCanvas();
        }

        if (audioCanvas == null)
        {
            audioCanvas = FindAnyKnownAudioCanvas();
        }
    }

    private void BindButtons()
    {
        if (listenersBound || pauseCanvas == null)
        {
            return;
        }

        BindCanvasButtons(
            pauseCanvas,
            backAction: null,
            mapAction: ShowOptionsCanvas,
            audioAction: null,
            isPauseMenu: true);

        BindCanvasButtons(
            GetOptionsCanvas(),
            backAction: ReturnToPauseMenu,
            mapAction: ShowControlsCanvas,
            audioAction: ShowAudioCanvas,
            isPauseMenu: false);

        BindCanvasButtons(
            controlsCanvas,
            backAction: ReturnToOptionsMenu,
            mapAction: ShowControlsMapCanvas,
            audioAction: ShowAudioCanvas,
            isPauseMenu: false);

        BindCanvasButtons(
            controlsMapCanvas,
            backAction: ReturnToControlsMenu,
            mapAction: null,
            audioAction: null,
            isPauseMenu: false);

        BindCanvasButtons(
            audioCanvas,
            backAction: ReturnToControlsMenu,
            mapAction: null,
            audioAction: null,
            isPauseMenu: false);

        listenersBound = true;
    }

    private void BindCanvasButtons(GameObject canvas, Action backAction, Action mapAction, Action audioAction, bool isPauseMenu)
    {
        if (canvas == null)
        {
            return;
        }

        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            string label = GetButtonLabel(button);
            if (string.IsNullOrEmpty(label))
            {
                continue;
            }

            if (label.Contains("volver") || label.Contains("atras") || label.Contains("back") || label.Contains("regresar"))
            {
                button.onClick.RemoveAllListeners();
                if (backAction != null)
                {
                    button.onClick.AddListener(() => backAction());
                }

                continue;
            }

            if (!isPauseMenu && (label.Contains("mapa") || label.Contains("control")))
            {
                if (mapAction != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => mapAction());
                }

                continue;
            }

            if (label.Contains("audio") || label.Contains("sonido"))
            {
                if (audioAction != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => audioAction());
                }

                continue;
            }

            if (!isPauseMenu)
            {
                continue;
            }

            button.onClick.RemoveAllListeners();

            if (label.Contains("continuar") || label.Contains("continue"))
            {
                button.onClick.AddListener(ResumeGame);
            }
            else if (label.Contains("rein") || label.Contains("restart") || label.Contains("volver a empezar"))
            {
                button.onClick.AddListener(RestartLevel);
            }
            else if (label.Contains("opcion") || label.Contains("option") || label.Contains("control") || label.Contains("ajuste") || label.Contains("config"))
            {
                button.onClick.AddListener(ShowOptionsCanvas);
            }
            else if (label.Contains("menu") && label.Contains("principal") || label == "menu")
            {
                button.onClick.AddListener(LoadMainMenu);
            }
            else if (label.Contains("salir") || label.Contains("exit") || label.Contains("quit"))
            {
                button.onClick.AddListener(QuitGame);
            }
        }
    }

    private void HideAllManagedCanvases()
    {
        SetCanvasActive(pauseCanvas, false);
        SetCanvasActive(GetOptionsCanvas(), false);
        SetCanvasActive(controlsCanvas, false);
        SetCanvasActive(optionsCanvas, false);
        SetCanvasActive(controlsMapCanvas, false);
        SetCanvasActive(audioCanvas, false);
    }

    private GameObject GetOptionsCanvas()
    {
        return optionsCanvas;
    }

    private GameObject GetControlsCanvas()
    {
        return controlsCanvas != null ? controlsCanvas : optionsCanvas;
    }

    private static void SetCanvasActive(GameObject canvas, bool active)
    {
        if (canvas != null)
        {
            canvas.SetActive(active);
        }
    }

    private static bool IsActive(GameObject canvas)
    {
        return canvas != null && canvas.activeSelf;
    }

    private static string GetButtonLabel(Button button)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
        {
            return string.Empty;
        }

        return text.text.Trim().ToLowerInvariant();
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        GameObject directMatch = GameObject.Find(objectName);
        if (directMatch != null)
        {
            return directMatch;
        }

        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate != null && candidate.name == objectName && candidate.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }

    private static GameObject FindAnyKnownOptionsCanvas()
    {
        string[] candidates =
        {
            "Canvas-Opciones",
            "CanvasOpciones",
            "Canvas-Options",
            "CanvasOptions",
            "CanvasOpt",
            "Canvas-Opt",
            "OptionsCanvas",
            "Opciones"
        };

        foreach (string candidate in candidates)
        {
            GameObject found = FindSceneObjectByName(candidate);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject FindAnyKnownControlsCanvas()
    {
        string[] candidates =
        {
            "CanvasControls",
            "Canvas-Controls",
            "CanvasControles",
            "Canvas-Controles",
            "ControlsCanvas",
            "Controles",
            "Controls"
        };

        foreach (string candidate in candidates)
        {
            GameObject found = FindSceneObjectByName(candidate);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject FindAnyKnownControlsMapCanvas()
    {
        string[] candidates =
        {
            "CanvasControlsMap",
            "Canvas-Controls-Map",
            "CanvasControlesMapa",
            "MapaControles",
            "ControlsMapCanvas"
        };

        foreach (string candidate in candidates)
        {
            GameObject found = FindSceneObjectByName(candidate);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject FindAnyKnownAudioCanvas()
    {
        string[] candidates =
        {
            "AudioCanvas",
            "CanvasAudio",
            "Canvas-Audio",
            "SoundOptionsCanvas",
            "CanvasOpt",
            "Canvas-Opt",
            "OpcionesAudio"
        };

        foreach (string candidate in candidates)
        {
            GameObject found = FindSceneObjectByName(candidate);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
