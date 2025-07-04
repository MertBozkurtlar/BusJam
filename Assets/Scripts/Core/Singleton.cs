using UnityEngine;

/// <summary>
/// Singleton abstract class
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if(instance == null)
                Debug.LogError("No instance of " + typeof(T) + " exists in the scene.");

            return instance;
        }
    }

    // Create the reference in Awake()
    protected void Awake()
    {
        if(instance != null)
        {
            Debug.LogWarning("An instance of " + typeof(T) + " already exists. Self-destructing.");
            // First disable this component to prevent any further execution
            enabled = false;
            // Then destroy the entire GameObject
            Destroy(gameObject);
            return;
        }
        
        instance = this as T;
        Init();
    }

    // Destroy the reference in OnDestroy()
    protected void OnDestroy()
    {
        if(this == instance)
        {
            instance = null;
        }
        Destroy();
    }

    // Init will replace the functionality of Awake()
    protected virtual void Init(){}

    // Destroy will replace the functionality of OnDestroy()
    protected virtual void Destroy(){}
}