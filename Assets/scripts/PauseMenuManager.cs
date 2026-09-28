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
    [SerializeField] private GameObject optionsCanvas;

    private bool isPaused;
    private bool listenersBound;

    private void Awake()
    {
        ResolveReferences();
        HideAllPauseUIs();
        Time.timeScale = 1f;
    }

    private void Start()
    {
        BindButtons();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (optionsCanvas != null && optionsCanvas.activeSelf)
            {
                ShowPauseMenu();
            }
            else
            {
                TogglePause();
            }
        }
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
        pauseCanvas.SetActive(true);

        if (optionsCanvas != null)
        {
            optionsCanvas.SetActive(false);
        }
    }

    public void ResumeGame()
    {
        ResolveReferences();

        isPaused = false;
        Time.timeScale = 1f;

        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }

        if (optionsCanvas != null)
        {
            optionsCanvas.SetActive(false);
        }
    }

    public void ShowPauseMenu()
    {
        ResolveReferences();

        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(true);
        }

        if (optionsCanvas != null)
        {
            optionsCanvas.SetActive(false);
        }
    }

    public void ShowOptionsCanvas()
    {
        ResolveReferences();

        if (optionsCanvas == null)
        {
            return;
        }

        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }

        optionsCanvas.SetActive(true);
    }

    public void ReturnToPauseMenu()
    {
        ShowPauseMenu();
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

        if (optionsCanvas == null)
        {
            optionsCanvas = FindAnyKnownOptionsCanvas();
        }
    }

    private void BindButtons()
    {
        if (listenersBound || pauseCanvas == null)
        {
            return;
        }

        Button[] buttons = pauseCanvas.GetComponentsInChildren<Button>(true);
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

            button.onClick.RemoveAllListeners();

            if (label.Contains("continuar") || label.Contains("continue"))
            {
                button.onClick.AddListener(ResumeGame);
            }
            else if (label.Contains("rein") || label.Contains("restart") || label.Contains("volver a empezar"))
            {
                button.onClick.AddListener(RestartLevel);
            }
            else if (label.Contains("opcion") || label.Contains("option"))
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

        if (optionsCanvas != null)
        {
            Button[] optionsButtons = optionsCanvas.GetComponentsInChildren<Button>(true);
            foreach (Button button in optionsButtons)
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
                    button.onClick.AddListener(ReturnToPauseMenu);
                }
            }
        }

        listenersBound = true;
    }

    private void HideAllPauseUIs()
    {
        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }

        if (optionsCanvas != null)
        {
            optionsCanvas.SetActive(false);
        }

        isPaused = false;
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
}
