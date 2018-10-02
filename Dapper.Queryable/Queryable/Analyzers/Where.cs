using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dapper.Queryable.Queryable.Analyzers
{
    public abstract class Where : AbstractAnalyzer
    {
        protected override void Analyze(Context context)
        {
            foreach (PropertyInfo queryProperty in context.QueryProperties
                .Where(p => this.IsSatisfied(context, p))
                .ToArray())
                context.Statements.Add(Expression.Block(this.GetWhereClause(context, queryProperty)));
        }

        protected abstract bool IsSatisfied(Context context, PropertyInfo queryProperty);

        protected abstract Expression GetWhereClause(Context context, PropertyInfo queryProperty);
    }
}
