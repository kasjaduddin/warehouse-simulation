using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rfid
{
    public class ReaderManager : MonoBehaviour
    {
        private BinTag detectedBinTag;

        public BinTag DetectedBinTag
        {
            get { return detectedBinTag; }
        }

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
                detectedBinTag = other.GetComponent<BinTag>();
            }
        }
    }
}