using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using SignalR.MagicHub.Filtering.Expressions;
using SignalR.MagicHub.Filtering.Parsing.Grammars;
using SignalR.MagicHub.Messaging.Filters;
using ConstantExpression = SignalR.MagicHub.Filtering.Expressions.ConstantExpression;

namespace SignalR.MagicHub.Filtering.Parsing
{
    public sealed class Sql92WhereClauseVisitor : Sql92WhereClauseBaseVisitor<IFilterExpression>
    {
        /// <summary>
        /// <inheritDoc/><p>The default implementation calls
        ///             <see cref="M:Antlr4.Runtime.Tree.IParseTree.Accept``1(Antlr4.Runtime.Tree.IParseTreeVisitor{``0})"/>
        ///             on the
        ///             specified tree.</p>
        /// </summary>
        public override IFilterExpression Visit(IParseTree tree)
        {
            var v = base.Visit(tree);
            return v;
        }

        /// <summary>
        /// Visit a parse tree produced by <see cref="Sql92WhereClauseParser.parse"/>.
        /// <para>
        /// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
        /// on <paramref name="context"/>.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <return>The visitor result.</return>
        public override IFilterExpression VisitParse(Sql92WhereClauseParser.ParseContext context)
        {
            var v = base.VisitParse(context);
            return v;
        }

        /// <summary>
        /// <inheritDoc/><p>The default implementation initializes the aggregate result to
        ///             <see cref="P:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.DefaultResult">defaultResult()</see>
        ///             . Before visiting each child, it
        ///             calls
        ///             <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.ShouldVisitNextChild(Antlr4.Runtime.Tree.IRuleNode,`0)">shouldVisitNextChild</see>
        ///             ; if the result
        ///             is
        /// <code>
        /// false
        /// </code>
        ///             no more children are visited and the current aggregate
        ///             result is returned. After visiting a child, the aggregate result is
        ///             updated by calling
        ///             <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.AggregateResult(`0,`0)">aggregateResult</see>
        ///             with the
        ///             previous aggregate result and the result of visiting the child.</p><p>The default implementation is not safe for use in visitors that modify
        ///             the tree structure. Visitors that modify the tree should override this
        ///             method to behave properly in respect to the specific algorithm in use.</p>
        /// </summary>
        public override IFilterExpression VisitChildren(IRuleNode node)
        {
            return base.VisitChildren(node);
        }



        /// <summary>
        /// This method is called after visiting each child in
        ///             <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)"/>
        ///             . This method is first called before the first
        ///             child is visited; at that point
        /// <code>
        /// currentResult
        /// </code>
        ///             will be the initial
        ///             value (in the default implementation, the initial value is returned by a
        ///             call to
        ///             <see cref="P:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.DefaultResult"/>
        ///             . This method is not called after the last
        ///             child is visited.
        ///             <p>The default implementation always returns
        /// <code>
        /// true
        /// </code>
        ///             , indicating that
        /// <code>
        /// visitChildren
        /// </code>
        ///             should only return after all children are visited.
        ///             One reason to override this method is to provide a "short circuit"
        ///             evaluation option for situations where the result of visiting a single
        ///             child has the potential to determine the result of the visit operation as
        ///             a whole.</p>
        /// </summary>
        /// <param name="node">The
        ///             <see cref="T:Antlr4.Runtime.Tree.IRuleNode"/>
        ///             whose children are currently being
        ///             visited.
        ///             </param><param name="currentResult">The current aggregate result of the children visited
        ///             to the current point.
        ///             </param>
        /// <returns>
        /// <code>
        /// true
        /// </code>
        ///             to continue visiting children. Otherwise return
        /// <code>
        /// false
        /// </code>
        ///             to stop visiting children and immediately return the
        ///             current aggregate result from
        ///             <see cref="M:Antlr4.Runtime.Tree.AbstractParseTreeVisitor`1.VisitChildren(Antlr4.Runtime.Tree.IRuleNode)"/>
        ///             .
        /// </returns>
        protected override bool ShouldVisitNextChild(IRuleNode node, IFilterExpression currentResult)
        {
            if (currentResult != null) return false;
            return base.ShouldVisitNextChild(node, currentResult);
        }

        #region visit expression cases

        /// <summary>
        /// Visit a parse tree produced by <see cref="Sql92WhereClauseParser.binaryExpr"/>.
        /// <para>
        /// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
        /// on <paramref name="context"/>.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <return>The visitor result.</return>
        public override IFilterExpression VisitBinaryExpr(Sql92WhereClauseParser.BinaryExprContext context)
        {

            return new LogicalExpression(Visit(context.lhs), Visit(context.rhs), GetOperator(context.op));
        }

        /// <summary>
        /// Visit a parse tree produced by <see cref="Sql92WhereClauseParser.constExpr"/>.
        /// <para>
        /// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
        /// on <paramref name="context"/>.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <return>The visitor result.</return>
        public override IFilterExpression VisitConstExpr(Sql92WhereClauseParser.ConstExprContext context)
        {
            return base.VisitConstExpr(context);
        }

        /// <summary>
        /// Visit a parse tree produced by <see cref="Sql92WhereClauseParser.columnExpr"/>.
        /// <para>
        /// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
        /// on <paramref name="context"/>.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <return>The visitor result.</return>
        public override IFilterExpression VisitColumnExpr(Sql92WhereClauseParser.ColumnExprContext context)
        {
            return base.VisitColumnExpr(context);
        }

        /// <summary>
        /// Visit a parse tree produced by <see cref="Sql92WhereClauseParser.isBetweenExpr"/>.
        /// <para>
        /// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
        /// on <paramref name="context"/>.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <return>The visitor result.</return>
        public override IFilterExpression VisitIsBetweenExpr(Sql92WhereClauseParser.IsBetweenExprContext context)
        {
            if (context.K_NOT() == null)
            {
                return new LogicalExpression(
                    new LogicalExpression(
                        Visit(context.val),
                        Visit(context.lbound.Payload),
                        FilterOperator.GreaterThanOrEqualTo),
                    new LogicalExpression(
                        Visit(context.val),
                        Visit(context.rbound.Payload),
                        FilterOperator.LessThanOrEqualTo),
                    FilterOperator.And);
            }
            else
            {
                return new LogicalExpression(
                    new LogicalExpression(
                        Visit(context.val),
                        Visit(context.lbound.Payload),
                        FilterOperator.LessThan),
                    new LogicalExpression(
                        Visit(context.val),
                        Visit(context.rbound.Payload),
                        FilterOperator.GreaterThan),
                    FilterOperator.Or);
            }
        }


        /// <summary>
        /// Visit a parse tree produced by <see cref="Sql92WhereClauseParser.parenExpr"/>.
        /// <para>
        /// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
        /// on <paramref name="context"/>.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <return>The visitor result.</return>
        public override IFilterExpression VisitParenExpr(Sql92WhereClauseParser.ParenExprContext context)
        {
            return base.VisitParenExpr(context);
        }

        /// <summary>
        /// Visit a parse tree produced by <see cref="Sql92WhereClauseParser.unaryExpr"/>.
        /// <para>
        /// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
        /// on <paramref name="context"/>.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <return>The visitor result.</return>
        //public override IFilterExpression VisitUnaryExpr(Sql92WhereClauseParser.UnaryExprContext context)
        //{
        //    return base.VisitUnaryExpr(context);
        //}



        #endregion

        /// <summary>
        /// Visit a parse tree produced by <see cref="Sql92WhereClauseParser.expr"/>.
        /// <para>
        /// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
        /// on <paramref name="context"/>.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <return>The visitor result.</return>
        //public override IFilterExpression VisitExpr(Sql92WhereClauseParser.ExprContext context)
        //{
        //    if (context.op != null)
        //    {
        //        return new LogicalExpression(
        //            Visit(context.children[0]),
        //            Visit(context.children[2]),
        //            GetOperator(context.op)
        //            );
        //    }
        //    var b = base.VisitExpr(context);
        //    return b;
        //}

        private FilterOperator GetOperator(IToken op)
        {
            string text = op.Text.ToLower();
            switch (text)
            {
                case "=":
                    return FilterOperator.EqualTo;
                case "!=":
                case "<>":
                    return FilterOperator.NotEqualTo;
                case ">":
                    return FilterOperator.GreaterThan;
                case ">=":
                    return FilterOperator.GreaterThanOrEqualTo;
                case "<":
                    return FilterOperator.LessThan;
                case "<=":
                    return FilterOperator.LessThanOrEqualTo;
                case "and":
                    return FilterOperator.And;
                case "or":
                    return FilterOperator.Or;
                default:
                    throw new Exception("invalid operator");
            }

        }

        /// <summary>
        /// Visit a parse tree produced by <see cref="Sql92WhereClauseParser.stringLiteral"/>.
        /// <para>
        /// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
        /// on <paramref name="context"/>.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <return>The visitor result.</return>
        public override IFilterExpression VisitStringLiteral(Sql92WhereClauseParser.StringLiteralContext context)
        {
            return base.VisitStringLiteral(context);
        }


        /// <summary>
        /// Visit a parse tree produced by <see cref="Sql92WhereClauseParser.literal_value"/>.
        /// <para>
        /// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
        /// on <paramref name="context"/>.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <return>The visitor result.</return>
        public override IFilterExpression VisitLiteral_value(Sql92WhereClauseParser.Literal_valueContext context)
        {
            IComparable constValue = null;
            Sql92WhereClauseParser.StringLiteralContext c = context.stringLiteral();
            if (c != null)
            {
                constValue = c.body.GetText();
            }
            else if (context.SIGNED_NUMBER() != null)
            {
                string textValue = context.GetText();
                bool isFloatingPoint = textValue.Contains(".") 
                    || textValue.IndexOf("E", StringComparison.CurrentCultureIgnoreCase) > -1;

                // ternary operator actually doesn't work here.
                if (isFloatingPoint)
                {
                    constValue = double.Parse(textValue);
                }
                else
                {
                    constValue = long.Parse(textValue);
                }
            }

            return new ConstantExpression(constValue);
        }

        /// <summary>
        /// Visit a parse tree produced by <see cref="Sql92WhereClauseParser.column_name"/>.
        /// <para>
        /// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
        /// on <paramref name="context"/>.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <return>The visitor result.</return>
        public override IFilterExpression VisitColumn_name(Sql92WhereClauseParser.Column_nameContext context)
        {
            return new VariableExpression(context.GetText());
        }

        /// <summary>
        /// Visit a parse tree produced by <see cref="Sql92WhereClauseParser.unary_operator"/>.
        /// <para>
        /// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
        /// on <paramref name="context"/>.
        /// </para>
        /// </summary>
        /// <param name="context">The parse tree.</param>
        /// <return>The visitor result.</return>
        //public override IFilterExpression VisitUnary_operator(Sql92WhereClauseParser.Unary_operatorContext context)
        //{
        //    return base.VisitUnary_operator(context);
        //}
    }
}
