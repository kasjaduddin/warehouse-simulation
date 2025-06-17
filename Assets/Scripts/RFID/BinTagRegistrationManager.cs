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
                    Debug.Log($"Tag sudah digunakan untuk bin: {tag.BinCode}");
                }
                else
                {
                    Debug.Log($"Tag belum digunakan");
                    //tag.BinCode = selectedBinCode;
                    //await SaveTag(tag, selectedBinCode);
                    //StartCoroutine(UpdateDataOnCompanySystem(selectedBinCode));
                }
            }
        }

        public async void WriteTag()
        {
            string selectedBinCode = binCodeDropdown.captionText.text;
            BinTag tag = rfidReader.DetectedBinTag;
            if (tag.BinCode != string.Empty)
            {
                Debug.Log($"Tag sudah digunakan untuk bin: {tag.BinCode}");
            }
            else
            {
                Debug.Log($"Tag belum digunakan, mendaftarkan untuk bin: {tag.BinCode}");
                tag.BinCode = selectedBinCode;
                try
                {
                    await SaveTag(tag, selectedBinCode);
                    StartCoroutine(UpdateDataOnCompanySystem(selectedBinCode));
                }
                catch
                {
                    Debug.LogError("Gagal menyimpan tag ke Firebase atau memperbarui sistem perusahaan.");
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
                    Debug.Log($"Tag {tag.Id} untuk Bin {binCode} berhasil ditambahkan ke Firebase.");
                }
                else
                {
                    Debug.LogError(message);
                }
            }));
        }

        private IEnumerator UpdateDataOnCompanySystem(string binCode)
        {
            int currentTags = -1;
            yield return StartCoroutine(FirebaseServices.ReadData("bins", data =>
            {
                if (data != null)
                {
                    foreach (var bin in data)
                    {
                        if (bin["code"].ToString() == binCode)
                        {
                            currentTags = int.Parse(bin["number_of_tags"].ToString());
                            break;
                        }
                    }
                }
                else
                {
                    Debug.LogError("Failed to retrieve data.");
                    currentTags = -1;
                }
            }));

            yield return new WaitUntil(() => currentTags != -1);
            int newTagCount = currentTags + 1;
            var binData = new Dictionary<string, object>
            {
                { "number_of_tags", newTagCount }
            };
            yield return StartCoroutine(FirebaseServices.ModifyData("bins", binData, binCode, "code", message =>
            {
                if (message.Contains("successfully"))
                {
                    Debug.Log($"Bin {binCode} berhasil diperbarui di Company System.");
                }
                else
                {
                    Debug.LogError(message);
                }
            }));
        }
    }
}