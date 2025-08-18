using CompanySystem;
using Record;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Record.TransactionRecord;

namespace Rfid
{
    public class ItemTagRegistrationManager : MonoBehaviour
    {
        [SerializeField]
        private ReaderManager rfidReader;
        [SerializeField]
        private GameObject popup;
        private GameObject itemTagPopup;

        private TMP_Dropdown transactionCodeDropdown;

        private GameObject table;
        private Transform container;
        private GameObject recordTemplate;
        private TransactionRecord transactionRecord;

        private ItemTag tag;
        private void OnEnable()
        {
            transactionCodeDropdown = transform.Find("Transaction Code Dropdown").GetComponent<TMP_Dropdown>();
            table = transform.Find("Item List").gameObject;
            container = table.transform.Find("Table Container");
            recordTemplate = container.Find("Record Template").gameObject;
            Invoke("GetTransactionCodes", 0.1f);

            itemTagPopup = popup.transform.Find("Item Tag").gameObject;
        }

        private void OnDisable()
        {
            transactionCodeDropdown.options.Clear();
            DestroyRecord();
        }

        public void GetTransactionCodes()
        {
            StartCoroutine(FirebaseServices.ReadData("transactions", data =>
            {
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        transactionCodeDropdown.options.Add(new TMP_Dropdown.OptionData(item["code"].ToString()));
                    }
                    transactionCodeDropdown.captionText.text = transactionCodeDropdown.options[0].text;
                    Invoke("LoadTransactionData", 0.1f);
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                }
            }));
        }

        public void LoadTransactionData()
        {
            DestroyRecord();
            StartCoroutine(FirebaseServices.ReadData("transactions", "code", transactionCodeDropdown.captionText.text, data =>
            {
                if (data != null)
                {
                    TransactionRecord record = new TransactionRecord(data);
                    transactionRecord = record;
                    ShowItems();
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                }
            }));
        }

        public void ReadTag()
        {
            tag = rfidReader.DetectedItemTag;
            if (!tag)
            {
                popup.SetActive(true);
                itemTagPopup.SetActive(true);
                itemTagPopup.transform.Find("Tag Not Found").gameObject.SetActive(true);
            }
            else
            {
                if (tag.Sku != string.Empty)
                {
                    popup.SetActive(true);
                    itemTagPopup.SetActive(true);

                    Transform informationPopup = itemTagPopup.transform.Find("Tag Information");
                    informationPopup.gameObject.SetActive(true);
                    informationPopup.Find("Text").GetComponent<TextMeshProUGUI>().text = $"Tag has been registered to Bin {tag.Sku}";
                }
                else
                {
                    popup.SetActive(true);
                    itemTagPopup.SetActive(true);
                    itemTagPopup.transform.Find("Tag Not Registered").gameObject.SetActive(true);
                }
            }
        }

        public void SelectItem(TextMeshProUGUI sku)
        {
            ResetInformation();

            Transform button = sku.transform.parent;
            button.GetComponent<Image>().color = new Color32(4, 83, 221, 255);

            StartCoroutine(FirebaseServices.ReadData("transactions", "code", transactionCodeDropdown.captionText.text, "items", "sku", sku.text, data =>
            {
                if (data != null)
                {
                    TransactionItem record = new TransactionItem(data);
                    ShowItemDetail(sku.text, record.Quantity.ToString());
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                }
            }));
        }

        private void ShowItems()
        {
            float templateHigh = 32f;
            int i = 0;
            for (int j = 0; j < transactionRecord.Items.Count; j++)
            {
                TransactionItem item = transactionRecord.Items[j];

                // Fill the UI elements with data
                if (item.Information.Equals("approved") && !item.Tagged)
                {
                    GameObject newRow = Instantiate(recordTemplate, container);
                    Transform newRowTransform = newRow.transform;
                    RectTransform entryRectTransform = newRow.GetComponent<RectTransform>();

                    entryRectTransform.anchoredPosition = new Vector2(0f, 46f + (-templateHigh * i));
                    newRowTransform.Find("Button").Find("SKU").GetComponent<TextMeshProUGUI>().text = item.Sku;
                    i++;
                }

                table.GetComponent<DynamicTableManager>().enabled = true;
            }
            recordTemplate.SetActive(false);
        }

        private void ShowItemDetail(string sku, string quantity)
        {
            StartCoroutine(FirebaseServices.ReadData("items", "sku", sku, data =>
            {
                if (data != null)
                {
                    Transform itemInformation = transform.Find("Item Information");
                    itemInformation.Find("SKU").GetComponent<TextMeshProUGUI>().text = data["sku"].ToString();
                    itemInformation.Find("Item Name").GetComponent<TextMeshProUGUI>().text = data["item_name"].ToString();
                    itemInformation.Find("Bin Code").GetComponent<TextMeshProUGUI>().text = data["bin_code"].ToString();
                    itemInformation.Find("Stock").GetComponent<TextMeshProUGUI>().text = data["quantity"].ToString();
                    itemInformation.Find("Quantity").GetComponent<TextMeshProUGUI>().text = quantity;
                    itemInformation.Find("Tags").GetComponent<TextMeshProUGUI>().text = data["number_of_tags"].ToString();
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                }
            }));
        }

        // Delete shows all item data in the item table
        private void DestroyRecord()
        {
            ResetInformation();
            foreach (Transform child in container)
            {
                if (child != recordTemplate.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            recordTemplate.SetActive(true);
        }

        private void ResetInformation()
        {
            foreach (Transform child in container)
            {
                child.Find("Button").GetComponent<Image>().color = new Color32(255, 255, 255, 0);
            }

            foreach (Transform child in transform.Find("Item Information"))
            {
                child.GetComponent<TextMeshProUGUI>().text = "";
            }
        }
    }
}