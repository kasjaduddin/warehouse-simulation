using Newtonsoft.Json.Linq;
using Record;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.LookDev;
using UnityEngine.UI;

namespace CompanySystem
{
    public class ItemListManager : MonoBehaviour
    {
        private JArray items; // Array to store items data
        public GameObject Table;
        public Transform container; // Container to hold the instantiated records
        public GameObject recordTemplate; // Template for displaying each record
        public static ItemRecord selectedRecord; // Variabel to hold selected record data

        public GameObject editPage; // Page to edit selected record data
        public GameObject deleteButton; // Button to delete selected record data

        // Start is called before the first frame update
        void OnEnable()
        {
            // Invoke GetData method after a short delay
            Invoke("GetData", 0.15f);
        }

        private void OnDisable()
        {
            DestroyRecord();
        }

        // Get all item data from Database
        public void GetData()
        {
            StartCoroutine(FirebaseServices.ReadData("items", data =>
            {
                if (data != null)
                {
                    items = data;
                    ShowRecord();
                }
                else
                {
                    recordTemplate.SetActive(false);
                    Debug.LogError("Failed to retrieve data.");
                }
            }));
        }

        // Display all item data to item table
        void ShowRecord()
        {
            float templateHigh = 91f;
            for (int i = 0; i < items.Count; i++)
            {
                GameObject newRow = Instantiate(recordTemplate, container);
                Transform newRowTransform = newRow.transform;
                RectTransform entryRectTransform = newRow.GetComponent<RectTransform>();

                // Fill the UI elements with data
                entryRectTransform.anchoredPosition = new Vector2(0f, 228f + (-templateHigh * i));
                newRowTransform.Find("Id").GetComponent<TextMeshProUGUI>().text = (i + 1).ToString();
                newRowTransform.Find("SKU").GetComponent<TextMeshProUGUI>().text = items[i]["sku"].ToString();
                newRowTransform.Find("Item Name").GetComponent<TextMeshProUGUI>().text = items[i]["item_name"].ToString();
                newRowTransform.Find("Bin Code").GetComponent<TextMeshProUGUI>().text = items[i]["bin_code"].ToString();
                newRowTransform.Find("Quantity").GetComponent<TextMeshProUGUI>().text = items[i]["quantity"].ToString();
                newRowTransform.Find("Number of Tag").GetComponent<TextMeshProUGUI>().text = items[i]["number_of_tags"].ToString();
                newRowTransform.Find("UOM").GetComponent<TextMeshProUGUI>().text = items[i]["uom"].ToString();

                if (i % 2 != 0)
                {
                    newRowTransform.Find("Record Background").GetComponent<Image>().color = new Color32(255, 255, 255, 255);
                }

                Table.GetComponent<DynamicTableManager>().enabled = true;
            }
            recordTemplate.SetActive(false);
        }

        private void GetRecord(Transform recordTransform)
        {
            string sku = recordTransform.Find("SKU").GetComponent<TextMeshProUGUI>().text;

            StartCoroutine(FirebaseServices.ReadData("items", "sku", sku, data =>
            {
                if (data != null)
                {
                    ItemRecord record = new ItemRecord(data);
                    selectedRecord = record;
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                }
            }));
        }

        public void OnEditButtonClick(Transform recordTransform)
        {
            StartCoroutine(OpenEditPage(recordTransform));
        }

        public void OnExpandButtonClick(Transform recordTransform)
        {
            StartCoroutine(ShowDeleteButton(recordTransform));
        }

        private IEnumerator OpenEditPage(Transform recordTransform)
        {
            GetRecord(recordTransform);

            yield return new WaitForSeconds(0.1f);
            gameObject.SetActive(false);
            editPage.SetActive(true);
        }

        public IEnumerator ShowDeleteButton(Transform recordTransform)
        {
            GetRecord(recordTransform);

            yield return new WaitForSeconds(0.1f);
            // Show delete button under expand button
            deleteButton.SetActive(true);
            float recordY = recordTransform.GetComponent<RectTransform>().anchoredPosition.y;
            RectTransform deleteButtonRectTransform = deleteButton.GetComponent<RectTransform>();
            deleteButtonRectTransform.anchoredPosition = new Vector2(deleteButtonRectTransform.anchoredPosition.x, recordY - 46f);
            deleteButton.transform.SetAsLastSibling();
        }

        public static void ResetSelectedRecord()
        {
            ItemRecord emptyItem = new ItemRecord();
            selectedRecord = emptyItem;
        }

        // Delete shows all item data in the item table
        public void DestroyRecord()
        {
            foreach (Transform child in container)
            {
                if (child != recordTemplate.transform && child != deleteButton.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            recordTemplate.SetActive(true);
            deleteButton.SetActive(false);
        }
    }
}