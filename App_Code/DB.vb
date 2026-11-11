Imports System.Data

Namespace ABC


    Public Module DB

        Public DBPath As String = System.AppDomain.CurrentDomain.BaseDirectory & "\App_Data\DB.mdb"
        Public DBConnection As String = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & DBPath & ";"

        Dim context1 As System.Web.HttpContext = System.Web.HttpContext.Current
        Dim Server_IP As String = context1.Request.ServerVariables("HTTP_X_FORWARDED_FOR")
        Dim Server_Name As String = context1.Request.ServerVariables("SERVER_NAME")


        Public Function GetDataTable(ByVal SQL As String) As Data.DataTable

            Dim Dcom As New Data.OleDb.OleDbCommand
            Dim Dcon As New Data.OleDb.OleDbConnection
            Dcon.ConnectionString = DBConnection

            Dim DA As New Data.OleDb.OleDbDataAdapter
            Dim DT As New Data.DataTable

            Dcom.Connection = Dcon
            Dcom.CommandText = SQL

            DA.SelectCommand = Dcom

            DA.Fill(DT)

            GetDataTable = DT
        End Function



    End Module

End Namespace
