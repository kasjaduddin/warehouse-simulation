using Record;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Record.TransactionRecord;

namespace CompanySystem
{
    public class TransactionDetailManager : MonoBehaviour
    {
        public GameObject Table;
        public Transform container; // Container to hold the instantiated records
        public GameObject recordTemplate; // Template for displaying each record
        public static TransactionItem selectedRecord; // Variabel to hold selected record data

        public GameObject editPage; // Page to edit selected record data
        public GameObject optionButtons;

        public GameObject popup;
        private GameObject warningPanel;

        // Start is called before the first frame update
        void OnEnable()
        {
            // Invoke GetData method after a short delay
            StartCoroutine(LoadData());
        }

        private void OnDisable()
        {
            DestroyRecord();
        }

        // Get all transaction data from Database
        private IEnumerator LoadData()
        {
            StartCoroutine(FirebaseServices.ReadData("transactions", "code", TransactionListManager.selectedRecord.Code, data =>
            {
                if (data != null)
                {
                    TransactionRecord record = new TransactionRecord(data);
                    TransactionListManager.selectedRecord = record;
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                }
            }));

            Transform information = gameObject.transform.Find("Information");
            information.Find("Code").GetComponent<TextMeshProUGUI>().text = TransactionListManager.selectedRecord.Code;
            information.Find("Invoice Number").GetComponent<TextMeshProUGUI>().text = TransactionListManager.selectedRecord.InvoiceNumber;
            information.Find("Invoice Date").GetComponent<TextMeshProUGUI>().text = TransactionListManager.selectedRecord.InvoiceDate;
            information.Find("Vendor").GetComponent<TextMeshProUGUI>().text = TransactionListManager.selectedRecord.Vendor;

            yield return new WaitForSeconds(0.1f);
            ShowItems();
        }

        // Display all transaction data to transaction table
        void ShowItems()
        {
            float templateHigh = 91f;
            for (int i = 0; i < TransactionListManager.selectedRecord.Items.Count; i++)
            {
                TransactionItem item = TransactionListManager.selectedRecord.Items[i];

                GameObject newRow = Instantiate(recordTemplate, container);
                Transform newRowTransform = newRow.transform;
                RectTransform entryRectTransform = newRow.GetComponent<RectTransform>();

                // Fill the UI elements with data
                entryRectTransform.anchoredPosition = new Vector2(0f, 46f + (-templateHigh * i));
                newRowTransform.Find("Id").GetComponent<TextMeshProUGUI>().text = (i + 1).ToString();
                newRowTransform.Find("SKU").GetComponent<TextMeshProUGUI>().text = item.Sku;
                newRowTransform.Find("Item Name").GetComponent<TextMeshProUGUI>().text = item.ItemName;
                newRowTransform.Find("Quantity").GetComponent<TextMeshProUGUI>().text = item.Quantity.ToString();
                newRowTransform.Find("Information").GetComponent<TextMeshProUGUI>().text = item.Information;

                if (TransactionListManager.selectedRecord.Items[i].Information.Equals("approved") || TransactionListManager.selectedRecord.Items[i].Information.Equals("packed"))
                {
                    newRowTransform.Find("Record Background").GetComponent<Image>().color = new Color32(22, 196, 127, 255);
                    newRowTransform.Find("Buttons").gameObject.SetActive(false);
                }
                else if (TransactionListManager.selectedRecord.Items[i].Information.Equals("rejected"))
                {
                    newRowTransform.Find("Record Background").GetComponent<Image>().color = new Color32(255, 41, 41, 255);
                    newRowTransform.Find("Buttons").gameObject.SetActive(false);
                }
                else if (i % 2 != 0)
                {
                    newRowTransform.Find("Record Background").GetComponent<Image>().color = new Color32(255, 255, 255, 255);
                }

                Table.GetComponent<DynamicTableManager>().enabled = true;
            }
            recordTemplate.SetActive(false);
        }

        private void GetRecord(Transform recordTransform)
        {
            string code = TransactionListManager.selectedRecord.Code;
            string sku = recordTransform.Find("SKU").GetComponent<TextMeshProUGUI>().text;

            StartCoroutine(FirebaseServices.ReadData("transactions", "code", code, "items", "sku", sku, data =>
            {
                if (data != null)
                {
                    TransactionItem record = new TransactionItem(data);
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
            StartCoroutine(ShowOptionButtons(recordTransform));
        }

        private IEnumerator OpenEditPage(Transform recordTransform)
        {
            GetRecord(recordTransform);

            yield return new WaitForSeconds(0.1f);
            gameObject.SetActive(false);
            editPage.SetActive(true);
        }

        public IEnumerator ShowOptionButtons(Transform recordTransform)
        {
            GetRecord(recordTransform);

            yield return new WaitForSeconds(0.1f);
            // Show option button under expand button
            optionButtons.SetActive(true);
            float recordY = recordTransform.GetComponent<RectTransform>().anchoredPosition.y;
            RectTransform optionButtonRectTransform = optionButtons.GetComponent<RectTransform>();
            optionButtonRectTransform.anchoredPosition = new Vector2(optionButtonRectTransform.anchoredPosition.x, recordY - 90f);
            optionButtons.transform.SetAsLastSibling();

            Button approveButton = optionButtons.transform.Find("Approve Button").GetComponent<Button>();
            Button rejectButton = optionButtons.transform.Find("Reject Button").GetComponent<Button>();
            Button deleteButton = optionButtons.transform.Find("Delete Button").GetComponent<Button>();

            approveButton.onClick.AddListener(() => StartCoroutine(ApproveItem(TransactionListManager.selectedRecord.Code, selectedRecord.Sku)));
            rejectButton.onClick.AddListener(() => RejectItem(TransactionListManager.selectedRecord.Code, selectedRecord.Sku));
            deleteButton.onClick.AddListener(() => DeleteItem(TransactionListManager.selectedRecord.Code, selectedRecord.Sku));
        }

        private IEnumerator ApproveItem(string code, string sku)
        {
            bool transactionUpdated = false;
            bool itemUpdated = false;
            int newQuantity = 0;

            StartCoroutine(FirebaseServices.ReadData("items", "sku", sku, data =>
            {
                if (data != null)
                {
                    newQuantity = int.Parse(data["quantity"].ToString()) + selectedRecord.Quantity; Debug.Log($"{data["quantity"]}+{selectedRecord.Quantity}={newQuantity}");
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                }
            }));

            var newItemData = new Dictionary<string, object>
            {
                { "sku", selectedRecord.Sku },
                { "information", "approved" }
            };
            yield return new WaitUntil(() => newQuantity != 0);
            var newItemQuantity = new Dictionary<string, object>
            {
                { "sku", selectedRecord.Sku },
                { "quantity", newQuantity }
            };

            ShowPopup();
            warningPanel = popup.transform.Find("Manage Item").gameObject;
            warningPanel.SetActive(true);

            string message = $"Items with sku {sku} will be approved\r\nfrom transaction {code}.\r\nAre you sure?";
            TextMeshProUGUI warningText = warningPanel.GetComponentInChildren<TextMeshProUGUI>();
            warningText.text = message;

            GameObject replaceItemButton = warningPanel.transform.Find("Buttons").Find("Yes Button").gameObject;

            // Add event trigger to replace bin button
            EventTrigger trigger = replaceItemButton.GetComponent<EventTrigger>() ?? replaceItemButton.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            // Create entry for click/ pointer down event
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };

            entry.callback.AddListener((eventData) =>
            {
                StartCoroutine(FirebaseServices.ModifyData("transactions", "code", code, "items", newItemData, sku, "sku", updateResult =>
                {
                    if (updateResult.Contains("successfully"))
                    {
                        transactionUpdated = true;
                    }
                }));
                StartCoroutine(FirebaseServices.ModifyData("items", newItemQuantity, sku, "sku", updateResult =>
                {
                    if (updateResult.Contains("successfully"))
                    {
                        itemUpdated = true;
                    }
                }));
            });
            trigger.triggers.Add(entry);

            yield return new WaitUntil(() => transactionUpdated && itemUpdated);
            if (transactionUpdated && itemUpdated)
            {
                HidePopup();
                StartCoroutine(RefreshTable());
            }
        }

        private void RejectItem(string code, string sku)
        {
            var newItemData = new Dictionary<string, object>
            {
                { "sku", sku },
                { "information", "rejected" }
            };

            ShowPopup();
            warningPanel = popup.transform.Find("Manage Item").gameObject;
            warningPanel.SetActive(true);

            string message = $"Items with sku {sku} will be rejected\r\nfrom transaction {code}.\r\nAre you sure?";
            TextMeshProUGUI warningText = warningPanel.GetComponentInChildren<TextMeshProUGUI>();
            warningText.text = message;

            GameObject replaceItemButton = warningPanel.transform.Find("Buttons").Find("Yes Button").gameObject;

            // Add event trigger to replace bin button
            EventTrigger trigger = replaceItemButton.GetComponent<EventTrigger>() ?? replaceItemButton.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            // Create entry for click/ pointer down event
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };

            entry.callback.AddListener((eventData) =>
            {
                StartCoroutine(FirebaseServices.ModifyData("transactions", "code", code, "items", newItemData, sku, "sku", updateResult =>
                {
                    if (updateResult.Contains("successfully"))
                    {
                        HidePopup();
                        StartCoroutine(RefreshTable());
                    }
                }));
            });
            trigger.triggers.Add(entry);
        }
        private void DeleteItem(string code, string sku)
        {
            ShowPopup();
            warningPanel = popup.transform.Find("Manage Item").gameObject;
            warningPanel.SetActive(true);

            string message = $"Items with sku {sku} will be removed\r\nfrom transaction {code}.\r\nAre you sure?";
            TextMeshProUGUI warningText = warningPanel.GetComponentInChildren<TextMeshProUGUI>();
            warningText.text = message;

            GameObject replaceItemButton = warningPanel.transform.Find("Buttons").Find("Yes Button").gameObject;

            // Add event trigger to replace bin button
            EventTrigger trigger = replaceItemButton.GetComponent<EventTrigger>() ?? replaceItemButton.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            // Create entry for click/ pointer down event
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };

            entry.callback.AddListener((eventData) =>
            {
                StartCoroutine(FirebaseServices.DeleteData("transactions", "code", code, "items", "sku", sku, deleteResult => 
                {
                    if (deleteResult.Contains("successfully"))
                    {
                        HidePopup();
                        StartCoroutine(RefreshTable());
                    }
                }));
            });
            trigger.triggers.Add(entry);
        }

        private IEnumerator RefreshTable()
        {
            yield return new WaitForSeconds(0.1f);
            gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        private void ShowPopup()
        {
            popup.SetActive(true);
        }

        private void HidePopup()
        {
            if (warningPanel != null && warningPanel.activeSelf)
            {
                warningPanel.transform.parent.gameObject.SetActive(false);
                warningPanel.SetActive(false);
            }
        }

        public static void ResetSelectedRecord()
        {
            TransactionItem emptyTransaction = new TransactionItem();
            selectedRecord = emptyTransaction;
        }

        // Delete shows all transaction data in the transaction table
        public void DestroyRecord()
        {
            foreach (Transform child in container)
            {
                if (child != recordTemplate.transform && child != optionButtons.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            recordTemplate.SetActive(true);
            optionButtons.SetActive(false);
        }
    }

}