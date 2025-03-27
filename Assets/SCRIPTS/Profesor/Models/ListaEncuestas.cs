using System.Collections.Generic;

[System.Serializable]
public class ListaEncuestas
{
    public List<string> encuestas;

    public ListaEncuestas(List<string> encuestas)
    {
        this.encuestas = encuestas;
    }


}
