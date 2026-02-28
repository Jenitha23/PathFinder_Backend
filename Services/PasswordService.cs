namespace PATHFINDER_BACKEND.Services
{
    /// <summary>
    /// Handles password hashing & verification.
    /// BCrypt automatically salts hashes which improves security.
    /// </summary>
    public class PasswordService
    {
        public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
    }
}