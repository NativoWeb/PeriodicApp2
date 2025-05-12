using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;

public class PerfilController : MonoBehaviour
{

    public GameObject MenuPanelUI;
    public GameObject PerfilPanel;
    public Button ButtonMenu;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ButtonMenu.onClick.AddListener(abrirMenu);

    }

    void abrirMenu()
    {
        MenuPanelUI.SetActive(true);
        EventTrigger trigger = PerfilPanel.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => {
            MenuPanelUI.SetActive(false);
        });
        trigger.triggers.Add(entry);

    }


}
