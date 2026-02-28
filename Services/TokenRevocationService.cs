using System.Collections.Concurrent;

namespace PATHFINDER_BACKEND.Services
{
    /// <summary>
    /// Tracks revoked JWT JTIs (for logout).
    /// NOTE: This is in-memory and will reset when the app restarts.
    /// For production or multi-instance deployments, use a shared store (Redis/DB).
    /// </summary>
    public class TokenRevocationService
    {
        private readonly ConcurrentDictionary<string, DateTime> _revokedJtis = new();

        public void Revoke(string jti, DateTime expiresUtc)
        {
            CleanupExpired();
            _revokedJtis[jti] = expiresUtc;
        }

        public bool IsRevoked(string jti)
        {
            CleanupExpired();
            return _revokedJtis.ContainsKey(jti);
        }

        private void CleanupExpired()
        {
            // Keep dictionary small by removing tokens already expired.
            var now = DateTime.UtcNow;
            foreach (var pair in _revokedJtis)
            {
                if (pair.Value <= now)
                {
                    _revokedJtis.TryRemove(pair.Key, out _);
                }
            }
        }
    }
}