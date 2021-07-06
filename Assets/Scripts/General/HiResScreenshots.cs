using UnityEngine;
using System.Collections;
using System.IO;
using System.Drawing;

public class HiResScreenshots : MonoBehaviour
{
    public int resWidth = 1920;
    public int resHeight = 1080;

    private bool takeHiResShot = false;

    public static string ScreenShotName(int width, int height)
    {
        return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

    public void TakeHiResShot()
    {
        takeHiResShot = true;
    }

    void LateUpdate()
    {
        takeHiResShot |= Input.GetKeyDown(KeyCode.F12);
        if (takeHiResShot)
        {
            StartCoroutine("SaveScreenshot");
        }
    }

    IEnumerator SaveScreenshot()
    {
        // We should only read the screen after all rendering is complete
        yield return new WaitForEndOfFrame();
        string filepath = ScreenShotName(Screen.width, Screen.height);
        ScreenCapture.CaptureScreenshot(filepath, 1);
        print("Saved to " + filepath);
        takeHiResShot = false;
    }
}