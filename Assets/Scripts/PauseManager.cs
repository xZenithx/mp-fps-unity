using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject PauseMenu;

    void Start()
    {
        PauseMenu = GameObject.FindGameObjectWithTag("Pause Menu");

        EnablePauseMenu();
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
