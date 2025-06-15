namespace ClimbUpAPI.Models.Enums
{
    public enum SessionState
    {
        Working,    // Aktif çalışma/odaklanma
        Break,      // Genel mola (kısa/uzun ayrımı yok)
        // LongBreak kaldırıldı
        Completed,  // Başarıyla tamamlandı
        Cancelled   // Erken sonlandırıldı
    }
}