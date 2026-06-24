using System.Collections.Generic;
using System.Linq;
using Azure.Maps.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurtleRoute.Tests
{
    [TestClass]
    public class ConfidenceComparerTests
    {
        private readonly ConfidenceComparer _comparer = new();

        [TestMethod]
        public void High_RanksBefore_Medium()
            => Assert.IsLessThan(0, _comparer.Compare(ConfidenceEnum.High, ConfidenceEnum.Medium));

        [TestMethod]
        public void Medium_RanksBefore_Low()
            => Assert.IsLessThan(0, _comparer.Compare(ConfidenceEnum.Medium, ConfidenceEnum.Low));

        [TestMethod]
        public void High_RanksBefore_Low()
            => Assert.IsLessThan(0, _comparer.Compare(ConfidenceEnum.High, ConfidenceEnum.Low));

        [TestMethod]
        public void Low_RanksBefore_Null()
            => Assert.IsLessThan(0, _comparer.Compare(ConfidenceEnum.Low, null));

        [TestMethod]
        public void Medium_RanksBefore_Null()
            => Assert.IsLessThan(0, _comparer.Compare(ConfidenceEnum.Medium, null));

        [TestMethod]
        public void High_RanksBefore_Null()
            => Assert.IsLessThan(0, _comparer.Compare(ConfidenceEnum.High, null));

        [TestMethod]
        public void Medium_RanksAfter_High()
            => Assert.IsGreaterThan(0, _comparer.Compare(ConfidenceEnum.Medium, ConfidenceEnum.High));

        [TestMethod]
        public void Low_RanksAfter_Medium()
            => Assert.IsGreaterThan(0, _comparer.Compare(ConfidenceEnum.Low, ConfidenceEnum.Medium));

        [TestMethod]
        public void Null_RanksAfter_High()
            => Assert.IsGreaterThan(0, _comparer.Compare(null, ConfidenceEnum.High));

        [TestMethod]
        public void Null_RanksAfter_Medium()
            => Assert.IsGreaterThan(0, _comparer.Compare(null, ConfidenceEnum.Medium));

        [TestMethod]
        public void Null_RanksAfter_Low()
            => Assert.IsGreaterThan(0, _comparer.Compare(null, ConfidenceEnum.Low));

        [TestMethod]
        public void EqualHigh_ReturnsZero()
            => Assert.AreEqual(0, _comparer.Compare(ConfidenceEnum.High, ConfidenceEnum.High));

        [TestMethod]
        public void EqualMedium_ReturnsZero()
            => Assert.AreEqual(0, _comparer.Compare(ConfidenceEnum.Medium, ConfidenceEnum.Medium));

        [TestMethod]
        public void EqualLow_ReturnsZero()
            => Assert.AreEqual(0, _comparer.Compare(ConfidenceEnum.Low, ConfidenceEnum.Low));

        [TestMethod]
        public void EqualNull_ReturnsZero()
            => Assert.AreEqual(0, _comparer.Compare(null, null));

        [TestMethod]
        public void OrderBy_ProducesHighMediumLowNullOrder()
        {
            ConfidenceEnum?[] input = { ConfidenceEnum.Low, null, ConfidenceEnum.High, ConfidenceEnum.Medium, ConfidenceEnum.Low };

            List<ConfidenceEnum?> sorted = input.OrderBy(c => c, _comparer).ToList();

            Assert.AreEqual(ConfidenceEnum.High, sorted[0]);
            Assert.AreEqual(ConfidenceEnum.Medium, sorted[1]);
            Assert.AreEqual(ConfidenceEnum.Low, sorted[2]);
            Assert.AreEqual(ConfidenceEnum.Low, sorted[3]);
            Assert.IsNull(sorted[4]);
        }

        [TestMethod]
        public void OrderBy_AllSameConfidence_PreservesStability()
        {
            ConfidenceEnum?[] input = { ConfidenceEnum.Medium, ConfidenceEnum.Medium, ConfidenceEnum.Medium };

            List<ConfidenceEnum?> sorted = input.OrderBy(c => c, _comparer).ToList();

            Assert.HasCount(3, sorted);
            Assert.IsTrue(sorted.All(c => c == ConfidenceEnum.Medium));
        }

        [TestMethod]
        public void OrderBy_AllNull_ReturnsAllNull()
        {
            ConfidenceEnum?[] input = { null, null, null };

            List<ConfidenceEnum?> sorted = input.OrderBy(c => c, _comparer).ToList();

            Assert.HasCount(3, sorted);
            Assert.IsTrue(sorted.All(c => c == null));
        }
    }
}