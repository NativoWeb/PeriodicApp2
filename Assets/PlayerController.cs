using UnityEngine;
using PeriodicApp.Core.Application.Interfaces;
using PeriodicApp.Presentation.ViewModels.Gameplay;
using PeriodicApp.Core.Application.DTOs;
using PeriodicApp.Infrastructure.Input;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputReaderAsset inputReader;
    [SerializeField] private CharacterController controller;
    [SerializeField] private float playerSpeed = 30f;
    [SerializeField] private float jumpHeight = 20f;
    [SerializeField] private float gravityValue = -9.81f;

    private PlayerMovementViewModel viewModel;

    private void Awake()
    {
        controller = controller != null ? controller : GetComponent<CharacterController>() ?? gameObject.AddComponent<CharacterController>();
        viewModel = new PlayerMovementViewModel(playerSpeed, jumpHeight, gravityValue);
    }

    private void Update()
    {
        if (inputReader == null)
        {
            Debug.LogWarning("PlayerController no tiene un inputReader asignado.");
            return;
        }

        MovementInput movementInput = inputReader.ReadMovement();
        bool jumpPressed = inputReader.IsJumpPressed();
        bool isGrounded = controller.isGrounded;

        MovementUpdate movement = viewModel.Tick(movementInput, jumpPressed, isGrounded, Time.deltaTime);

        var horizontalMove = new Vector3(movement.HorizontalSpeed, 0f, movement.VerticalSpeed);
        if (movement.HasMovement)
        {
            transform.forward = horizontalMove.normalized;
        }

        var verticalMove = new Vector3(0f, movement.VerticalVelocity, 0f);
        controller.Move((horizontalMove + verticalMove) * Time.deltaTime);
    }
}
