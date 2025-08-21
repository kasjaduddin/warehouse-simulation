using UnityEngine;

namespace Rfid
{
    public class ReaderManager : MonoBehaviour
    {
        private BinTag detectedBinTag;
        private ItemTag detectedItemTag;

        public BinTag DetectedBinTag
        {
            get { return detectedBinTag; }
        }

        public ItemTag DetectedItemTag
        {
            get { return detectedItemTag; }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("BinTag"))
            {
                detectedBinTag = other.GetComponent<BinTag>();
            }
            
            if (other.CompareTag("ItemTag"))
            {
                detectedItemTag = other.GetComponent<ItemTag>();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("BinTag"))
            {
                detectedBinTag = null;
            }

            if (other.CompareTag("ItemTag"))
            {
                detectedItemTag = null;
            }
        }
    }
}