namespace ClubDoorman.Services.Core.Configuration;

/// <summary>
/// Опции для пороговых значений нарушений перед баном
/// </summary>
public class ViolationThresholdOptions
{
    /// <summary>
    /// Количество повторных нарушений ML фильтра перед баном (0 = отключено)
    /// </summary>
    public int MlViolationsBeforeBan { get; set; } = 0;
    
    /// <summary>
    /// Количество повторных нарушений стоп-слов перед баном (0 = отключено)
    /// </summary>
    public int StopWordsViolationsBeforeBan { get; set; } = 0;
    
    /// <summary>
    /// Количество повторных нарушений эмодзи перед баном (0 = отключено)
    /// </summary>
    public int EmojiViolationsBeforeBan { get; set; } = 0;
    
    /// <summary>
    /// Количество повторных нарушений lookalike символов перед баном (0 = отключено)
    /// </summary>
    public int LookalikeViolationsBeforeBan { get; set; } = 0;
    
    /// <summary>
    /// Количество повторных нарушений банальных приветствий перед баном (0 = отключено)
    /// </summary>
    public int BoringGreetingsViolationsBeforeBan { get; set; } = 0;
    
    /// <summary>
    /// Количество непройденных капч перед баном (0 = отключено)
    /// </summary>
    public int CaptchaViolationsBeforeBan { get; set; } = 0;
}