using CompanySystem;
using Newtonsoft.Json.Linq;
using Record;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rfid
{
    public class RemoveItemTagManager : MonoBehaviour
    {
        [SerializeField]
        private ReaderManager rfidReader;
        [SerializeField]
        private GameObject popup;
        private GameObject itemTagPopup;
        private TextMeshProUGUI selectedReservation;

        bool checking = false; // Flag to indicate if the system is currently checking for tags
        bool isCheckingInProgress = false; // Flag to prevent multiple checks from being initiated simultaneously
        bool packing = false; // Flag to indicate if the system is currently packing items
        bool isPackingInProgress = false; // Flag to prevent multiple packing operations from being initiated simultaneously

        private JArray reservations; // Array to store reservations data
        private GameObject reservationTable;
        private Transform reservationContainer; // Container to hold the instantiated records
        private GameObject reservationRecordTemplate; // Template for displaying each record
        private ReservationRecord reservationRecord;

        private GameObject itemTable;
        private Transform itemContainer; // Container to hold the instantiated records
        private GameObject itemRecordTemplate; // Template for displaying each record

        private GameObject tagInformation;

        private new ItemTag tag;

        private string pendingTransactionCode;
        private string pendingSku;
        private int pendingTransactionQty;

        private void OnEnable()
        {
            reservationTable = transform.Find("Reservation List").gameObject;
            reservationContainer = reservationTable.transform.Find("Table Container");
            reservationRecordTemplate = reservationContainer.Find("Record Template").gameObject;

            itemTable = transform.Find("Item List").gameObject;
            itemContainer = itemTable.transform.Find("Table Container");
            itemRecordTemplate = itemContainer.Find("Record Template").gameObject;

            tagInformation = transform.Find("Tag Information").gameObject;

            Invoke("LoadReservationData", 0.1f);

            itemTagPopup = popup.transform.Find("Item Tag").gameObject;
        }

        private void OnDisable()
        {
            DestroyReservationRecord();
            DestroyItemRecord();
            ClearTagInformation();
        }

        private void Update()
        {
            if (checking && !isCheckingInProgress)
            {
                isCheckingInProgress = true;
                StartCoroutine(CheckingTag());
            }

            if (packing && !isPackingInProgress)
            {
                isPackingInProgress = true;
                StartCoroutine(PackingItem());
            }
        }

        public void LoadReservationData()
        {
            DestroyReservationRecord();
            StartCoroutine(FirebaseServices.ReadData("reservations", data =>
            {
                if (data != null)
                {
                    reservations = data;
                    StartCoroutine(ShowReservation());
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                }
            }));
        }

        public void SelectReservation(TextMeshProUGUI code)
        {
            UnselectRecord();
            selectedReservation = code;
            code.transform.parent.GetComponent<Image>().color = new Color32(4, 83, 221, 255);
            StartCoroutine(GetItems(code.text));
        }

        public void StartPacking()
        {
            packing = true;
            isPackingInProgress = false;
        }

        public void StopPacking()
        {
            packing = false;
        }

        public void StartChecking()
        {
            checking = true;
            isCheckingInProgress = false;
        }

        public void StopChecking()
        {
            checking = false;
            ClearTagInformation();
        }

        private IEnumerator PackingItem()
        {
            tag = rfidReader.DetectedItemTag;

            if (tag == null)
            {
                popup.SetActive(true);
                itemTagPopup.SetActive(true);
                itemTagPopup.transform.Find("Tag Not Found").gameObject.SetActive(true);
                isPackingInProgress = false;
                yield break;
            }

            if (string.IsNullOrEmpty(tag.Sku))
            {
                popup.SetActive(true);
                itemTagPopup.SetActive(true);
                itemTagPopup.transform.Find("Tag Not Registered").gameObject.SetActive(true);
                isPackingInProgress = false;
                yield break;
            }

            yield return CheckItemAvailability(tag.TransactionCode, tag.Sku);
            isPackingInProgress = false;
        }

        private IEnumerator CheckItemAvailability(string transactionCode, string sku)
        {
            int transactionQty = -1;
            int itemQty = -1;
            bool? successTransaction = null;
            bool? successItem = null;

            yield return StartCoroutine(FirebaseServices.ReadData("transactions", "code", transactionCode, "items", "sku", sku, data =>
            {
                if (data != null)
                {
                    transactionQty = int.Parse(data["quantity"].ToString());
                    successTransaction = true;
                }
                else
                {
                    successTransaction = false;
                }
            }));

            yield return StartCoroutine(FirebaseServices.ReadData("items", "sku", sku, data =>
            {
                if (data != null)
                {
                    itemQty = int.Parse(data["quantity"].ToString());
                    successItem = true;
                }
                else
                {
                    successItem = false;
                }
            }));

            yield return new WaitUntil(() => successTransaction.HasValue && successItem.HasValue);

            if (successTransaction.Value && successItem.Value && itemQty >= transactionQty)
            {
                pendingTransactionCode = transactionCode;
                pendingSku = sku;
                pendingTransactionQty = transactionQty;

                popup.SetActive(true);
                itemTagPopup.SetActive(true);
                Transform readyPopup = itemTagPopup.transform.Find("Ready for Packing");
                readyPopup.gameObject.SetActive(true);

                TextMeshProUGUI confirmationText = readyPopup.Find("Text").GetComponent<TextMeshProUGUI>();
                confirmationText.text = $"Are you sure you want to picking item {sku}?";

                Button yesButton = readyPopup.Find("Buttons/Yes Button").GetComponent<Button>();
                yesButton.onClick.RemoveAllListeners();
                yesButton.onClick.AddListener(OnConfirmPacking);
            }
            else
            {
                popup.SetActive(true);
                itemTagPopup.SetActive(true);
                itemTagPopup.transform.Find("Insufficient Quantity").gameObject.SetActive(true);
            }
        }

        private void OnConfirmPacking()
        {
            StartCoroutine(PackingItem(pendingTransactionCode, pendingSku, pendingTransactionQty));

            Transform readyPopup = itemTagPopup.transform.Find("Ready for Packing");
            readyPopup.gameObject.SetActive(false);
        }

        private IEnumerator PackingItem(string transactionCode, string sku, int transactionQty)
        {
            bool? successItemUpdate = null;
            bool? successTransactionUpdate = null;
            bool? successReservationUpdate = null;
            bool? successTagDelete = null;

            // 1. Update quantity di items
            yield return StartCoroutine(FirebaseServices.ReadData("items", "sku", sku, data =>
            {
                if (data != null)
                {
                    int currentQty = int.Parse(data["quantity"].ToString());
                    int newQty = currentQty - transactionQty;

                    var updatedData = new Dictionary<string, object>
                    {
                        { "quantity", newQty },
                        { "sku", sku }
                    };

                    StartCoroutine(FirebaseServices.ModifyData("items", updatedData, sku, "sku", msg =>
                    {
                        successItemUpdate = msg.Contains("successfully");
                    }));
                }
                else
                {
                    successItemUpdate = false;
                }
            }));

            // 2. Tandai item di transaksi sebagai packed
            var packedData = new Dictionary<string, object>
            {
                { "information", "packed" },
                { "sku", sku }
            };
            yield return StartCoroutine(FirebaseServices.ModifyData("transactions", "code", transactionCode, "items", packedData, sku, "sku", msg =>
            {
                successTransactionUpdate = msg.Contains("successfully");
            }));

            // 3. Update quantity dan packed status di reservation
            yield return StartCoroutine(FirebaseServices.ReadData("reservations", "code", selectedReservation.text, "items", "sku", sku, data =>
            {
                if (data != null)
                {
                    int currentQty = int.Parse(data["quantity"].ToString());
                    int newQty = currentQty - transactionQty;

                    var updatedData = new Dictionary<string, object>
                    {
                        { "quantity", newQty },
                        { "sku", sku }
                    };

                    if (newQty == 0)
                    {
                        updatedData.Add("packed", true);
                    }

                    StartCoroutine(FirebaseServices.ModifyData("reservations", "code", selectedReservation.text, "items", updatedData, sku, "sku", msg =>
                    {
                        successReservationUpdate = msg.Contains("successfully");
                    }));
                }
                else
                {
                    successReservationUpdate = false;
                }
            }));

            yield return StartCoroutine(FirebaseServices.DeleteData("rfid/item_tags", "id", tag.Id, msg =>
            {
                successTagDelete = msg.Contains("successfully");
            }));

            // 5. Tunggu semua operasi selesai
            yield return new WaitUntil(() =>
                successItemUpdate.HasValue &&
                successTransactionUpdate.HasValue &&
                successReservationUpdate.HasValue &&
                successTagDelete.HasValue
            );

            if (successItemUpdate.Value && successTransactionUpdate.Value && successReservationUpdate.Value && successTagDelete.Value)
            {
                StartCoroutine(GetItems(selectedReservation.text));
            }
            else
            {
                popup.SetActive(true);
                itemTagPopup.SetActive(true);
                itemTagPopup.transform.Find("Error Packing Item").gameObject.SetActive(true);
            }
        }


        private IEnumerator CheckingTag()
        {
            tag = rfidReader.DetectedItemTag;
            if (!tag)
            {
                ClearTagInformation();
            }
            else
            {
                if (tag.Sku != string.Empty)
                {
                    yield return StartCoroutine(ShowTagInformation(tag.TransactionCode, tag.Sku));
                }
                else
                {
                    popup.SetActive(true);
                    itemTagPopup.SetActive(true);
                    itemTagPopup.transform.Find("Tag Not Registered").gameObject.SetActive(true);
                }
            }

            isCheckingInProgress = false;
        }

        private IEnumerator ShowTagInformation(string transactionCode, string sku)
        {
            bool? successReadTransaction = null;
            bool? successReadItem = null;

            string binCode = string.Empty;
            string itemName = string.Empty;
            string quantity = string.Empty;

            yield return StartCoroutine(FirebaseServices.ReadData("transactions", "code", transactionCode, "items", "sku", sku, data =>
            {
                if (data != null)
                {
                    itemName = data["item_name"].ToString();
                    quantity = data["quantity"].ToString();

                    successReadTransaction = true;
                }
                else
                {
                    successReadTransaction = false;
                }
            }));

            yield return StartCoroutine(FirebaseServices.ReadData("items", "sku", sku, data =>
            {
                if (data != null)
                {
                    binCode = data["bin_code"].ToString();

                    successReadItem = true;
                }
                else
                {
                    successReadItem = false;
                }
            }));

            yield return new WaitUntil(() => successReadTransaction.HasValue && successReadItem.HasValue);
            if (successReadTransaction.Value && successReadItem.Value)
            {
                SetTextIfChanged(tagInformation.transform, "Bin Code", binCode);
                SetTextIfChanged(tagInformation.transform, "SKU", sku);
                SetTextIfChanged(tagInformation.transform, "Name", itemName);
                SetTextIfChanged(tagInformation.transform, "Quantity", quantity);
            }
            else
            {
                SetTextIfChanged(tagInformation.transform, "Bin Code", string.Empty);
                SetTextIfChanged(tagInformation.transform, "SKU", string.Empty);
                SetTextIfChanged(tagInformation.transform, "Name", string.Empty);
                SetTextIfChanged(tagInformation.transform, "Quantity", string.Empty);
            }
        }

        private void ClearTagInformation()
        {
            tagInformation.transform.Find("Bin Code").GetComponent<TextMeshProUGUI>().text = string.Empty;
            tagInformation.transform.Find("SKU").GetComponent<TextMeshProUGUI>().text = string.Empty;
            tagInformation.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = string.Empty;
            tagInformation.transform.Find("Quantity").GetComponent<TextMeshProUGUI>().text = string.Empty;
        }

        private void SetTextIfChanged(Transform parent, string childName, string newValue)
        {
            var textComponent = parent.Find(childName).GetComponent<TextMeshProUGUI>();
            if (textComponent.text != newValue)
            {
                textComponent.text = newValue;
            }
        }

        private IEnumerator ShowReservation()
        {
            yield return new WaitUntil(() => reservations != null);
            float templateHigh = 32f;
            for (int i = 0; i < reservations.Count; i++)
            {
                bool itemAvailibilityChecked = false;
                bool haveUnpackedItem = false;
                StartCoroutine(FirebaseServices.ReadData("reservations", "code", reservations[i]["code"].ToString(), data =>
                {
                    if (data != null)
                    {
                        if (CheckItemAvailibility(data))
                        {
                            haveUnpackedItem = true;
                        }
                        itemAvailibilityChecked = true;
                    }
                    else
                    {
                        Debug.LogError("Failed to retrieve data.");
                    }
                }));

                yield return new WaitUntil(() => itemAvailibilityChecked);
                if (haveUnpackedItem)
                {
                    GameObject newRow = Instantiate(reservationRecordTemplate, reservationContainer);
                    Transform newRowTransform = newRow.transform;
                    RectTransform entryRectTransform = newRow.GetComponent<RectTransform>();

                    entryRectTransform.anchoredPosition = new Vector2(0f, 46f + (-templateHigh * i));
                    newRowTransform.Find("Button").Find("Code").GetComponent<TextMeshProUGUI>().text = reservations[i]["code"].ToString();

                    reservationTable.GetComponent<DynamicTableManager>().enabled = true;
                }
            }
            reservationRecordTemplate.SetActive(false);
        }

        private bool CheckItemAvailibility(JObject data)
        {
            ReservationRecord record = new ReservationRecord(data);

            int counter = 0;
            foreach (var item in record.Items)
            {
                if (item.Information.Equals("approved") && !item.Packed)
                {
                    counter++;
                }
            }

            if (record.Items.Count > 0 && counter > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private IEnumerator GetItems(string code)
        {
            transform.Find("Reservation Code").GetComponent<TextMeshProUGUI>().text = code;

            StartCoroutine(FirebaseServices.ReadData("reservations", "code", code, data =>
            {
                if (data != null)
                {
                    ReservationRecord record = new ReservationRecord(data);
                    reservationRecord = record;
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                }
            }));

            yield return new WaitForSeconds(0.2f);
            StartCoroutine(ShowItems());
        }

        private IEnumerator ShowItems()
        {
            float templateHigh = 32f;
            int i = 0;
            for (int j = 0; j < reservationRecord.Items.Count; j++)
            {
                if (reservationRecord.Items[j].Information.Equals("approved") && !reservationRecord.Items[j].Packed)
                {
                    ItemRecord item = new ItemRecord();
                    StartCoroutine(FirebaseServices.ReadData("items", "sku", reservationRecord.Items[j].Sku, data =>
                    {
                        if (data != null)
                        {
                            ItemRecord record = new ItemRecord(data);
                            item = record;
                        }
                        else
                        {
                            Debug.LogError("Failed to retrieve data.");
                        }
                    }));

                    yield return new WaitUntil(() => item.Sku == reservationRecord.Items[j].Sku);

                    GameObject newRow = Instantiate(itemRecordTemplate, itemContainer);
                    Transform newRowTransform = newRow.transform;
                    RectTransform entryRectTransform = newRow.GetComponent<RectTransform>();

                    entryRectTransform.anchoredPosition = new Vector2(0f, 46f + (-templateHigh * i));
                    newRowTransform.Find("No").GetComponent<TextMeshProUGUI>().text = (i + 1).ToString();
                    newRowTransform.Find("Bin Code").GetComponent<TextMeshProUGUI>().text = item.BinCode;
                    newRowTransform.Find("SKU").GetComponent<TextMeshProUGUI>().text = item.Sku;
                    newRowTransform.Find("Item Name").GetComponent<TextMeshProUGUI>().text = item.ItemName;
                    newRowTransform.Find("Quantity").GetComponent<TextMeshProUGUI>().text = reservationRecord.Items[i].Quantity.ToString();
                    newRowTransform.Find("Stock").GetComponent<TextMeshProUGUI>().text = item.Quantity.ToString();
                    i++;
                }
                itemTable.GetComponent<DynamicTableManager>().enabled = true;
            }
            itemRecordTemplate.SetActive(false);
        }

        private void DestroyReservationRecord()
        {
            foreach (Transform child in reservationContainer)
            {
                if (child != reservationRecordTemplate.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            reservationRecordTemplate.SetActive(true);
        }

        private void DestroyItemRecord()
        {
            foreach (Transform child in itemContainer)
            {
                if (child != itemRecordTemplate.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            itemRecordTemplate.SetActive(true);
        }

        private void UnselectRecord()
        {
            foreach (Transform child in reservationContainer)
            {
                child.Find("Button").GetComponent<Image>().color = new Color32(255, 255, 255, 0);
            }

            DestroyItemRecord();
        }
    }
}