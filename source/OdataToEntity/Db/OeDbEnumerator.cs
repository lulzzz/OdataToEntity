﻿using OdataToEntity.Parsers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OdataToEntity.Db
{
    public interface IOeDbEnumerator
    {
        Object ClearBuffer();
        IOeDbEnumerator CreateChild(OeEntryFactory entryFactory);
        Task<bool> MoveNextAsync();

        Object Current { get; }
        OeEntryFactory EntryFactory { get; }
    }

    public sealed class OeDbEnumerator : IOeDbEnumerator
    {
        private sealed class DataContext
        {
            private readonly Dictionary<OeEntryFactory, OeDbEnumerator> _pool;

            public DataContext(OeAsyncEnumerator asyncEnumerator)
            {
                AsyncEnumerator = asyncEnumerator;
                Buffer = new List<Object>(1024);

                _pool = new Dictionary<OeEntryFactory, OeDbEnumerator>();
            }

            public OeDbEnumerator GetFromPool(OeDbEnumerator parentEnumerator, OeEntryFactory entryFactory)
            {
                if (_pool.TryGetValue(entryFactory, out OeDbEnumerator dbEnumerator))
                    dbEnumerator.Initialize();
                else
                {
                    dbEnumerator = new OeDbEnumerator(parentEnumerator, entryFactory);
                    _pool.Add(entryFactory, dbEnumerator);
                }
                return dbEnumerator;
            }

            public OeAsyncEnumerator AsyncEnumerator { get; }
            public List<Object> Buffer { get; }
            public bool Eof { get; set; }
        }

        private int _bufferPosition;
        private readonly HashSet<Object> _uniqueConstraint;

        public OeDbEnumerator(OeAsyncEnumerator asyncEnumerator, OeEntryFactory entryFactory)
        {
            Context = new DataContext(asyncEnumerator);
            EntryFactory = entryFactory;

            _bufferPosition = -1;
        }
        private OeDbEnumerator(OeDbEnumerator parentEnumerator, OeEntryFactory entryFactory)
        {
            ParentEnumerator = parentEnumerator;
            Context = parentEnumerator.Context;
            EntryFactory = entryFactory;

            if (entryFactory.ResourceInfo.IsCollection.GetValueOrDefault())
                _uniqueConstraint = new HashSet<Object>(entryFactory.EqualityComparer);

            Initialize();
        }

        public Object ClearBuffer()
        {
            if (ParentEnumerator != null)
                throw new InvalidOperationException($"ClearBuffer can not from child {nameof(OeDbEnumerator)}");

            Object lastValue = Context.Buffer[Context.Buffer.Count - 1];
            int bufferCount = Context.Buffer.Count;
            Context.Buffer.Clear();
            if (_bufferPosition < bufferCount - 1)
                Context.Buffer.Add(lastValue);

            if (!Context.Eof)
                _bufferPosition = -1;

            return lastValue;
        }
        public IOeDbEnumerator CreateChild(OeEntryFactory entryFactory)
        {
            return Context.GetFromPool(this, entryFactory);
        }
        private void Initialize()
        {
            _bufferPosition = ParentEnumerator._bufferPosition;

            if (_uniqueConstraint != null)
            {
                _uniqueConstraint.Clear();
                Object value = Current;
                if (value != null)
                    _uniqueConstraint.Add(value);
            }
        }
        private static bool IsEquals(OeEntryFactory entryFactory, Object value1, Object value2)
        {
            if (Object.ReferenceEquals(value1, value2))
                return true;
            if (value1 == null || value2 == null)
                return false;

            return entryFactory.EqualityComparer.Equals(value1, value2);
        }
        private bool IsSame(Object value)
        {
            Object nextValue = Current;
            if (IsEquals(EntryFactory, value, nextValue))
            {
                if (value == null && ParentEnumerator != null)
                    return IsSameParent();

                return true;
            }

            if (_uniqueConstraint == null || _uniqueConstraint.Add(nextValue))
                return false;

            return IsSameParent();
        }
        private bool IsSameParent()
        {
            Object currentParentValue = ParentEnumerator.EntryFactory.GetValue(Context.Buffer[_bufferPosition]);
            return IsEquals(ParentEnumerator.EntryFactory, ParentEnumerator.Current, currentParentValue);
        }
        public async Task<bool> MoveNextAsync()
        {
            if (ParentEnumerator == null && Context.Eof)
                return false;

            Object value = _bufferPosition < 0 ? null : Current;
            do
            {
                _bufferPosition++;
                if (_bufferPosition >= Context.Buffer.Count)
                {
                    if (!await Context.AsyncEnumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        Context.Eof = true;
                        return false;
                    }

                    Context.Buffer.Add(Context.AsyncEnumerator.Current);
                }
            }
            while (IsSame(value));

            if (ParentEnumerator == null)
                return true;

            return IsSameParent();
        }

        private DataContext Context { get; }
        public Object Current => EntryFactory.GetValue(Context.Buffer[_bufferPosition]);
        public OeEntryFactory EntryFactory { get; }
        private OeDbEnumerator ParentEnumerator { get; }
    }
}