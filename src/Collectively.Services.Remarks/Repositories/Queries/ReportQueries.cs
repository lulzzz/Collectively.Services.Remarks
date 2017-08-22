using System;
using System.Threading.Tasks;
using Collectively.Common.Extensions;
using Collectively.Common.Mongo;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Queries;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Collectively.Services.Remarks.Repositories.Queries
{
    public static class ReportQueries
    {
        public static IMongoCollection<Report> Reports(this IMongoDatabase database)
            => database.GetCollection<Report>();

        public static async Task<bool> ExistsAsync(this IMongoCollection<Report> reports,
            Guid remarkId, Guid? resourceId, string type, string userId)
        {
            if(resourceId == Guid.Empty)
            {
                resourceId = null;
            }

            return await reports.AsQueryable().AnyAsync(x => x.RemarkId == remarkId
                && x.ResourceId == resourceId && x.UserId == userId && x.Type == type);
        }

        public static IMongoQueryable<Report> Query(this IMongoCollection<Report> reports,
            BrowseReports query)
        {
            var values = reports.AsQueryable();
            if(query.Type.NotEmpty())
            {
                values = values.Where(x => x.Type == query.Type);
            }

            return values;
        }
    }
}