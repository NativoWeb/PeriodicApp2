using System.Collections.Generic;
using UnityEngine;

namespace MiProjeto.Datos
{
    [System.Serializable]
    public class ListaEncuestas
    {
        public List<string> encuestas;

        public ListaEncuestas(List<string> encuestas)
        {
            this.encuestas = encuestas;
        }
    }
}
