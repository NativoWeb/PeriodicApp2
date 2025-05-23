// Enums.cs
using UnityEngine;

public enum RankingMode { General, Amigos, Comunidades }

public interface IRankingObserver
{
    void OnRankingStateChanged(RankingMode newMode, string comunidadId);
}