using UnityEngine;
using System.Threading.Tasks;

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
            Debug.Log("BinTag ID: " + id);
            GetBinCode();
        }

        private async Task GetTagId()
        {
            await Task.Yield();

            if (PlayerPrefs.HasKey(gameObject.name))
            {
                Debug.Log("Tag ID already exists in PlayerPrefs for: " + gameObject.name);
                id = PlayerPrefs.GetInt(gameObject.name);
            }
            else
            {
                Debug.Log("Tag ID not found in PlayerPrefs for: " + gameObject.name);
                int parsedNumber;
                if (int.TryParse(gameObject.name.Substring(4), out parsedNumber))
                {
                    PlayerPrefs.SetInt(gameObject.name, parsedNumber);
                    id = parsedNumber;
                }
                else
                {
                    Debug.Log("Invalid format");
                }
            }
        }

        private void GetBinCode()
        {
            //StartCoroutine(FirebaseServices.ReadData("rfid/bin_tags", "id", id.ToString(), data =>
            //{
            //    Debug.Log($"Mencari Bin Code untuk Tag ID: {id} di Firebase...");
            //    Debug.Log($"Data yang diterima: {data}");
            //    if (data != null)
            //    {
            //        binCode = data["bin_code"].ToString();
            //        Debug.Log($"Bin Code untuk {gameObject.name}: {data["bin_code"]}");
            //    }
            //    else
            //    {
            //        Debug.LogError("ERROR: Gagal mengambil data bin tags dari Firebase.");
            //    }
            //}));
        }
    }
}
