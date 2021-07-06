using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrabModeSingleton<T> : GrabModeBase where T : GrabModeBase
{
    // Check to see if we're about to be destroyed.
    protected static bool m_ShuttingDown = false;
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
