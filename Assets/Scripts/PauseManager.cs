using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public GameObject PauseMenu;

    void Awake()
    {
        PauseMenu = GameObject.FindGameObjectWithTag("Pause Menu");

        DontDestroyOnLoad(PauseMenu.transform.parent);
        DontDestroyOnLoad(gameObject);

        EnablePauseMenu();

        Instance = this;
    }

    public void EnablePauseMenu()
    {
        PauseMenu.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void DisablePauseMenu()
    {
        PauseMenu.SetActive(false);

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
        return PauseMenu.activeSelf;
    }
}
