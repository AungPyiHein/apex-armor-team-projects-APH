namespace POS.Shared.Models
{
    public class PaginationFilter
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;
        private int _pageNumber = 1;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 1 : (value > MaxPageSize ? MaxPageSize : value);
        }

        public string? SearchTerm { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? ProcessedById { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SortColumn { get; set; }
        public string SortDirection { get; set; } = "asc";
    }
}
