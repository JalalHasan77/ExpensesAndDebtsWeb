Imports System.Configuration
Imports System.Linq
Imports System.Net
Imports System.Net.Mail
Imports System.Security.Cryptography
Imports System.Text
Imports System.Web
Imports System.Web.Caching

''' <summary>
''' One-time codes for the Payment tab, for visitors who are not on the intranet (no Staff ID / Windows login):
''' CPR -> a 6-digit code sent to the member's mobile (SMS) and email -> the visitor is that member for this session.
'''
''' Safety: the code comes from a cryptographic random generator and is kept only as a salted hash in the
''' server-side Session (never in the page); it expires after 5 minutes and allows 5 tries; a CPR can request
''' 5 codes per hour and one address 10 codes per hour; the answer is the same whether the CPR is registered or not.
'''
''' web.config:
'''   Email - the standard SMTP settings:
'''     <system.net><mailSettings><smtp from="noreply@yourdomain.com">
'''       <network host="smtp.yourdomain.com" port="587" enableSsl="true" userName="..." password="..." />
'''     </smtp></mailSettings></system.net>
'''   SMS - your SMS gateway's HTTP address, with {mobile} and {message} where they go:
'''     <appSettings><add key="OtpSmsUrl" value="https://gateway.example/send?user=U&amp;pass=P&amp;to={mobile}&amp;text={message}" /></appSettings>
'''   Testing only (shows the code on screen, and only when the page is opened on the server itself):
'''     <add key="OtpShowCodeForTesting" value="true" />
''' </summary>
Public Class PaymentOtp

    Public Const CodeLength As Integer = 6
    Public Const MaxAttempts As Integer = 5
    Public Shared ReadOnly Lifetime As TimeSpan = TimeSpan.FromMinutes(5)
    Public Shared ReadOnly ResendAfter As TimeSpan = TimeSpan.FromSeconds(60)
    Private Const MaxSendsPerCprPerHour As Integer = 5
    Private Const MaxSendsPerAddressPerHour As Integer = 10

    ''' <summary>A code waiting to be entered (kept in Session, server side).</summary>
    <Serializable>
    Public Class Pending
        Public Property Cpr As String = ""
        Public Property MemberId As String = ""     ' "" when the CPR is not registered: no code can ever match
        Public Property Salt As String = ""
        Public Property CodeHash As String = ""
        Public Property SentUtc As DateTime
        Public Property ExpiresUtc As DateTime
        Public Property Attempts As Integer
        Public ReadOnly Property IsExpired As Boolean
            Get
                Return DateTime.UtcNow > ExpiresUtc
            End Get
        End Property
    End Class

    ''' <summary>A uniformly random 6-digit code (000000-999999).</summary>
    Public Shared Function NewCode() As String
        Dim bytes(3) As Byte
        Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
            Do
                rng.GetBytes(bytes)
                Dim value As UInteger = BitConverter.ToUInt32(bytes, 0)
                If value < 4294000000UI Then Return (value Mod 1000000UI).ToString("000000")   ' no bias
            Loop
        End Using
    End Function

    Public Shared Function NewSalt() As String
        Dim bytes(15) As Byte
        Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
            rng.GetBytes(bytes)
        End Using
        Return Convert.ToBase64String(bytes)
    End Function

    Public Shared Function Hash(code As String, salt As String) As String
        Using sha As SHA256 = SHA256.Create()
            Return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(salt & ":" & code)))
        End Using
    End Function

    ''' <summary>Compares two hashes in constant time.</summary>
    Public Shared Function SameHash(a As String, b As String) As Boolean
        If a Is Nothing OrElse b Is Nothing OrElse a.Length <> b.Length Then Return False
        Dim diff As Integer = 0
        For i As Integer = 0 To a.Length - 1
            diff = diff Or (AscW(a(i)) Xor AscW(b(i)))
        Next
        Return diff = 0
    End Function

    ''' <summary>Counts a send for this CPR and address; False when either has asked for too many codes this hour.</summary>
    Public Shared Function AllowSend(cpr As String, address As String) As Boolean
        Return Count("otp-cpr:" & cpr, MaxSendsPerCprPerHour) And Count("otp-ip:" & address, MaxSendsPerAddressPerHour)
    End Function

    Private Shared ReadOnly countLock As New Object()

    Private Shared Function Count(key As String, max As Integer) As Boolean
        SyncLock countLock
            Dim cache As Cache = HttpRuntime.Cache
            Dim n As Integer = If(cache(key) Is Nothing, 0, CInt(cache(key)))
            If n >= max Then Return False
            If n = 0 Then
                cache.Insert(key, 1, Nothing, DateTime.UtcNow.AddHours(1), Cache.NoSlidingExpiration)
            Else
                cache(key) = n + 1   ' keeps the original one-hour expiry
            End If
            Return True
        End SyncLock
    End Function

    ' ---------------- sending ----------------

    Private Shared Function Message(code As String) As String
        Return "Your Expenses & Debt code is " & code & ". It is valid for " & CInt(Lifetime.TotalMinutes) & " minutes. Do not share it."
    End Function

    ''' <summary>Sends the code by email (web.config SMTP settings). "" = sent; otherwise why not.</summary>
    Public Shared Function SendEmail(toAddress As String, memberName As String, code As String) As String
        If String.IsNullOrWhiteSpace(toAddress) Then Return "no email"
        Try
            Using msg As New MailMessage()
                Dim from As String = ConfigurationManager.AppSettings("OtpFromEmail")
                If Not String.IsNullOrWhiteSpace(from) Then msg.From = New MailAddress(from)
                msg.To.Add(New MailAddress(toAddress))
                msg.Subject = "Your Expenses & Debt code: " & code
                msg.Body = "Dear " & memberName & "," & vbCrLf & vbCrLf & Message(code) & vbCrLf & vbCrLf &
                           "If you did not ask for it, you can ignore this email."
                Using smtp As New SmtpClient()
                    smtp.Send(msg)
                End Using
            End Using
            Return ""
        Catch ex As Exception
            Return ex.Message
        End Try
    End Function

    ''' <summary>Sends the code by SMS through the gateway in appSettings("OtpSmsUrl"). "" = sent; otherwise why not.</summary>
    Public Shared Function SendSms(mobile As String, code As String) As String
        If String.IsNullOrWhiteSpace(mobile) Then Return "no mobile"
        Dim template As String = ConfigurationManager.AppSettings("OtpSmsUrl")
        If String.IsNullOrWhiteSpace(template) Then Return "no SMS gateway set (appSettings OtpSmsUrl)"
        Try
            Dim digits As String = New String(mobile.Where(Function(ch) Char.IsDigit(ch)).ToArray())
            If digits.Length = 8 Then digits = "973" & digits   ' a Bahrain number without the country code
            Dim url As String = template.Replace("{mobile}", Uri.EscapeDataString(digits)).
                                         Replace("{message}", Uri.EscapeDataString(Message(code)))
            ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol Or SecurityProtocolType.Tls12
            Using wc As New WebClient()
                wc.DownloadString(url)
            End Using
            Return ""
        Catch ex As Exception
            Return ex.Message
        End Try
    End Function

    ''' <summary>Testing only: show the code on screen when the setting is on AND the page is opened on the server itself.</summary>
    Public Shared Function ShowCodeForTesting(request As HttpRequest) As Boolean
        Return String.Equals(ConfigurationManager.AppSettings("OtpShowCodeForTesting"), "true", StringComparison.OrdinalIgnoreCase) AndAlso
               request IsNot Nothing AndAlso request.IsLocal
    End Function

    ''' <summary>"850112345" -> "•••••2345"</summary>
    Public Shared Function MaskCpr(cpr As String) As String
        If cpr Is Nothing OrElse cpr.Length < 4 Then Return "••••"
        Return New String(ChrW(&H2022), cpr.Length - 4) & cpr.Substring(cpr.Length - 4)
    End Function

End Class
