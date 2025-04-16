using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class ElementoSeleccionable : MonoBehaviour
{
    public Button BtnInvisible;
    public TMP_Text txtNombre;
    public string nombreElemento;
    private bool seleccionada = false;
    private Vector3 posicionOriginal;

    private void Start()
    {
        nombreElemento = txtNombre.text;
        BtnInvisible.onClick.AddListener(() => SeleccionarCarta());
        StartCoroutine(EsperarYGuardarPosicion());
    }

    private IEnumerator EsperarYGuardarPosicion()
    {
        yield return null; // espera 1 frame
        posicionOriginal = transform.localPosition;
    }

    void SeleccionarCarta()
    {
        GameManager.instancia.ElementoSeleccionado(nombreElemento, this);
    }


    public void ToggleSeleccionVisual()
    {
        seleccionada = !seleccionada;
        if (seleccionada)
            transform.localPosition += new Vector3(0, 30f, 0);
        else
            transform.localPosition = posicionOriginal;
    }

    public void ResetVisual()
    {
        seleccionada = false;
        transform.localPosition = posicionOriginal;
    }
}
