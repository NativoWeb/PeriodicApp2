using UnityEngine;

namespace QuantumAI.UI
{
    /// <summary>
    /// Ajusta automáticamente la posición del toast para que aparezca
    /// dentro del área segura del dispositivo, evitando notches y cámaras.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaToastPositioner : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("Margen adicional desde el borde superior del área segura")]
        [SerializeField] private float topMargin = 20f;

        [Tooltip("Actualizar posición en cada frame (útil si el dispositivo rota)")]
        [SerializeField] private bool updateEveryFrame = false;

        private RectTransform rectTransform;
        private Rect lastSafeArea;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (updateEveryFrame)
            {
                // Solo actualizar si el safe area cambió (rotación del dispositivo)
                if (Screen.safeArea != lastSafeArea)
                {
                    ApplySafeArea();
                }
            }
        }

        /// <summary>
        /// Aplica el ajuste de safe area a la posición del toast
        /// </summary>
        public void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;
            lastSafeArea = safeArea;

            // Calcular el offset desde la parte superior
            float screenHeight = Screen.height;
            float safeAreaTop = screenHeight - safeArea.yMax;

            // Convertir a coordenadas de Canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Ajustar la posición Y considerando el safe area
                Vector2 anchoredPos = rectTransform.anchoredPosition;

                // El safe area top nos dice cuántos píxeles desde arriba están ocupados
                // Agregamos el margen adicional y la altura del toast para que quede completamente visible
                float newYPos = -(safeAreaTop + topMargin);

                rectTransform.anchoredPosition = new Vector2(anchoredPos.x, newYPos);

                Debug.Log($"[SafeAreaToastPositioner] Ajustado a safe area. Screen: {screenHeight}px, Safe top: {safeAreaTop}px, Toast Y: {newYPos}");
            }
        }

        /// <summary>
        /// Fuerza la actualización del safe area (llamar después de instanciar el toast)
        /// </summary>
        public void ForceUpdate()
        {
            ApplySafeArea();
        }

#if UNITY_EDITOR
        // Visualización en el editor
        private void OnValidate()
        {
            if (Application.isPlaying && rectTransform != null)
            {
                ApplySafeArea();
            }
        }
#endif
    }
}
