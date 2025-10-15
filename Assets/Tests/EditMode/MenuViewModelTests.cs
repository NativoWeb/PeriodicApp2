using NUnit.Framework;
using PeriodicApp.Presentation.ViewModels.Menus;

public class MenuViewModelTests
{
    [Test]
    public void Initialize_ShouldShowMainMenu()
    {
        var viewModel = new MenuViewModel(0.5f);
        MenuViewState? lastState = null;
        viewModel.StateChanged += state => lastState = state;

        viewModel.Initialize();

        Assert.IsTrue(lastState?.ShowMainMenu);
        Assert.IsFalse(lastState?.ShowSelection);
    }

    [Test]
    public void RequestSelection_ShouldKeepMainMenuVisibleUntilDelayed()
    {
        var viewModel = new MenuViewModel(0.5f);
        viewModel.Initialize();

        viewModel.RequestSelectionPanel();

        Assert.IsTrue(viewModel.State.ShowMainMenu);
        Assert.IsFalse(viewModel.State.ShowSelection);
    }
}
