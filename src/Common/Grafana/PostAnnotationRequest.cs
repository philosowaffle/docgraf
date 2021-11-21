namespace Common.Grafana
{
    public class PostAnnotationRequest
    {
        /// <summary>
        /// epoch numbers in millisecond
        /// </summary>
        public long Time { get; set; }
        /// <summary>
        /// epoch numbers in millisecond
        /// </summary>
        public long TimeEnd { get; set; }
        public string[]? Tags { get; set; }
        public string? Text { get; set; }
    }
}
