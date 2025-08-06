namespace ClubDoorman.Services.UserBan;

/// <summary>
/// Типы банов пользователей
/// </summary>
public enum BanTypeEnum
{
    /// <summary>
    /// Бан за длинное имя пользователя
    /// </summary>
    LongName,
    
    /// <summary>
    /// Бан пользователя из блэклиста
    /// </summary>
    Blacklist,
    
    /// <summary>
    /// Автоматический бан по различным причинам
    /// </summary>
    AutoBan,
    
    /// <summary>
    /// Ручной бан администратором
    /// </summary>
    ManualBan,
    
    /// <summary>
    /// Бан по профилю пользователя
    /// </summary>
    ProfileBan,
    
    /// <summary>
    /// Бан канала
    /// </summary>
    ChannelBan,
    
    /// <summary>
    /// Бан за неудачную капчу
    /// </summary>
    CaptchaBan,
    
    /// <summary>
    /// Бан за повторное нарушение
    /// </summary>
    RepeatedViolation
} 