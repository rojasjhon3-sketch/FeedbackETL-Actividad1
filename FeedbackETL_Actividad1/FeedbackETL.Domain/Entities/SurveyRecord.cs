namespace FeedbackETL.Domain.Entities;

public sealed class SurveyRecord
{
    public int Id { get; set; }
    public DateTime SurveyDate { get; set; }
    public string Department { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}
