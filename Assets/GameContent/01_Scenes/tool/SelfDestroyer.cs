using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestroyer : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 2.5f);
    }
}