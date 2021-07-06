using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

public class Paparazzi : MonoBehaviour
{
    public static string destinationFolder = @"C:\Users\gcid\Desktop\crisis_simulation_thesis\Training Snapshots\";
    [SerializeField]
    bool takeScreenshots = true;
    [SerializeField]
    float screenshotsCooldownSeconds = 300f;

    [SerializeField] Camera mainCamera;
    Vector3 defaultPosition;
    Quaternion defaultRotation;
    List<Camera> availableCameras;

    public void PrepareAndStart()
    {
        if (!takeScreenshots)
            return;
        
        destinationFolder = Path.Combine(destinationFolder, System.DateTime.Now.ToString("HH-mm")) + "\\";
        System.IO.Directory.CreateDirectory(destinationFolder);
        defaultPosition = mainCamera.transform.position;
        defaultRotation = mainCamera.transform.rotation;
        availableCameras = GetComponentsInChildren<Building>().Select(b => b.GetComponentInChildren<Camera>()).ToList();
        availableCameras.ForEach(c => c.name = c.transform.parent.name);
        availableCameras.ForEach(c => c.gameObject.SetActive(false));
        StartCoroutine("TakeScreenshots");
    }
    IEnumerator TakeScreenshots()
    {
        yield return new WaitForSecondsRealtime(2f);
        while (true)
        {
            FindObjectOfType<Canvas>().enabled = false;
            mainCamera.GetComponentInParent<CameraMovement>().enabled = false;
            defaultPosition = mainCamera.transform.position;
            defaultRotation = mainCamera.transform.rotation;
            foreach (Camera camera in availableCameras)
            {
                mainCamera.transform.position = camera.transform.position;
                mainCamera.transform.rotation = camera.transform.rotation;
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                string dateString = System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm");
                string screenshotName = string.Format("{0}_Camera_{1}.png", dateString, camera.name);
                string outputPath = Path.Combine(destinationFolder, screenshotName);
                ScreenCapture.CaptureScreenshot(outputPath);

            }
            mainCamera.transform.position = defaultPosition;
            mainCamera.transform.rotation = defaultRotation;
            Camera.main.GetComponentInParent<CameraMovement>().enabled = true;
            FindObjectOfType<Canvas>().enabled = true;
            yield return new WaitForSecondsRealtime(screenshotsCooldownSeconds);
        }
    }
}
