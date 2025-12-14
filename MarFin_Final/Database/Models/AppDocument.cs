namespace MarFin_Final.Models
{
    public class AppDocument
    {
        public int DocumentId { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public int UploadedBy { get; set; }
        public User? UploadedByUser { get; set; }

        public string DocumentName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = "Other";
        public string FilePath { get; set; } = string.Empty;
        public int? FileSize { get; set; }
        public string? MimeType { get; set; }

        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedDate { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.Now;

        // Display-only properties
        public string CustomerDisplayName { get; set; } = "No Customer";
        public string UploadedByName { get; set; } = "";
        public string FormattedFileSize => FileSize.HasValue ? FormatFileSize(FileSize.Value) : "-";
        public string FormattedUploadDate => UploadDate.ToString("MMM dd, yyyy");

        public List<Customer> Documents { get; set; } = new List<Customer>();
        private string FormatFileSize(int bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            double size = bytes;
            while (size >= 1024 && counter < suffixes.Length - 1)
            {
                size /= 1024;
                counter++;
            }
            return $"{size:0.##} {suffixes[counter]}";
        }
    }
}