using UnityEngine;
using System.Threading.Tasks;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace Rfid
{
    public class ItemTag : MonoBehaviour
    {
        private int id;
        private string sku;

        public int Id
        {
            get { return id; }
        }

        public string Sku
        {
            get { return sku; }
            set { sku = value; }
        }

        void Start()
        {
            StartAsync();
        }

        private async void StartAsync()
        {
            await GetTagId();
            GetSku();
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

        private void GetSku()
        {
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
    }
}

