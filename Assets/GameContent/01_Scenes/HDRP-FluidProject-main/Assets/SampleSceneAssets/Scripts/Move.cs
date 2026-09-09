using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    
        public float speed = 5f;

        void Update()
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    
}
