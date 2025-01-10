using System.Data;
using System.Data.Common;
using Trino.Data.ADO;
using Trino.Data.ADO.Client;

namespace Trino.Client.Test
{
    [TestClass]
    public class TrinoParameterCollectionTests
    {
        private static TrinoParameterCollection CreateCollection()
        {
            return new TrinoParameterCollection();
        }

        private static DbParameter CreateParameter(string name, object value)
        {
            return new TrinoParameter
            {
                ParameterName = name,
                Value = value
            };
        }

        [TestMethod]
        public void Add_ValidParameter_AddsToCollection()
        {
            var collection = CreateCollection();
            var parameter = CreateParameter("@param1", "value1");

            var index = collection.Add(parameter);

            Assert.AreEqual(0, index);
            Assert.AreEqual(1, collection.Count);
            Assert.AreSame(parameter, collection[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_NullParameter_ThrowsArgumentNullException()
        {
            var collection = CreateCollection();

            collection.Add(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Add_NonParameterObject_ThrowsArgumentException()
        {
            var collection = CreateCollection();

            collection.Add("not a parameter");
        }

        [TestMethod]
        public void Contains_ExistingParameterName_ReturnsTrue()
        {
            var collection = CreateCollection();
            var parameter = CreateParameter("@param1", "value1");
            collection.Add(parameter);

            var contains = collection.Contains("@param1");

            Assert.IsTrue(contains);
        }

        [TestMethod]
        public void Contains_NonExistingParameterName_ReturnsFalse()
        {
            var collection = CreateCollection();

            var contains = collection.Contains("@nonexistent");

            Assert.IsFalse(contains);
        }

        [TestMethod]
        public void Clear_RemovesAllParameters()
        {
            var collection = CreateCollection();
            collection.Add(CreateParameter("@param1", "value1"));
            collection.Add(CreateParameter("@param2", "value2"));

            collection.Clear();

            Assert.AreEqual(0, collection.Count);
        }

        [TestMethod]
        public void IndexOfKey_ExistingParameter_ReturnsCorrectIndex()
        {
            var collection = CreateCollection();
            var parameter1 = CreateParameter("@param1", "value1");
            var parameter2 = CreateParameter("@param2", "value2");
            collection.Add(parameter1);
            collection.Add(parameter2);

            var index = collection.IndexOf("@param2");

            Assert.AreEqual(1, index);
        }

        [TestMethod]
        public void RemoveAt_ValidIndex_RemovesParameter()
        {
            var collection = CreateCollection();
            collection.Add(CreateParameter("@param1", "value1"));
            collection.Add(CreateParameter("@param2", "value2"));

            collection.RemoveAt(0);

            Assert.AreEqual(1, collection.Count);
            Assert.AreEqual("@param2", ((IDataParameter)collection[0]).ParameterName);
        }

        [TestMethod]
        public void RemoveAt_ParameterName_RemovesCorrectParameter()
        {
            var collection = CreateCollection();
            collection.Add(CreateParameter("@param1", "value1"));
            collection.Add(CreateParameter("@param2", "value2"));

            collection.RemoveAt("@param1");

            Assert.AreEqual(1, collection.Count);
            Assert.AreEqual("@param2", ((IDataParameter)collection[0]).ParameterName);
        }

        [TestMethod]
        public void Insert_ValidParameter_InsertsAtCorrectPosition()
        {
            var collection = CreateCollection();
            collection.Add(CreateParameter("@param1", "value1"));
            collection.Add(CreateParameter("@param3", "value3"));
            var parameter2 = CreateParameter("@param2", "value2");

            collection.Insert(1, parameter2);

            Assert.AreEqual(3, collection.Count);
            Assert.AreEqual("@param2", ((IDataParameter)collection[1]).ParameterName);
        }

        [TestMethod]
        public void GetEnumerator_ReturnsAllParameters()
        {
            var collection = CreateCollection();
            var parameter1 = CreateParameter("@param1", "value1");
            var parameter2 = CreateParameter("@param2", "value2");
            collection.Add(parameter1);
            collection.Add(parameter2);

            var parameters = collection.Cast<IDataParameter>().ToList();

            Assert.AreEqual(2, parameters.Count);
            CollectionAssert.Contains(parameters, parameter1);
            CollectionAssert.Contains(parameters, parameter2);
        }

        [TestMethod]
        public void Contains_CaseInsensitiveParameterName_ReturnsTrue()
        {
            var collection = CreateCollection();
            collection.Add(CreateParameter("@PARAM1", "value1"));

            var contains = collection.Contains("@param1");

            Assert.IsTrue(contains);
        }

        [TestMethod]
        public void AddRange_ValidParameters_AddsAllParameters()
        {
            var collection = CreateCollection();
            var parameters = new[]
            {
                CreateParameter("@param1", "value1"),
                CreateParameter("@param2", "value2")
            };

            collection.AddRange(parameters);

            Assert.AreEqual(2, collection.Count);
            Assert.AreEqual("@param1", ((IDataParameter)collection[0]).ParameterName);
            Assert.AreEqual("@param2", ((IDataParameter)collection[1]).ParameterName);
        }

        [TestMethod]
        public void Remove_ExistingParameter_RemovesFromCollection()
        {
            var collection = CreateCollection();
            var parameter = CreateParameter("@param1", "value1");
            collection.Add(parameter);
            Assert.AreEqual(1, collection.Count);

            collection.Remove(parameter);

            Assert.AreEqual(0, collection.Count);
            Assert.IsFalse(collection.Contains(parameter));
        }

        [TestMethod]
        public void Remove_NonExistingParameter_CollectionRemainsSame()
        {
            var collection = CreateCollection();
            var parameter1 = CreateParameter("@param1", "value2");
            var parameter2 = CreateParameter("@param2", "value2");
            collection.Add(parameter1);

            collection.Remove(parameter2);

            Assert.AreEqual(1, collection.Count);
            Assert.IsTrue(collection.Contains(parameter1));
        }


        [TestMethod]
        public void IndexOf_ExistingParameter_ReturnsCorrectIndex()
        {
            // Arrange
            var collection = CreateCollection();
            var parameter1 = CreateParameter("@param1", "value1");
            var parameter2 = CreateParameter("@param2", "value2");
            collection.Add(parameter1);
            collection.Add(parameter2);

            var index = collection.IndexOf(parameter2);

            Assert.AreEqual(1, index);
        }

        [TestMethod]
        public void IndexOf_NonExistingParameter_ReturnsNegativeOne()
        {
            var collection = CreateCollection();
            var parameter1 = CreateParameter("@param1", "value1");
            collection.Add(parameter1);
            var nonExistingParameter = CreateParameter("@param2", "value2");

            var index = collection.IndexOf(nonExistingParameter);

            Assert.AreEqual(-1, index);
        }

        [TestMethod]
        public void IndexOf_EmptyCollection_ReturnsNegativeOne()
        {
            var collection = CreateCollection();
            var parameter = CreateParameter("@param1", "value1");

            var index = collection.IndexOf(parameter);

            Assert.AreEqual(-1, index);
        }

        [TestMethod]
        public void CopyTo_ValidArray_CopiesAllElements()
        {
            var collection = CreateCollection();
            var parameter1 = CreateParameter("@param1", "value1");
            var parameter2 = CreateParameter("@param2", "value2");
            collection.Add(parameter1);
            collection.Add(parameter2);
            var targetArray = new IDataParameter[2];

            collection.CopyTo(targetArray, 0);

            Assert.AreEqual(parameter1, targetArray[0]);
            Assert.AreEqual(parameter2, targetArray[1]);
        }
    }
}