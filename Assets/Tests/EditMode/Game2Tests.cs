using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Game2Tests
{
    private class Game2TestDouble : Game2
    {
        new void Awake()
        {
            // Evita la lógica de carga de recursos/Firebase durante las pruebas.
        }
    }

    [Test]
    public void MostrarPregunta_ReordenaOpcionesYReiniciaTemporizador()
    {
        const int seed = 12345;

        var gameObject = new GameObject("Game2Test");
        var game2 = gameObject.AddComponent<Game2TestDouble>();

        game2.botonesRespuestas = CreateButtons(3);
        game2.txtPregunta = CreateTMPText("Pregunta");
        game2.imgElemento = CreateImage();
        game2.txtTemporizador = CreateUIText();

        var pregunta = new PreguntaJuego
        {
            pregunta = "¿Cuál es el elemento?",
            opciones = new List<string> { "Oro", "Plata", "Cobre" },
            imagen = string.Empty,
            respuesta_correcta = "Oro"
        };

        SetPrivateField(game2, "preguntas", new List<PreguntaJuego> { pregunta });

        UnityEngine.Random.InitState(seed);
        var ordenEsperado = pregunta.opciones.OrderBy(_ => UnityEngine.Random.value).ToList();
        UnityEngine.Random.InitState(seed);

        game2.MostrarPregunta();

        var ordenObtenido = game2.botonesRespuestas
            .Take(pregunta.opciones.Count)
            .Select(boton => boton.GetComponentInChildren<TextMeshProUGUI>().text)
            .ToList();

        Assert.AreEqual(ordenEsperado, ordenObtenido, "Las opciones deben barajarse con la misma semilla establecida.");

        var tiempoRestante = (float)GetPrivateField(game2, "tiempoRestante");
        var tiempoActivo = (bool)GetPrivateField(game2, "tiempoActivo");

        Assert.AreEqual(10f, tiempoRestante, "El temporizador debe reiniciarse a 10 segundos.");
        Assert.IsTrue(tiempoActivo, "El temporizador debe quedar activo tras mostrar la pregunta.");

        foreach (var boton in game2.botonesRespuestas)
        {
            Object.DestroyImmediate(boton.gameObject);
        }
        Object.DestroyImmediate(game2.txtPregunta.gameObject);
        Object.DestroyImmediate(game2.imgElemento.gameObject);
        Object.DestroyImmediate(game2.txtTemporizador.gameObject);
        Object.DestroyImmediate(gameObject);
    }

    private static Button[] CreateButtons(int count)
    {
        var buttons = new Button[count];
        for (int i = 0; i < count; i++)
        {
            var buttonGO = new GameObject($"Button_{i}", typeof(RectTransform));
            buttonGO.AddComponent<CanvasRenderer>();
            buttonGO.AddComponent<Image>();
            var button = buttonGO.AddComponent<Button>();

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(buttonGO.transform);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = string.Empty;

            buttons[i] = button;
        }

        return buttons;
    }

    private static TextMeshProUGUI CreateTMPText(string initialText)
    {
        var go = new GameObject("TMP_Text", typeof(RectTransform));
        go.AddComponent<CanvasRenderer>();
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = initialText;
        return text;
    }

    private static Image CreateImage()
    {
        var go = new GameObject("Image", typeof(RectTransform));
        go.AddComponent<CanvasRenderer>();
        return go.AddComponent<Image>();
    }

    private static Text CreateUIText()
    {
        var go = new GameObject("UI_Text", typeof(RectTransform));
        go.AddComponent<CanvasRenderer>();
        var text = go.AddComponent<Text>();
        text.text = string.Empty;
        return text;
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"No se encontró el campo privado '{fieldName}'.");
        field.SetValue(instance, value);
    }

    private static object GetPrivateField(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"No se encontró el campo privado '{fieldName}'.");
        return field.GetValue(instance);
    }
}
