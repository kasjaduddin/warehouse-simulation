using UnityEngine;
using System.Threading.Tasks;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace Rfid
{
    public class ItemTag : MonoBehaviour
    {
        private int id;
        private string sku;
        private string transactionCode;

        public int Id
        {
            get { return id; }
        }

        public string Sku
        {
            get { return sku; }
            set { sku = value; }
        }

        public string TransactionCode
        {
            get { return transactionCode; }
            set { transactionCode = value; }
        }

        void Start()
        {
            StartAsync();
        }

        private async void StartAsync()
        {
            await GetTagId();
            GetSku();
            SetTransactionCode();
        }

        private async Task GetTagId()
        {
            await Task.Yield();

            int parsedNumber;
            if (int.TryParse(gameObject.name.Substring(13), out parsedNumber))
            {
                id = parsedNumber;
            }
            else
            {
                Debug.Log("Invalid format");
                id = -1;
            }
        }

        private async void GetSku()
        {
            await Task.Yield();

            sku = string.Empty;
            StartCoroutine(FirebaseServices.ReadData("rfid/item_tags", data =>
            {
                if (data != null)
                {
                    foreach (var tag in data)
                    {
                        if (tag["id"].ToString() == id.ToString())
                        {
                            sku = tag["sku"].ToString();
                        }
                    }
                }
                else
                {
                    Debug.Log("No item tag found");
                }
            }));
        }

        public async void SetTransactionCode()
        {
            await Task.Yield();

            transactionCode = string.Empty;
            StartCoroutine(FirebaseServices.ReadData("rfid/item_tags", data =>
            {
                if (data != null)
                {
                    foreach (var tag in data)
                    {
                        if (tag["id"].ToString() == id.ToString())
                        {
                            transactionCode = tag["transaction_code"].ToString();
                        }
                    }
                }
                else
                {
                    Debug.Log("No item tag found");
                }
            }));
        }
    }
}

