﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coolector.Common.Extensions;
using Coolector.Common.Mongo;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Queries;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Coolector.Services.Remarks.Repositories.Queries
{
    public static class RemarkQueries
    {
        public static IMongoCollection<Remark> Remarks(this IMongoDatabase database)
            => database.GetCollection<Remark>();

        public static async Task<Remark> GetByIdAsync(this IMongoCollection<Remark> remarks, Guid id)
        {
            if (id == Guid.Empty)
                return null;

            return await remarks.AsQueryable().FirstOrDefaultAsync(x => x.Id == id);
        }

        public static async Task<IEnumerable<Remark>> QueryAsync(this IMongoCollection<Remark> remarks,
            BrowseRemarks query)
        {
            if (!IsLocationProvided(query) && query.AuthorId.Empty() && !query.Latest)
                return Enumerable.Empty<Remark>();

            if (query.Page <= 0)
                query.Page = 1;
            if (query.Results <= 0)
                query.Results = 10;

            var filterBuilder = new FilterDefinitionBuilder<Remark>();
            var filter = FilterDefinition<Remark>.Empty;
            if (IsLocationProvided(query))
            {
                filter = filterBuilder.GeoWithinCenterSphere(x => x.Location,
                    query.Longitude, query.Latitude, query.Radius / 1000 / 6378.1);
            }
            if (query.Latest)
                filter = filterBuilder.Where(x => x.Id != Guid.Empty);
            if (query.AuthorId.NotEmpty())
                filter = filter & filterBuilder.Where(x => x.Author.UserId == query.AuthorId);
            if (!query.Description.Empty())
                filter = filter & filterBuilder.Where(x => x.Description.Contains(query.Description));

            return await remarks.Find(filter)
                .SortByDescending(x => x.CreatedAt)
                .Skip(query.Results * (query.Page - 1))
                .Limit(query.Results)
                .ToListAsync();
        }

        private static bool IsLocationProvided(BrowseRemarks query)
            => (Math.Abs(query.Latitude) <= 0.0000000001
                || Math.Abs(query.Longitude) <= 0.0000000001
                || query.Radius <= 0) == false;
    }
}