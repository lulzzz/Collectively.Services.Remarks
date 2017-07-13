﻿using System;
using  Collectively.Common.Extensions;
using  Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class RemarkCategory : ValueObject<RemarkCategory>
    {
        public Guid Id { get; protected set; }
        public string Name { get; protected set; }

        protected RemarkCategory(Guid id, string name)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Category id can not be empty.", nameof(name));
            if (name.Empty())
                throw new ArgumentException("Category name can not be empty.", nameof(name));

            Id = id;
            Name = name;
        }

        public static RemarkCategory Create(Category category)
            => new RemarkCategory(category.Id, category.Name);

        protected override bool EqualsCore(RemarkCategory other) => Id.Equals(other.Id);

        protected override int GetHashCodeCore() => Id.GetHashCode();
    }
}