using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using SignalR.MagicHub.Filtering.Parsing.Grammars;
using SignalR.MagicHub.Messaging.Filters;
using SignalR.MagicHub.Performance;

namespace SignalR.MagicHub.Filtering.Parsing
{
    public sealed class Sql92FilterExpressionFactory : IFilterExpressionFactory
    {
        private static readonly Sql92WhereClauseVisitor _visitor = new Sql92WhereClauseVisitor();
        private IMagicHubPerformanceCounterManager _counters;

        public Sql92FilterExpressionFactory(IMagicHubPerformanceCounterManager counters)
        {
            _counters = counters;
        }
        public Task<IFilterExpression> GetExpressionAsync(string filterString)
        {
            return Task.Run(() =>
            {
                AntlrInputStream inputStream = new CaseInsensitiveInputStream(filterString);

                ITokenSource lexer = new Sql92WhereClauseLexer(inputStream);
                ITokenStream tokens = new CommonTokenStream(lexer);
                Sql92WhereClauseParser parser = new Sql92WhereClauseParser(tokens) {BuildParseTree = true};

                IParseTree tree = parser.parse();

                var ret = _visitor.Visit(tree);
                _counters.NumberOfFiltersParsedTotal.Increment();
                _counters.NumberOfFiltersParsedPerSec.Increment();
                return ret;
            });
        }
    }
}
