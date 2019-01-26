using OdataToEntity.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OdataToEntity.Db
{
    public class OeBoundFunctionParameter
    {
        private readonly OeQueryExpression _source;
        private readonly OeQueryExpression _result;

        protected OeBoundFunctionParameter(OeQueryExpression source, OeQueryExpression result)
        {
            _source = source;
            _result = result;
        }

        protected IQueryable ApplyFilter(IQueryable source, Object dataContext)
        {
            return _source.ApplyTo(source, dataContext);
        }
        protected IQueryable ApplySelect(IQueryable result, Object dataContext)
        {
            return _result.ApplyTo(result, dataContext);
        }
        public OeEntryFactory CreateEntryFactory()
        {
            return _result.EntryFactory.CreateEntryFactoryFromTuple();
        }

        public OeEntryFactory EntryFactory => _result.EntryFactory;
    }

    public sealed class OeBoundFunctionParameter<TSource, TResult> : OeBoundFunctionParameter
    {
        public OeBoundFunctionParameter(OeQueryExpression source, OeQueryExpression result)
            : base(source, result)
        {

        }

        public IQueryable<TSource> ApplyFilter(IQueryable<TSource> source, Object dataContext)
        {
            return (IQueryable<TSource>)base.ApplyFilter(source, dataContext);
        }
        public IQueryable ApplySelect(IQueryable<TResult> result, Object dataContext)
        {
            return base.ApplySelect(result, dataContext);
        }
        public IAsyncEnumerable<TResult> Materialize(IQueryable result, CancellationToken cancellationToken = default)
        {
            var asyncEnumerator = new OeAsyncEnumeratorAdapter(result, cancellationToken == default ? CancellationToken.None : cancellationToken);
            return new OeEntityAsyncEnumeratorAdapter<TResult>(asyncEnumerator, EntryFactory);
        }
    }

}
