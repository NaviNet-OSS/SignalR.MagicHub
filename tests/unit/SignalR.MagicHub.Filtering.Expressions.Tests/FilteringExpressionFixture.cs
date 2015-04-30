using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;

namespace SignalR.MagicHub.Filtering.Expressions.Tests
{
    /// <summary>
    /// Test fixture for filtering expressions logic
    /// </summary>
    ///<remarks>Covers basic logic to test all filtering operators: GreaterThan, GreaterThanOrEqualTo, EqualTo, NotEqualTo, LessThanOrEqualTo, LessThan, And, Or </remarks>
    [TestFixture]
    public class FilteringExpressionFixture
    {
        #region Variable expression
        
        [Test]
        public void Test_Variable_expression_when_context_is_null()
        {
            var expression = new VariableExpression("Topic");
            var mock = new Mock<IReadOnlyDictionary<string, object>>();

            var result = expression.EvaluateAsync(mock.Object);
            
            Assert.IsTrue(result.Result.Equals(NullEvaluationResult.Value));
        }

        [Test]
        public void Test_Variable_expression_when_is_not_icompatible()
        {
            var expression = new VariableExpression("Topic");
            var mock = new Mock<IReadOnlyDictionary<string, object>>();
            object officeNID;
            mock.Setup(s => s.TryGetValue("OfficeNID", out officeNID)).Returns(true).Callback(() => { officeNID = new byte[]{1,2,3,4}; });
            var result = expression.EvaluateAsync(mock.Object);

            Assert.IsTrue(result.Result.Equals(NullEvaluationResult.Value));
        }

        #endregion

        #region Border cases

        /// <summary>
        /// Expression Is Not Set
        /// </summary>
        [Test]
        public void Test_when_Expression_is_not_set()
        {
            //Arrange
            var expression = new LogicalExpression(null, null, 0);

            // Act
            var evaluation = expression.EvaluateAsync(GetDefaultMessageContext);

            // Assert
            Assert.IsTrue((bool)evaluation.Result);
        }

        /// <summary>
        /// Left part of Expression Is Not Set
        /// </summary>
        [Test]
        public void Test_when_Left_part_of_Expression_is_not_set()
        {
            try
            {
                //Arrange
                var expression = new LogicalExpression(null, new ConstantExpression("10"), FilterOperator.EqualTo);

                // Act
                var evaluation = expression.EvaluateAsync(GetDefaultMessageContext);

                // Assert
                var result = evaluation.Result is bool;
            }
            catch (AggregateException ae)
            {
                Assert.IsTrue(ae.Flatten().InnerExceptions.Select(e => e.GetType() == typeof(NullReferenceException)).Any());
                return;
            }
            Assert.Fail("Expected NullReferenceException did not occur");
        }

        /// <summary>
        /// Right part of Expression Is Not Set
        /// </summary>
        [Test]
        public void Test_when_Right_part_of_Expression_is_not_set()
        {
            try
            {
                //Arrange
                var expression = new LogicalExpression(new ConstantExpression("10"), null, FilterOperator.EqualTo);

                // Act
                var evaluation = expression.EvaluateAsync(GetDefaultMessageContext);

                // Assert
                var result = evaluation.Result is bool;
            }
            catch (AggregateException ae)
            {
                Assert.IsTrue(ae.Flatten().InnerExceptions.Select(e => e.GetType() == typeof(NullReferenceException)).Any());
                return;
            }
            Assert.Fail("Expected NullReferenceException did not occur");
        }

        /// <summary>
        /// Message context Is Not Set
        /// </summary>
        [Test]
        public void Test_when_Context_is_not_set()
        {
            //Arrange
            var expression = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("10"), FilterOperator.EqualTo);
            try
            {
                // Act
                var evaluation = expression.EvaluateAsync(null);
                var result = evaluation.Result is bool;
            }
            catch (AggregateException ae)
            {
                Assert.IsTrue(ae.Flatten().InnerExceptions.Select(e => e.GetType() == typeof(NullReferenceException)).Any());
                return;
            }
            // Assert
            Assert.Fail("Expected Exception was not thrown");
        }

        /// <summary>
        /// Right part of Expression Is Not Set
        /// </summary>
        [Test]
        public void Test_when_Context_does_not_have_a_key()
        {
            //Arrange
            var expression = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("10"), FilterOperator.EqualTo);
            // Act
            var mock = new Mock<IReadOnlyDictionary<string, IComparable>>();

            var evaluation = expression.EvaluateAsync(new Mock<IReadOnlyDictionary<string, object>>().Object);

            //Assert
            Assert.IsFalse((bool)evaluation.Result);
        }

        /// <summary>
        /// Checks if OfficeNid is less than 10 is false, because in fact it 15 {OfficeNID,15}
        /// </summary>
        [Test]
        public void Test_when_Context_does_not_have_a_key_Is_Not_LessThan_10()
        {
            //Arrange
            var expression = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("10"), FilterOperator.LessThan);            

            //Act
            var evaluation = expression.EvaluateAsync(new Mock<IReadOnlyDictionary<string, object>>().Object);

            // Assert
            Assert.IsFalse((bool)evaluation.Result);
        }
        #endregion

        #region And

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public void Test_that_officenid_equals_15_AND_equals_15()
        {
            // Arrange
            var left = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("15"), FilterOperator.EqualTo);
            var right = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("15"), FilterOperator.EqualTo);
            var expression = new LogicalExpression(left, right, FilterOperator.And);

            // Act
            var evaluation = expression.EvaluateAsync(GetDefaultMessageContext);
            // Assert
            Assert.IsTrue((bool)evaluation.Result);
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public void Test_that_officenid_equals_10_AND_equals_15_false()
        {
            // Arrange
            var left = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("10"), FilterOperator.EqualTo);
            var right = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("15"), FilterOperator.EqualTo);
            var expression = new LogicalExpression(left, right, FilterOperator.And);

            // Act
            var evaluation = expression.EvaluateAsync(GetDefaultMessageContext);
            // Assert
            Assert.IsFalse((bool)evaluation.Result);
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public void Test_that_officenid_equals_10_AND_equals_10_false()
        {
            // Arrange
            var left = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("10"), FilterOperator.EqualTo);
            var right = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("10"), FilterOperator.EqualTo);
            var expression = new LogicalExpression(left, right, FilterOperator.And);

            // Act
            var evaluation = expression.EvaluateAsync(GetDefaultMessageContext);
            // Assert
            Assert.IsFalse((bool)evaluation.Result);
        }

        #endregion

        #region Or

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public void Test_that_officenid_equals_15_OR_equals_15()
        {
            // Arrange
            var left = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("15"), FilterOperator.EqualTo);
            var right = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("15"), FilterOperator.EqualTo);
            var expression = new LogicalExpression(left, right, FilterOperator.Or);

            // Act
            var evaluation = expression.EvaluateAsync(GetDefaultMessageContext);
            // Assert
            Assert.IsTrue((bool)evaluation.Result);
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public void Test_that_officenid_equals_10_OR_equals_15()
        {
            // Arrange
            var left = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("10"), FilterOperator.EqualTo);
            var right = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("15"), FilterOperator.EqualTo);
            var expression = new LogicalExpression(left, right, FilterOperator.Or);

            // Act
            var evaluation = expression.EvaluateAsync(GetDefaultMessageContext);
            // Assert
            Assert.IsTrue((bool)evaluation.Result);
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public void Test_that_officenid_equals_10_OR_equals_20_false()
        {
            // Arrange
            var left = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("10"), FilterOperator.EqualTo);
            var right = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression("20"), FilterOperator.EqualTo);
            var expression = new LogicalExpression(left, right, FilterOperator.Or);

            // Act
            var evaluation = expression.EvaluateAsync(GetDefaultMessageContext);
            // Assert
            Assert.IsFalse((bool)evaluation.Result);
        }

        #endregion

        #region Operators Logic

        /// <summary>
        /// Logical Expression test for operations like GreaterThan, GreaterThanOrEqualTo, EqualTo, NotEqualTo, LessThanOrEqualTo, LessThan 
        /// </summary>
        [Test]
        public void Test_Logical_Expressions()
        {
            // Arrange
            var logicBox = new List<LogicCondition>();

            //Checks if OfficeNid greater than 10 is true, because in fact it is 15 
            logicBox.Add(new LogicCondition("10", FilterOperator.GreaterThan, true));
            // Checks if OfficeNid greater than 15 is false, because in fact it is 15 
            logicBox.Add(new LogicCondition("15", FilterOperator.GreaterThan, false));

            // Checks if OfficeNid greater than or equals to 10 is true, because in fact it is 15 
            logicBox.Add(new LogicCondition("10", FilterOperator.GreaterThanOrEqualTo, true));
            // Checks if OfficeNid greater than or equals to 15 is true, because in fact it is 15 
            logicBox.Add(new LogicCondition("15", FilterOperator.GreaterThanOrEqualTo, true));
            // Checks if OfficeNid greater than or equals to 20 is false, because in fact it is 15 
            logicBox.Add(new LogicCondition("20", FilterOperator.GreaterThanOrEqualTo, false));

            // Checks if OfficeNid Equals 15 is true, when in fact it is 15 
            logicBox.Add(new LogicCondition("15", FilterOperator.EqualTo, true));
            // Checks if OfficeNid Equals 10 is false, when in fact it is 15 
            logicBox.Add(new LogicCondition("10", FilterOperator.EqualTo, false));

            // Checks if OfficeNid NotEquals 15 is false, because in fact it is 15 
            logicBox.Add(new LogicCondition("15", FilterOperator.NotEqualTo, false));
            // Checks if OfficeNid NotEquals 5 is true, because in fact it is 15 
            logicBox.Add(new LogicCondition("5", FilterOperator.NotEqualTo, true));

            // Checks if OfficeNid is less than or equals 10 is false, because in fact it 15 
            logicBox.Add(new LogicCondition("10", FilterOperator.LessThanOrEqualTo, false));
            // Checks if OfficeNid is less than or equals 15 is true, because in fact it equals 15 
            logicBox.Add(new LogicCondition("15", FilterOperator.LessThanOrEqualTo, true));
            // Checks if OfficeNid is less than or equals 20 is true, because in fact it equals 15 
            logicBox.Add(new LogicCondition("20", FilterOperator.LessThanOrEqualTo, true));

            // Checks if OfficeNid is less than 10 is false, because in fact it is 15 
            logicBox.Add(new LogicCondition("10", FilterOperator.LessThan, false));
            // Checks if OfficeNid is less than 15 is false, because in fact it is 15 
            logicBox.Add(new LogicCondition("15", FilterOperator.LessThan, false));

            // Assert
            foreach (var variable in logicBox)
            {
                LogicEvaluationFactory(variable.Constant, variable.Operator, variable.ExpectedResult);
            }

        }

        #endregion

        

        #region helpers
        
        private void LogicEvaluationFactory(string constant, FilterOperator op, bool expected)
        {
            // Arrange
            var expression = new LogicalExpression(new VariableExpression("OfficeNID"), new ConstantExpression(constant), op);
            // Act
            var evaluation = expression.EvaluateAsync(GetDefaultMessageContext);
            // Assert
            Assert.IsTrue((bool)evaluation.Result == expected);
        }

        private class LogicCondition
        {
            public LogicCondition(string constant, FilterOperator op, bool expected)
            {
                this.Constant = constant;
                this.Operator = op;
                this.ExpectedResult = expected;
            }
            public string Constant { get; private set; }
            public FilterOperator Operator { get; private set; }
            public bool ExpectedResult { get; private set; }
        }


        /// <summary>
        /// Gets default message context
        /// </summary>
        /// <remarks>
        /// Message context contains the following keys: {OfficeNID, 15}, {PatientID, 11}, MemberID, 987654321}, {Name, 'Richard Dawkins'}
        /// </remarks>
        /// <returns>Message context</returns>
        private IReadOnlyDictionary<string, object> GetDefaultMessageContext
        {
            get
            {
                var mock = new Mock<IReadOnlyDictionary<string, object>>();
                mock.Setup(s => s.ContainsKey(It.IsAny<string>())).Returns(true);

                object officeNID = "15";
                object patientID = "15";
                object memberID = "1234567890";
                object name = "Richard Dawkins";

                mock.SetupGet(s => s["OfficeNID"]).Returns("15");
                mock.SetupGet(s => s["PatientID"]).Returns("11");
                mock.SetupGet(s => s["MemberID"]).Returns("1234567890");
                mock.SetupGet(s => s["Name"]).Returns("Richard Dawkins");

                mock.Setup(s => s.TryGetValue("OfficeNID", out officeNID)).Returns(true).Callback(() => { officeNID = "15"; });
                mock.Setup(s => s.TryGetValue("PatientID", out patientID)).Returns(true).Callback(() => { patientID = "11"; });
                mock.Setup(s => s.TryGetValue("MemberID", out memberID)).Returns(true).Callback(() => { memberID = "1234567890"; });
                mock.Setup(s => s.TryGetValue("Name", out name)).Returns(true).Callback(() => { name = "Richard Dawkins"; });
              
                return mock.Object;
            }
        }

        #endregion
    }
}
