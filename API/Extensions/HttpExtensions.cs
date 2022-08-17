using System.Text.Json;
using API.Helpers;

namespace API.Extensions
{
    public static class HttpExtensions
    {
        public static void AddPaginationHeader(this HttpResponse response, 
            int currentPage, int totalPages, int pageSize, int totalCount) 
        {
            var paginationHeader = new PaginationHeader(currentPage, totalPages, pageSize, totalCount);
            
            response.Headers.Add("Pagination", JsonSerializer.Serialize(paginationHeader, 
                new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase}));

            response.Headers.Add("Access-Control-Expose-Headers", "Pagination");
        }
    }
}