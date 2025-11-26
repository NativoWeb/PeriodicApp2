using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Obsolete("Este script está obsoleto. Usa ObjectRotator con RotationType.RotateAroundSelf en su lugar. Ver ROTATION_SCRIPTS_MIGRATION.md")]
public class Rotate : MonoBehaviour
{
    public int speed;
    // Start is called before the first frame update
    void Start()
    {
   
    }

    // Update is called once per frame
    void Update()
    {
      transform.RotateAround(transform.position, Vector3.back, speed * Time.deltaTime);  
    }
}
