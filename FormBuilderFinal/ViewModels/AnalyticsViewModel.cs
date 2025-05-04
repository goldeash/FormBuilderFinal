namespace FormBuilder.ViewModels
{
    public class AnalyticsViewModel
    {
        public int TemplateId { get; set; }
        public string TemplateTitle { get; set; }
        public List<QuestionAnalytics> QuestionsAnalytics { get; set; } = new();
    }
}

public class QuestionAnalytics
{
    public int QuestionId { get; set; }
    public string QuestionTitle { get; set; }
    public string QuestionType { get; set; }
    public double? AverageValue { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
    public double? AverageLength { get; set; }
    public Dictionary<string, double> OptionPercentages { get; set; } = new();
    public int TotalResponses { get; set; }
}