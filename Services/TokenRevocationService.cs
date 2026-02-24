using System.Collections.Concurrent;

namespace PATHFINDER_BACKEND.Services
{
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
