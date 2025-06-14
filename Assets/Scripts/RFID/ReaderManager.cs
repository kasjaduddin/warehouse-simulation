using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rfid
{
    public class ReaderManager : MonoBehaviour
    {
        void Start()
        {

        }

        void Update()
        {

        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("BinTag"))
            {
                Debug.Log("RFID Tag Detected: " + other.name);
            }
        }
    }
}