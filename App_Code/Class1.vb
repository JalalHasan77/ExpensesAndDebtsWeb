Imports Microsoft.VisualBasic

Public Module UnixToArabianTime
    ' Converts Unix time to UTC DateTime.
    Public Function UnixToUtc(unix As Long, Optional treatAsMilliseconds As Boolean = False) As DateTime
        Dim epoch As New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        Dim seconds As Long = If(treatAsMilliseconds, unix \ 1000, unix)
        Return epoch.AddSeconds(seconds)
    End Function

    ' Converts Unix time to a specific time zone DateTime (e.g., "Arab Standard Time").
    Public Function UnixToTimeZone(unix As Long,
                               timeZoneId As String,
                               Optional treatAsMilliseconds As Boolean = False) As DateTime
        Dim utc As DateTime = UnixToUtc(unix, treatAsMilliseconds)
        Dim tz As TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)
        Return TimeZoneInfo.ConvertTimeFromUtc(utc, tz)
    End Function

    ' Convenience: converts to local machine time.
    Public Function UnixToLocal(unix As Long, Optional treatAsMilliseconds As Boolean = False) As DateTime
        Return UnixToUtc(unix, treatAsMilliseconds).ToLocalTime()
    End Function

End Module