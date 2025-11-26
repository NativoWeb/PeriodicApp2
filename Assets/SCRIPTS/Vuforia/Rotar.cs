using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Obsolete("Este script está obsoleto. Usa ObjectRotator con RotationType.SelfRotate en su lugar. Ver ROTATION_SCRIPTS_MIGRATION.md")]
public class Rotar : MonoBehaviour {

	public float rotationSpeed = 50.0f;
	// Use this for initialization
	void Start () {

	}




	void Update() {

		transform.Rotate (0, rotationSpeed * Time.deltaTime , 0);

	}

}