using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Rfid
{
    public class BinTagRegistrationManager : MonoBehaviour
    {
        [SerializeField]
        private ReaderManager rfidReader;
        [SerializeField]
        private GameObject popup;
        private GameObject binTagPopup;

        private TMP_Dropdown binCodeDropdown;
        private TextMeshProUGUI description;
        private TextMeshProUGUI tags;

        private void OnEnable()
        {
            binCodeDropdown = transform.Find("Bin Code Dropdown").GetComponent<TMP_Dropdown>();
            description = transform.Find("Description").GetComponent<TextMeshProUGUI>();
            tags = transform.Find("Tags").GetComponent<TextMeshProUGUI>();
            Invoke("GetBinCodes", 0.1f);

            binTagPopup = popup.transform.Find("Bin Tag").gameObject;
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
                    foreach (var bin in data)
                    {
                        binCodeDropdown.options.Add(new TMP_Dropdown.OptionData(bin["code"].ToString()));
                    }
                    binCodeDropdown.captionText.text = binCodeDropdown.options[0].text;
                    Invoke("LoadBinData", 0.1f);
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                }
            }));
        }

        public void LoadBinData()
        {
            StartCoroutine(FirebaseServices.ReadData("bins", data =>
            {
                if (data != null)
                {
                    foreach (var bin in data)
                    {
                        if (bin["code"].ToString() == binCodeDropdown.captionText.text)
                        {
                            description.text = bin["information"].ToString();
                            tags.text = bin["number_of_tags"].ToString();
                            break;
                        }
                    }
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                }
            }));
        }

        public void ReadTag()
        {
            string selectedBinCode = binCodeDropdown.captionText.text;
            BinTag tag = rfidReader.DetectedBinTag;
            if (!tag)
            {
                popup.SetActive(true);
                binTagPopup.SetActive(true);
                binTagPopup.transform.Find("Tag Not Found").gameObject.SetActive(true);
            }
            else
            {
                if (tag.BinCode != string.Empty)
                {
                    popup.SetActive(true);
                    binTagPopup.SetActive(true);

                    Transform informationPopup = binTagPopup.transform.Find("Tag Information");
                    informationPopup.gameObject.SetActive(true);
                    informationPopup.Find("Text").GetComponent<TextMeshProUGUI>().text = $"Tag has been registered to Bin {tag.BinCode}";
                }
                else
                {
                    popup.SetActive(true);
                    binTagPopup.SetActive(true);
                    binTagPopup.transform.Find("Tag Not Registered").gameObject.SetActive(true);
                }
            }
        }

        public async void WriteTag()
        {
            string selectedBinCode = binCodeDropdown.captionText.text;
            BinTag tag = rfidReader.DetectedBinTag;
            if (!tag)
            {
                popup.SetActive(true);
                binTagPopup.SetActive(true);
                binTagPopup.transform.Find("Tag Not Found").gameObject.SetActive(true);
            } 
            else
            {
                if (tag.BinCode != string.Empty)
                {
                    popup.SetActive(true);
                    binTagPopup.SetActive(true);

                    Transform informationPopup = binTagPopup.transform.Find("Tag Registered");
                    informationPopup.gameObject.SetActive(true);
                    informationPopup.Find("Text").GetComponent<TextMeshProUGUI>().text = $"Tag has been registered to {tag.BinCode}. Remove tag data?";
                }
                else
                {
                    tag.BinCode = selectedBinCode;
                    try
                    {
                        await SaveTag(tag, selectedBinCode);
                    }
                    catch
                    {
                        popup.SetActive(true);
                        binTagPopup.SetActive(true);
                        binTagPopup.transform.Find("Error Write Tag").gameObject.SetActive(true);
                    }
                }
            }
        }

        private async Task SaveTag(BinTag tag, string binCode)
        {
            await Task.Yield();

            var tagData = new Dictionary<string, object>
            {
                { "id", tag.Id },
                { "bin_code", binCode }
            };
            StartCoroutine(FirebaseServices.WriteData("rfid/bin_tags", tagData, message =>
            {
                if (message.Contains("successfully"))
                {
                    StartCoroutine(UpdateDataOnCompanySystem(binCode));
                }
                else
                {
                    popup.SetActive(true);
                    binTagPopup.SetActive(true);
                    binTagPopup.transform.Find("Error Write Tag").gameObject.SetActive(true);
                }
            }));
        }

        private IEnumerator UpdateDataOnCompanySystem(string binCode)
        {
            int currentTags = -1;
            bool? successReadData = null;
            yield return StartCoroutine(FirebaseServices.ReadData("bins", data =>
            {
                if (data != null)
                {
                    foreach (var bin in data)
                    {
                        if (bin["code"].ToString() == binCode)
                        {
                            currentTags = int.Parse(bin["number_of_tags"].ToString());
                            successReadData = true;
                            break;
                        }
                    }
                }
                else
                {
                    popup.SetActive(true);
                    binTagPopup.SetActive(true);
                    binTagPopup.transform.Find("Error Write Tag").gameObject.SetActive(true);
                    successReadData = false;
                }
            }));

            yield return new WaitUntil(() => successReadData != null);
            if (successReadData == true)
            {
                int newTagCount = currentTags + 1;
                var binData = new Dictionary<string, object>
                {
                    { "number_of_tags", newTagCount }
                };
                yield return StartCoroutine(FirebaseServices.ModifyData("bins", binData, binCode, "code", message =>
                {
                    if (message.Contains("successfully"))
                    {
                        popup.SetActive(true);
                        binTagPopup.SetActive(true);

                        Transform informationPopup = binTagPopup.transform.Find("Success Write Tag");
                        informationPopup.gameObject.SetActive(true);
                        informationPopup.Find("Text").GetComponent<TextMeshProUGUI>().text = $"Successfully registered tag to Bin {binCode}";

                        tags.text = (int.Parse(tags.text) + 1).ToString();
                    }
                    else
                    {
                        popup.SetActive(true);
                        binTagPopup.SetActive(true);
                        binTagPopup.transform.Find("Error Write Tag").gameObject.SetActive(true);
                    }
                }));
            }
        }
    }
}