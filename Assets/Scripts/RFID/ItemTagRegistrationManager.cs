using CompanySystem;
using Record;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        private TextMeshProUGUI tags;

        private string selectedTransactionCode;
        private string selectedSku;
        private ItemTag tag;
        private void OnEnable()
        {
            transactionCodeDropdown = transform.Find("Transaction Code Dropdown").GetComponent<TMP_Dropdown>();
            table = transform.Find("Item List").gameObject;
            container = table.transform.Find("Table Container");
            recordTemplate = container.Find("Record Template").gameObject;
            tags = transform.Find("Item Information").Find("Tags").GetComponent<TextMeshProUGUI>();
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
                    informationPopup.Find("Text").GetComponent<TextMeshProUGUI>().text = $"Tag has been registered to Item {tag.Sku}";
                }
                else
                {
                    popup.SetActive(true);
                    itemTagPopup.SetActive(true);
                    itemTagPopup.transform.Find("Tag Not Registered").gameObject.SetActive(true);
                }
            }
        }

        public async void WriteTag()
        {
            selectedTransactionCode = transactionCodeDropdown.captionText.text;
            selectedSku = transform.Find("Item Information").Find("SKU").GetComponent<TextMeshProUGUI>().text;
            if (selectedSku == string.Empty)
            {
                popup.SetActive(true);
                itemTagPopup.SetActive(true);
                itemTagPopup.transform.Find("No Item Selected").gameObject.SetActive(true);
                return;
            }

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
                    Transform informationPopup = itemTagPopup.transform.Find("Tag Registered");
                    informationPopup.gameObject.SetActive(true);
                    informationPopup.Find("Text").GetComponent<TextMeshProUGUI>().text = $"Tag has been registered to Item {tag.Sku}. Remove tag data?";
                }
                else
                {
                    try
                    {
                        await SaveTag(tag, selectedTransactionCode, selectedSku);
                    }
                    catch
                    {
                        popup.SetActive(true);
                        itemTagPopup.SetActive(true);
                        itemTagPopup.transform.Find("Error Write Tag").gameObject.SetActive(true);
                    }
                }
            }
        }

        private async Task SaveTag(ItemTag tag, string transactionCode, string sku)
        {
            await Task.Yield();

            var tagData = new Dictionary<string, object>
            {
                { "id", tag.Id },
                { "sku", sku }
            };
            StartCoroutine(FirebaseServices.WriteData("rfid/item_tags", tagData, message =>
            {
                if (message.Contains("successfully"))
                {
                    StartCoroutine(UpdateDataOnCompanySystem(transactionCode, sku, true));
                }
                else
                {
                    popup.SetActive(true);
                    itemTagPopup.SetActive(true);
                    itemTagPopup.transform.Find("Error Write Tag").gameObject.SetActive(true);
                }
            }));
        }

        private IEnumerator UpdateDataOnCompanySystem(string transactionCode, string sku, bool addTag)
        {
            int currentTags = -1;
            bool? successReadData = null;
            bool? successModifyItemData = null;
            bool? successModifyTransactionData = null;

            yield return FirebaseServices.ReadData("items", "sku", sku, data =>
            {
                if (data != null)
                {
                    currentTags = int.Parse(data["number_of_tags"].ToString());
                    successReadData = true;
                }
                else
                {
                    popup.SetActive(true);
                    itemTagPopup.SetActive(true);

                    if (addTag)
                    {
                        itemTagPopup.transform.Find("Error Write Tag").gameObject.SetActive(true);
                    }
                    else
                    {
                        itemTagPopup.transform.Find("Error Remove Tag Data").gameObject.SetActive(true);
                    }

                    successReadData = false;
                }
            });

            yield return new WaitUntil(() => successReadData != null);
            if (successReadData == true)
            {
                int newTagCount = addTag ? currentTags + 1 : currentTags - 1;
                var itemData = new Dictionary<string, object>
                {
                    { "number_of_tags", newTagCount }
                };
                var transactionData = new Dictionary<string, object>
                {
                    { "sku", sku },
                    { "tagged", true }
                };

                yield return StartCoroutine(FirebaseServices.ModifyData("items", itemData, sku, "sku", message =>
                {
                    if (message.Contains("successfully"))
                    {
                        successModifyItemData = true;
                    }
                    else
                    {
                        successModifyItemData = false;
                    }
                }));

                yield return StartCoroutine(FirebaseServices.ModifyData("transactions", "code", transactionCode, "items", transactionData, sku, "sku", message =>
                {

                    if (message.Contains("successfully"))
                    {
                        successModifyTransactionData = true;
                    }
                    else
                    {
                        successModifyTransactionData = false;
                    }
                }));

                yield return new WaitUntil(() => successModifyItemData.HasValue && successModifyTransactionData.HasValue);
                if (successModifyItemData.Value && successModifyTransactionData.Value)
                {
                    popup.SetActive(true);
                    itemTagPopup.SetActive(true);

                    if (addTag)
                    {
                        tag.Sku = selectedSku;

                        Transform informationPopup = itemTagPopup.transform.Find("Success Write Tag");
                        informationPopup.gameObject.SetActive(true);
                        informationPopup.Find("Text").GetComponent<TextMeshProUGUI>().text = $"Successfully registered tag to Item {sku}";

                        tags.text = (int.Parse(tags.text) + 1).ToString();
                        LoadTransactionData();
                    }
                    else
                    {
                        tag.Sku = string.Empty;

                        Transform registeredPopup = itemTagPopup.transform.Find("Tag Registered");
                        registeredPopup.gameObject.SetActive(false);

                        Transform informationPopup = itemTagPopup.transform.Find("Success Remove Tag Data");
                        informationPopup.gameObject.SetActive(true);

                        if (transactionCodeDropdown.captionText.text == transactionCode && selectedSku == sku)
                        {
                            tags.text = (int.Parse(tags.text) - 1).ToString();
                        }
                    }
                }
                else
                {
                    popup.SetActive(true);
                    itemTagPopup.SetActive(true);

                    if (addTag)
                    {
                        itemTagPopup.transform.Find("Error Write Tag").gameObject.SetActive(true);
                    }
                    else
                    {
                        itemTagPopup.transform.Find("Error Remove Tag Data").gameObject.SetActive(true);
                    }
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