using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotepadManager : MonoBehaviour
{
    [SerializeField]
    private Image transactionTab;
    [SerializeField]
    private Image reservationTab;
    [SerializeField]
    private TextMeshProUGUI notepadText;

    private Color activeColor = new Color32(255, 255, 255, 255);
    private Color inactiveColor = new Color32(208, 208, 208, 255);

    private void Start()
    {
        transactionTab.color = activeColor;
        reservationTab.color = inactiveColor;
        notepadText.text = "Handover\r\n\r\nInvoice Number: 1000\t\t\t1 January 2025\r\nVendor: Wedangan\r\n ___________________________________\r\n| No | Item Name     | Quantity | Information  |\r\n|---------------------------------------------------------|\r\n|  1  | Mineral Water |       2      |   Bottles       |\r\n|___________________________________|";
    }

    public void OpenTab(GameObject clickedButton)
    {
        if (clickedButton.name == transactionTab.gameObject.name)
        {
            transactionTab.color = activeColor;
            reservationTab.color = inactiveColor;
            notepadText.text = "Handover\r\n\r\nInvoice Number: 1000\t\t\t1 January 2025\r\nVendor: Wedangan\r\n ___________________________________\r\n| No | Item Name     | Quantity | Information  |\r\n|---------------------------------------------------------|\r\n|  1  | Mineral Water |       2      |   Bottles       |\r\n|___________________________________|";
        }
        else if (clickedButton.name == reservationTab.gameObject.name)
        {
            transactionTab.color = inactiveColor;
            reservationTab.color = activeColor;
            notepadText.text = "Goods Dispatch\r\n\r\nClient: Indo Market\t\t\t2 February 2025\r\n ___________________________________\r\n| No | Item Name     | Quantity | Information  |\r\n|---------------------------------------------------------|\r\n|  1  | Mineral Water |       2      |   Bottles       |\r\n|___________________________________|";
        }
    }
}