using UnityEngine;
using System.Collections.Generic;

public class RankingStateManager : MonoBehaviour
{
    private static RankingStateManager _instance;
    public static RankingStateManager Instance => _instance;

    private RankingMode _currentMode = RankingMode.General;
    private string _selectedComunidadId;
    private bool _isNotifying = false;

    private readonly List<IRankingObserver> _observers = new List<IRankingObserver>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void RegisterObserver(IRankingObserver observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
        }
    }

    public void UnregisterObserver(IRankingObserver observer)
    {
        if (_observers.Contains(observer))
        {
            _observers.Remove(observer);
        }
    }

    public void SwitchToGeneral()
    {
        if (_currentMode == RankingMode.General || _isNotifying) return;

        _isNotifying = true;
        _currentMode = RankingMode.General;
        _selectedComunidadId = null;
        NotifyObservers();
        _isNotifying = false;
    }

    public void SwitchToAmigos()
    {
        if (_currentMode == RankingMode.Amigos || _isNotifying) return;

        _isNotifying = true;
        _currentMode = RankingMode.Amigos;
        _selectedComunidadId = null;
        NotifyObservers();
        _isNotifying = false;
    }

    public void SwitchToComunidades(string comunidadId = null)
    {
        if ((_currentMode == RankingMode.Comunidades && _selectedComunidadId == comunidadId) || _isNotifying) return;

        _isNotifying = true;
        _currentMode = RankingMode.Comunidades;
        _selectedComunidadId = comunidadId;
        NotifyObservers();
        _isNotifying = false;
    }

    private void NotifyObservers()
    {
        foreach (var observer in _observers)
        {
            observer.OnRankingStateChanged(_currentMode, _selectedComunidadId);
        }
    }
}