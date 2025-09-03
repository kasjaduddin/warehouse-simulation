using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Record
{
    public struct BinRecord
    {
        public string Code;
        public string Information;
        public int NumberOfTags;
        public bool Active;

        public BinRecord(string code, string information)
        {
            Code = code;
            Information = information;
            NumberOfTags = 0;
            Active = true;
        }

        public BinRecord(JObject record)
        {
            Code = record["code"].ToString();
            Information = record["information"].ToString();
            NumberOfTags = Int16.Parse(record["number_of_tags"].ToString());
            Active = bool.Parse(record["active"].ToString());
        }
    }

    public struct ItemRecord
    {
        public string Sku;
        public string ItemName;
        public string BinCode;
        public int Quantity;
        public int NumberOfTags;
        public bool Active;
        public string UOM;

        public ItemRecord(string sku, string itemName, string binCode, string uom)
        {
            Sku = sku;
            ItemName = itemName;
            BinCode = binCode;
            Quantity = 0;
            UOM = uom;
            Active = true;
            NumberOfTags = 0;
        }

        public ItemRecord(JObject record)
        {
            Sku = record["sku"].ToString();
            ItemName = record["item_name"].ToString();
            BinCode = record["bin_code"].ToString();
            Quantity = Int16.Parse(record["quantity"].ToString());
            UOM = record["uom"].ToString();
            Active = bool.Parse(record["active"].ToString());
            NumberOfTags = Int16.Parse(record["number_of_tags"].ToString());
        }
    }

    public struct TransactionRecord
    {
        public struct TransactionItem
        {
            public string Sku;
            public string ItemName;
            public int Quantity;
            public string Information;
            public int Tagged;

            public TransactionItem(JObject item)
            {
                Sku = item["sku"].ToString();
                ItemName = item["item_name"].ToString();
                Quantity = int.Parse(item["quantity"].ToString());
                Information = item["information"].ToString();
                Tagged = int.Parse(item["tagged"].ToString());
            }

            public Dictionary<string, object> ToDictionary()
            {
                return new Dictionary<string, object>
                {
                    { "sku", Sku },
                    { "item_name", ItemName },
                    { "quantity", Quantity },
                    { "information", Information },
                    { "tagged", Tagged }
                };
            }
        }

        public string Code;
        public string InvoiceNumber;
        public string InvoiceDate;
        public string Vendor;
        public List<TransactionItem> Items;

        public TransactionRecord(string invoiceNumber, string invoiceDate, string vendor)
        {
            string date = $"{DateTime.Now.Year}{DateTime.Now.Month:D2}{DateTime.Now.Day:D2}";
            string time = $"{DateTime.Now.Hour:D2}{DateTime.Now.Minute:D2}{DateTime.Now.Second:D2}";

            Code = $"TRANS-{date}-{time}";
            InvoiceNumber = invoiceNumber;
            InvoiceDate = invoiceDate;
            Vendor = vendor;
            Items = new List<TransactionItem>();
        }

        public TransactionRecord(JObject record)
        {
            Code = record["code"].ToString();
            InvoiceNumber = record["invoice_number"].ToString();
            InvoiceDate = record["invoice_date"].ToString();
            Vendor = record["vendor"].ToString();
            Items = new List<TransactionItem>();
            if (record["items"] != null)
            {
                foreach (var item in record["items"])
                {
                    Items.Add(new TransactionItem(item.ToObject<JObject>()));
                }
            }
        }
    }

    public struct ReservationRecord
    {
        public struct ReservationItem
        {
            public string Sku;
            public string ItemName;
            public int Quantity;
            public string Information;
            public bool Packed;

            public ReservationItem(JObject item)
            {
                Sku = item["sku"].ToString();
                ItemName = item["item_name"].ToString();
                Quantity = int.Parse(item["quantity"].ToString());
                Information = item["information"].ToString();
                Packed = bool.Parse(item["packed"].ToString());
            }

            public Dictionary<string, object> ToDictionary()
            {
                return new Dictionary<string, object>
                {
                    { "sku", Sku },
                    { "item_name", ItemName },
                    { "quantity", Quantity },
                    { "information", Information },
                    { "packed", Packed }
                };
            }
        }

        public string Code;
        public string ReservationDate;
        public string Client;
        public List<ReservationItem> Items;

        public ReservationRecord(string reservatioDate, string client)
        {
            string date = $"{DateTime.Now.Year}{DateTime.Now.Month:D2}{DateTime.Now.Day:D2}";
            string time = $"{DateTime.Now.Hour:D2}{DateTime.Now.Minute:D2}{DateTime.Now.Second:D2}";

            Code = $"RESV-{date}-{time}";
            ReservationDate = reservatioDate;
            Client = client;
            Items = new List<ReservationItem>();
        }

        public ReservationRecord(JObject record)
        {
            Code = record["code"].ToString();
            ReservationDate = record["reservation_date"].ToString();
            Client = record["client"].ToString();
            Items = new List<ReservationItem>();
            if (record["items"] != null)
            {
                foreach (var item in record["items"])
                {
                    Items.Add(new ReservationItem(item.ToObject<JObject>()));
                }
            }
        }
    }
}