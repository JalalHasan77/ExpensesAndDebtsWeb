Imports System.Data
Imports System.Data.OleDb
Imports System.Collections.Generic
Imports System.Web

''' <summary>
''' Data access for the Expenses and Debts page (Orders.mdb, MS Access).
''' Same functions as DB.vb of the desktop Form1, as a class with Shared members
''' (a Module's functions would clash with the site's own GetDataTable helpers).
''' Every call opens its own connection, so requests never share one.
''' </summary>
Public Class OrdersDB

    ''' <summary>Orders.mdb lives in the site's App_Data folder.</summary>
    Public Shared ReadOnly Property DBPath As String
        Get
            Return HttpContext.Current.Server.MapPath("~/App_Data/Orders.mdb")
        End Get
    End Property

    ''' <summary>Jet 4.0 needs the application pool to run as 32-bit ("Enable 32-Bit Applications" = True).</summary>
    Public Shared ReadOnly Property DBConnection As String
        Get
            Return "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & DBPath & ";"
        End Get
    End Property

    Public Shared Function GetDataTable(ByVal SQL As String) As DataTable
        Dim dt As New DataTable
        Using cn As New OleDbConnection(DBConnection)
            Using da As New OleDbDataAdapter(SQL, cn)
                da.Fill(dt)
            End Using
        End Using
        Return dt
    End Function

    ''' <summary>Runs one UPDATE / INSERT / DELETE statement and returns the number of rows affected.</summary>
    Public Shared Function ExecuteNonQuery(ByVal SQL As String) As Integer
        Using cn As New OleDbConnection(DBConnection)
            cn.Open()
            Using cmd As New OleDbCommand(SQL, cn)
                Return cmd.ExecuteNonQuery()
            End Using
        End Using
    End Function

    ''' <summary>Runs several statements as one transaction: all are saved, or none are.</summary>
    Public Shared Sub ExecuteInTransaction(ByVal statements As IEnumerable(Of String))
        Using cn As New OleDbConnection(DBConnection)
            cn.Open()
            Using tr As OleDbTransaction = cn.BeginTransaction()
                Try
                    For Each sql As String In statements
                        Using cmd As New OleDbCommand(sql, cn, tr)
                            cmd.ExecuteNonQuery()
                        End Using
                    Next
                    tr.Commit()
                Catch
                    tr.Rollback()
                    Throw
                End Try
            End Using
        End Using
    End Sub

    ' ---------- IDs (stored as text, e.g. "00007") ----------

    ''' <summary>Highest number used in a text ID column (0 if the table is empty).</summary>
    Public Shared Function GetMaxID(ByVal table As String, ByVal idColumn As String) As Long
        Dim dt As DataTable = GetDataTable("SELECT MAX(Val([" & idColumn & "] & '')) AS MaxID FROM [" & table & "]")
        If dt.Rows.Count = 0 OrElse IsDBNull(dt.Rows(0)("MaxID")) Then Return 0
        Return CLng(dt.Rows(0)("MaxID"))
    End Function

    ''' <summary>Next ID padded with zeros: GetNextID("Orders", "Order_ID", 5) -> "00008"</summary>
    Public Shared Function GetNextID(ByVal table As String, ByVal idColumn As String, ByVal width As Integer) As String
        Return (GetMaxID(table, idColumn) + 1).ToString().PadLeft(width, "0"c)
    End Function

    ' ---------- Building INSERT / UPDATE statements ----------

    ''' <summary>One row to insert or update: table + field/value pairs (all values saved as text).</summary>
    Public Class DbRow
        Public ReadOnly Table As String
        Public ReadOnly Fields As New List(Of KeyValuePair(Of String, String))
        Public Key As String

        Public Sub New(ByVal tableName As String)
            Table = tableName
        End Sub

        Public Sub Add(ByVal field As String, ByVal value As String)
            Fields.Add(New KeyValuePair(Of String, String)(field, If(value, "")))
        End Sub

        Public Function InsertSQL() As String
            Dim names As New List(Of String)
            Dim values As New List(Of String)
            For Each f As KeyValuePair(Of String, String) In Fields
                names.Add("[" & f.Key & "]")
                values.Add(SqlText(f.Value))
            Next
            Return "INSERT INTO [" & Table & "] (" & String.Join(", ", names) & ") VALUES (" & String.Join(", ", values) & ")"
        End Function

        Public Function UpdateSQL(ByVal keyField As String, Optional ByVal extraSet As String = Nothing) As String
            Dim sets As New List(Of String)
            For Each f As KeyValuePair(Of String, String) In Fields
                sets.Add("[" & f.Key & "] = " & SqlText(f.Value))
            Next
            If Not String.IsNullOrEmpty(extraSet) Then sets.Add(extraSet)
            Return "UPDATE [" & Table & "] SET " & String.Join(", ", sets) & " WHERE [" & keyField & "] = " & SqlText(Key)
        End Function
    End Class

    ''' <summary>Text value for SQL: quoted, with single quotes doubled.</summary>
    Public Shared Function SqlText(ByVal value As String) As String
        Return "'" & If(value, "").Replace("'", "''") & "'"
    End Function

    ' ---------- Field sizes ----------

    ''' <summary>One message per value that does not fit its field, or per missing table/field (empty = all fine).</summary>
    Public Shared Function CheckFieldSizes(ByVal rows As IEnumerable(Of DbRow)) As List(Of String)
        Dim problems As New List(Of String)
        Dim sizesByTable As New Dictionary(Of String, Dictionary(Of String, Integer))(StringComparer.OrdinalIgnoreCase)

        For Each row As DbRow In rows
            If Not sizesByTable.ContainsKey(row.Table) Then sizesByTable(row.Table) = GetFieldSizes(row.Table)
            Dim sizes As Dictionary(Of String, Integer) = sizesByTable(row.Table)

            If sizes.Count = 0 Then
                Dim msg As String = "Table " & row.Table & " does not exist"
                If Not problems.Contains(msg) Then problems.Add(msg)
                Continue For
            End If

            For Each f As KeyValuePair(Of String, String) In row.Fields
                Dim maxLen As Integer
                If Not sizes.TryGetValue(f.Key, maxLen) Then
                    Dim msg As String = row.Table & "." & f.Key & ": field does not exist"
                    If Not problems.Contains(msg) Then problems.Add(msg)
                ElseIf maxLen > 0 AndAlso f.Value.Length > maxLen Then
                    Dim msg As String = row.Table & "." & f.Key & ": size " & maxLen & ", needs " & f.Value.Length & " ('" & f.Value & "')"
                    If Not problems.Contains(msg) Then problems.Add(msg)
                End If
            Next
        Next
        Return problems
    End Function

    ''' <summary>Field -> maximum length for every field of a table (0 = not a Text field). Empty = no such table.</summary>
    Public Shared Function GetFieldSizes(ByVal table As String) As Dictionary(Of String, Integer)
        Dim sizes As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        Using cn As New OleDbConnection(DBConnection)
            cn.Open()
            Dim schema As DataTable = cn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, New Object() {Nothing, Nothing, table, Nothing})
            For Each r As DataRow In schema.Rows
                Dim maxLen As Integer = 0
                If Not IsDBNull(r("CHARACTER_MAXIMUM_LENGTH")) Then maxLen = CInt(r("CHARACTER_MAXIMUM_LENGTH"))
                sizes(r("COLUMN_NAME").ToString()) = maxLen
            Next
        End Using
        Return sizes
    End Function

End Class
