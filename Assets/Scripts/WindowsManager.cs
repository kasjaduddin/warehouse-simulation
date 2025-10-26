using System.Collections;
using UnityEngine;

public class WindowsManager : MonoBehaviour
{
    [SerializeField]
    private GameObject window; // Window game object
    [SerializeField]
    private GameObject notepad; // Notepad game object
    [SerializeField]
    private GameObject companySystem; // Company system game object
    [SerializeField]
    private GameObject rfid; // RFID game object

    public void OpenWindow()
    {
        window.SetActive(true);
    }

    public void CloseWindow()
    {
        GameObject windowPanel = window.transform.Find("Background").gameObject;
        foreach (Transform child in windowPanel.transform)
        {
            child.gameObject.SetActive(child.gameObject.name == "Power Button" || child.gameObject.name == "Shortcut");
        }
        window.SetActive(false);
    }

    public void OnClickNotepadShortcut()
    {
        StartCoroutine(OpenNotepad());
    }

    public void OnClickCompanySystemShortcut()
    {
        StartCoroutine(OpenCompanySystem());
    }

    public void OnClickRfidShortcut()
    {
        StartCoroutine(OpenRfid());
    }

    private IEnumerator OpenNotepad()
    {
        if (!notepad.activeSelf)
        {
            CloseNotepad();
            yield return new WaitForSeconds(0.1f);
            notepad.SetActive(true);
        }
    }

    public void CloseNotepad()
    {
        notepad.SetActive(false);
    }

    private IEnumerator OpenCompanySystem()
    {
        if (!companySystem.activeSelf)
        {
            CloseCompanySystem();
            CompanySystem.BinListManager binListManager = companySystem.transform.Find("Master Data Bin").Find("Bin List").GetComponent<CompanySystem.BinListManager>();
            yield return new WaitUntil(() => binListManager.enabled == false);
            binListManager.enabled = true;
            companySystem.SetActive(true);
        }
    }

    public void CloseCompanySystem()
    {
        foreach (Transform child in companySystem.transform)
        {
            if (child.gameObject.name == "Master Data Bin")
            {
                string listName = "Bin List";
                child.transform.Find(listName).GetComponent<CompanySystem.BinListManager>().enabled = false;
                ActivteList(child, listName);
                child.gameObject.SetActive(true); 
            }
            else
            {
                child.gameObject.SetActive(child.gameObject.name == "Title Bar" || child.gameObject.name == "Navigation Manager");
            }
        }
        companySystem.SetActive(false);
    }

    private IEnumerator OpenRfid()
    {
        if (!rfid.activeSelf)
        {
            CloseRfid();
            yield return new WaitForSeconds(0.1f);
            rfid.SetActive(true);
        }
    }

    public void CloseRfid()
    {
        foreach (Transform child in rfid.transform)
        {
            string name = child.gameObject.name;
            child.gameObject.SetActive(name.Equals("Title Bar") || name.Equals("Main Menu"));
        }
        rfid.SetActive(false);
    }

    private void ActivteList(Transform parent, string listName)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(child.gameObject.name == listName);
        }
    }
}
