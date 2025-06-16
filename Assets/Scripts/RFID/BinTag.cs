using UnityEngine;
using System.Threading.Tasks;
using System;
using Unity.VisualScripting;

namespace Rfid
{
    public class BinTag : MonoBehaviour
    {
        private int id;
        private string binCode;

        public int Id
        {
            get { return id; }
        }

        public string BinCode
        {
            get { return binCode; }
            set { binCode = value; }
        }

        void Start()
        {
            StartAsync();
        }

        private async void StartAsync()
        {
            await GetTagId();
            GetBinCode();
        }

        private async Task GetTagId()
        {
            await Task.Yield();

            int parsedNumber;
            if (int.TryParse(gameObject.name.Substring(4), out parsedNumber))
            {
                id = parsedNumber;
            }
            else
            {
                Debug.Log("Invalid format");
                id = -1;
            }
        }

        private void GetBinCode()
        {
            binCode = string.Empty;
            StartCoroutine(FirebaseServices.ReadData("rfid/bin_tags", data =>
            {
                if (data != null)
                {
                    foreach (var tag in data)
                    {
                        if (tag["id"].ToString() == id.ToString())
                        {
                            binCode = tag["bin_code"].ToString();
                            Debug.Log($"Bin Code untuk {gameObject.name}: {data["bin_code"]}");
                        }
                    }
                }
                else
                {
                    Debug.LogError("ERROR: Gagal mengambil data bin tags dari Firebase.");
                }
            }));
        }
    }
}
