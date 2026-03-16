using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickTableProyect.Aplicacion;
using System;


namespace QuickTableProyect.Tests
{
    [TestClass]
    public class PasswordServiceTests
    {
        private PasswordService passwordService;

        [TestInitialize]
        public void SetUp()
        {
            passwordService = new PasswordService();
        }

        [TestMethod]
        public void HashPassword_DebeRetornarHashDistintoAlPlano()
        {
            // Arrange
            string passwordPlano = "Mesero123!";

            // Act
            string hash = passwordService.HashPassword(passwordPlano);

            // Assert
            Assert.IsNotNull(hash, "El hash no debe ser nulo.");
            Assert.AreNotEqual(passwordPlano, hash, "El hash no debe ser igual al texto plano.");
            Assert.IsTrue(hash.Length > 20, "El hash debería tener longitud mayor que el password original.");
        }

        [TestMethod]
        public void VerifyPassword_DebeRetornarTrue_ConPasswordCorrecto()
        {
            // Arrange
            string passwordPlano = "AdminSecure!";
            string hash = passwordService.HashPassword(passwordPlano);

            // Act
            bool esValido = passwordService.VerifyPassword(passwordPlano, hash);

            // Assert
            Assert.IsTrue(esValido, "La verificación debe ser verdadera con la contraseña correcta.");
        }

        [TestMethod]
        public void VerifyPassword_DebeRetornarFalse_ConPasswordIncorrecto()
        {
            // Arrange
            string passwordCorrecta = "Cajero123!";
            string hash = passwordService.HashPassword(passwordCorrecta);
            string passwordIncorrecta = "Cajero1234!";

            // Act
            bool esValido = passwordService.VerifyPassword(passwordIncorrecta, hash);

            // Assert
            Assert.IsFalse(esValido, "La verificación debe ser falsa con la contraseña incorrecta.");
        }

        [TestMethod]
        public void HashPassword_DebeLanzarExcepcion_SiPasswordEsVacio()
        {
            // Arrange
            string passwordVacia = "";

            try
            {
                // Act
                passwordService.HashPassword(passwordVacia);

                // Si llega aquí, NO lanzó excepción y la prueba debe fallar
                Assert.Fail("Se esperaba una ArgumentException cuando la contraseña está vacía, pero no se lanzó ninguna excepción.");
            }
            catch (ArgumentException)
            {
                // OK: se lanzó la excepción esperada
                // No hacemos nada, la prueba pasa
            }
            catch (Exception ex)
            {
                // Se lanzó una excepción diferente: la prueba debe fallar
                Assert.Fail($"Se esperaba ArgumentException, pero se lanzó: {ex.GetType().Name}");
            }
        }



        [TestMethod]
        public void VerifyPassword_DebeRetornarFalse_SiHashEsNuloOVacio()
        {
            // Arrange
            string passwordPlano = "Mesero123!";

            // Act
            bool resultadoConHashNulo = passwordService.VerifyPassword(passwordPlano, null);
            bool resultadoConHashVacio = passwordService.VerifyPassword(passwordPlano, "");

            // Assert
            Assert.IsFalse(resultadoConHashNulo, "Debe devolver false si el hash es nulo.");
            Assert.IsFalse(resultadoConHashVacio, "Debe devolver false si el hash es vacío.");
        }
    }
}
