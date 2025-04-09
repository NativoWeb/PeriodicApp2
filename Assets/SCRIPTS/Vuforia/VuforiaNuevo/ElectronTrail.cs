//using UnityEngine;

//public class ElectronTrail : MonoBehaviour
//{
//    private float baseRadius;
//    private Transform orbitTransform;
//    private TrailRenderer trail;
//    private OrbitAnimation orbitAnimation;
//    private Vector3 initialLocalPos;

//    public void Initialize(int level, float angle)
//    {
//        orbitTransform = transform.parent;
//        baseRadius = transform.localPosition.magnitude;
//        initialLocalPos = transform.localPosition.normalized * baseRadius;
//        orbitAnimation = orbitTransform.GetComponent<OrbitAnimation>();

//        trail = gameObject.AddComponent<TrailRenderer>();
//        trail.time = 2f;
//        trail.startWidth = 0.02f;
//        trail.endWidth = 0f;
//        trail.material = new Material(Shader.Find("Standard"));
//        trail.material.color = new Color(1f, 1f, 1f, 0.4f); // Blanco semi-transparente
//        trail.material.SetFloat("_Mode", 3); // Transparent mode
//        trail.minVertexDistance = 0.01f;
//        trail.emitting = false;
//    }

//    public void EnableTrail()
//    {
//        trail.emitting = true;
//    }

//    void Update()
//    {
//        if (orbitTransform == null || orbitAnimation == null) return;

//        transform.localPosition = orbitAnimation.GetCurrentRotation() * initialLocalPos;
//        transform.rotation = Quaternion.LookRotation(transform.position - orbitTransform.position);
//    }
//}