using Moq;
using NUnit.Framework;
using SignalR.MagicHub.Filtering.Expressions;
using SignalR.MagicHub.Messaging.Filters;
using SignalR.MagicHub.Performance;

namespace SignalR.MagicHub.Filtering.Parsing.Tests
{
    [TestFixture]
    public class Sql92ExpressionParsingFixture
    {
        // This field should represent Inputs.Length, and Expected.Length
        // It drives the cases performed
        public const int NumberOfCases = 14;

        public static readonly string[] Inputs =
        {
            @"Foo = 5",
            @"Foo > 5.1",
            @"Foo = -5",
            @"Foo = '5'",
            @"Foo > 5",
            @"Foo >= 5",
            @"Foo < 5",
            @"Foo <= 5",
            @"Foo != 5",
            @"Foo <> 5",
            @"Foo = 5 and Bar = '6'",
            @"(Foo = 5) and Bar = '6'",
            @"Foo = 5 and (Bar= '6' or Baz = -1)",
            @"(Foo = 5 or Bar = '6') and Baz = -1",
            @"Foo BETWEEN -1 AND 5",
            @"Foo NOT BETWEEN -1 AND 5"

        };

        public static readonly IFilterExpression[] Expected =
        {
            new LogicalExpression(
                new VariableExpression("Foo"),
                new ConstantExpression(5L),
                FilterOperator.EqualTo),
            new LogicalExpression(
                new VariableExpression("Foo"),
                new ConstantExpression(5.1D),
                FilterOperator.GreaterThan),
            new LogicalExpression(
                new VariableExpression("Foo"),
                new ConstantExpression(-5L),
                FilterOperator.EqualTo),
            new LogicalExpression(
                new VariableExpression("Foo"),
                new ConstantExpression("5"),
                FilterOperator.EqualTo),
            new LogicalExpression(
                new VariableExpression("Foo"),
                new ConstantExpression(5L),
                FilterOperator.GreaterThan),
            new LogicalExpression(
                new VariableExpression("Foo"),
                new ConstantExpression(5L),
                FilterOperator.GreaterThanOrEqualTo),
            new LogicalExpression(
                new VariableExpression("Foo"),
                new ConstantExpression(5L),
                FilterOperator.LessThan),
            new LogicalExpression(
                new VariableExpression("Foo"),
                new ConstantExpression(5L),
                FilterOperator.LessThanOrEqualTo),
            new LogicalExpression(
                new VariableExpression("Foo"),
                new ConstantExpression(5L),
                FilterOperator.NotEqualTo),
            new LogicalExpression(
                new VariableExpression("Foo"),
                new ConstantExpression(5L),
                FilterOperator.NotEqualTo),

            new LogicalExpression
            (
                new LogicalExpression
                (
                    new VariableExpression("Foo"),
                    new ConstantExpression(5L),
                    FilterOperator.EqualTo
                ),
                new LogicalExpression
                (
                    new VariableExpression("Bar"),
                    new ConstantExpression("6"),
                    FilterOperator.EqualTo
                ),
                FilterOperator.And
            ),
            new LogicalExpression
            (
                new LogicalExpression
                (
                    new VariableExpression("Foo"),
                    new ConstantExpression(5L),
                    FilterOperator.EqualTo
                ),
                new LogicalExpression
                (
                    new VariableExpression("Bar"),
                    new ConstantExpression("6"),
                    FilterOperator.EqualTo
                ),
                FilterOperator.And
            ),
//          @"Foo = 5 and (Bar= '6' or Baz = -1)",
            new LogicalExpression
            (
                new LogicalExpression
                (
                    new VariableExpression("Foo"),
                    new ConstantExpression(5L),
                    FilterOperator.EqualTo
                ),     
                new LogicalExpression
                    (
                        new LogicalExpression
                        (
                            new VariableExpression("Bar"), 
                            new ConstantExpression("6"), 
                            FilterOperator.EqualTo
                        ),
                        new LogicalExpression
                        (
                            new VariableExpression("Baz"), 
                            new ConstantExpression(-1L), 
                            FilterOperator.EqualTo
                        ),
                        FilterOperator.Or
                    ), 
                FilterOperator.And
            ),
//          @"(Foo = 5 and Bar = '6') and Baz = -1"
            new LogicalExpression
            (
               new LogicalExpression
               (
                    new LogicalExpression
                    (
                        new VariableExpression("Foo"), 
                        new ConstantExpression(5L), 
                        FilterOperator.EqualTo
                    ),
                    new LogicalExpression
                    (
                        new VariableExpression("Bar"), 
                        new ConstantExpression("6"), 
                        FilterOperator.EqualTo
                    ),
                    FilterOperator.Or
                ), 
                new LogicalExpression
                (
                    new VariableExpression("Baz"),
                    new ConstantExpression(-1L),
                    FilterOperator.EqualTo
                ),     
                
                FilterOperator.And
            ),
            new LogicalExpression
            (
                new LogicalExpression
                (
                    new VariableExpression("Foo"), 
                    new ConstantExpression(-1L), 
                    FilterOperator.GreaterThanOrEqualTo
                ),
                new LogicalExpression
                (
                    new VariableExpression("Foo"), 
                    new ConstantExpression(5L), 
                    FilterOperator.LessThanOrEqualTo
                ),
                FilterOperator.And),
            new LogicalExpression
            (
                new LogicalExpression
                (
                    new VariableExpression("Foo"), 
                    new ConstantExpression(-1L), 
                    FilterOperator.GreaterThanOrEqualTo
                ),
                new LogicalExpression
                (
                    new VariableExpression("Foo"), 
                    new ConstantExpression(5L), 
                    FilterOperator.LessThanOrEqualTo
                ),
                FilterOperator.Or
            )
        };

        [Test]
        public async void Test_Parsing([Range(1, NumberOfCases)]int idx)
        {
            // Arrange 
            idx--;
            string input = Inputs[idx];
            var expected= Expected[idx];

            var factory = new Sql92FilterExpressionFactory(new MagicHubPerformanceCounterManager());

            // Act
            var actual = await factory.GetExpressionAsync(input);


            // Arrange
            Assert.That(actual, Is.EqualTo(expected));
        }

    }
}
