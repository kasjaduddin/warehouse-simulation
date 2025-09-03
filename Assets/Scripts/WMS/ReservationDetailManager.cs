using Record;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Record.ReservationRecord;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

namespace CompanySystem
{
    public class ReservationDetailManager : MonoBehaviour
    {
        public GameObject Table;
        public Transform container; // Container to hold the instantiated records
        public GameObject recordTemplate; // Template for displaying each record
        public static ReservationItem selectedRecord; // Variabel to hold selected record data

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

        // Get all reservation data from Database
        private IEnumerator LoadData()
        {
            StartCoroutine(FirebaseServices.ReadData("reservations", "code", ReservationListManager.selectedRecord.Code, data =>
            {
                if (data != null)
                {
                    ReservationRecord record = new ReservationRecord(data);
                    ReservationListManager.selectedRecord = record;
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                }
            }));

            Transform information = gameObject.transform.Find("Information");
            information.Find("Code").GetComponent<TextMeshProUGUI>().text = ReservationListManager.selectedRecord.Code;
            information.Find("Reservation Date").GetComponent<TextMeshProUGUI>().text = ReservationListManager.selectedRecord.ReservationDate;
            information.Find("Client").GetComponent<TextMeshProUGUI>().text = ReservationListManager.selectedRecord.Client;

            yield return new WaitForSeconds(0.1f);
            StartCoroutine(ShowItems());
        }

        // Display all reservation data to reservation table
        private IEnumerator ShowItems()
        {
            float templateHigh = 91f;
            for (int i = 0; i < ReservationListManager.selectedRecord.Items.Count; i++)
            {
                ReservationItem item = ReservationListManager.selectedRecord.Items[i];
                string stock = null;
                StartCoroutine(FirebaseServices.ReadData("items", "sku", item.Sku, data =>
                {
                    if (data != null)
                    {
                        stock = data["quantity"].ToString();
                    }
                    else
                    {
                        Debug.LogError("Failed to retrieve data.");
                    }
                }));

                yield return new WaitUntil(() => stock != null);
                GameObject newRow = Instantiate(recordTemplate, container);
                Transform newRowTransform = newRow.transform;
                RectTransform entryRectTransform = newRow.GetComponent<RectTransform>();

                // Fill the UI elements with data
                entryRectTransform.anchoredPosition = new Vector2(0f, 60f + (-templateHigh * i));
                newRowTransform.Find("Id").GetComponent<TextMeshProUGUI>().text = (i + 1).ToString();
                newRowTransform.Find("SKU").GetComponent<TextMeshProUGUI>().text = item.Sku;
                newRowTransform.Find("Item Name").GetComponent<TextMeshProUGUI>().text = item.ItemName;
                newRowTransform.Find("Quantity").GetComponent<TextMeshProUGUI>().text = item.Quantity.ToString();
                newRowTransform.Find("Information").GetComponent<TextMeshProUGUI>().text = item.Information;
                newRowTransform.Find("Stock").GetComponent<TextMeshProUGUI>().text = stock;

                if (ReservationListManager.selectedRecord.Items[i].Information.Equals("approved"))
                {
                    newRowTransform.Find("Record Background").GetComponent<Image>().color = new Color32(22, 196, 127, 255);
                    newRowTransform.Find("Buttons").gameObject.SetActive(false);
                }
                else if (ReservationListManager.selectedRecord.Items[i].Information.Equals("rejected"))
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
            string code = ReservationListManager.selectedRecord.Code;
            string sku = recordTransform.Find("SKU").GetComponent<TextMeshProUGUI>().text;

            StartCoroutine(FirebaseServices.ReadData("reservations", "code", code, "items", "sku", sku, data =>
            {
                if (data != null)
                {
                    ReservationItem record = new ReservationItem(data);
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

            approveButton.onClick.AddListener(() => StartCoroutine(ApproveItem(ReservationListManager.selectedRecord.Code, selectedRecord.Sku)));
            rejectButton.onClick.AddListener(() => RejectItem(ReservationListManager.selectedRecord.Code, selectedRecord.Sku));
            deleteButton.onClick.AddListener(() => DeleteItem(ReservationListManager.selectedRecord.Code, selectedRecord.Sku));
        }

        private IEnumerator ApproveItem(string code, string sku)
        {
            bool reservationUpdated = false;

            var newReservationData = new Dictionary<string, object>
            {
                { "sku", selectedRecord.Sku },
                { "information", "approved" }
            };

            ShowPopup();
            warningPanel = popup.transform.Find("Manage Item").gameObject;
            warningPanel.SetActive(true);

            string message = $"Items with sku {sku} will be approved\r\nfrom reservation {code}.\r\nAre you sure?";
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
                StartCoroutine(FirebaseServices.ModifyData("reservations", "code", code, "items", newReservationData, sku, "sku", updateResult =>
                {
                    if (updateResult.Contains("successfully"))
                    {
                        reservationUpdated = true;
                    }
                }));
            });
            trigger.triggers.Add(entry);

            yield return new WaitUntil(() => reservationUpdated);
            if (reservationUpdated)
            {
                HidePopup();
                StartCoroutine(RefreshTable());
            }
        }

        private void RejectItem(string code, string sku)
        {
            var newReservationData = new Dictionary<string, object>
            {
                { "sku", sku },
                { "information", "rejected" }
            };

            ShowPopup();
            warningPanel = popup.transform.Find("Manage Item").gameObject;
            warningPanel.SetActive(true);

            string message = $"Items with sku {sku} will be rejected\r\nfrom reservation {code}.\r\nAre you sure?";
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
                StartCoroutine(FirebaseServices.ModifyData("reservations", "code", code, "items", newReservationData, sku, "sku", updateResult =>
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

            string message = $"Items with sku {sku} will be removed\r\nfrom reservation {code}.\r\nAre you sure?";
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
                StartCoroutine(FirebaseServices.DeleteData("reservations", "code", code, "items", "sku", sku, deleteResult =>
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
            ReservationItem emptyReservation = new ReservationItem();
            selectedRecord = emptyReservation;
        }

        // Delete shows all reservation data in the reservation table
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