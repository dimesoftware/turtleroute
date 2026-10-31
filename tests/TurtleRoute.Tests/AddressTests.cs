using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    [TestClass]
    public class AddressTests
    {
        [TestMethod]
        [DataRow("undefined")]
        [DataRow("NULL")]
        [DataRow("none")]
        [DataRow("NaN")]
        [DataRow("missing")]
        [DataRow("-")]
        [DataRow(" , ")]
        [DataRow("XXXX")]
        public void Constructor_Placeholder_ShouldBeNull(string placeholder)
        {
            Address address = new("Katwilgweg", "2", placeholder, "Antwerpen", placeholder, "BE");

            Assert.IsNull(address.ZipCode);
            Assert.IsNull(address.State);
            Assert.AreEqual("Katwilgweg, 2, Antwerpen, BE", address.ToString());
        }

        [TestMethod]
        public void Constructor_LineBreaks_ShouldBeCommaSeparated()
        {
            Address address = new("Katwilgweg 2\r\nbus 3\n", "", "2050", "Antwerpen", "", "BE");

            Assert.AreEqual("Katwilgweg 2, bus 3", address.Street);
        }

        [TestMethod]
        public void Constructor_OnlyPlaceholders_ShouldNotThrow()
        {
            Address address = new("-", "", "", "none", "", "undefined");

            Assert.IsNull(address.Street);
            Assert.IsNull(address.City);
            Assert.IsNull(address.Country);
        }

        [TestMethod]
        public void Constructor_Empty_ShouldThrow()
            => Assert.Throws<ArgumentException>(() => new Address("", "2", "2050", " ", "", null));
    }
}
