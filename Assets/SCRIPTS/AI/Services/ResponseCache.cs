using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using QuantumAI.Config;

namespace QuantumAI.Services
{
    /// <summary>
    /// Sistema de caché para respuestas de IA
    /// Reduce costos de API al almacenar respuestas comunes
    /// </summary>
    public class ResponseCache : MonoBehaviour
    {
        private Dictionary<string, CachedResponse> cache;
        private AIConfig config;
        private int maxEntries = 100;
        private int cacheDurationMinutes = 60;

        private void Awake()
        {
            cache = new Dictionary<string, CachedResponse>();
            LoadConfig();
        }

        private void LoadConfig()
        {
            config = Core.QuantumAICore.Instance?.Config;
            if (config != null)
            {
                maxEntries = config.maxCacheEntries;
                cacheDurationMinutes = config.cacheDurationMinutes;
            }
        }

        /// <summary>
        /// Verifica si existe una respuesta cacheada válida
        /// </summary>
        public bool HasResponse(string query)
        {
            if (!IsEnabled()) return false;

            string key = GenerateKey(query);

            if (cache.ContainsKey(key))
            {
                var cached = cache[key];
                if (!cached.IsExpired(cacheDurationMinutes))
                {
                    cached.hitCount++;
                    return true;
                }
                else
                {
                    // Expirado, remover
                    cache.Remove(key);
                }
            }

            return false;
        }

        /// <summary>
        /// Obtiene una respuesta del caché
        /// </summary>
        public string GetResponse(string query)
        {
            if (!IsEnabled()) return null;

            string key = GenerateKey(query);

            if (cache.ContainsKey(key))
            {
                var cached = cache[key];
                if (!cached.IsExpired(cacheDurationMinutes))
                {
                    cached.lastAccessed = DateTime.Now;
                    cached.hitCount++;
                    LogCacheHit(query);
                    return cached.response;
                }
                else
                {
                    cache.Remove(key);
                }
            }

            return null;
        }

        /// <summary>
        /// Almacena una respuesta en el caché
        /// </summary>
        public void StoreResponse(string query, string response)
        {
            if (!IsEnabled()) return;
            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(response)) return;

            string key = GenerateKey(query);

            // Si el caché está lleno, remover la entrada menos usada
            if (cache.Count >= maxEntries && !cache.ContainsKey(key))
            {
                RemoveLeastUsedEntry();
            }

            cache[key] = new CachedResponse
            {
                query = query,
                response = response,
                createdAt = DateTime.Now,
                lastAccessed = DateTime.Now,
                hitCount = 0
            };

            LogCacheStore(query);
        }

        /// <summary>
        /// Limpia el caché completamente
        /// </summary>
        public void Clear()
        {
            cache.Clear();
            Debug.Log("[ResponseCache] Caché limpiado");
        }

        /// <summary>
        /// Limpia entradas expiradas
        /// </summary>
        public void ClearExpired()
        {
            List<string> toRemove = new List<string>();

            foreach (var kvp in cache)
            {
                if (kvp.Value.IsExpired(cacheDurationMinutes))
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (string key in toRemove)
            {
                cache.Remove(key);
            }

            if (toRemove.Count > 0)
            {
                Debug.Log($"[ResponseCache] Removidas {toRemove.Count} entradas expiradas");
            }
        }

        /// <summary>
        /// Obtiene estadísticas del caché
        /// </summary>
        public CacheStats GetStats()
        {
            int totalHits = 0;
            int expiredCount = 0;

            foreach (var entry in cache.Values)
            {
                totalHits += entry.hitCount;
                if (entry.IsExpired(cacheDurationMinutes))
                {
                    expiredCount++;
                }
            }

            return new CacheStats
            {
                totalEntries = cache.Count,
                totalHits = totalHits,
                expiredEntries = expiredCount,
                maxEntries = maxEntries
            };
        }

        #region Private Methods

        private bool IsEnabled()
        {
            return config != null && config.enableResponseCache;
        }

        private string GenerateKey(string query)
        {
            // Normalizar query
            string normalized = query.ToLower().Trim();

            // Generar hash MD5 para la clave
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(normalized);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private void RemoveLeastUsedEntry()
        {
            string leastUsedKey = null;
            int minHitCount = int.MaxValue;
            DateTime oldestAccess = DateTime.MaxValue;

            foreach (var kvp in cache)
            {
                // Priorizar por menor hitCount, luego por acceso más antiguo
                if (kvp.Value.hitCount < minHitCount ||
                    (kvp.Value.hitCount == minHitCount && kvp.Value.lastAccessed < oldestAccess))
                {
                    minHitCount = kvp.Value.hitCount;
                    oldestAccess = kvp.Value.lastAccessed;
                    leastUsedKey = kvp.Key;
                }
            }

            if (leastUsedKey != null)
            {
                cache.Remove(leastUsedKey);
                Debug.Log($"[ResponseCache] Entrada menos usada removida (hits: {minHitCount})");
            }
        }

        private void LogCacheHit(string query)
        {
            if (config != null && config.enableDebugLogs)
            {
                string preview = query.Length > 50 ? query.Substring(0, 50) + "..." : query;
                Debug.Log($"[ResponseCache] Cache HIT: {preview}");
            }
        }

        private void LogCacheStore(string query)
        {
            if (config != null && config.enableDebugLogs)
            {
                string preview = query.Length > 50 ? query.Substring(0, 50) + "..." : query;
                Debug.Log($"[ResponseCache] Almacenado: {preview}");
            }
        }

        #endregion

        #region Maintenance

        private void Update()
        {
            // Limpiar entradas expiradas cada 5 minutos
            if (Time.frameCount % (60 * 5 * 60) == 0) // Aproximadamente cada 5 min
            {
                ClearExpired();
            }
        }

        #endregion
    }

    /// <summary>
    /// Entrada del caché
    /// </summary>
    [System.Serializable]
    public class CachedResponse
    {
        public string query;
        public string response;
        public DateTime createdAt;
        public DateTime lastAccessed;
        public int hitCount;

        public bool IsExpired(int durationMinutes)
        {
            return (DateTime.Now - createdAt).TotalMinutes > durationMinutes;
        }
    }

    /// <summary>
    /// Estadísticas del caché
    /// </summary>
    [System.Serializable]
    public class CacheStats
    {
        public int totalEntries;
        public int totalHits;
        public int expiredEntries;
        public int maxEntries;

        public float UsagePercentage => maxEntries > 0 ? (float)totalEntries / maxEntries * 100f : 0f;
        public float HitRate => totalEntries > 0 ? (float)totalHits / totalEntries : 0f;
    }
}
