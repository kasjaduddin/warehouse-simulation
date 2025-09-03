using Record;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;


namespace CompanySystem
{
    public class EditItemManager : MonoBehaviour
    {
        public TMP_InputField skuInputField;
        public TMP_InputField itemNameInputField;
        public TMP_Dropdown binCodeDropdown;
        public TMP_Dropdown uomDropdown;

        public GameObject itemTable;

        public GameObject popup;
        private GameObject warningPanel;

        void OnEnable()
        {
            // Invoke GetData method after a short delay
            Invoke("GetBinCodes", 0.1f);
        }

        private void OnDisable()
        {
            binCodeDropdown.options.Clear();
        }
        public void GetBinCodes()
        {
            StartCoroutine(FirebaseServices.ReadData("bins", data =>
            {
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        binCodeDropdown.options.Add(new TMP_Dropdown.OptionData(item["code"].ToString()));
                    }
                    LoadItem();
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                }
            }));
        }

        private void LoadItem()
        {
            foreach (var option in binCodeDropdown.options)
            {
                if (option.text == ItemListManager.selectedRecord.BinCode.ToString())
                {
                    binCodeDropdown.value = binCodeDropdown.options.IndexOf(option);
                    binCodeDropdown.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = option.text;
                    break;
                }
            }
            skuInputField.text = ItemListManager.selectedRecord.Sku.ToString();
            itemNameInputField.text = ItemListManager.selectedRecord.ItemName.ToString();
            foreach (var option in uomDropdown.options)
            {
                if (option.text == ItemListManager.selectedRecord.UOM.ToString())
                {
                    uomDropdown.value = uomDropdown.options.IndexOf(option);
                    break;
                }
            }
        }

        // Edit item to system
        public void EditItem()
        {
            ItemRecord newItem = new ItemRecord(skuInputField.text, itemNameInputField.text, binCodeDropdown.captionText.text, uomDropdown.captionText.text);
            string oldSku = ItemListManager.selectedRecord.Sku;
            string oldBinCode = ItemListManager.selectedRecord.BinCode;

            var newItemData = new Dictionary<string, object>
            {
                { "sku", newItem.Sku },
                { "item_name", newItem.ItemName },
                { "bin_code", newItem.BinCode },
                { "uom", newItem.UOM }
            };
            
            StartCoroutine(FirebaseServices.ModifyData("items", newItemData, oldBinCode, "bin_code", oldSku, "sku", message =>
            {
                if (message.Contains("successfully"))
                {
                    HidePopup();
                    ResetInput();
                    gameObject.SetActive(false);
                    RefreshTable();
                    ItemListManager.ResetSelectedRecord();
                }
                else if (message.Contains("registered") && message.Contains("Replace"))
                {
                    ShowPopup();
                    ItemRegisteredHandler(message, newItem.Sku);
                }
                else if (message.Contains("registered") && !message.Contains("Replace"))
                {
                    ShowPopup();
                    message = $"Bin with bin code {newItem.BinCode} already in use";
                    BinInUseHandler(message);
                }
                else
                {
                    Debug.LogError(message);
                }
            }));
        }

        // Reset input field
        public void ResetInput()
        {
            if (skuInputField.text.Length > 0)
                skuInputField.text = skuInputField.text.Remove(0);
            if (itemNameInputField.text.Length > 0)
                itemNameInputField.text = itemNameInputField.text.Remove(0);

            binCodeDropdown.options.Clear();
        }

        private void RefreshTable()
        {
            itemTable.SetActive(false);
            itemTable.SetActive(true);
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

        private void ItemRegisteredHandler(string message, string sku)
        {
            warningPanel = popup.transform.Find("Item Registered").gameObject;
            warningPanel.SetActive(true);

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
                StartCoroutine(FirebaseServices.DeleteData("items", "sku", sku, deleteResult =>
                {
                    if (deleteResult.Contains("successfully"))
                    {
                        EditItem();
                    }
                }));
            });
            trigger.triggers.Add(entry);
        }

        private void BinInUseHandler(string message)
        {
            warningPanel = popup.transform.Find("Bin In Use").gameObject;
            warningPanel.SetActive(true);

            TextMeshProUGUI warningText = warningPanel.GetComponentInChildren<TextMeshProUGUI>();
            warningText.text = message;
        }
    }
}
