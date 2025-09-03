using UnityEngine;
using Newtonsoft.Json.Linq;
using TMPro;
using Record;
using System;
using UnityEngine.UI;
using System.Collections;


namespace CompanySystem
{
    public class BinListManager : MonoBehaviour
    {
        private JArray bins; // Array to store bins data
        public GameObject Table;
        public Transform container; // Container to hold the instantiated records
        public GameObject recordTemplate; // Template for displaying each record
        public static BinRecord selectedRecord; // Variabel to hold selected record data

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

        // Get all bin data from Google Sheets
        public void GetData()
        {
            StartCoroutine(FirebaseServices.ReadData("bins", data =>
            {
                if (data != null)
                {
                    bins = data;
                    ShowRecord();
                }
                else
                {
                    recordTemplate.SetActive(false);
                    Debug.LogError("Failed to retrieve data.");
                }
            }));
        }

        // Display all bin data to bin table
        void ShowRecord()
        {
            float templateHigh = 91f;
            for (int i = 0; i < bins.Count; i++)
            {
                GameObject newRow = Instantiate(recordTemplate, container);
                Transform newRowTransform = newRow.transform;
                RectTransform entryRectTransform = newRow.GetComponent<RectTransform>();

                // Fill the UI elements with data
                entryRectTransform.anchoredPosition = new Vector2(0f, 228f + (-templateHigh * i));
                newRowTransform.Find("Id").GetComponent<TextMeshProUGUI>().text = (i + 1).ToString();
                newRowTransform.Find("Code").GetComponent<TextMeshProUGUI>().text = bins[i]["code"].ToString();
                newRowTransform.Find("Information").GetComponent<TextMeshProUGUI>().text = bins[i]["information"].ToString();
                newRowTransform.Find("Number of Tag").GetComponent<TextMeshProUGUI>().text = bins[i]["number_of_tags"].ToString();

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
            string code = recordTransform.Find("Code").GetComponent<TextMeshProUGUI>().text;

            StartCoroutine(FirebaseServices.ReadData("bins", "code", code, data =>
            {
                if (data != null)
                {
                    BinRecord record = new BinRecord(data);
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
            BinRecord emptyBin = new BinRecord();
            selectedRecord = emptyBin;
        }

        // Delete shows all bin data in the bin table
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