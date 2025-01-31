using System.Threading.Tasks;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public GameObject PauseCanvas;

    void Awake()
    {
        DontDestroyOnLoad(PauseCanvas);
        DontDestroyOnLoad(gameObject);

        Instance = this;

        PauseCanvas.SetActive(false);
    }

    public void EnablePauseMenu()
    {
        PauseCanvas.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public async void DisablePauseMenu()
    {
        PauseCanvas.SetActive(false);

        await Task.Yield();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void TogglePause()
    {
        if (IsPaused())
        {
            DisablePauseMenu();
        }
        else
        {
            EnablePauseMenu();
        }
    }

    public bool IsPaused()
    {
        return PauseCanvas.activeSelf;
    }
}
