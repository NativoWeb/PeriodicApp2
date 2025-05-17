using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class FirestoreService :IServicioFirestore
{
    private readonly FirebaseFirestore firestore;

    public FirestoreService(FirebaseFirestore firestore)
    {
        this.firestore = firestore;
    }

    public async Task<bool> NombreUsuarioDisponible(string nombre)
    {
        Query query = firestore.Collection("users").WhereEqualTo("DisplayName", nombre);
        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        return snapshot.Count == 0;
    }

    public async Task GuardarDatosUsuario(string userId, Dictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("No se porporciono userId para GuardarDatosUsuario.");
        }

        DocumentReference docRef = firestore.Collection("users").Document(userId);
        await docRef.SetAsync(data, SetOptions.MergeAll);
        Debug.Log("Datos del usuario guardados correctamente");
    }

    public async Task SubirJson(string userId, string misionesJson, string categoriasJson)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("UserId vacio al subir JSON");
        }

        List<Task> tareas = new List<Task>();

        if(!string.IsNullOrEmpty(misionesJson) && misionesJson != "{}")
        {
            var data = new Dictionary<string, object>
            {
                { "misiones", misionesJson},
                { "timestamp", FieldValue.ServerTimestamp}
            };

            tareas.Add(firestore.Collection("users").Document(userId).Collection("datos").Document("misiones").SetAsync(data, SetOptions.MergeAll));
        }

        if(!string.IsNullOrEmpty(categoriasJson) && categoriasJson != "{}")
        {
            var data = new Dictionary<string, object>
            {
                {"categorias", categoriasJson },
                {"timestamp", FieldValue.ServerTimestamp }
            };

            tareas.Add(firestore.Collection("users").Document(userId).Collection("datos").Document("categorias").SetAsync(data, SetOptions.MergeAll));
        }

        if (tareas.Count > 0)
        {
            await Task.WhenAll(tareas);
            Debug.Log("Misiones y categorias subidas correctamente");
        }
        else
        {
            Debug.LogWarning("No se encontraron datos validos para subir");
        }
    }

    public async Task ActualizarRango(string userId, int xp)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("UserId vacio para actualizar rango. ");
            return;
        }

        var docRef = firestore.Collection("users").Document(userId);
        var snapshot = await docRef.GetSnapshotAsync();

        if(snapshot.Exists && snapshot.ContainsField("Rango"))
        {
            string nuevoRango = CalcularRango(xp);
            string rangoActual = snapshot.GetValue<string>("Rango");

            if(nuevoRango != rangoActual)
            {
                await docRef.UpdateAsync("Rango", nuevoRango);
                Debug.Log("Rango actualizado a: {nuevoRango}");
            }
        }
    }

    private string CalcularRango(int xp)
    {
        if (xp >= 25000) return "Amo del caos químico";
        if (xp >= 9000) return "Visionario Cuántico";
        if (xp >= 3000) return "Arquitecto molecular";
        return "Novato de laboratorio";
    }

    public async Task<Dictionary<string, object>> ObtenerUsuarioAsync(string userId)
    {
        DocumentSnapshot snapshot = await FirebaseFirestore.DefaultInstance.Collection("users").Document(userId).GetSnapshotAsync();

        if (snapshot.Exists)
            return snapshot.ToDictionary();

        return new Dictionary<string, object>();
    }

    public async Task GuardarEstadoEncuestaConocimientoAsync(string userId, bool estado)
    {
        var firestore = FirebaseFirestore.DefaultInstance;
        var userRef = firestore.Collection("users").Document(userId);
        await userRef.UpdateAsync("EstadoEncuestaConocimiento", estado);
    }


}
