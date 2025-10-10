using System;

namespace QuickTableProyect.Aplicacion
{
    public class PasswordService
    {
        private const int WorkFactor = 12;

        public string HashPassword(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword))
            {
                throw new ArgumentException("La contraseña no puede estar vacía");
            }

            return BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);
        }

        public bool VerifyPassword(string plainPassword, string hashedPassword)
        {
            if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(hashedPassword))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
            }
            catch
            {
                return false;
            }
        }
    }
}
