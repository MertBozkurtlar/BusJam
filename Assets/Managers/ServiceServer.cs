using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ServiceServer : Singleton<ServiceServer>
{
    public GameManager gameManager;
    protected override void Init() 
    {
        gameManager = this.AddComponent<GameManager>();
    }
    
    protected override void Destroy()
    {
        Destroy(gameManager);
    }
}
