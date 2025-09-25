using NUnit.Framework;
using PeriodicApp.Core.Application.DTOs;
using PeriodicApp.Presentation.ViewModels.Gameplay;

public class PlayerMovementViewModelTests
{
    [Test]
    public void Tick_WhenGroundedAndMovingForward_ShouldProduceHorizontalVelocity()
    {
        var viewModel = new PlayerMovementViewModel(5f, 2f, -9.81f);
        var input = new MovementInput(0f, 1f);

        var result = viewModel.Tick(input, false, true, 0.016f);

        Assert.That(result.VerticalVelocity, Is.EqualTo(-9.81f * 0.016f).Within(0.001f));
        Assert.That(result.HorizontalSpeed, Is.EqualTo(0f));
        Assert.That(result.VerticalSpeed, Is.EqualTo(5f));
        Assert.IsTrue(result.HasMovement);
    }

    [Test]
    public void Tick_WhenJumpPressed_ShouldSetVerticalVelocity()
    {
        var viewModel = new PlayerMovementViewModel(5f, 2f, -9.81f);
        var input = new MovementInput(0f, 0f);

        var result = viewModel.Tick(input, true, true, 0.016f);

        Assert.Greater(result.VerticalVelocity, 0f);
    }
}
