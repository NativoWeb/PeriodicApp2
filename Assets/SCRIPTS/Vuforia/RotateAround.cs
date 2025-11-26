using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Obsolete("Este script está obsoleto. Usa ObjectRotator con RotationType.RotateAroundTarget en su lugar. Ver ROTATION_SCRIPTS_MIGRATION.md")]
public class RotateAround : MonoBehaviour
{
    public GameObject objeto;
    public float rotationSpeed = 50.0f;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(objeto.transform.position, objeto.transform.forward, rotationSpeed * Time.deltaTime);

    }
}
