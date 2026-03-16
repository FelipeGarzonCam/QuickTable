using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickTableProyect.Aplicacion;

namespace QuickTableProyect.Tests
{
    [TestClass]
    public class CryptoServiceTests
    {
        private CryptoService cryptoService;

        [TestInitialize]
        public void SetUp()
        {
            cryptoService = new CryptoService();
        }

        [TestMethod]
        public void EncriptarYDesencriptarUID_DebeRecuperarElUIDOriginal()
        {
            // Arrange
            string uidOriginal = "ABCDEF123456";

            // Act
            string uidEncriptado = cryptoService.EncriptarUID(uidOriginal);
            string uidDesencriptado = cryptoService.DesencriptarUID(uidEncriptado);

            // Assert
            Assert.IsNotNull(uidEncriptado, "El UID encriptado no debe ser nulo.");
            Assert.AreEqual(uidOriginal, uidDesencriptado, "El UID desencriptado debe coincidir con el valor original.");
        }

        [TestMethod]
        public void DesencriptarUID_DebeRetornarMismoValorSiNoEsBase64Valido()
        {
            // Arrange
            string valorInvalido = "no-es-base64";

            // Act
            string resultado = cryptoService.DesencriptarUID(valorInvalido);

            // Assert
            Assert.AreEqual(valorInvalido, resultado, "Si la desencriptación falla, debe devolverse el valor original.");
        }

        [TestMethod]
        public void EncriptarUID_DebeSerDeterministico_ParaMismoInput()
        {
            // Arrange
            string uidOriginal = "112233AABBCC";

            // Act
            string uidEncriptado1 = cryptoService.EncriptarUID(uidOriginal);
            string uidEncriptado2 = cryptoService.EncriptarUID(uidOriginal);

            // Assert
            // Si en el futuro cambias la implementación para que use IV aleatorio,
            // esta prueba te avisará del cambio de comportamiento.
            Assert.AreEqual(uidEncriptado1, uidEncriptado2, "La encriptación debería ser determinística para el mismo UID.");
        }
    }
}
