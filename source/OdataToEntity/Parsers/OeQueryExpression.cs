using Microsoft.OData.Edm;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace OdataToEntity.Parsers
{
    public sealed class OeQueryExpression
    {
        private readonly IEdmEntitySet _entitySet;
        private readonly Expression _expression;
        private IQueryable _source;

        public OeQueryExpression(IEdmModel edmModel, IEdmEntitySet entitySet, Expression expression)
        {
            EdmModel = edmModel;
            _entitySet = entitySet;
            _expression = expression;
        }

        public IQueryable ApplyTo(IQueryable source, Object dataContext)
        {
            if (_expression == null)
                return source;

            _source = source;
            Expression expression = OeQueryContext.TranslateSource(EdmModel, dataContext, _expression, GetQuerySource);
            return source.Provider.CreateQuery(expression);
        }
        public IQueryable<T> ApplyTo<T>(IQueryable<T> source, Object dataContext)
        {
            return (IQueryable<T>)ApplyTo((IQueryable)source, dataContext);
        }
        public OeEntryFactory CreateEntryFactory()
        {
            if (_expression != null && OeExpressionHelper.IsTupleType(OeExpressionHelper.GetCollectionItemType(_expression.Type)))
            {
                Db.OeDataAdapter dataAdapter = EdmModel.GetDataAdapter(_entitySet.Container);
                Type clrType = dataAdapter.EntitySetAdapters.Find(_entitySet).EntityType;

                OePropertyAccessor[] propertyAccessors = OePropertyAccessor.CreateFromType(clrType, EntryFactory.EntitySet);
                OePropertyAccessor[] accessors = new OePropertyAccessor[EntryFactory.Accessors.Length];
                for (int i = 0; i < accessors.Length; i++)
                    accessors[i] = Array.Find(propertyAccessors, pa => pa.EdmProperty == EntryFactory.Accessors[i].EdmProperty);

                return OeEntryFactory.CreateEntryFactory(_entitySet, accessors);
            }

            return EntryFactory;
        }
        public IQueryable GetQuerySource(Object dataContext)
        {
            Db.OeDataAdapter dataAdapter = EdmModel.GetDataAdapter(_entitySet.Container);
            return dataAdapter.EntitySetAdapters.Find(_entitySet).GetEntitySet(dataContext);
        }
        private IQueryable GetQuerySource(IEdmEntitySet entitySet)
        {
            return entitySet == _entitySet ? _source : null;
        }

        public IEdmModel EdmModel { get; }
        public OeEntryFactory EntryFactory { get; set; }
    }
}
