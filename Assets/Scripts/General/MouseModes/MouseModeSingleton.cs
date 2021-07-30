using UnityEngine;
using System;


public class MouseMode : MonoBehaviour
{
    protected bool isActive = false;
    public bool IsActive { get { return isActive; } }
    protected bool CanUpdate()
    {
        return MouseModeManager.Instance.CanUpdateMode() && isActive;
    }
    public virtual void OnModeEnter()
    {
        throw new NotImplementedException(string.Format("{0} has not implemented OnModeEnter method.", name));
    }

    public virtual void OnModeExit()
    {
        throw new NotImplementedException(string.Format("{0} has not implemented OnModeExit method.", name));
    }
}

/// <summary>
/// Inherit from this base class to create a singleton.
/// e.g. public class MyClassName : Singleton<MyClassName> {}
/// http://wiki.unity3d.com/index.php/Singleton
/// </summary>
public class MouseModeSingleton<T> : MouseMode where T : MouseMode
{
    // Check to see if we're about to be destroyed.
    private static bool m_ShuttingDown = false;
    protected static object m_Lock = new object();
    protected static T m_Instance;

    public static T Instance
    {
        get
        {
            if (m_ShuttingDown)
            {
                Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                    "' already destroyed. Returning null.");
                return null;
            }

            lock (m_Lock)
            {
                // if (m_Instance == null)
                // {
                //     // Search for existing instance.
                //     m_Instance = (T)FindObjectOfType(typeof(T));
                //     DontDestroyOnLoad(m_Instance);

                //     if (m_Instance == null)
                //     {
                //         Debug.LogWarning("No object created with " + typeof(T).ToString());
                //     }
                // }

                return m_Instance;
            }
        }
    }

    private void OnApplicationQuit()
    {
        m_ShuttingDown = true;
    }


    private void OnDestroy()
    {
        m_ShuttingDown = true;
    }

}