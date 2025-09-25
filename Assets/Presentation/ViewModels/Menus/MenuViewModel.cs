using System;

namespace PeriodicApp.Presentation.ViewModels.Menus
{
    public sealed class MenuViewModel
    {
        private MenuViewState _state;

        public MenuViewModel(float selectionDelaySeconds)
        {
            SelectionDelaySeconds = selectionDelaySeconds;
            _state = MenuViewState.MainMenu();
        }

        public float SelectionDelaySeconds { get; }

        public MenuViewState State => _state;

        public event Action<MenuViewState>? StateChanged;

        public void Initialize()
        {
            UpdateState(MenuViewState.MainMenu());
        }

        public void RequestSelectionPanel()
        {
            UpdateState(MenuViewState.SelectionRequested());
        }

        public void ShowSelection()
        {
            UpdateState(MenuViewState.Selection());
        }

        public void ShowMainMenu()
        {
            UpdateState(MenuViewState.MainMenu());
        }

        private void UpdateState(MenuViewState newState)
        {
            _state = newState;
            StateChanged?.Invoke(_state);
        }
    }

    public readonly struct MenuViewState
    {
        private MenuViewState(bool showMainMenu, bool showSelection)
        {
            ShowMainMenu = showMainMenu;
            ShowSelection = showSelection;
        }

        public bool ShowMainMenu { get; }

        public bool ShowSelection { get; }

        public static MenuViewState MainMenu() => new MenuViewState(true, false);

        public static MenuViewState SelectionRequested() => new MenuViewState(true, false);

        public static MenuViewState Selection() => new MenuViewState(false, true);
    }
}
