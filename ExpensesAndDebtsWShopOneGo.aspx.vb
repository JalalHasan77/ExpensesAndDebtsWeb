Imports System.Data
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports Ledger.Core
Imports Ledger.Core.Data

Partial Class ExpensesAndDebtsWShopOneGo
    Inherits System.Web.UI.Page

    ' Views (MultiView3): 0 Order | 1 Main Dashboard | 2 Matrices | 3 Items | 4 Transactions | 5 Payment | 6 Accounts
    ' (the Accounts link is shown between Items and Transactions)
    ' Money: every balance comes from the ledger (Ledger.Core in Bin; Accounts / Journals / Entries in Orders.mdb),
    ' the same library as the desktop form, so both apps calculate and save orders the same way.
    ' Accounts are the real members: each is a ledger member "<Person> - <Account>" (Members.Owner_ID = the person,
    ' Members.Account_Type = Capital / Loan); a person only groups their accounts (see PERSONS AND ACCOUNTS).

    Private Const OrderTabIndex As Integer = 0
    Private Const DashboardTabIndex As Integer = 1
    Private Const MatricesTabIndex As Integer = 2
    Private Const ItemsTabIndex As Integer = 3
    Private Const TransactionsTabIndex As Integer = 4
    Private Const PaymentTabIndex As Integer = 5
    Private Const AccountsTabIndex As Integer = 6

    Private Const TxNormal As String = "Expense"
    Private Shared ReadOnly TransactionTypes As String() = {TxNormal, "Shop Purchase", "Shop Sale", "Shop Loss"}
    Private Const OnCreditText As String = "(on credit)"
    Private Const SettlementPrefix As String = "Settlement: "

    ' ====================================================================
    '  ORDER MODEL (kept in ViewState between postbacks)
    ' ====================================================================

    <Serializable>
    Public Class MemberLine
        Public Property Id As String = ""
        Public Property Name As String = ""
        ''' <summary>Order line key: the member ID, or "shopId:stock" / "shopId:loss" for the shop's two goods lines.</summary>
        Public Property LineKey As String = ""
        Public Property IsShopLine As Boolean
        Public Property ExBalance As Decimal
        Public Property ExCash As Decimal
        Public Property ExExpense As Decimal
        Public Property ExReceivable As Decimal
        Public Property ExPayable As Decimal
        Public Property Credit As Decimal
        Public Property Withdraw As Decimal
        Public Property Qty As New List(Of Decimal)        ' one per item column
        Public Property GroupIds As New List(Of Integer)   ' the member's groups (Order grid filter)
        Public Property InViewedOrder As Boolean = True    ' read-only view: False = not part of that order
        ''' <summary>An account line: the person it belongs to ("" for the shop and its goods lines).</summary>
        Public Property OwnerId As String = ""
        Public Property OwnerName As String = ""
        Public Property ShortName As String = ""           ' the account's own name ("Cash"); Name = "Jalal Hasan - Cash"
        Public Property AccountType As String = ""         ' Capital / Loan
        ''' <summary>Split payment: several accounts pay for this line, each its amount (empty = one payer, as picked in the Preview).</summary>
        Public Property Splits As New List(Of SplitPart)
    End Class

    ''' <summary>One payer of a split line: the paying account, the money it pays from, and how much.</summary>
    <Serializable>
    Public Class SplitPart
        Public Property Account As String = ""             ' the payer (member name, as in the Preview's Account list)
        Public Property PaidFrom As String = ""            ' the payer's fund (as in Paid From)
        Public Property Amount As Decimal
    End Class

    <Serializable>
    Public Class ItemCol
        Public Property Title As String = ""
        Public Property Price As Decimal
        Public Property Profit As Decimal                  ' profit per unit (the "Profit" row above Subtotal)
        Public Property IsProfits As Boolean               ' the calculated [Profits] item (price 1.000)
    End Class

    ''' <summary>A Preview row: one order line, or (AutoRow) another member the order changes (payer, the shop).</summary>
    <Serializable>
    Public Class PreviewLine
        Public Property MemberId As String = ""
        Public Property LineKey As String = ""             ' "" for an AutoRow
        Public Property Name As String = ""
        Public Property ExBalance As Decimal?              ' figures: blank on the shop's goods lines (they show on the Shop's row)
        Public Property ExCash As Decimal?
        Public Property ExExpense As Decimal?
        Public Property ExReceivable As Decimal?
        Public Property ExPayable As Decimal?
        Public Property Credit As Decimal?
        Public Property Debt As Decimal?
        Public Property Total As Decimal?                  ' what the line is charged (items + withdraw + delivery / VAT share)
        Public Property Lines As String = ""               ' OrderItems text
        Public Property Account As String = ""             ' who pays (member name)
        Public Property TxType As String = ""
        Public Property PaidFrom As String = ""            ' the payer's fund (or "(on credit)" for a Shop Sale)
        Public Property PaidFromOptions As New List(Of String)
        Public Property AccountLocked As Boolean
        Public Property TypeLocked As Boolean
        Public Property ShopDefaulted As Boolean     ' a shop order: the row was put "on account" once (the user may change it)
        Public Property Section As Integer          ' a shop order: 0 = the first grid (purchase), 1 = the second (profit, stock, members)
        Public Property NextBalance As Decimal?
        Public Property Cash As Decimal?
        Public Property Expense As Decimal?
        Public Property Receivable As Decimal?
        Public Property Payable As Decimal?
        Public Property AutoRow As Boolean
    End Class

    <Serializable>
    Public Class OrderState
        Public Property Source As String = ""
        Public Property OrderDate As String = ""          ' yyyy-MM-dd
        Public Property OrderNo As String = ""            ' HH:mm
        Public Property Delivery As Decimal
        Public Property VAT As Decimal
        Public Property ShopId As String = ""
        Public Property GroupFilter As Integer               ' Order grid: 0 = all members, else a Group_ID
        Public Property ShopFilter As String = ""            ' Order grid with a Profits column: the one shop whose accounts show
        Public Property Members As New List(Of MemberLine)
        Public Property Items As New List(Of ItemCol)
        Public Property Preview As New List(Of PreviewLine)
        Public Property SplitKey As String = ""                  ' the line whose Split payment dialog is open
        Public Property SplitDraft As New List(Of SplitPart)     ' that dialog's rows, until Save
        Public Property PreviewBuilt As Boolean
        Public Property PreviewCurrent As Boolean
        Public Property StatusKind As String = ""            ' "", ok, bad, warn, info, done
        Public Property StatusText As String = ""
        Public Property ViewingOrderId As String = Nothing   ' Nothing = normal (editable) order
        Public Property IsCancelled As Boolean               ' the viewed order was cancelled
        Public Property SavedTotal As Decimal
    End Class

    Private Property State As OrderState
        Get
            If ViewState("OrderState") Is Nothing Then ViewState("OrderState") = New OrderState()
            Return CType(ViewState("OrderState"), OrderState)
        End Get
        Set(value As OrderState)
            ViewState("OrderState") = value
        End Set
    End Property

    Private ReadOnly Property IsViewing As Boolean
        Get
            Return State.ViewingOrderId IsNot Nothing
        End Get
    End Property

    ' ====================================================================
    '  THE LEDGER (one per request; reads are cached until something is posted)
    ' ====================================================================

    Private ledgerValue As LedgerService
    Private queriesCache As LedgerQueries
    Private membersCache As List(Of Member)
    Private accountsCache As List(Of Account)
    Private ledgerChecked As Boolean
    Private ledgerProblemText As String = ""

    Private ReadOnly Property Ledger As LedgerService
        Get
            If ledgerValue Is Nothing Then
                ledgerValue = New LedgerService(New AccessLedgerStore(OrdersDB.DBConnection), CurrentStaffId)
                ledgerValue.Administrators.Clear()
                ledgerValue.Administrators.Add(AdminStaffId)
            End If
            Return ledgerValue
        End Get
    End Property

    Private Function LQ() As LedgerQueries
        If queriesCache Is Nothing Then queriesCache = Ledger.Queries
        Return queriesCache
    End Function

    Private Function LedgerMembers() As List(Of Member)
        If membersCache Is Nothing Then membersCache = Ledger.Store.GetMembers()
        Return membersCache
    End Function

    Private Function LedgerAccounts() As List(Of Account)
        If accountsCache Is Nothing Then accountsCache = Ledger.Store.GetAccounts()
        Return accountsCache
    End Function

    ''' <summary>After posting: the next read sees the new journals and accounts.</summary>
    Private Sub ResetLedgerCache()
        queriesCache = Nothing
        membersCache = Nothing
        accountsCache = Nothing
        accountInfoCache = Nothing
        shopCustomerCache = Nothing
    End Sub

    ''' <summary>"" when the ledger tables are there; otherwise what to do.</summary>
    Private Function LedgerProblem() As String
        If Not ledgerChecked Then
            ledgerChecked = True
            Try
                LedgerMembers()
                LedgerAccounts()
            Catch ex As Exception
                ledgerProblemText = "The ledger tables are not in Orders.mdb yet: run Ledger.Setup on " & OrdersDB.DBPath &
                                    " first, and put Ledger.Core.dll in the site's Bin folder. (" & ex.Message & ")"
            End Try
        End If
        Return ledgerProblemText
    End Function

    Private Function LedgerName(memberId As String) As String
        Dim m As Member = LedgerMembers().FirstOrDefault(Function(x) String.Equals(x.Id, memberId, StringComparison.OrdinalIgnoreCase))
        Return If(m Is Nothing, If(memberId, ""), m.Name)
    End Function

    Private Function FindShopId() As String
        Dim shop As Member = LedgerMembers().FirstOrDefault(Function(m) m.MemberType = MemberTypes.Shop AndAlso m.IsActive)
        Return If(shop Is Nothing, "", shop.Id)
    End Function

    ''' <summary>A member's active funds, the default fund first.</summary>
    Private Function FundsOf(memberId As String) As List(Of Account)
        Return LedgerAccounts().Where(Function(a) a.Kind = AccountKind.Fund AndAlso a.IsActive AndAlso
                                                  String.Equals(a.MemberId, memberId, StringComparison.OrdinalIgnoreCase)).
                                OrderByDescending(Function(a) a.IsDefaultFund).ThenBy(Function(a) a.SortOrder).ThenBy(Function(a) a.Name).ToList()
    End Function

    Private Function FundIdByName(memberId As String, fundName As String) As Integer
        Dim f As Account = FundsOf(memberId).FirstOrDefault(Function(a) SameName(a.Name, fundName))
        If f Is Nothing AndAlso fundName.EndsWith(LoanSuffix) Then   ' a bank loan / card: "CrediMax (loan)"
            Dim loanName As String = fundName.Substring(0, fundName.Length - LoanSuffix.Length)
            f = LoansOf(memberId).FirstOrDefault(Function(a) SameName(a.Name, loanName))
        End If
        Return If(f Is Nothing, 0, f.Id)
    End Function

    ''' <summary>Shown after a bank loan / credit card in a Paid From list: paying with it raises what is owed on it.</summary>
    Private Const LoanSuffix As String = " (loan)"

    ''' <summary>A member's active bank loans / credit cards (an order can be paid with them directly).</summary>
    Private Function LoansOf(memberId As String) As List(Of Account)
        Return LedgerAccounts().Where(Function(a) a.Kind = AccountKind.Loan AndAlso a.IsActive AndAlso
                                                  String.Equals(a.MemberId, memberId, StringComparison.OrdinalIgnoreCase)).
                                OrderBy(Function(a) a.SortOrder).ThenBy(Function(a) a.Name).ToList()
    End Function

    Private Shared Function Plus(a As MemberFigures, b As MemberFigures) As MemberFigures
        Return New MemberFigures With {
            .MemberId = a.MemberId, .Cash = a.Cash + b.Cash, .OwedToThem = a.OwedToThem + b.OwedToThem,
            .OwedToMembers = a.OwedToMembers + b.OwedToMembers, .Loans = a.Loans + b.Loans, .Stock = a.Stock + b.Stock,
            .Expense = a.Expense + b.Expense, .Income = a.Income + b.Income, .Opening = a.Opening + b.Opening}
    End Function

    Private Shared Function Minus(a As MemberFigures, b As MemberFigures) As MemberFigures
        Return New MemberFigures With {
            .MemberId = a.MemberId, .Cash = a.Cash - b.Cash, .OwedToThem = a.OwedToThem - b.OwedToThem,
            .OwedToMembers = a.OwedToMembers - b.OwedToMembers, .Loans = a.Loans - b.Loans, .Stock = a.Stock - b.Stock,
            .Expense = a.Expense - b.Expense, .Income = a.Income - b.Income, .Opening = a.Opening - b.Opening}
    End Function

    Private Function SummariesById() As Dictionary(Of String, MemberFigures)
        Dim d As New Dictionary(Of String, MemberFigures)(StringComparer.OrdinalIgnoreCase)
        For Each f As MemberFigures In LQ().Summaries()
            d(f.MemberId) = f
        Next
        Return d
    End Function

    Private Shared Function FiguresOf(d As Dictionary(Of String, MemberFigures), memberId As String) As MemberFigures
        Dim f As MemberFigures = Nothing
        If d.TryGetValue(memberId, f) Then Return f
        Return New MemberFigures With {.MemberId = memberId}
    End Function
    ' ====================================================================
    '  PERSONS AND ACCOUNTS
    '  Every account is a ledger member of its own: its own balance, items, Credit / Withdraw and debts.
    '  It is saved as a member named "<Person> - <Account>" (member names are unique) with
    '    Members.Owner_ID     = the person it belongs to
    '    Members.Account_Type = Capital / Loan
    '  and one fund of its own, named like the account (what Paid From shows).
    '  A person (Owner_ID empty, type Person) only groups their accounts: name, Staff ID, contacts, groups, status.
    '  Both columns are added to Members the first time the page opens (ALTER TABLE, nothing else changes),
    '  and a person who has no accounts yet gets them from their old funds / loans (or a "Cash" one).
    ' ====================================================================

    Private Const OwnerColumn As String = "Owner_ID"
    Private Const AccountTypeColumn As String = "Account_Type"
    Private Const LoanLimitColumn As String = "Loan_Limit"     ' on a loan's _Have account: the loan limit
    Private Const LoanOfColumn As String = "Loan_Of"           ' on a loan's _Spent account: its _Have account
    Private Const AccountNameSep As String = " - "
    Private Const DefaultAccountName As String = "Cash"

    Private Class AccountInfo
        Public OwnerId As String = ""
        Public AccountType As String = ""
        Public LoanLimit As Decimal? = Nothing
        Public LoanOf As String = ""
    End Class

    Private accountInfoCache As Dictionary(Of String, AccountInfo)
    Private shopCustomerCache As HashSet(Of String)   ' members of Customer Type Shop
    Private Shared accountColumnsReady As Boolean

    ''' <summary>Adds Members.Owner_ID and Members.Account_Type when they are missing (once per application start).</summary>
    Private Shared Sub EnsureAccountColumns()
        If accountColumnsReady Then Return
        Using cn As System.Data.OleDb.OleDbConnection = OpenOrdersDb()
            Dim cols As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Using cmd As New System.Data.OleDb.OleDbCommand("SELECT TOP 1 * FROM Members", cn)
                Using rd As System.Data.OleDb.OleDbDataReader = cmd.ExecuteReader(CommandBehavior.SchemaOnly)
                    For i As Integer = 0 To rd.FieldCount - 1
                        cols.Add(rd.GetName(i))
                    Next
                End Using
            End Using
            If Not cols.Contains(OwnerColumn) Then
                Using cmd As New System.Data.OleDb.OleDbCommand("ALTER TABLE Members ADD COLUMN " & OwnerColumn & " TEXT(10)", cn)
                    cmd.ExecuteNonQuery()
                End Using
            End If
            If Not cols.Contains(AccountTypeColumn) Then
                Using cmd As New System.Data.OleDb.OleDbCommand("ALTER TABLE Members ADD COLUMN " & AccountTypeColumn & " TEXT(20)", cn)
                    cmd.ExecuteNonQuery()
                End Using
            End If
            If Not cols.Contains(LoanLimitColumn) Then
                Using cmd As New System.Data.OleDb.OleDbCommand("ALTER TABLE Members ADD COLUMN " & LoanLimitColumn & " CURRENCY", cn)
                    cmd.ExecuteNonQuery()
                End Using
            End If
            If Not cols.Contains(LoanOfColumn) Then
                Using cmd As New System.Data.OleDb.OleDbCommand("ALTER TABLE Members ADD COLUMN " & LoanOfColumn & " TEXT(10)", cn)
                    cmd.ExecuteNonQuery()
                End Using
            End If
        End Using
        accountColumnsReady = True
    End Sub

    ''' <summary>Member_ID -> owner and type, for the members that are accounts.</summary>
    Private Function AccountInfos() As Dictionary(Of String, AccountInfo)
        If accountInfoCache Is Nothing Then
            Dim d As New Dictionary(Of String, AccountInfo)(StringComparer.OrdinalIgnoreCase)
            EnsureAccountColumns()
            Dim dt As DataTable = OrdersDB.GetDataTable("SELECT Member_ID, " & OwnerColumn & ", " & AccountTypeColumn & ", " &
                                                        LoanLimitColumn & ", " & LoanOfColumn & " FROM Members")
            Dim shops As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each r As DataRow In dt.Rows
                Dim owner As String = ColText(r, OwnerColumn).Trim()
                If owner = "" Then
                    If SameName(ColText(r, AccountTypeColumn).Trim(), CustomerTypeShop) Then shops.Add(r("Member_ID").ToString())
                    Continue For
                End If
                Dim limitValue As Object = r(LoanLimitColumn)
                d(r("Member_ID").ToString()) = New AccountInfo With {
                    .OwnerId = owner, .AccountType = ColText(r, AccountTypeColumn).Trim(),
                    .LoanLimit = If(limitValue Is Nothing OrElse IsDBNull(limitValue), CType(Nothing, Decimal?), DbDecimal(limitValue)),
                    .LoanOf = ColText(r, LoanOfColumn).Trim()}
            Next
            accountInfoCache = d
            shopCustomerCache = shops
        End If
        Return accountInfoCache
    End Function

    ''' <summary>The person an account belongs to ("" when the member is not an account).</summary>
    Private Function OwnerOf(memberId As String) As String
        Dim info As AccountInfo = Nothing
        Return If(AccountInfos().TryGetValue(If(memberId, ""), info), info.OwnerId, "")
    End Function

    Private Function AccountTypeOf(memberId As String) As String
        Dim info As AccountInfo = Nothing
        If Not AccountInfos().TryGetValue(If(memberId, ""), info) Then Return ""
        Return If(info.AccountType = "", AccTypeCapital, info.AccountType)
    End Function

    Private Function IsAccountMember(m As Member) As Boolean
        Return m IsNot Nothing AndAlso OwnerOf(m.Id) <> ""
    End Function

    ''' <summary>A person: groups their accounts (not an account, not the shop, not a bank).</summary>
    Private Function IsPerson(m As Member) As Boolean
        Return m IsNot Nothing AndAlso m.MemberType = MemberTypes.Person AndAlso Not IsAccountMember(m)
    End Function

    ''' <summary>A person's active accounts: Capital first, then Loans; by name. Loans "_Spent" only when asked.</summary>
    Private Function AccountsOfPerson(personId As String, Optional includeSpent As Boolean = True) As List(Of Member)
        Return LedgerMembers().Where(Function(m) m.IsActive AndAlso SameName(OwnerOf(m.Id), personId) AndAlso
                                                  (includeSpent OrElse Not m.Name.EndsWith(LoanSpentSuffix, StringComparison.OrdinalIgnoreCase))).
                               OrderBy(Function(m) If(AccountTypeOf(m.Id) = AccTypeLoan, 1, 0)).ThenBy(Function(m) m.Name).ToList()
    End Function

    ''' <summary>The account's own name: "Jalal Hasan - Cash" -> "Cash".</summary>
    Private Function ShortAccountName(m As Member) As String
        Dim owner As String = LedgerName(OwnerOf(m.Id))
        Dim prefix As String = owner & AccountNameSep
        If owner <> "" AndAlso m.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) AndAlso m.Name.Length > prefix.Length Then
            Return m.Name.Substring(prefix.Length)
        End If
        Return m.Name
    End Function

    ''' <summary>
    ''' An account's Balance: the money it holds minus the money it owes (the ledger's Net without the money
    ''' others owe it; that shows in the Receivable column only). Paying for someone lowers the payer's Balance.
    ''' </summary>
    Private Function Bal(f As MemberFigures) As Decimal
        If IsShopBook(f.MemberId) Then Return f.Net   ' a shop (and its accounts): what members owe it counts
        Return f.Net - f.OwedToThem
    End Function

    ''' <summary>Is this member a shop (Customer Type Shop) or one of a shop's accounts?</summary>
    Private Function IsShopBook(memberId As String) As Boolean
        If String.IsNullOrEmpty(memberId) Then Return False
        If IsShopCustomer(memberId) Then Return True
        Dim owner As String = OwnerOf(memberId)
        Return owner <> "" AndAlso IsShopCustomer(owner)
    End Function

    ''' <summary>The money an account holds: its cash plus its opening balance (negative opening = owed).</summary>
    Private Shared Function CashOf(f As MemberFigures) As Decimal
        Return f.Cash + f.Opening
    End Function

    ''' <summary>A person's figures: the total of their accounts (and of anything still on the person).</summary>
    Private Function PersonFigures(sums As Dictionary(Of String, MemberFigures), personId As String) As MemberFigures
        Dim total As MemberFigures = FiguresOf(sums, personId)
        For Each a As Member In AccountsOfPerson(personId)
            total = Plus(total, FiguresOf(sums, a.Id))
        Next
        Return total
    End Function

    ''' <summary>
    ''' Creates an account for a person: a member "<Person> - <Account>" with the person's groups and status,
    ''' a fund of its own named like the account, and its owner / type in Members.
    ''' </summary>
    Private Function CreateAccountMember(person As Member, accountName As String, accountType As String,
                                         Optional openingBalance As Decimal = 0D) As Member
        Dim m As Member = Ledger.CreateMember(person.Name & AccountNameSep & accountName, MemberTypes.Person,
                                              groupIds:=New List(Of Integer)(person.GroupIds), openingBalance:=openingBalance,
                                              status:=If(person.IsFrozen, MemberStatuses.Frozen, MemberStatuses.Active))
        Using cn As System.Data.OleDb.OleDbConnection = OpenOrdersDb()
            Using cmd As New System.Data.OleDb.OleDbCommand("UPDATE Members SET " & OwnerColumn & " = ?, " & AccountTypeColumn & " = ? WHERE Member_ID = ?", cn)
                cmd.Parameters.Add("@owner", System.Data.OleDb.OleDbType.VarWChar, 10).Value = person.Id
                cmd.Parameters.Add("@type", System.Data.OleDb.OleDbType.VarWChar, 20).Value = accountType
                cmd.Parameters.Add("@id", System.Data.OleDb.OleDbType.VarWChar, 255).Value = m.Id
                cmd.ExecuteNonQuery()
            End Using
        End Using
        Ledger.CreateFund(m.Id, accountName, isDefault:=True)
        ResetLedgerCache()
        Return m
    End Function

    ' ---------- Loans: <Name>_Have and <Name>_Spent kept in step with the loan limit ----------
    ' _Have = what is left to use (money in hand), _Spent = what was used (money owed, a negative balance).
    ' Always: _Have + amount spent = Loan Limit, i.e. _Spent balance = _Have balance - Loan Limit.
    ' Orders, Credit and Withdraw go through _Have; after every change the _Spent account is set to match
    ' (its opening balance in Members is adjusted, so it needs no journals of its own).

    Private Class LoanPair
        Public Have As Member
        Public Spent As Member
        Public Limit As Decimal
    End Class

    ''' <summary>The opening balances in Members (Member_ID -> Member_Opening_Balance).</summary>
    Private Shared Function OpeningBalances() As Dictionary(Of String, Decimal)
        Dim d As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        For Each r As DataRow In OrdersDB.GetDataTable("SELECT Member_ID, Member_Opening_Balance FROM Members").Rows
            d(r("Member_ID").ToString()) = DbDecimal(r("Member_Opening_Balance"))
        Next
        Return d
    End Function

    ''' <summary>The _Spent account of a _Have account: linked by Loan_Of, else the same owner and name with _Spent.</summary>
    Private Function SpentOf(have As Member) As Member
        Dim linked As Member = LedgerMembers().FirstOrDefault(Function(m) m.IsActive AndAlso SameName(LoanOfOf(m.Id), have.Id))
        If linked IsNot Nothing Then Return linked
        If Not have.Name.EndsWith(LoanHaveSuffix, StringComparison.OrdinalIgnoreCase) Then Return Nothing
        Dim spentName As String = have.Name.Substring(0, have.Name.Length - LoanHaveSuffix.Length) & LoanSpentSuffix
        Return LedgerMembers().FirstOrDefault(Function(m) m.IsActive AndAlso SameName(m.Name, spentName) AndAlso
                                                           SameName(OwnerOf(m.Id), OwnerOf(have.Id)))
    End Function

    ''' <summary>The _Have account of a _Spent account, or Nothing.</summary>
    Private Function HaveOf(spent As Member) As Member
        Return LedgerMembers().Where(Function(m) m.IsActive AndAlso (LoanLimitOf(m.Id).HasValue OrElse
                                                 m.Name.EndsWith(LoanHaveSuffix, StringComparison.OrdinalIgnoreCase))).
                               FirstOrDefault(Function(m) SpentOf(m) IsNot Nothing AndAlso SameName(SpentOf(m).Id, spent.Id))
    End Function

    Private Function LoanLimitOf(memberId As String) As Decimal?
        Dim info As AccountInfo = Nothing
        Return If(AccountInfos().TryGetValue(If(memberId, ""), info), info.LoanLimit, CType(Nothing, Decimal?))
    End Function

    Private Function LoanOfOf(memberId As String) As String
        Dim info As AccountInfo = Nothing
        Return If(AccountInfos().TryGetValue(If(memberId, ""), info), info.LoanOf, "")
    End Function

    ''' <summary>Every loan with a limit: its _Have and _Spent accounts.</summary>
    Private Function LoanPairs() As List(Of LoanPair)
        Dim list As New List(Of LoanPair)
        For Each h As Member In LedgerMembers().Where(Function(m) m.IsActive AndAlso LoanLimitOf(m.Id).HasValue)
            Dim s As Member = SpentOf(h)
            If s IsNot Nothing Then list.Add(New LoanPair With {.Have = h, .Spent = s, .Limit = LoanLimitOf(h.Id).Value})
        Next
        Return list
    End Function

    Private Sub SetOpeningBalance(cn As System.Data.OleDb.OleDbConnection, memberId As String, opening As Decimal)
        Using cmd As New System.Data.OleDb.OleDbCommand("UPDATE Members SET Member_Opening_Balance = ? WHERE Member_ID = ?", cn)
            cmd.Parameters.Add("@opening", System.Data.OleDb.OleDbType.Currency).Value = Math.Round(opening, 3)
            cmd.Parameters.Add("@id", System.Data.OleDb.OleDbType.VarWChar, 255).Value = memberId
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>Saves a loan's limit on its _Have account and links its _Spent account to it.</summary>
    Private Sub SaveLoanLink(cn As System.Data.OleDb.OleDbConnection, haveId As String, spentId As String, limit As Decimal)
        Using cmd As New System.Data.OleDb.OleDbCommand("UPDATE Members SET " & LoanLimitColumn & " = ? WHERE Member_ID = ?", cn)
            cmd.Parameters.Add("@limit", System.Data.OleDb.OleDbType.Currency).Value = Math.Round(limit, 3)
            cmd.Parameters.Add("@id", System.Data.OleDb.OleDbType.VarWChar, 255).Value = haveId
            cmd.ExecuteNonQuery()
        End Using
        Using cmd As New System.Data.OleDb.OleDbCommand("UPDATE Members SET " & LoanOfColumn & " = ? WHERE Member_ID = ?", cn)
            cmd.Parameters.Add("@have", System.Data.OleDb.OleDbType.VarWChar, 10).Value = haveId
            cmd.Parameters.Add("@id", System.Data.OleDb.OleDbType.VarWChar, 255).Value = spentId
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Sets every loan's _Spent account to _Have - Loan Limit (after orders, Credit / Withdraw, cancellations,
    ''' or changes made elsewhere). Returns how many were changed.
    ''' </summary>
    Private Function SyncLoanPairs() As Integer
        Dim pairs As List(Of LoanPair) = LoanPairs()
        If pairs.Count = 0 Then Return 0
        Dim sums As Dictionary(Of String, MemberFigures) = SummariesById()
        Dim openings As Dictionary(Of String, Decimal) = OpeningBalances()
        Dim changed As Integer = 0
        Using cn As System.Data.OleDb.OleDbConnection = OpenOrdersDb()
            For Each p As LoanPair In pairs
                Dim haveNet As Decimal = Bal(FiguresOf(sums, p.Have.Id))
                Dim spentNet As Decimal = Bal(FiguresOf(sums, p.Spent.Id))
                Dim target As Decimal = Math.Round(haveNet - p.Limit, 3)
                If Math.Round(spentNet, 3) = target Then Continue For
                Dim spentOpening As Decimal = If(openings.ContainsKey(p.Spent.Id), openings(p.Spent.Id), 0D)
                SetOpeningBalance(cn, p.Spent.Id, spentOpening + (target - spentNet))
                changed += 1
            Next
        End Using
        If changed > 0 Then ResetLedgerCache()
        Return changed
    End Function

    ''' <summary>
    ''' Sets a loan so that the amount spent is spentAmount and the limit is limit: _Have = limit - spent,
    ''' _Spent = -spent (both through their opening balances), and saves the limit and the link.
    ''' </summary>
    Private Sub SetLoanFigures(have As Member, spent As Member, limit As Decimal, spentAmount As Decimal)
        Dim sums As Dictionary(Of String, MemberFigures) = SummariesById()
        Dim openings As Dictionary(Of String, Decimal) = OpeningBalances()
        Dim haveNet As Decimal = Bal(FiguresOf(sums, have.Id))
        Dim spentNet As Decimal = Bal(FiguresOf(sums, spent.Id))
        Dim haveOpening As Decimal = If(openings.ContainsKey(have.Id), openings(have.Id), 0D)
        Dim spentOpening As Decimal = If(openings.ContainsKey(spent.Id), openings(spent.Id), 0D)
        Using cn As System.Data.OleDb.OleDbConnection = OpenOrdersDb()
            SetOpeningBalance(cn, have.Id, haveOpening + ((limit - spentAmount) - haveNet))
            SetOpeningBalance(cn, spent.Id, spentOpening + ((-spentAmount) - spentNet))
            SaveLoanLink(cn, have.Id, spent.Id, limit)
        End Using
        ResetLedgerCache()
    End Sub

    ''' <summary>
    ''' Administrator, first page load: every person without accounts gets them, one per old fund / loan of theirs
    ''' (the default fund first, carrying the person's opening balance), or a single "Cash" one. The person's old
    ''' funds / loans are then hidden. A person who already has journals keeps them and is only reported.
    ''' </summary>
    Private Sub EnsurePersonAccounts()
        Dim notes As New List(Of String)
        Try
            AccountInfos()   ' adds the columns if needed
            Dim openings As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
            For Each r As DataRow In OrdersDB.GetDataTable("SELECT Member_ID, Member_Opening_Balance FROM Members").Rows
                openings(r("Member_ID").ToString()) = DbDecimal(r("Member_Opening_Balance"))
            Next
            For Each p As Member In LedgerMembers().Where(Function(x) x.IsActive AndAlso IsPerson(x)).ToList()
                If AccountsOfPerson(p.Id).Count > 0 Then Continue For
                If LQ().Statement(p.Id).Any() Then
                    notes.Add(p.Name & " has journals of their own, so no accounts were made for them")
                    Continue For
                End If
                Dim old As List(Of Account) = FundsOf(p.Id).Concat(LoansOf(p.Id)).ToList()
                Dim plan As New List(Of Tuple(Of String, String))
                For Each a As Account In old
                    plan.Add(Tuple.Create(a.Name, If(a.Kind = AccountKind.Loan, AccTypeLoan, AccTypeCapital)))
                Next
                If plan.Count = 0 Then plan.Add(Tuple.Create(DefaultAccountName, AccTypeCapital))
                Dim opening As Decimal = If(openings.ContainsKey(p.Id), openings(p.Id), 0D)
                For i As Integer = 0 To plan.Count - 1
                    CreateAccountMember(p, plan(i).Item1, plan(i).Item2, If(i = 0, opening, 0D))
                Next
                ' The opening balance moved to the first account; the old funds / loans are no longer used
                Using cn As System.Data.OleDb.OleDbConnection = OpenOrdersDb()
                    Dim pid As String = OrdersDB.SqlText(p.Id)
                    For Each sql As String In {"UPDATE Members SET Member_Opening_Balance = 0 WHERE Member_ID = " & pid,
                                               "UPDATE Accounts SET Is_Active = False WHERE Member_ID = " & pid & " AND Account_Kind IN ('Fund', 'Loan')"}
                        Using cmd As New System.Data.OleDb.OleDbCommand(sql, cn)
                            cmd.ExecuteNonQuery()
                        End Using
                    Next
                End Using
                ResetLedgerCache()
            Next
        Catch ex As Exception
            notes.Add("The accounts could not be prepared: " & ex.Message)
        End Try
        If notes.Count > 0 Then ShowMsg(String.Join(". ", notes) & ".", True)
    End Sub

    ' ====================================================================
    '  PAGE
    ' ====================================================================

    ''' <summary>A main tab was clicked (CommandArgument = its index in MultiView3).</summary>
    Protected Sub MainTab_Command(sender As Object, e As CommandEventArgs)
        Dim index As Integer
        If Integer.TryParse(Convert.ToString(e.CommandArgument), index) Then ShowTab(index)
    End Sub

    ''' <summary>Shows a tab (its look is set in PreRender by HighlightMainTab). Tabs the user may not open are ignored.</summary>
    Private Sub ShowTab(Index As Integer)
        If Index < 0 OrElse Index >= MultiView3.Views.Count Then Return
        If Not CanOpenTab(Index) Then Return
        MultiView3.ActiveViewIndex = Index
    End Sub

    ''' <summary>
    ''' Main tabs use the same look as Order | Preview: the active one is white and joins the body.
    ''' Tabs the user may not open are greyed out and cannot be clicked.
    ''' </summary>
    Private Sub HighlightMainTab()
        Dim tabs As LinkButton() = {lnkTab0, lnkTab1, lnkTab2, lnkTab3, lnkTab4, lnkTab5, lnkTab6}
        For i As Integer = 0 To tabs.Length - 1
            Dim allowed As Boolean = CanOpenTab(i)
            tabs(i).Enabled = allowed
            tabs(i).ToolTip = If(allowed, "", "Not available for your account")
            tabs(i).CssClass = "subtab" & If(i = MultiView3.ActiveViewIndex, " active", "") & If(allowed, "", " locked")
        Next
    End Sub

    ' ====================================================================
    '  ACCESS
    '  Staff ID 2271 (administrator): every tab, and any member can be picked in Payment / Transactions.
    '  Everybody else: only Transactions and Payment, both fixed to their own member.
    ' ====================================================================

    Private Const AdminStaffId As String = "2271"

    Private ReadOnly Property IsAdmin As Boolean
        Get
            Return String.Equals(CurrentLogin, AdminStaffId, StringComparison.OrdinalIgnoreCase) OrElse
                   String.Equals(CurrentStaffId, AdminStaffId, StringComparison.OrdinalIgnoreCase)
        End Get
    End Property

    Private Function CanOpenTab(index As Integer) As Boolean
        Return IsAdmin OrElse index = TransactionsTabIndex OrElse index = PaymentTabIndex
    End Function

    Private Function NotRegisteredText() As String
        If CurrentLogin = "" Then Return "Sign in on the Payment tab with your CPR and the one-time code sent to your mobile and email."
        Return "No member has the Staff ID """ & CurrentLogin & """ (Members.Member_StaffID)."
    End Function

    ' ====================================================================
    '  PAYMENT (ported from Default.aspx)
    '  Shows the member's balance and a QR code to pay what they owe:
    '    amount = -Balance when the balance is negative, otherwise 0.000 (can be changed, then Re-Generate QR)
    '  Administrator: any member can be picked.  Everybody else: fixed to their own member.
    ' ====================================================================

    Private Const PayIban As String = "BH10BBKU00200001164082"

    Protected Sub btnPayRegenerate_Click(sender As Object, e As EventArgs) Handles btnPayRegenerate.Click
        Dim amount As Decimal = Math.Max(0D, Math.Round(ParseNumber(txtPayAmount.Text), 3))
        txtPayAmount.Text = Money(amount)
        Try
            imgPayQr.ImageUrl = MakePaymentQr(amount, Convert.ToString(ViewState("PayLoaded")))
        Catch ex As Exception
            lblPayMsg.Text = Server.HtmlEncode("Could not create the QR code: " & ex.Message)
            lblPayMsg.Visible = True
        End Try
    End Sub

    ''' <summary>Payment tab (PreRender): member list, then the member's figures and QR when the member changes.</summary>
    Private Sub RefreshPayment()
        If Not IsAdmin AndAlso CurrentMemberId = "" Then
            ShowPayVerify()   ' outside the intranet: CPR, then the one-time code
            Return
        End If
        pnlPayVerify.Visible = False
        lnkPaySignOut.Visible = (VisitorMemberId <> "" AndAlso Convert.ToString(ViewState("UserMemberID")) = "")
        ' Member list: everybody for the administrator, only themselves for other users
        Dim current As String = If(ddlPayMember.Items.Count = 0, CurrentMemberId, ddlPayMember.SelectedValue)
        ddlPayMember.Items.Clear()
        Try
            If IsAdmin Then
                ' People (their accounts are added up) and the shop; not the accounts, not banks
                For Each m As Member In LedgerMembers().Where(Function(x) x.IsActive AndAlso x.MemberType <> MemberTypes.Bank AndAlso Not IsAccountMember(x)).
                                                        OrderBy(Function(x) x.Name)
                    ddlPayMember.Items.Add(New ListItem(m.Name, m.Id))
                Next
            ElseIf CurrentMemberId <> "" Then
                ddlPayMember.Items.Add(New ListItem(LedgerName(CurrentMemberId), CurrentMemberId))
                current = CurrentMemberId
            End If
        Catch ex As Exception
            lblPayMsg.Text = Server.HtmlEncode("Could not read the members: " & ex.Message)
            lblPayMsg.Visible = True
            pnlPay.Visible = False
            Return
        End Try
        ddlPayMember.Enabled = IsAdmin

        Dim it As ListItem = ddlPayMember.Items.FindByValue(If(current, ""))
        If it Is Nothing AndAlso ddlPayMember.Items.Count > 0 Then it = ddlPayMember.Items(0)
        If it Is Nothing Then
            lblPayMsg.Text = Server.HtmlEncode(NotRegisteredText())
            lblPayMsg.Visible = True
            pnlPay.Visible = False
            ViewState("PayLoaded") = Nothing
            Return
        End If
        ddlPayMember.ClearSelection()
        it.Selected = True
        pnlPay.Visible = True

        ' Reload figures and QR only when another member is shown (each QR is a new image file)
        If Convert.ToString(ViewState("PayLoaded")) <> it.Value Then
            LoadPaymentMember(it.Value)
            ViewState("PayLoaded") = it.Value
        End If
    End Sub

    Private Sub LoadPaymentMember(memberId As String)
        Try
            Dim q As LedgerQueries = LQ()
            ' A person: the total of all their accounts
            Dim ids As New List(Of String) From {memberId}
            ids.AddRange(AccountsOfPerson(memberId).Select(Function(a) a.Id))
            Dim f As MemberFigures = q.Summary(memberId)
            For Each accId As String In ids.Skip(1)
                f = Plus(f, q.Summary(accId))
            Next
            lblPayName.Text = Server.HtmlEncode(LedgerName(memberId))
            lblPayBalance.Text = Money(Bal(f))
            lblPayBalance.CssClass = If(Bal(f) < 0D, "neg", "")

            ' Last update = the member's most recent journal (order, deposit, settlement, ...)
            Dim last As StatementLine = ids.SelectMany(Function(x) q.Statement(x)).
                                            OrderBy(Function(x) x.Journal.JournalDate).ThenBy(Function(x) x.Journal.Id).LastOrDefault()
            lblPayUpdated.Text = If(last Is Nothing, "-",
                Server.HtmlEncode(last.Journal.JournalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) &
                                  If(last.Journal.OrderId <> "", "  (order " & last.Journal.OrderId & ")", "")))

            ' Amount owed (a negative Net balance), 0.000 otherwise
            Dim amount As Decimal = If(Bal(f) < 0D, Math.Abs(Bal(f)), 0D)
            txtPayAmount.Text = Money(amount)
            imgPayQr.ImageUrl = MakePaymentQr(amount, memberId)
        Catch ex As Exception
            lblPayMsg.Text = Server.HtmlEncode("Could not load the payment details: " & ex.Message)
            lblPayMsg.Visible = True
        End Try
    End Sub

    ' ====================================================================
    '  PAYMENT SIGN-IN FOR VISITORS (no Staff ID: the page is opened from outside the intranet)
    '  1) CPR  ->  2) a 6-digit code is sent to the member's mobile and email  ->  3) six boxes  ->  QR and figures.
    '  The same answer is shown whether the CPR is registered or not; the code lives only (hashed) in the Session.
    ' ====================================================================

    Private Property PayOtp As PaymentOtp.Pending
        Get
            Return TryCast(Session("PayOtp"), PaymentOtp.Pending)
        End Get
        Set(value As PaymentOtp.Pending)
            If value Is Nothing Then Session.Remove("PayOtp") Else Session("PayOtp") = value
        End Set
    End Property

    Private Sub ShowPayVerify()
        pnlPay.Visible = False
        lnkPaySignOut.Visible = False
        ddlPayMember.Items.Clear()
        ddlPayMember.Enabled = False
        pnlPayVerify.Visible = True
        Dim pending As PaymentOtp.Pending = PayOtp
        Dim waiting As Boolean = pending IsNot Nothing AndAlso Not pending.IsExpired
        pnlPayCprStep.Visible = Not waiting
        pnlPayOtpStep.Visible = waiting
        If waiting Then
            hfPayOtp.Value = ""
            Dim wait As Integer = CInt(Math.Ceiling((pending.SentUtc + PaymentOtp.ResendAfter - DateTime.UtcNow).TotalSeconds))
            lnkPayResend.Enabled = wait <= 0
            lnkPayResend.Text = If(wait > 0, "Send a new code (in " & wait & " s)", "Send a new code")
        End If
    End Sub

    Private Sub PayVerifyMessage(text As String, isError As Boolean)
        lblPayVerifyMsg.Text = Server.HtmlEncode(text)
        lblPayVerifyMsg.CssClass = "order-msg " & If(isError, "err", "ok")
        lblPayVerifyMsg.Visible = True
    End Sub

    Protected Sub btnPaySendOtp_Click(sender As Object, e As EventArgs) Handles btnPaySendOtp.Click
        Dim cpr As String = New String(txtPayCpr.Text.Where(Function(ch) Char.IsDigit(ch)).ToArray())
        If cpr.Length <> 9 Then
            PayVerifyMessage("Please enter your CPR: 9 digits.", True)
            Return
        End If
        SendPayOtp(cpr)
    End Sub

    Protected Sub lnkPayResend_Click(sender As Object, e As EventArgs) Handles lnkPayResend.Click
        Dim pending As PaymentOtp.Pending = PayOtp
        If pending Is Nothing Then Return
        If DateTime.UtcNow < pending.SentUtc + PaymentOtp.ResendAfter Then Return
        SendPayOtp(pending.Cpr)
    End Sub

    Protected Sub lnkPayChangeCpr_Click(sender As Object, e As EventArgs) Handles lnkPayChangeCpr.Click
        PayOtp = Nothing
        txtPayCpr.Text = ""
    End Sub

    Protected Sub lnkPaySignOut_Click(sender As Object, e As EventArgs) Handles lnkPaySignOut.Click
        Session.Remove("PayVisitorMemberId")
        PayOtp = Nothing
        ViewState("PayLoaded") = Nothing
        txtPayCpr.Text = ""
    End Sub

    Private Sub SendPayOtp(cpr As String)
        If Not PaymentOtp.AllowSend(cpr, Request.UserHostAddress) Then
            PayVerifyMessage("Too many codes were requested. Please try again in an hour.", True)
            Return
        End If

        Dim code As String = PaymentOtp.NewCode()
        Dim pending As New PaymentOtp.Pending With {
            .Cpr = cpr, .Salt = PaymentOtp.NewSalt(), .SentUtc = DateTime.UtcNow, .ExpiresUtc = DateTime.UtcNow + PaymentOtp.Lifetime}
        pending.CodeHash = PaymentOtp.Hash(code, pending.Salt)

        Dim member As Member = Nothing
        Try
            member = LedgerMembers().FirstOrDefault(Function(m) m.IsActive AndAlso m.MemberType <> MemberTypes.Bank AndAlso
                                                                Not String.IsNullOrEmpty(m.Cpr) AndAlso SameName(m.Cpr, cpr))
        Catch
        End Try
        If member IsNot Nothing Then
            Dim smsProblem As String = PaymentOtp.SendSms(member.Mobile, code)
            Dim emailProblem As String = PaymentOtp.SendEmail(member.Email, member.Name, code)
            If smsProblem <> "" AndAlso emailProblem <> "" AndAlso Not PaymentOtp.ShowCodeForTesting(Request) Then
                PayVerifyMessage("The code could not be sent. Please ask the administrator to check your mobile and email, " &
                                 "and the site's SMS / email settings.", True)
                Return
            End If
            pending.MemberId = member.Id
        End If
        PayOtp = pending   ' an unregistered CPR also gets this step (no code can match), so nobody learns who is registered

        Dim notice As String = "A " & PaymentOtp.CodeLength & "-digit code has been sent to the mobile and the email registered with CPR " &
                               PaymentOtp.MaskCpr(cpr) & ". Enter it below; it is valid for " & CInt(PaymentOtp.Lifetime.TotalMinutes) & " minutes."
        If PaymentOtp.ShowCodeForTesting(Request) AndAlso member IsNot Nothing Then notice &= "   [testing: " & code & "]"
        PayVerifyMessage(notice, False)
    End Sub

    Protected Sub btnPayVerify_Click(sender As Object, e As EventArgs) Handles btnPayVerify.Click
        Dim pending As PaymentOtp.Pending = PayOtp
        If pending Is Nothing OrElse pending.IsExpired Then
            PayOtp = Nothing
            PayVerifyMessage("The code has expired. Please enter your CPR again for a new one.", True)
            Return
        End If
        Dim code As String = New String(If(hfPayOtp.Value, "").Where(Function(ch) Char.IsDigit(ch)).ToArray())
        If code.Length <> PaymentOtp.CodeLength Then
            PayVerifyMessage("Please enter all " & PaymentOtp.CodeLength & " digits of the code.", True)
            Return
        End If

        pending.Attempts += 1
        If pending.MemberId <> "" AndAlso PaymentOtp.SameHash(PaymentOtp.Hash(code, pending.Salt), pending.CodeHash) Then
            PayOtp = Nothing
            Session("PayVisitorMemberId") = pending.MemberId   ' signed in for this session
            ViewState("PayLoaded") = Nothing
            Return   ' PreRender shows the QR and the figures
        End If
        If pending.Attempts >= PaymentOtp.MaxAttempts Then
            PayOtp = Nothing
            PayVerifyMessage("Too many wrong codes. Please enter your CPR again for a new one.", True)
        Else
            PayVerifyMessage("That code is not correct. " & (PaymentOtp.MaxAttempts - pending.Attempts) & " tries left.", True)
        End If
    End Sub


    ''' <summary>
    ''' Draws the payment QR ({"iban":"...","amount":"0.000"}) as a JPEG in ~/Pictures and returns its URL.
    ''' Same output as Default.aspx (Net.Codecrete.QrCodeGenerator, 8 px per module, quiet zone 4).
    ''' </summary>
    Private Function MakePaymentQr(amount As Decimal, memberId As String) As String
        Dim text As String = "{""iban"":""" & PayIban & """,""amount"":""" & Money(amount) & """}"
        Dim qr = Net.Codecrete.QrCodeGenerator.QrCode.EncodeText(text, Net.Codecrete.QrCodeGenerator.QrCode.Ecc.Medium)

        Dim moduleScale As Integer = 8
        Dim border As Integer = 4
        Dim size As Integer = (qr.Size + border * 2) * moduleScale

        Dim picturesPath As String = Server.MapPath("~/Pictures")
        If Not System.IO.Directory.Exists(picturesPath) Then System.IO.Directory.CreateDirectory(picturesPath)
        Dim safeId As String = System.Text.RegularExpressions.Regex.Replace(If(memberId, ""), "[^A-Za-z0-9]", "")
        Dim fileName As String = "qr_" & safeId & "_" & DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff", CultureInfo.InvariantCulture) & ".jpg"
        Dim fullPath As String = System.IO.Path.Combine(picturesPath, fileName)

        Using bmp As New System.Drawing.Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            Using g As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(bmp)
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half
                g.Clear(System.Drawing.Color.White)
                Using brush As New System.Drawing.SolidBrush(System.Drawing.Color.Black)
                    For y As Integer = 0 To qr.Size - 1
                        For x As Integer = 0 To qr.Size - 1
                            If qr.GetModule(x, y) Then
                                g.FillRectangle(brush, (x + border) * moduleScale, (y + border) * moduleScale, moduleScale, moduleScale)
                            End If
                        Next
                    Next
                End Using
            End Using

            Dim jpgCodec As System.Drawing.Imaging.ImageCodecInfo = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders().
                First(Function(c) c.MimeType = "image/jpeg")
            Using encParams As New System.Drawing.Imaging.EncoderParameters(1)
                encParams.Param(0) = New System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L)
                bmp.Save(fullPath, jpgCodec, encParams)
            End Using
        End Using

        Return "~/Pictures/" & fileName
    End Function

    Private Sub ExpensesAndDebts_Load(sender As Object, e As EventArgs) Handles Me.Load
        lblOrderMsg.Visible = False   ' a message shows for one request only
        lblItemsMsg.Visible = False
        lblPayMsg.Visible = False
        lblPayVerifyMsg.Visible = False
        lblAccMsg.Visible = False
        lblTxMsg.Visible = False
        lblDashMsg.Visible = False
        lblMxMsg.Visible = False
        If Not Page.IsPostBack Then
            If Not String.IsNullOrEmpty(Request("ID")) Then lblID.Text = Request("ID")
            If Not String.IsNullOrEmpty(Request("mode")) Then lblMode.Text = Request("mode")
            If Not String.IsNullOrEmpty(Request("isDialogue")) Then lblIsDialogue.Text = Request("isDialogue")

            IdentifyUser()         ' Windows login -> Members.Member_StaffID -> Member_Name
            If IsAdmin AndAlso LedgerProblem() = "" Then
                EnsurePersonAccounts()   ' accounts are the members (see PERSONS AND ACCOUNTS)
                Try
                    SyncLoanPairs()      ' loans: _Spent = _Have - limit (also after orders saved elsewhere)
                Catch ex As Exception
                    ShowMsg("The loan accounts could not be checked: " & ex.Message, True)
                End Try
                Try
                    EnsureShopsGroup()   ' every shop in the group "Shops" (also shops made before this rule)
                Catch ex As Exception
                    ShowMsg("The shops could not be put in the group ""Shops"": " & ex.Message, True)
                End Try
            End If
            If LedgerProblem() = "" Then ResetOrder()
            If IsAdmin Then
                ShowTab(OrderTabIndex)     ' Order is the first tab
            Else
                ShowTab(PaymentTabIndex)   ' other users start on their own Payment
            End If
        ElseIf IsAdmin Then
            ' The group picked in the Order grid's corner (a plain <select>, posted with every request)
            Dim posted As String = Request.Form("groupFilter")
            Dim g As Integer
            If posted IsNot Nothing AndAlso Integer.TryParse(posted, NumberStyles.Integer, CultureInfo.InvariantCulture, g) Then State.GroupFilter = g
            ' The shop picked under it (only there when the order has a Profits column)
            Dim postedShop As String = Request.Form("shopFilter")
            If postedShop IsNot Nothing Then State.ShopFilter = postedShop
            SyncFromUi()   ' only the administrator has the Order tab
        End If
    End Sub

    Protected Sub Page_PreRender(sender As Object, e As EventArgs) Handles Me.PreRender
        ' Never show a tab the user may not open
        If Not CanOpenTab(MultiView3.ActiveViewIndex) Then MultiView3.ActiveViewIndex = PaymentTabIndex

        BindHeader()
        ' The members list (three lines at the top right) is for the administrator only: it shows CPRs and contacts
        phMenu.Visible = IsAdmin
        phMembersPanel.Visible = IsAdmin
        Dim problem As String = LedgerProblem()
        If IsAdmin AndAlso problem = "" Then RenderMembersList()
        If problem <> "" Then
            For Each lbl As Label In {lblOrderMsg, lblItemsMsg, lblPayMsg, lblAccMsg, lblTxMsg, lblDashMsg, lblMxMsg}
                lbl.Text = Server.HtmlEncode(problem)
                lbl.CssClass = "order-msg err"
                lbl.Visible = True
            Next
            HighlightMainTab()
            Return
        End If

        BindOrderTab()
        HighlightMainTab()
        ' The read-only tabs are rebuilt from the ledger whenever they are shown
        Select Case MultiView3.ActiveViewIndex
            Case ItemsTabIndex : RefreshItems()
            Case PaymentTabIndex : RefreshPayment()
            Case AccountsTabIndex : RefreshAccounts()
            Case TransactionsTabIndex : RefreshTransactions()
            Case DashboardTabIndex : RefreshDashboard()
            Case MatricesTabIndex : RefreshMatrices()
        End Select
        ApplyReadOnlyMode()
    End Sub

    ' ====================================================================
    '  CURRENT USER (Windows authentication)
    '  The Windows login (DOMAIN\2271 -> "2271") is looked up in Members.Member_StaffID;
    '  that member's Member_Name is the user. Found once per visit, kept in ViewState.
    '  Needs Windows authentication on: web.config <authentication mode="Windows" />
    '  and, in IIS, Windows Authentication enabled + Anonymous Authentication disabled for the site.
    ' ====================================================================

    ''' <summary>Windows login without the domain ("" if Windows authentication is not on).</summary>
    Protected ReadOnly Property CurrentLogin As String
        Get
            Return Convert.ToString(ViewState("UserLogin"))
        End Get
    End Property

    ''' <summary>Member_StaffID of the current user (= their Windows login).</summary>
    Protected ReadOnly Property CurrentStaffId As String
        Get
            Return Convert.ToString(ViewState("UserStaffID"))
        End Get
    End Property

    ''' <summary>Member_Name of the current user ("" if their Staff ID is not in Members).</summary>
    Protected ReadOnly Property CurrentUserName As String
        Get
            Return Convert.ToString(ViewState("UserName"))
        End Get
    End Property

    ''' <summary>Member_ID of the current user: from the Windows login, else the visitor verified by CPR + OTP ("" if neither).</summary>
    Protected ReadOnly Property CurrentMemberId As String
        Get
            Dim fromLogin As String = Convert.ToString(ViewState("UserMemberID"))
            Return If(fromLogin <> "", fromLogin, VisitorMemberId)
        End Get
    End Property

    ''' <summary>A visitor (no Staff ID) who proved who they are with their CPR and a one-time code, for this session.</summary>
    Private ReadOnly Property VisitorMemberId As String
        Get
            If Session Is Nothing Then Return ""
            Return Convert.ToString(Session("PayVisitorMemberId"))
        End Get
    End Property

    Private Sub IdentifyUser()
        Dim login As String = WindowsLogin()
        ViewState("UserLogin") = login
        ViewState("UserStaffID") = login
        ViewState("UserName") = ""
        ViewState("UserMemberID") = ""
        If login = "" Then Return

        Try
            Dim dt As DataTable = OrdersDB.GetDataTable(
                "SELECT Member_ID, Member_Name, Member_StaffID FROM Members WHERE Trim(Member_StaffID & '') = " & OrdersDB.SqlText(login))
            If dt.Rows.Count > 0 Then
                ViewState("UserName") = ColText(dt.Rows(0), "Member_Name").Trim()
                ViewState("UserMemberID") = dt.Rows(0)("Member_ID").ToString()
                ViewState("UserStaffID") = ColText(dt.Rows(0), "Member_StaffID").Trim()
            End If
        Catch ex As Exception
            ' e.g. the Member_StaffID field is not in the Members table yet
            ' (shown on the Order tab and the Payment tab: the Main Dashboard is empty now)
            For Each lbl As Label In {lblOrderMsg, lblPayMsg}
                lbl.Text = Server.HtmlEncode("Could not identify the user from Members.Member_StaffID: " & ex.Message)
                lbl.CssClass = "order-msg err"
                lbl.Visible = True
            Next
        End Try
    End Sub

    ''' <summary>The Windows account opening the page, without the domain: "EBU\2271" -> "2271", "2271@bank.com" -> "2271".</summary>
    Private Function WindowsLogin() As String
        Dim name As String = ""
        If User IsNot Nothing AndAlso User.Identity IsNot Nothing AndAlso User.Identity.IsAuthenticated Then name = User.Identity.Name
        If String.IsNullOrEmpty(name) Then name = Convert.ToString(Request.ServerVariables("LOGON_USER"))
        name = If(name, "").Trim()
        If name.Contains("\") Then name = name.Substring(name.LastIndexOf("\"c) + 1)
        If name.Contains("@") Then name = name.Substring(0, name.IndexOf("@"c))
        Return name
    End Function

    ''' <summary>Top strip: date / time, then "User Name - Staff ID".</summary>
    Private Sub BindHeader()
        lblNow.Text = DateTime.Now.ToString("ddd dd MMM yyyy   HH:mm:ss", CultureInfo.InvariantCulture)

        If CurrentUserName <> "" Then
            lblUser.Text = Server.HtmlEncode(CurrentUserName & " - " & CurrentStaffId)
        ElseIf VisitorMemberId <> "" AndAlso LedgerProblem() = "" Then
            lblUser.Text = Server.HtmlEncode(LedgerName(VisitorMemberId) & " - verified by CPR")
        ElseIf CurrentLogin <> "" Then
            lblUser.Text = "<span class=""unknown"">" & Server.HtmlEncode(CurrentLogin & " - not registered (no member with this Staff ID)") & "</span>"
        Else
            lblUser.Text = "<span class=""unknown"">Unknown user (Windows authentication is not enabled)</span>"
        End If
    End Sub


    ' ====================================================================
    '  READING THE SCREEN INTO THE MODEL (every postback)
    ' ====================================================================

    Private Sub SyncFromUi()
        If IsViewing Then Return
        Dim st As OrderState = State
        Dim before As String = OrderSignature(st)

        st.Source = txtOrderSource.Text.Trim()
        st.OrderDate = txtOrderDate.Text.Trim()
        st.OrderNo = txtOrderNo.Text.Trim()
        st.Delivery = Math.Max(0D, ParseNumber(txtDelivery.Text))
        st.VAT = Math.Max(0D, ParseNumber(txtVAT.Text))

        ' Profit per unit, typed in the "Profit" row (plain inputs named profit_<column>)
        For c As Integer = 0 To st.Items.Count - 1
            If st.Items(c).IsProfits Then Continue For
            Dim posted As String = Request.Form("profit_" & c.ToString(CultureInfo.InvariantCulture))
            If posted IsNot Nothing Then st.Items(c).Profit = Math.Round(ParseNumber(posted), 3)
        Next

        For Each ri As RepeaterItem In rptMembers.Items
            Dim idx As Integer = CInt(CType(ri.FindControl("hfIdx"), HiddenField).Value)
            If idx < 0 OrElse idx >= st.Members.Count Then Continue For
            Dim m As MemberLine = st.Members(idx)
            m.Credit = ParseNumber(CType(ri.FindControl("txtCredit"), TextBox).Text)
            m.Withdraw = ParseNumber(CType(ri.FindControl("txtWithdraw"), TextBox).Text)
            Dim inner As Repeater = CType(ri.FindControl("rptQty"), Repeater)
            For Each qi As RepeaterItem In inner.Items
                If qi.ItemIndex < m.Qty.Count Then
                    m.Qty(qi.ItemIndex) = ParseNumber(CType(qi.FindControl("txtQty"), TextBox).Text)
                End If
            Next
        Next

        ' Account / Transaction Type / Paid From picked in the Preview
        For Each ri As RepeaterItem In rptPreview.Items
            Dim idx As Integer = CInt(CType(ri.FindControl("hfIdx"), HiddenField).Value)
            If idx < 0 OrElse idx >= st.Preview.Count Then Continue For
            Dim p As PreviewLine = st.Preview(idx)
            If p.AutoRow Then Continue For
            If Not p.AccountLocked Then p.Account = CType(ri.FindControl("ddlAccount"), DropDownList).SelectedValue
            If Not p.TypeLocked Then p.TxType = CType(ri.FindControl("ddlType"), DropDownList).SelectedValue
            p.PaidFrom = CType(ri.FindControl("ddlPaidFrom"), DropDownList).SelectedValue
        Next
        For Each m As MemberLine In st.Members.Where(Function(x) x.IsShopLine)
            m.Credit = 0D : m.Withdraw = 0D   ' the shop's goods lines have no Credit / Withdraw
        Next

        RecalculateProfits(st)   ' the Profits item is calculated, never typed
        If OrderSignature(st) <> before Then MarkPreviewOutdated()
    End Sub

    ''' <summary>Everything that changes the calculation (not the Preview selections).</summary>
    Private Function OrderSignature(st As OrderState) As String
        Dim sb As New System.Text.StringBuilder()
        sb.Append(st.Delivery).Append("|").Append(st.VAT).Append("|").Append(st.Items.Count)
        For Each it As ItemCol In st.Items
            sb.Append("|").Append(it.Title).Append(":").Append(it.Price).Append(":").Append(it.Profit)
        Next
        For Each m As MemberLine In st.Members
            sb.Append("|").Append(m.Id).Append(":").Append(m.Credit).Append(":").Append(m.Withdraw)
            For Each q As Decimal In m.Qty
                sb.Append(",").Append(q)
            Next
        Next
        Return sb.ToString()
    End Function

    ' ====================================================================
    '  SHOWING THE MODEL (every request, in PreRender)
    ' ====================================================================

    Private Sub BindOrderTab()
        Dim st As OrderState = State
        Dim viewing As Boolean = IsViewing

        ' Header
        txtOrderSource.Text = st.Source
        txtOrderDate.Text = st.OrderDate
        txtOrderNo.Text = st.OrderNo
        txtDelivery.Text = Money(st.Delivery)
        txtVAT.Text = Money(st.VAT)
        txtDelivery.Attributes("data-role") = "delivery"
        txtVAT.Attributes("data-role") = "vat"
        txtDelivery.Attributes("data-calc") = ""
        txtVAT.Attributes("data-calc") = ""
        lblTotal.Attributes("data-role") = "total"
        lblTotal.Text = Money(If(viewing, st.SavedTotal, OrderTotal(st)))

        ' Shop result from the saved balances: Shop_Profit - Shop_Loss
        Dim shopProfit As Decimal, shopLoss As Decimal
        If ReadShopResult(shopProfit, shopLoss) Then
            lblShopResult.Text = Money(shopProfit - shopLoss)
            lblShopResultSub.Text = "Profit " & Money(shopProfit) & "  " & ChrW(&H2212) & "  Loss " & Money(shopLoss)
        Else
            lblShopResult.Text = "-"
            lblShopResultSub.Text = "shop accounts not found"
        End If
        lblShopResult.CssClass = If(shopProfit - shopLoss < 0D, "neg", "")

        For Each tb As TextBox In {txtOrderSource, txtOrderDate, txtOrderNo, txtDelivery, txtVAT}
            tb.ReadOnly = viewing
        Next
        btnAddMember.Enabled = Not viewing
        btnAddItem.Enabled = Not viewing
        btnCalculate.Enabled = Not viewing
        btnCreateProfits.Enabled = Not viewing AndAlso ProfitsIndex(st) < 0
        btnNewOrder.CssClass = "btn-op" & If(viewing, " gold", "")

        txtNewItemTitle.Attributes("onkeydown") = "return edTitleKey(event,'" & txtNewItemPrice.ClientID & "');"

        ' Order grid
        litOrderTop.Text = BuildOrderTopRows(st)
        Dim visible As List(Of MemberLine) = VisibleMembers(st)
        PrepareGridOwners(visible)
        rptMembers.DataSource = visible
        rptMembers.DataBind()

        ' Preview
        FillDropDown(ddlHeaderAccount, MemberNames(st), "")
        FillDropDown(ddlHeaderType, TransactionTypes, "")
        FillDropDown(ddlHeaderPaidFrom, st.Preview.SelectMany(Function(p) p.PaidFromOptions).Distinct(StringComparer.OrdinalIgnoreCase), "")
        ddlHeaderAccount.Enabled = Not viewing
        ddlHeaderType.Enabled = Not viewing
        ddlHeaderPaidFrom.Enabled = Not viewing
        btnCancelOrder.Visible = viewing AndAlso IsAdmin AndAlso Not st.IsCancelled
        rptPreview.DataSource = st.Preview
        rptPreview.DataBind()
        lblNoPreview.Visible = (st.Preview.Count = 0)
        lblNoPreview.Text = If(viewing, "This order has no member records.", "Click Calculate to build the Preview.")

        ' Status bar
        pnlStatus.Visible = (st.StatusKind <> "")
        pnlStatus.CssClass = "status-bar status-" & If(st.StatusKind = "", "info", st.StatusKind)
        pnlStatus.Attributes("data-role") = "status"
        pnlStatus.Attributes("data-kind") = st.StatusKind
        lblStatus.Text = Server.HtmlEncode(st.StatusText)
        lblStatus.Attributes("data-role") = "status-text"
        btnProcess.Visible = (st.StatusKind = "ok" AndAlso st.PreviewCurrent AndAlso Not viewing)
        btnProcess.Attributes("data-role") = "process"

        ' Sub-tab look
        lnkSubOrder.CssClass = "subtab" & If(mvOrder.ActiveViewIndex = 0, " active", "")
        lnkSubPreview.CssClass = "subtab" & If(mvOrder.ActiveViewIndex = 1, " active", "")
    End Sub

    ''' <summary>
    ''' The five rows above the members, as in Form1:
    '''   grey block | Profit per unit (typed),
    '''   grey block | Subtotal per item,   grey block | No. Of Item per item,
    '''   grey block | item titles (yellow),   Members | exBalance | Credit | Withdraw | prices (blue)
    ''' </summary>
    Private Function BuildOrderTopRows(st As OrderState) As String
        Dim sb As New System.Text.StringBuilder()

        ' Profit per unit of each item (typed); empty under the Profits item itself
        sb.Append("<tr><td class=""lock"" colspan=""4"">Profit</td>")
        For c As Integer = 0 To st.Items.Count - 1
            If st.Items(c).IsProfits Then
                sb.Append("<td class=""prof""></td>")
            Else
                sb.AppendFormat("<td class=""prof""><input type=""text"" name=""profit_{0}"" class=""cell-in"" value=""{1}"" data-calc="""" data-profit=""{0}""{2} /></td>",
                                c, If(st.Items(c).Profit = 0D, "", Money(st.Items(c).Profit)), If(IsViewing, " readonly=""readonly""", ""))
            End If
        Next
        sb.Append("</tr>")

        sb.Append("<tr><td class=""lock"" colspan=""4"">Subtotal</td>")
        For c As Integer = 0 To st.Items.Count - 1
            Dim cnt As Decimal = ItemCount(st, c)
            sb.AppendFormat("<td data-sub=""{0}"" data-price=""{1}"">{2}</td>", c,
                            st.Items(c).Price.ToString("0.000", CultureInfo.InvariantCulture), Money(cnt * st.Items(c).Price))
        Next
        sb.Append("</tr>")

        sb.Append("<tr><td class=""lock"" colspan=""4"">No. Of Item</td>")
        For c As Integer = 0 To st.Items.Count - 1
            sb.AppendFormat("<td id=""cnt_{0}"">{1}</td>", c, Qty(ItemCount(st, c)))
        Next
        sb.Append("</tr>")

        ' The group filter sits in the grey corner, just above the Members heading
        sb.Append("<tr class=""titles""><td class=""lock grp-cell"" colspan=""4"">")
        sb.Append("<select name=""groupFilter"" class=""grp-filter"" title=""Show the members of one group"" onchange=""__doPostBack('groupFilter','')"">")
        sb.AppendFormat("<option value=""0""{0}>All members</option>", If(st.GroupFilter = 0, " selected=""selected""", ""))
        For Each g As MemberGroup In GroupList()
            sb.AppendFormat("<option value=""{0}""{1}>{2}</option>", g.Id, If(st.GroupFilter = g.Id, " selected=""selected""", ""), Server.HtmlEncode(g.Name))
        Next
        sb.Append("</select>")
        ' With a Profits column: the shop whose accounts are listed (one shop at a time)
        Dim shownShop As String = ShownShopId(st)
        If shownShop <> "" Then
            sb.Append("<select name=""shopFilter"" class=""grp-filter shop-filter"" title=""The shop whose accounts are listed"" onchange=""__doPostBack('shopFilter','')"">")
            For Each s As Member In ShopCustomers()
                sb.AppendFormat("<option value=""{0}""{1}>Shop: {2}</option>", Server.HtmlEncode(s.Id),
                                If(SameName(s.Id, shownShop), " selected=""selected""", ""), Server.HtmlEncode(s.Name))
            Next
            sb.Append("</select>")
        End If
        sb.Append("</td>")
        For Each it As ItemCol In st.Items
            sb.AppendFormat("<td class=""it"">{0}</td>", Server.HtmlEncode(it.Title))
        Next
        sb.Append("</tr>")

        sb.Append("<tr class=""colhead""><td class=""first"">Members</td><td>exBalance</td><td>Credit</td><td>Withdraw</td>")
        For Each it As ItemCol In st.Items
            sb.AppendFormat("<td>{0}</td>", Money(it.Price))
        Next
        sb.Append("</tr>")
        Return sb.ToString()
    End Function

    ' ====================================================================
    '  MEMBERS PANEL (administrator): the three lines at the top right open it.
    '  Every member with their details, groups, status (Active / Frozen) and comment, editable in place.
    '  Drawn as plain inputs (no ViewState); Save reads them back from the form and saves the changed rows.
    ' ====================================================================

    ''' <summary>A row whose save failed keeps what was typed (and its message) when the list is drawn again.</summary>
    Private Class MemberEdit
        Public Name, StaffId, Mobile, Email, Cpr, Status, Comment, ErrorText As String
        Public GroupIds As List(Of Integer)
    End Class

    Private failedEdits As New Dictionary(Of String, MemberEdit)(StringComparer.OrdinalIgnoreCase)

    Private Function PostedEdit(id As String) As MemberEdit
        ' Unvalidated: a comment may contain characters such as "<" (everything is HTML-encoded when shown)
        Dim form As System.Collections.Specialized.NameValueCollection = Request.Unvalidated.Form
        Dim f As Func(Of String, String) = Function(k) If(form("mm_" & k & "_" & id), "").Trim()
        Dim groups As New List(Of Integer)
        For Each v As String In If(form.GetValues("mm_grp_" & id), New String() {})
            Dim g As Integer
            If Integer.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, g) Then groups.Add(g)
        Next
        Return New MemberEdit With {.Name = f("name"), .StaffId = f("staff"), .Mobile = f("mobile"), .Email = f("email"),
                                    .Cpr = f("cpr"), .Status = f("status"), .Comment = f("comment"), .GroupIds = groups}
    End Function

    Private Sub RenderMembersList()
        Dim members As List(Of Member)
        Dim sums As Dictionary(Of String, MemberFigures)
        Try
            members = LedgerMembers().Where(Function(m) m.IsActive AndAlso Not IsAccountMember(m)).OrderBy(Function(m) Val(m.Id)).ToList()
            sums = SummariesById()
        Catch ex As Exception
            litMembersList.Text = "<p class=""tab-note"">" & Server.HtmlEncode("Could not read the members: " & ex.Message) & "</p>"
            Return
        End Try
        Dim groups As List(Of MemberGroup) = GroupList()
        Dim enc As Func(Of String, String) = Function(t) Server.HtmlEncode(If(t, ""))

        Dim sb As New System.Text.StringBuilder()
        sb.Append("<table class=""mm-grid""><tr><th>ID</th><th>Name</th><th>Type</th><th>Groups</th><th>Staff ID</th><th>Mobile No</th>" &
                  "<th>Email</th><th>CPR</th><th>Status</th><th>Comment</th><th>Net balance</th></tr>")
        For Each m As Member In members
            Dim ed As MemberEdit = Nothing
            Dim failed As Boolean = failedEdits.TryGetValue(m.Id, ed)
            If Not failed Then
                ed = New MemberEdit With {.Name = m.Name, .StaffId = m.StaffId, .Mobile = m.Mobile, .Email = m.Email, .Cpr = m.Cpr,
                                          .Status = m.Status, .Comment = m.Comment, .GroupIds = m.GroupIds}
            End If
            Dim frozen As Boolean = SameName(ed.Status, MemberStatuses.Frozen)
            Dim id As String = enc(m.Id)
            Dim typeText As String = If(m.MemberType = MemberTypes.Person, "Person", m.MemberType)
            Dim net As Decimal = Bal(If(IsPerson(m), PersonFigures(sums, m.Id), FiguresOf(sums, m.Id)))   ' a person: all their accounts

            sb.AppendFormat("<tr class=""mm-row{0}{1}{2}"" data-text=""{3}"">", If(frozen, " frozen", ""), If(failed, " err dirty", ""), "",
                            enc((m.Id & " " & m.Name & " " & typeText).ToLowerInvariant()))
            sb.AppendFormat("<td class=""id""><input type=""hidden"" name=""mm_ids"" value=""{0}"" />{0}</td>", id)
            sb.AppendFormat("<td><input type=""text"" class=""mm-name"" name=""mm_name_{0}"" value=""{1}"" maxlength=""100"" aria-label=""Name"" />{2}</td>",
                            id, enc(ed.Name), If(failed, "<div class=""warn-text"">" & enc(ed.ErrorText) & "</div>", ""))
            sb.AppendFormat("<td class=""mm-type"">{0}</td>", enc(typeText))
            sb.Append("<td class=""mm-groups"">")
            For Each g As MemberGroup In groups
                sb.AppendFormat("<label><input type=""checkbox"" name=""mm_grp_{0}"" value=""{1}""{2} /> {3}</label>",
                                id, g.Id, If(ed.GroupIds IsNot Nothing AndAlso ed.GroupIds.Contains(g.Id), " checked=""checked""", ""), enc(g.Name))
            Next
            sb.Append("</td>")
            sb.AppendFormat("<td><input type=""text"" name=""mm_staff_{0}"" value=""{1}"" maxlength=""50"" aria-label=""Staff ID"" /></td>", id, enc(ed.StaffId))
            sb.AppendFormat("<td><input type=""text"" name=""mm_mobile_{0}"" value=""{1}"" maxlength=""30"" aria-label=""Mobile No"" /></td>", id, enc(ed.Mobile))
            sb.AppendFormat("<td><input type=""text"" name=""mm_email_{0}"" value=""{1}"" maxlength=""100"" aria-label=""Email"" /></td>", id, enc(ed.Email))
            sb.AppendFormat("<td><input type=""text"" name=""mm_cpr_{0}"" value=""{1}"" maxlength=""11"" inputmode=""numeric"" aria-label=""CPR"" /></td>", id, enc(ed.Cpr))
            sb.AppendFormat("<td><select class=""mm-status"" name=""mm_status_{0}"" aria-label=""Status""><option value=""Active""{1}>Active</option>" &
                            "<option value=""Frozen""{2}>Frozen</option></select></td>",
                            id, If(frozen, "", " selected=""selected"""), If(frozen, " selected=""selected""", ""))
            sb.AppendFormat("<td><textarea name=""mm_comment_{0}"" rows=""1"" maxlength=""1000"" aria-label=""Comment"">{1}</textarea></td>", id, enc(ed.Comment))
            sb.AppendFormat("<td class=""num{0}"">{1}</td>", If(net < 0D, " neg", ""), Money(net))
            sb.Append("</tr>")
        Next
        sb.Append("</table>")
        litMembersList.Text = sb.ToString()
    End Sub

    Protected Sub btnMembersSave_Click(sender As Object, e As EventArgs) Handles btnMembersSave.Click
        If Not IsAdmin Then Return
        hfMembersOpen.Value = "1"   ' the panel stays open with the result
        Dim saved As New List(Of String)
        failedEdits.Clear()
        Dim byId As Dictionary(Of String, Member) = LedgerMembers().ToDictionary(Function(m) m.Id, StringComparer.OrdinalIgnoreCase)

        For Each id As String In If(Request.Unvalidated.Form.GetValues("mm_ids"), New String() {})
            Dim m As Member = Nothing
            If Not byId.TryGetValue(id, m) Then Continue For
            Dim ed As MemberEdit = PostedEdit(id)
            Dim status As String = If(SameName(ed.Status, MemberStatuses.Frozen), MemberStatuses.Frozen, MemberStatuses.Active)
            Dim same As Boolean = ed.Name = m.Name AndAlso ed.StaffId = If(m.StaffId, "") AndAlso ed.Mobile = If(m.Mobile, "") AndAlso
                                  ed.Email = If(m.Email, "") AndAlso ed.Cpr = If(m.Cpr, "") AndAlso status = m.Status AndAlso
                                  ed.Comment = If(m.Comment, "").Trim() AndAlso
                                  New HashSet(Of Integer)(ed.GroupIds).SetEquals(m.GroupIds)
            If same Then Continue For
            Try
                Ledger.UpdateMember(id, ed.Name, ed.StaffId, ed.Mobile, ed.Cpr, ed.Email, ed.GroupIds, status, ed.Comment)
                saved.Add(ed.Name)
                If IsPerson(m) Then SyncPersonAccounts(m, ed.Name, ed.GroupIds, status)
            Catch ex As Exception
                ed.ErrorText = ex.Message
                failedEdits(id) = ed
            End Try
        Next
        ResetLedgerCache()
        groupsCache = Nothing

        Dim parts As New List(Of String)
        If saved.Count > 0 Then parts.Add(saved.Count & " member(s) saved")
        If failedEdits.Count > 0 Then parts.Add(failedEdits.Count & " not saved (in red below)")
        lblMembersMsg.Text = Server.HtmlEncode(If(parts.Count = 0, "Nothing was changed.", String.Join("; ", parts) & "."))
        lblMembersMsg.CssClass = "order-msg " & If(failedEdits.Count > 0, "err", "ok")
        lblMembersMsg.Visible = True
        If saved.Count > 0 Then RefreshOrderMembers()
    End Sub

    ''' <summary>
    ''' A person was saved: their accounts follow the new name ("<Person> - <Account>"), groups and status
    ''' (a frozen person's accounts are frozen too).
    ''' </summary>
    Private Sub SyncPersonAccounts(person As Member, newName As String, groupIds As List(Of Integer), status As String)
        For Each a As Member In AccountsOfPerson(person.Id)
            Dim shortName As String = ShortAccountName(a)
            Ledger.UpdateMember(a.Id, newName & AccountNameSep & shortName, a.StaffId, a.Mobile, a.Cpr, a.Email, groupIds, status, a.Comment)
        Next
    End Sub

    ''' <summary>
    ''' After the members list was saved: the Order grid follows (names, groups; frozen members leave, unfrozen ones join),
    ''' keeping everything typed in the current order.
    ''' </summary>
    Private Sub RefreshOrderMembers()
        Dim st As OrderState = State
        If IsViewing Then Return
        Dim fresh As List(Of MemberLine) = LoadMembers()
        For Each line As MemberLine In fresh
            Dim old As MemberLine = st.Members.FirstOrDefault(Function(x) SameName(x.LineKey, line.LineKey))
            If old IsNot Nothing Then
                line.Credit = old.Credit : line.Withdraw = old.Withdraw : line.Qty = old.Qty : line.Splits = old.Splits
            End If
            While line.Qty.Count < st.Items.Count
                line.Qty.Add(0D)
            End While
        Next
        ' Someone frozen who already has something in this order stays, so nothing typed is lost (Process will say they are frozen)
        For Each old As MemberLine In st.Members
            If fresh.Any(Function(x) SameName(x.LineKey, old.LineKey)) Then Continue For
            If old.Credit <> 0D OrElse old.Withdraw <> 0D OrElse old.Qty.Any(Function(q) q <> 0D) Then fresh.Add(old)
        Next
        st.Members = fresh
        If st.PreviewBuilt Then MarkPreviewOutdated()
    End Sub

    ' ---------- Member groups ----------

    Private groupsCache As List(Of MemberGroup)

    Private Function GroupList() As List(Of MemberGroup)
        If groupsCache Is Nothing Then
            Try
                groupsCache = Ledger.Store.GetGroups()
            Catch
                groupsCache = New List(Of MemberGroup)
            End Try
        End If
        Return groupsCache
    End Function

    ''' <summary>
    ''' The Order grid's rows for the group picked (all of them for "All members"). The shop's goods lines,
    ''' and anyone with something typed in this order, always stay, so nothing typed is hidden.
    ''' </summary>
    ''' <summary>
    ''' The lines shown on the grid. A shop's accounts show only when the order has a Profits column, and then
    ''' only the picked shop's (whatever the group filter). Anyone with something typed always stays.
    ''' </summary>
    Private Function VisibleMembers(st As OrderState) As List(Of MemberLine)
        Dim shownShop As String = ShownShopId(st)
        Dim groupOn As Boolean = st.GroupFilter <> 0 AndAlso GroupList().Any(Function(g) g.Id = st.GroupFilter)
        Return st.Members.Where(Function(m)
                                    If HasTyped(m) OrElse m.IsShopLine Then Return True
                                    If m.OwnerId <> "" AndAlso IsShopCustomer(m.OwnerId) Then Return SameName(m.OwnerId, shownShop)
                                    Return Not groupOn OrElse m.GroupIds.Contains(st.GroupFilter)
                                End Function).ToList()
    End Function

    ' ---------- Shops on the Order grid ----------

    ''' <summary>The active shops (Customer Type Shop), by name.</summary>
    Private Function ShopCustomers() As List(Of Member)
        Return LedgerMembers().Where(Function(m) m.IsActive AndAlso IsShopCustomer(m.Id)).OrderBy(Function(m) m.Name).ToList()
    End Function

    ''' <summary>
    ''' The shop whose accounts the grid lists: "" without a Profits column; else the one picked, or at first
    ''' the shop with the most movements (its own and its accounts' Member_Movements).
    ''' </summary>
    Private Function ShownShopId(st As OrderState) As String
        If ProfitsIndex(st) < 0 Then Return ""
        Dim shops As List(Of Member) = ShopCustomers()
        If shops.Count = 0 Then Return ""
        If shops.Any(Function(s) SameName(s.Id, st.ShopFilter)) Then Return st.ShopFilter
        Dim moves As New Dictionary(Of String, Double)(StringComparer.OrdinalIgnoreCase)
        For Each r As DataRow In OrdersDB.GetDataTable("SELECT Member_ID, Member_Movements FROM Members").Rows
            moves(r("Member_ID").ToString()) = Val(ColText(r, "Member_Movements"))
        Next
        Dim score As Func(Of Member, Double) =
            Function(s) If(moves.ContainsKey(s.Id), moves(s.Id), 0) +
                        AccountsOfPerson(s.Id).Sum(Function(a) If(moves.ContainsKey(a.Id), moves(a.Id), 0))
        st.ShopFilter = shops.OrderByDescending(score).ThenBy(Function(s) s.Name).First().Id
        Return st.ShopFilter
    End Function

    ''' <summary>Add New Member: the groups as checkboxes, keeping the ticks already made.</summary>
    Private Sub FillGroupChecks(Optional tick As Integer = 0)
        Dim ticked As New HashSet(Of String)(cblNewMemberGroups.Items.Cast(Of ListItem)().Where(Function(i) i.Selected).Select(Function(i) i.Value))
        If tick <> 0 Then ticked.Add(tick.ToString(CultureInfo.InvariantCulture))
        cblNewMemberGroups.Items.Clear()
        For Each g As MemberGroup In GroupList()
            Dim it As New ListItem(g.Name, g.Id.ToString(CultureInfo.InvariantCulture))
            it.Selected = ticked.Contains(it.Value)
            cblNewMemberGroups.Items.Add(it)
        Next
    End Sub

    Protected Sub btnNewGroupAdd_Click(sender As Object, e As EventArgs) Handles btnNewGroupAdd.Click
        If Not IsAdmin Then Return
        lblAddMemberErr.Visible = False
        Try
            Dim g As MemberGroup = Ledger.CreateGroup(txtNewGroupName.Text)
            groupsCache = Nothing
            FillGroupChecks(g.Id)   ' the new group is ticked
            txtNewGroupName.Text = ""
        Catch ex As Exception
            AddMemberError(ex.Message)
        End Try
        pnlAddMember.Visible = True
    End Sub

    Protected Sub rptMembers_ItemDataBound(sender As Object, e As RepeaterItemEventArgs) Handles rptMembers.ItemDataBound
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then Exit Sub
        Dim m As MemberLine = CType(e.Item.DataItem, MemberLine)
        Dim viewing As Boolean = IsViewing

        ' The member's place in the whole list (the grid may show only one group)
        CType(e.Item.FindControl("hfIdx"), HiddenField).Value = State.Members.IndexOf(m).ToString(CultureInfo.InvariantCulture)
        CType(e.Item.FindControl("litExBalance"), Literal).Text = If(m.IsShopLine OrElse (viewing AndAlso Not m.InViewedOrder), "", Money(m.ExBalance))
        BindOwnerCells(e.Item, m)

        Dim txtCredit As TextBox = CType(e.Item.FindControl("txtCredit"), TextBox)
        Dim txtWithdraw As TextBox = CType(e.Item.FindControl("txtWithdraw"), TextBox)
        txtCredit.Text = BlankMoney(m.Credit)
        txtWithdraw.Text = BlankMoney(m.Withdraw)
        txtCredit.Attributes("data-calc") = ""
        txtCredit.Attributes("data-f") = "c"      ' the person's header row adds these up
        txtWithdraw.Attributes("data-calc") = ""
        txtWithdraw.Attributes("data-kind") = "w"
        txtCredit.ReadOnly = viewing OrElse m.IsShopLine   ' the shop's goods lines: items only
        txtWithdraw.ReadOnly = viewing OrElse m.IsShopLine

        Dim inner As Repeater = CType(e.Item.FindControl("rptQty"), Repeater)
        AddHandler inner.ItemDataBound,
            Sub(s As Object, ie As RepeaterItemEventArgs)
                If ie.Item.ItemType <> ListItemType.Item AndAlso ie.Item.ItemType <> ListItemType.AlternatingItem Then Exit Sub
                Dim t As TextBox = CType(ie.Item.FindControl("txtQty"), TextBox)
                Dim q As Decimal = CDec(ie.Item.DataItem)
                t.Text = If(q = 0D, "", Qty(q))
                t.Attributes("data-col") = ie.Item.ItemIndex.ToString()
                t.Attributes("data-calc") = ""
                t.ReadOnly = viewing

                ' The Profits item is calculated (also live in the browser), so it cannot be typed
                Dim cols As List(Of ItemCol) = State.Items
                If ie.Item.ItemIndex < cols.Count AndAlso cols(ie.Item.ItemIndex).IsProfits Then
                    t.ReadOnly = True
                    t.CssClass = "cell-in profits"
                    t.Text = If(q = 0D, "", Money(q))
                    t.Attributes("data-profits") = "1"
                End If
            End Sub
        inner.DataSource = m.Qty
        inner.DataBind()
    End Sub

    ' ---------- People and their accounts on the Order grid ----------
    ' A person with several accounts: a header row ([+] / [-], their totals) and an indented row per account,
    ' hidden until [+] is clicked (open when something is typed on them). A person with one account: one row
    ' with the person's name. Items, Credit and Withdraw are typed on the account rows: each is its own member.

    ''' <summary>Owner ID -> their account lines shown on the grid now (set in BindOrderTab, before binding).</summary>
    Private gridOwners As New Dictionary(Of String, List(Of MemberLine))(StringComparer.OrdinalIgnoreCase)
    Private gridOpenOwners As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>The people whose accounts are open (hfAccOpen: person IDs separated by "|").</summary>
    Private Function OpenAccountMembers() As HashSet(Of String)
        Return New HashSet(Of String)(If(hfAccOpen.Value, "").Split(New Char() {"|"c}, StringSplitOptions.RemoveEmptyEntries),
                                      StringComparer.OrdinalIgnoreCase)
    End Function

    Private Shared Function HasTyped(m As MemberLine) As Boolean
        Return m.Credit <> 0D OrElse m.Withdraw <> 0D OrElse m.Qty.Any(Function(q) q <> 0D)
    End Function

    ''' <summary>Groups the visible lines by person; people with something typed on an account are opened.</summary>
    Private Sub PrepareGridOwners(visible As List(Of MemberLine))
        gridOwners = New Dictionary(Of String, List(Of MemberLine))(StringComparer.OrdinalIgnoreCase)
        For Each m As MemberLine In visible.Where(Function(x) x.OwnerId <> "")
            If Not gridOwners.ContainsKey(m.OwnerId) Then gridOwners(m.OwnerId) = New List(Of MemberLine)
            gridOwners(m.OwnerId).Add(m)
        Next
        gridOpenOwners = OpenAccountMembers()
        For Each kv As KeyValuePair(Of String, List(Of MemberLine)) In gridOwners
            If kv.Value.Any(Function(x) HasTyped(x)) Then gridOpenOwners.Add(kv.Key)
        Next
        hfAccOpen.Value = String.Join("|", gridOpenOwners)
    End Sub

    Private Function IsUnderHeader(m As MemberLine) As Boolean
        Return m.OwnerId <> "" AndAlso gridOwners.ContainsKey(m.OwnerId) AndAlso gridOwners(m.OwnerId).Count > 1
    End Function

    ''' <summary>Used by the markup: the attributes of a grid row (account under a header, loan colour).</summary>
    Protected Function RowAttrs(dataItem As Object) As String
        Dim m As MemberLine = TryCast(dataItem, MemberLine)
        If m Is Nothing Then Return ""
        Dim classes As New List(Of String)
        If IsUnderHeader(m) Then classes.Add("acc-sub")
        If m.AccountType = AccTypeLoan Then classes.Add("loan")
        Dim sb As New System.Text.StringBuilder()
        If classes.Count > 0 Then sb.AppendFormat(" class=""{0}""", String.Join(" ", classes))
        If IsUnderHeader(m) Then
            sb.AppendFormat(" data-acc-of=""{0}""", Server.HtmlEncode(m.OwnerId))
            If Not gridOpenOwners.Contains(m.OwnerId) Then sb.Append(" style=""display:none""")
        End If
        Return sb.ToString()
    End Function

    ''' <summary>The name cell of a grid row, and the person's header row above their first account.</summary>
    Private Sub BindOwnerCells(item As RepeaterItem, m As MemberLine)
        Dim litName As Literal = CType(item.FindControl("litMemName"), Literal)
        Dim litHead As Literal = CType(item.FindControl("litOwnerHead"), Literal)
        litHead.Text = ""
        Const gap As String = "<span class=""acc-tgl-gap""></span>"

        If Not IsUnderHeader(m) Then
            If m.OwnerId <> "" Then
                ' The person's only account: the person's name (the account shows when pointed at)
                litName.Text = gap & "<span title=""" & Server.HtmlEncode(m.Name) & """>" & Server.HtmlEncode(m.OwnerName) & "</span>"
            Else
                litName.Text = gap & Server.HtmlEncode(m.Name)
            End If
            Return
        End If

        litName.Text = "<span title=""" & Server.HtmlEncode(m.Name) & """>" & Server.HtmlEncode(m.ShortName) & "</span>"
        Dim lines As List(Of MemberLine) = gridOwners(m.OwnerId)
        If lines(0) IsNot m Then Return

        ' The header row: [+] / [-], the person's name, and the totals of their accounts
        Dim st As OrderState = State
        Dim isOpen As Boolean = gridOpenOwners.Contains(m.OwnerId)
        Dim ownerAttr As String = Server.HtmlEncode(m.OwnerId)
        Dim viewing As Boolean = IsViewing
        Dim sb As New System.Text.StringBuilder()
        sb.AppendFormat("<tr class=""acc-head"" data-owner=""{0}"">", ownerAttr)
        sb.AppendFormat("<td class=""mem""><button type=""button"" class=""acc-tgl"" data-id=""{0}"" title=""{1}"" onclick=""return edToggleAcc(this);"">{2}</button>{3}</td>",
                        ownerAttr, If(isOpen, "Hide the accounts", "Show the accounts"), If(isOpen, "&#8722;", "+"), Server.HtmlEncode(m.OwnerName))
        Dim inOrder As List(Of MemberLine) = lines.Where(Function(x) Not viewing OrElse x.InViewedOrder).ToList()
        sb.AppendFormat("<td class=""ro"">{0}</td>", If(viewing AndAlso inOrder.Count = 0, "", Money(inOrder.Sum(Function(x) x.ExBalance))))
        sb.AppendFormat("<td class=""ro"" data-sum=""c"">{0}</td>", BlankMoney(lines.Sum(Function(x) x.Credit)))
        sb.AppendFormat("<td class=""ro"" data-sum=""w"">{0}</td>", BlankMoney(lines.Sum(Function(x) x.Withdraw)))
        For c As Integer = 0 To st.Items.Count - 1
            Dim col As Integer = c
            Dim q As Decimal = lines.Sum(Function(x) If(col < x.Qty.Count, x.Qty(col), 0D))
            sb.AppendFormat("<td class=""ro"" data-sum=""q{0}"">{1}</td>", c, If(q = 0D, "", If(st.Items(c).IsProfits, Money(q), Qty(q))))
        Next
        sb.Append("</tr>")
        litHead.Text = sb.ToString()
    End Sub

    Protected Sub rptPreview_ItemDataBound(sender As Object, e As RepeaterItemEventArgs) Handles rptPreview.ItemDataBound
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then Exit Sub
        Dim p As PreviewLine = CType(e.Item.DataItem, PreviewLine)
        CType(e.Item.FindControl("hfIdx"), HiddenField).Value = e.Item.ItemIndex.ToString()
        ' A shop order: the second grid starts at its first row (same columns, under the first)
        Dim litSection As Literal = CType(e.Item.FindControl("litSection"), Literal)
        Dim prevRow As PreviewLine = If(e.Item.ItemIndex > 0, State.Preview(e.Item.ItemIndex - 1), Nothing)
        If litSection IsNot Nothing AndAlso prevRow IsNot Nothing AndAlso p.Section > prevRow.Section Then
            litSection.Text = "</table><div class=""pv-sec"">" &
                {"", "After the purchase: profit, stock and members (from the new balances above)",
                 "Not sold: from stock to loss (from the new balances above)",
                 "Zeroing: profit and loss to the budget (from the new balances above)"}(p.Section) & "</div>" &
                "<table class=""pv-grid""><tr><th>Member</th><th>exBalance</th><th>exCash</th><th>exExpense</th><th>exReceivable</th><th>exPayable</th>" &
                "<th>Credit</th><th>Debt</th><th>OrderItems</th><th>Total</th><th>Account</th><th>Transaction Type</th><th>Paid From</th>" &
                "<th>Next Balance</th><th>Cash</th><th>Expense</th><th>Receivable</th><th>Payable</th></tr>"
        End If

        Dim ddlAccount As DropDownList = CType(e.Item.FindControl("ddlAccount"), DropDownList)
        Dim ddlType As DropDownList = CType(e.Item.FindControl("ddlType"), DropDownList)
        Dim ddlPaidFrom As DropDownList = CType(e.Item.FindControl("ddlPaidFrom"), DropDownList)
        Dim lnkSplit As LinkButton = CType(e.Item.FindControl("lnkSplit"), LinkButton)
        Dim litSplit As Literal = CType(e.Item.FindControl("litSplit"), Literal)
        litSplit.Text = ""
        If p.AutoRow Then
            ' A member the order changes without a line of their own (payer, the shop): figures only
            ddlAccount.Visible = False : ddlType.Visible = False : ddlPaidFrom.Visible = False
            lnkSplit.Visible = False
            Return
        End If
        Dim accountChoices As List(Of String) = MemberNames(State)
        Dim shopOrder As ShopOrderAccounts = ShopOrderOf(State)
        If shopOrder IsNot Nothing AndAlso Not IsLineOfShop(shopOrder, p.MemberId) Then accountChoices.Insert(0, OnAccountName(shopOrder))
        FillDropDown(ddlAccount, accountChoices, p.Account)
        FillDropDown(ddlType, TransactionTypes, p.TxType)
        ddlPaidFrom.Items.Clear()
        For Each fundName As String In p.PaidFromOptions
            ddlPaidFrom.Items.Add(New ListItem(fundName, fundName))
        Next
        If ddlPaidFrom.Items.Count = 0 Then ddlPaidFrom.Items.Add(New ListItem("", ""))
        Dim it As ListItem = ddlPaidFrom.Items.FindByValue(If(p.PaidFrom, ""))
        If it IsNot Nothing Then ddlPaidFrom.SelectedValue = it.Value

        Dim editable As Boolean = Not IsViewing
        ddlAccount.Enabled = editable AndAlso Not p.AccountLocked
        ddlType.Enabled = editable AndAlso Not p.TypeLocked
        ddlPaidFrom.Enabled = editable AndAlso p.PaidFromOptions.Count > 0

        ' Split payment: the round + (an Expense row of a member); when split, the payers instead of the dropdowns
        Dim mline As MemberLine = State.Members.FirstOrDefault(Function(x) SameName(x.LineKey, p.LineKey))
        Dim canSplit As Boolean = editable AndAlso IsAdmin AndAlso mline IsNot Nothing AndAlso Not mline.IsShopLine AndAlso p.TxType = TxNormal AndAlso Not p.AccountLocked
        lnkSplit.Visible = canSplit
        If canSplit AndAlso mline.Splits.Count > 0 Then
            ddlAccount.Visible = False
            ddlPaidFrom.Visible = False
            lnkSplit.CssClass = "pv-split on"
            lnkSplit.ToolTip = "Change the split payment"
            litSplit.Text = "<span class=""pv-parts"">" &
                String.Join("<br />", mline.Splits.Select(Function(x) "<b>" & Server.HtmlEncode(x.Account) & "</b> &middot; " &
                                                                   Server.HtmlEncode(x.PaidFrom) & " &middot; " & Money(x.Amount))) & "</span>"
        End If
    End Sub

    ' ---------- Split payment dialog ----------

    ''' <summary>The Paid From choices of a payer: their funds, then their loans / cards.</summary>
    Private Function PayerFundOptions(payerName As String) As List(Of String)
        Dim options As New List(Of String)
        Dim payerId As String = IdByName(payerName)
        If String.IsNullOrEmpty(payerId) Then Return options
        options.AddRange(FundsOf(payerId).Select(Function(a) a.Name))
        options.AddRange(LoansOf(payerId).Select(Function(a) a.Name & LoanSuffix))
        Return options
    End Function

    Protected Sub rptPreview_ItemCommand(source As Object, e As RepeaterCommandEventArgs) Handles rptPreview.ItemCommand
        If e.CommandName <> "Split" OrElse IsViewing OrElse Not IsAdmin Then Return
        Dim st As OrderState = State
        Dim key As String = Convert.ToString(e.CommandArgument)
        Dim m As MemberLine = st.Members.FirstOrDefault(Function(x) SameName(x.LineKey, key))
        Dim p As PreviewLine = st.Preview.FirstOrDefault(Function(x) Not x.AutoRow AndAlso SameName(x.LineKey, key))
        If m Is Nothing OrElse p Is Nothing OrElse m.IsShopLine Then Return
        st.SplitKey = key
        If m.Splits.Count > 0 Then
            st.SplitDraft = m.Splits.Select(Function(x) New SplitPart With {.Account = x.Account, .PaidFrom = x.PaidFrom, .Amount = x.Amount}).ToList()
        Else
            ' Start from the row's payer paying everything, and an empty row for the next payer
            st.SplitDraft = New List(Of SplitPart) From {
                New SplitPart With {.Account = p.Account, .PaidFrom = p.PaidFrom, .Amount = If(p.Total, 0D)},
                New SplitPart()}
        End If
        lblSplitErr.Visible = False
        BindSplitDialog()
        pnlSplit.Visible = True
    End Sub

    Private Function SplitDue() As Decimal
        Dim st As OrderState = State
        Dim p As PreviewLine = st.Preview.FirstOrDefault(Function(x) Not x.AutoRow AndAlso SameName(x.LineKey, st.SplitKey))
        Return If(p Is Nothing, 0D, If(p.Total, 0D))
    End Function

    Private Sub BindSplitDialog()
        Dim st As OrderState = State
        Dim p As PreviewLine = st.Preview.FirstOrDefault(Function(x) Not x.AutoRow AndAlso SameName(x.LineKey, st.SplitKey))
        lblSplitFor.Text = Server.HtmlEncode(If(p Is Nothing, "", p.Name))
        Dim due As String = Money(SplitDue())
        litSplitDue.Text = "<b id=""splitDue"" data-due=""" & due & """>" & due & "</b>"
        rptSplit.DataSource = st.SplitDraft
        rptSplit.DataBind()
    End Sub

    Protected Sub rptSplit_ItemDataBound(sender As Object, e As RepeaterItemEventArgs) Handles rptSplit.ItemDataBound
        If e.Item.ItemType <> ListItemType.Item AndAlso e.Item.ItemType <> ListItemType.AlternatingItem Then Exit Sub
        Dim part As SplitPart = CType(e.Item.DataItem, SplitPart)
        FillDropDown(CType(e.Item.FindControl("ddlSplitAccount"), DropDownList), MemberNames(State), part.Account)
        Dim ddlFund As DropDownList = CType(e.Item.FindControl("ddlSplitFund"), DropDownList)
        Dim options As List(Of String) = PayerFundOptions(part.Account)
        ddlFund.Items.Clear()
        For Each f As String In options
            ddlFund.Items.Add(New ListItem(f, f))
        Next
        If ddlFund.Items.Count = 0 Then ddlFund.Items.Add(New ListItem("", ""))
        If Not options.Contains(If(part.PaidFrom, "")) Then part.PaidFrom = If(options.Count > 0, options(0), "")
        ddlFund.SelectedValue = If(ddlFund.Items.FindByValue(part.PaidFrom) IsNot Nothing, part.PaidFrom, ddlFund.Items(0).Value)
        CType(e.Item.FindControl("txtSplitAmount"), TextBox).Text = If(part.Amount = 0D, "", Money(part.Amount))
    End Sub

    ''' <summary>Reads the dialog's rows back. False when an amount is not a number of 0 or more.</summary>
    Private Function SyncSplitDraft() As Boolean
        Dim st As OrderState = State
        Dim ok As Boolean = True
        Dim parts As New List(Of SplitPart)
        For Each ri As RepeaterItem In rptSplit.Items
            Dim amount As Decimal
            If Not TryAmount(CType(ri.FindControl("txtSplitAmount"), TextBox).Text, False, amount) Then ok = False
            parts.Add(New SplitPart With {
                .Account = CType(ri.FindControl("ddlSplitAccount"), DropDownList).SelectedValue,
                .PaidFrom = CType(ri.FindControl("ddlSplitFund"), DropDownList).SelectedValue,
                .Amount = amount})
        Next
        st.SplitDraft = parts
        Return ok
    End Function

    Protected Sub SplitAccount_Changed(sender As Object, e As EventArgs)
        SyncSplitDraft()
        BindSplitDialog()   ' another payer: their own Paid From list
        pnlSplit.Visible = True
    End Sub

    Protected Sub rptSplit_ItemCommand(source As Object, e As RepeaterCommandEventArgs) Handles rptSplit.ItemCommand
        If e.CommandName <> "Remove" Then Return
        SyncSplitDraft()
        Dim idx As Integer = CInt(e.CommandArgument)
        If idx >= 0 AndAlso idx < State.SplitDraft.Count Then State.SplitDraft.RemoveAt(idx)
        If State.SplitDraft.Count = 0 Then State.SplitDraft.Add(New SplitPart())
        BindSplitDialog()
        pnlSplit.Visible = True
    End Sub

    Protected Sub lnkSplitAdd_Click(sender As Object, e As EventArgs) Handles lnkSplitAdd.Click
        SyncSplitDraft()
        State.SplitDraft.Add(New SplitPart())
        BindSplitDialog()
        pnlSplit.Visible = True
    End Sub

    Private Sub SplitError(text As String)
        lblSplitErr.Text = Server.HtmlEncode(text)
        lblSplitErr.Visible = True
        BindSplitDialog()
        pnlSplit.Visible = True
    End Sub

    Protected Sub btnSplitSave_Click(sender As Object, e As EventArgs) Handles btnSplitSave.Click
        If Not IsAdmin OrElse IsViewing Then Return
        Dim st As OrderState = State
        Dim m As MemberLine = st.Members.FirstOrDefault(Function(x) SameName(x.LineKey, st.SplitKey))
        Dim p As PreviewLine = st.Preview.FirstOrDefault(Function(x) Not x.AutoRow AndAlso SameName(x.LineKey, st.SplitKey))
        If m Is Nothing OrElse p Is Nothing Then
            pnlSplit.Visible = False
            Return
        End If
        If Not SyncSplitDraft() Then
            SplitError("Each amount should be a number of 0 or more, e.g. 2 or 1.500.")
            Return
        End If
        ' Rows left completely empty are ignored
        Dim parts As List(Of SplitPart) = st.SplitDraft.Where(Function(x) x.Amount <> 0D OrElse x.Account <> "").ToList()
        If parts.Count = 0 Then
            SplitError("Please enter at least one payer and amount.")
            Return
        End If
        For Each x As SplitPart In parts
            If x.Account = "" OrElse x.PaidFrom = "" Then
                SplitError("Every row needs an Account and its Paid From.")
                Return
            End If
            If x.Amount <= 0D Then
                SplitError(x.Account & ": please enter the amount it pays (above 0).")
                Return
            End If
        Next
        If parts.GroupBy(Function(x) x.Account & "|" & x.PaidFrom, StringComparer.OrdinalIgnoreCase).Any(Function(g) g.Count() > 1) Then
            SplitError("The same account and Paid From appear twice: put their amounts on one row.")
            Return
        End If
        Dim due As Decimal = SplitDue()
        Dim given As Decimal = parts.Sum(Function(x) x.Amount)
        If Math.Abs(given - due) > 0.0005D Then
            SplitError("The amounts add up to " & Money(given) & " but the line's total is " & Money(due) & " (" &
                       If(given < due, Money(due - given) & " still to give", Money(given - due) & " too much") & ").")
            Return
        End If

        If parts.Count = 1 Then
            ' One payer: no split, just that payer on the row
            m.Splits.Clear()
            p.Account = parts(0).Account
            RefreshPaidFrom(p)
            p.PaidFrom = parts(0).PaidFrom
        Else
            m.Splits = parts
        End If
        pnlSplit.Visible = False
        st.SplitKey = ""
        RecalculatePreview()
    End Sub

    Protected Sub btnSplitClear_Click(sender As Object, e As EventArgs) Handles btnSplitClear.Click
        Dim st As OrderState = State
        Dim m As MemberLine = st.Members.FirstOrDefault(Function(x) SameName(x.LineKey, st.SplitKey))
        If m IsNot Nothing Then m.Splits.Clear()
        pnlSplit.Visible = False
        st.SplitKey = ""
        RecalculatePreview()
    End Sub

    Protected Sub btnSplitCancel_Click(sender As Object, e As EventArgs) Handles btnSplitCancel.Click
        pnlSplit.Visible = False
        State.SplitKey = ""
    End Sub

    Private Sub FillDropDown(ddl As DropDownList, values As IEnumerable(Of String), selected As String)
        ddl.Items.Clear()
        ddl.Items.Add(New ListItem("", ""))
        For Each v As String In values
            ddl.Items.Add(New ListItem(v, v))
        Next
        Dim it As ListItem = ddl.Items.FindByValue(If(selected, ""))
        If it IsNot Nothing Then ddl.SelectedValue = it.Value
    End Sub

    ' ---------- helpers used by the markup ----------

    Protected Function M(value As Object) As String
        If value Is Nothing OrElse IsDBNull(value) Then Return ""
        Return Money(CDec(value))
    End Function

    Protected Function NegCss(value As Object) As String
        If value Is Nothing OrElse IsDBNull(value) Then Return ""
        Return If(CDec(value) < 0D, "neg", "")
    End Function

    Protected Function HtmlLines(value As Object) As String
        Dim t As String = Convert.ToString(value)
        Return String.Join("<br />", t.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries).Select(Function(l) Server.HtmlEncode(l)))
    End Function

    ' ====================================================================
    '  BUTTONS
    ' ====================================================================

    Protected Sub lnkSubOrder_Click(sender As Object, e As EventArgs) Handles lnkSubOrder.Click
        mvOrder.ActiveViewIndex = 0
    End Sub

    Protected Sub lnkSubPreview_Click(sender As Object, e As EventArgs) Handles lnkSubPreview.Click
        mvOrder.ActiveViewIndex = 1
    End Sub

    ' ---------- Add New Member (saved to the Members table) ----------

    ' ---------- Add New Member: Customer Type Shop ----------
    ' A shop is saved like a person (a name that groups its accounts), marked Account_Type = "Shop" in Members,
    ' with exactly five accounts of type Shop: <Shop>_Budget, _Cash, _Loss, _Profit, _Stock. No others can be
    ' added or deleted. (Not the ledger's own Shop member type: that one drives the Shop_Stock / Shop_Loss
    ' goods lines and Shop Purchase / Sale.)

    Private Const CustomerTypeShop As String = "Shop"
    Private Const ShopsGroupName As String = "Shops"   ' every shop belongs to this group (made when missing)

    ''' <summary>The ID of the group "Shops", created when it does not exist yet.</summary>
    Private Function ShopsGroupId() As Integer
        Dim g As MemberGroup = GroupList().FirstOrDefault(Function(x) SameName(x.Name, ShopsGroupName))
        If g Is Nothing Then
            g = Ledger.CreateGroup(ShopsGroupName)
            groupsCache = Nothing
        End If
        Return g.Id
    End Function

    ''' <summary>
    ''' Puts every shop (and its accounts) in the group "Shops", keeping any other groups it has.
    ''' Run on the administrator's first page load, so shops made before this rule are fixed too.
    ''' </summary>
    Private Sub EnsureShopsGroup()
        Dim shops As List(Of Member) = LedgerMembers().Where(Function(m) m.IsActive AndAlso IsShopCustomer(m.Id)).ToList()
        If shops.Count = 0 Then Return
        Dim gid As Integer = ShopsGroupId()
        Dim changed As Boolean = False
        For Each s As Member In shops
            For Each m As Member In {s}.Concat(AccountsOfPerson(s.Id))
                If m.GroupIds IsNot Nothing AndAlso m.GroupIds.Contains(gid) Then Continue For
                Dim groups As New List(Of Integer)(If(m.GroupIds, New List(Of Integer)))
                groups.Add(gid)
                Ledger.UpdateMember(m.Id, m.Name, m.StaffId, m.Mobile, m.Cpr, m.Email, groups, m.Status, m.Comment)
                changed = True
            Next
        Next
        If changed Then ResetLedgerCache()
    End Sub
    Private Shared ReadOnly ShopAccountSuffixes As String() = {"_Budget", "_Cash", "_Loss", "_Profit", "_Stock"}

    Private Sub AddShop(shopName As String)
        Dim shop As Member
        Try
            shop = Ledger.CreateMember(shopName, MemberTypes.Person, groupIds:=New List(Of Integer) From {ShopsGroupId()}, status:=MemberStatuses.Active)
            Using cn As System.Data.OleDb.OleDbConnection = OpenOrdersDb()
                Using cmd As New System.Data.OleDb.OleDbCommand("UPDATE Members SET " & AccountTypeColumn & " = ? WHERE Member_ID = ?", cn)
                    cmd.Parameters.Add("@type", System.Data.OleDb.OleDbType.VarWChar, 20).Value = CustomerTypeShop
                    cmd.Parameters.Add("@id", System.Data.OleDb.OleDbType.VarWChar, 255).Value = shop.Id
                    cmd.ExecuteNonQuery()
                End Using
            End Using
            ResetLedgerCache()
        Catch ex As Exception
            ResetLedgerCache()
            AddMemberError(ex.Message)   ' the dialog stays open with what was typed
            Return
        End Try
        Dim made As New List(Of String)
        Try
            For Each suffix As String In ShopAccountSuffixes
                CreateAccountMember(shop, shopName & suffix, CustomerTypeShop)
                made.Add(shopName & suffix)
            Next
        Catch ex As Exception
            ResetLedgerCache()
            pnlAddMember.Visible = False
            ShowMsg("Shop """ & shopName & """ was added with " & made.Count & " of its 5 accounts: " & ex.Message, True)
            RefreshOrderMembers()
            Return
        End Try
        ResetLedgerCache()
        RefreshOrderMembers()   ' its accounts are on the Order grid straight away
        pnlAddMember.Visible = False
        ShowMsg("Shop """ & shopName & """ added as member " & shop.Id & " with its accounts " & String.Join(", ", made) & ".", False)
    End Sub

    ''' <summary>Is this member a shop customer (Customer Type Shop)?</summary>
    Private Function IsShopCustomer(memberId As String) As Boolean
        AccountInfos()
        Return shopCustomerCache IsNot Nothing AndAlso shopCustomerCache.Contains(If(memberId, ""))
    End Function

    Protected Sub btnAddMember_Click(sender As Object, e As EventArgs) Handles btnAddMember.Click
        ddlNewMemberKind.SelectedValue = "Individual"
        txtNewMemberName.Text = ""
        txtNewMemberStaffId.Text = ""
        txtNewMemberMobile.Text = ""
        txtNewMemberCpr.Text = ""
        txtNewMemberEmail.Text = ""
        txtNewMemberBalance.Text = Money(0D)
        txtNewGroupName.Text = ""
        txtNewMemberComment.Text = ""
        ddlNewMemberStatus.SelectedValue = MemberStatuses.Active
        lblAddMemberErr.Visible = False
        cblNewMemberGroups.Items.Clear()
        FillGroupChecks(State.GroupFilter)   ' the group shown on the grid is ticked to start with
        pnlAddMember.Visible = True
        txtNewMemberName.Focus()
    End Sub

    Protected Sub btnAddMemberCancel_Click(sender As Object, e As EventArgs) Handles btnAddMemberCancel.Click
        pnlAddMember.Visible = False
    End Sub

    Protected Sub btnAddMemberOk_Click(sender As Object, e As EventArgs) Handles btnAddMemberOk.Click
        If Not IsAdmin Then Return   ' administrator only
        Dim memberName As String = txtNewMemberName.Text.Trim()
        Dim balance As Decimal = Math.Round(ParseNumber(txtNewMemberBalance.Text), 3)
        lblAddMemberErr.Visible = False
        If ddlNewMemberKind.SelectedValue = CustomerTypeShop Then
            ' A shop: only its name; five accounts of its own
            If memberName = "" Then
                AddMemberError("Please enter the shop name.")
                Return
            End If
            AddShop(String.Join(" ", memberName.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)))
            Return
        End If
        If memberName = "" Then
            AddMemberError("Please enter a member name.")
            Return
        End If
        Dim groupIds As List(Of Integer) = cblNewMemberGroups.Items.Cast(Of ListItem)().Where(Function(i) i.Selected).
                                               Select(Function(i) Integer.Parse(i.Value, CultureInfo.InvariantCulture)).ToList()
        If groupIds.Count = 0 Then
            AddMemberError("Please tick at least one group.")
            Return
        End If

        ' A person (Staff ID = Windows login, Mobile No) and their "Cash" account (a member of its own, see
        ' PERSONS AND ACCOUNTS); the opening balance goes on the Cash account: positive = cash in hand, negative = owed
        Dim m As Member
        Try
            m = Ledger.CreateMember(memberName, MemberTypes.Person, txtNewMemberStaffId.Text, txtNewMemberMobile.Text,
                                    txtNewMemberCpr.Text, txtNewMemberEmail.Text, groupIds,
                                    status:=ddlNewMemberStatus.SelectedValue, comment:=txtNewMemberComment.Text)
        Catch ex As Exception
            AddMemberError(ex.Message)   ' the dialog stays open with what was typed
            Return
        End Try
        Dim cash As Member
        Try
            cash = CreateAccountMember(m, DefaultAccountName, AccTypeCapital, balance)
        Catch ex As Exception
            ResetLedgerCache()
            pnlAddMember.Visible = False
            ShowMsg("""" & m.Name & """ was added, but their Cash account was not: " & ex.Message &
                    " (it is made the next time the page is opened).", True)
            Return
        End Try
        ResetLedgerCache()
        If m.IsFrozen Then
            ' Frozen from the start: saved, but not on the Order grid
            pnlAddMember.Visible = False
            ShowMsg("""" & m.Name & """ added as member " & m.Id & " (frozen: not shown on the Order grid).", False)
            Return
        End If

        Dim mline As New MemberLine With {.Id = cash.Id, .LineKey = cash.Id, .Name = cash.Name, .ShortName = DefaultAccountName,
                                          .OwnerId = m.Id, .OwnerName = m.Name, .AccountType = AccTypeCapital,
                                          .ExBalance = balance, .GroupIds = groupIds,
                                          .ExCash = balance, .ExPayable = 0D}   ' the opening balance is the money it holds (negative = owed)
        For i As Integer = 1 To State.Items.Count
            mline.Qty.Add(0D)
        Next
        Dim at As Integer = State.Members.FindIndex(Function(x) x.IsShopLine)   ' above the shop's goods lines
        If at < 0 Then State.Members.Add(mline) Else State.Members.Insert(at, mline)
        MarkPreviewOutdated()

        pnlAddMember.Visible = False
        ShowMsg("""" & m.Name & """ added as member " & m.Id & " with a ""Cash"" account.", False)
    End Sub

    Private Sub AddMemberError(text As String)
        lblAddMemberErr.Text = Server.HtmlEncode(text)
        lblAddMemberErr.Visible = True
        pnlAddMember.Visible = True
    End Sub

    ' ---------- Add New Item (adds a column) ----------

    Protected Sub btnAddItem_Click(sender As Object, e As EventArgs) Handles btnAddItem.Click
        txtNewItemTitle.Text = ""
        txtNewItemPrice.Text = Money(0D)
        pnlAddItem.Visible = True
        txtNewItemTitle.Focus()
    End Sub

    Protected Sub btnAddItemCancel_Click(sender As Object, e As EventArgs) Handles btnAddItemCancel.Click
        pnlAddItem.Visible = False
    End Sub

    Protected Sub btnAddItemOk_Click(sender As Object, e As EventArgs) Handles btnAddItemOk.Click
        If Not IsAdmin Then Return   ' administrator only
        Dim title As String = txtNewItemTitle.Text.Trim()
        If title = "" Then
            ShowMsg("Please enter a title.", True)
            Return
        End If
        AddItemColumn(title, Math.Round(ParseNumber(txtNewItemPrice.Text), 3))
        MarkPreviewOutdated()
        pnlAddItem.Visible = False
        mvOrder.ActiveViewIndex = 0
    End Sub

    Private Sub AddItemColumn(title As String, price As Decimal)
        State.Items.Add(New ItemCol With {.Title = title, .Price = price})
        For Each mline As MemberLine In State.Members
            mline.Qty.Add(0D)
        Next
    End Sub

    ' ---------- Profits item (as in Form1) ----------
    ' [Create Profit Column] inserts a [Profits] item just after Withdraw (the first item column).
    ' It is a normal item with a unit price of 1.000, so it takes part in everything items do
    ' (Subtotal, No. Of Item, TOTAL, Preview, Process). Its quantity per member is calculated:
    '   Profits = sum over the other items of (member's quantity x the item's Profit value)
    ' No profit is charged to the shop's own accounts.

    Private Const ProfitsItemTitle As String = "Profits"

    Protected Sub btnCreateProfits_Click(sender As Object, e As EventArgs) Handles btnCreateProfits.Click
        If Not IsAdmin Then Return   ' administrator only
        Dim st As OrderState = State
        If IsViewing Then Return
        If ProfitsIndex(st) >= 0 Then
            ShowMsg("The Profits column already exists.", True)
            Return
        End If

        st.Items.Insert(0, New ItemCol With {.Title = ProfitsItemTitle, .Price = 1D, .IsProfits = True})
        For Each mline As MemberLine In st.Members
            mline.Qty.Insert(0, 0D)
        Next
        RecalculateProfits(st)
        MarkPreviewOutdated()
        mvOrder.ActiveViewIndex = 0
    End Sub

    Private Function ProfitsIndex(st As OrderState) As Integer
        Return st.Items.FindIndex(Function(it) it.IsProfits)
    End Function

    Private Sub RecalculateProfits(st As OrderState)
        Dim pIdx As Integer = ProfitsIndex(st)
        If pIdx < 0 Then Return

        For Each mline As MemberLine In st.Members
            If pIdx >= mline.Qty.Count Then Continue For
            Dim profit As Decimal = 0D
            If Not IsShopAccount(mline.Name) AndAlso Not (mline.OwnerId <> "" AndAlso IsShopCustomer(mline.OwnerId)) Then   ' no profit on a shop's accounts
                For c As Integer = 0 To st.Items.Count - 1
                    If c = pIdx OrElse c >= mline.Qty.Count Then Continue For
                    profit += mline.Qty(c) * st.Items(c).Profit
                Next
            End If
            mline.Qty(pIdx) = Math.Round(profit, 3)
        Next
    End Sub

    ' ====================================================================
    '  SHOP (in the ledger: a member of type Shop with a Cash fund and Stock, Profit, Loss accounts)
    '  The Order grid has two extra lines that belong to the shop:
    '    Shop_Stock : goods bought from a vendor INTO stock (Shop Purchase, paid by Account / Paid From, default the shop)
    '    Shop_Loss  : goods in stock lost or stolen (Shop Loss)
    '  A member line set to Shop Sale buys from the shop: cost + profit (the Profits item), paid from the buyer's
    '  Paid From fund, or "(on credit)" = the buyer owes the shop.
    ' ====================================================================

    Private Const ShopName As String = "Shop"
    Private Const ShopStockName As String = "Shop_Stock"
    Private Const ShopLossName As String = "Shop_Loss"

    Private Const TxShopPurchase As String = "Shop Purchase"
    Private Const TxShopSale As String = "Shop Sale"
    Private Const TxShopLoss As String = "Shop Loss"

    Private Shared ReadOnly ShopAccountNames As String() = {ShopName, ShopStockName, "Shop_Profit", ShopLossName}

    Private Shared Function SameName(a As String, b As String) As Boolean
        Return String.Equals(If(a, "").Trim(), If(b, "").Trim(), StringComparison.OrdinalIgnoreCase)
    End Function

    Private Shared Function IsShopAccount(name As String) As Boolean
        Return ShopAccountNames.Any(Function(n) SameName(name, n))
    End Function

    ''' <summary>Used by the markup: no profit on the shop's own rows.</summary>
    Protected Function IsShopName(value As Object) As Boolean
        Dim name As String = Convert.ToString(value)
        If IsShopAccount(name) Then Return True
        ' A shop's account (Customer Type Shop): no profit either
        Dim line As MemberLine = State.Members.FirstOrDefault(Function(m) SameName(m.Name, name))
        Return line IsNot Nothing AndAlso line.OwnerId <> "" AndAlso IsShopCustomer(line.OwnerId)
    End Function

    Private Shared Function DefaultTypeOf(m As MemberLine) As String
        If m.IsShopLine Then Return If(SameName(m.Name, ShopStockName), TxShopPurchase, TxShopLoss)
        Return TxNormal
    End Function

    ' ====================================================================
    '  SHOP ORDER (the grid has a Profits column and a shop is picked, and the order has items)
    '  The Preview shows the four steps in one grid (one row per account). Process saves them as four orders,
    '  one per step: <date> <source>_01Buying <time>, _02Selling, _03Loss, _04Zeroing.
    '  Grid 1 - the purchase (_Budget is not touched):
    '    <Shop>_Cash  : "Pay X for:" the order total (items + Delivery + VAT); its balance goes up by X (-10 -> -6).
    '    <Shop>_Stock : takes every item of the order; its balance goes down by X (0 -> -4).
    '  Grid 2 - starting from the balances of grid 1:
    '    <Shop>_Profit: credited the profit of everything sold to the members (the Profits column).
    '    <Shop>_Stock : credited the cost of the items the members took (its balance goes up by them); the rest is the loss.
    '    the members  : as usual (items + Profits, Credit, Withdraw; payer / split as picked). Delivery and VAT were
    '                   paid by _Cash in grid 1, so they are not charged again here.
    '  Grid 3 - the loss (what _Stock paid less what was sold: unsold items, Delivery, VAT), from grid 2's balances:
    '    <Shop>_Loss  : withdraws their total (its balance goes down).
    '    <Shop>_Stock : is credited their total (its balance goes up).
    '  Grid 4 - Zeroing, from grid 3's balances:
    '    <Shop>_Budget: gets Profit + Loss (the net: profit less the loss).
    '    <Shop>_Profit: withdraws its balance, so it ends at zero.
    '    <Shop>_Loss  : is credited its balance back, so it ends at zero.
    ' ====================================================================

    Private Class ShopOrderAccounts
        Public ShopId As String = ""
        Public Cash As MemberLine
        Public Stock As MemberLine
        Public Profit As MemberLine
        Public Loss As MemberLine
        Public Budget As MemberLine
        Public Function IsShopAccount(memberId As String) As Boolean
            Return {Cash, Stock, Profit}.Any(Function(m) SameName(m.Id, memberId))
        End Function
    End Class

    Private Shared Function RowHasItems(m As MemberLine) As Boolean
        Return m IsNot Nothing AndAlso m.Qty.Any(Function(q) q <> 0D)
    End Function

    ''' <summary>The shown shop's _Cash, _Stock and _Profit when this is a shop order, else Nothing.</summary>
    Private Function ShopOrderOf(st As OrderState) As ShopOrderAccounts
        Dim shopId As String = ShownShopId(st)
        If shopId = "" Then Return Nothing
        Dim acc As New ShopOrderAccounts With {.ShopId = shopId}
        For Each m As MemberLine In st.Members.Where(Function(x) Not x.IsShopLine AndAlso SameName(x.OwnerId, shopId))
            If m.Name.EndsWith("_Cash", StringComparison.OrdinalIgnoreCase) Then acc.Cash = m
            If m.Name.EndsWith("_Stock", StringComparison.OrdinalIgnoreCase) Then acc.Stock = m
            If m.Name.EndsWith("_Profit", StringComparison.OrdinalIgnoreCase) Then acc.Profit = m
            If m.Name.EndsWith("_Loss", StringComparison.OrdinalIgnoreCase) Then acc.Loss = m
            If m.Name.EndsWith("_Budget", StringComparison.OrdinalIgnoreCase) Then acc.Budget = m
        Next
        If acc.Cash Is Nothing OrElse acc.Stock Is Nothing OrElse acc.Profit Is Nothing OrElse acc.Loss Is Nothing OrElse acc.Budget Is Nothing Then Return Nothing
        If Not st.Members.Any(Function(m) Not m.IsShopLine AndAlso RowHasItems(m)) Then Return Nothing
        Return acc
    End Function

    ''' <summary>Is this order line one of the shop's accounts (any of its five)?</summary>
    Private Function IsLineOfShop(shop As ShopOrderAccounts, memberId As String) As Boolean
        Return SameName(OwnerOf(memberId), shop.ShopId)
    End Function

    ''' <summary>
    ''' A shop order's step name: the step goes after the source, before the time at the end of the title:
    ''' "20261112 Market 0142" + "_01Buying" -> "20261112 Market_01Buying 0142". No time at the end: added at the end.
    ''' </summary>
    Private Shared Function ShopStepTitle(title As String, stepName As String) As String
        title = If(title, "").Trim()
        Dim cut As Integer = title.LastIndexOf(" "c)
        If cut > 0 Then
            Dim tail As String = title.Substring(cut + 1)
            If tail <> "" AndAlso tail.All(Function(ch) Char.IsDigit(ch)) Then Return title.Substring(0, cut) & stepName & " " & tail
        End If
        Return title & stepName
    End Function

    ''' <summary>The Account choice "the shop paid, the member owes it" on a member's row of a shop order.</summary>
    Private Function OnAccountName(shop As ShopOrderAccounts) As String
        Return LedgerName(shop.ShopId) & OnAccountSuffix
    End Function
    Private Const OnAccountSuffix As String = " (on account)"

    Private Function FirstFundId(memberId As String) As Integer
        Dim f As Account = FundsOf(memberId).FirstOrDefault()
        If f Is Nothing Then Throw New LedgerException(LedgerName(memberId) & " has no fund.")
        Return f.Id
    End Function

    ''' <summary>Everything a shop order posts: one journal and one order per grid.</summary>
    Private Class ShopPosting
        Public Grid1 As Journal            ' buying: _Cash pays, _Stock takes every item
        Public Grid2 As Journal            ' selling: the members' order + Profit + Stock out
        Public Grid3 As Journal            ' loss: the items not sold - _Loss withdraws, _Stock credited
        Public Grid4 As Journal            ' zeroing: Profit and Loss to zero, the net to Budget
        Public ProfitToZero As Decimal     ' _Profit's balance after grid 3
        Public LossToZero As Decimal       ' _Loss's balance after grid 3 (normally negative)
        Public BuyDoc As OrderDocument     ' the orders Process saves: <title>_Buying, _Selling, _Loss, _Zeroing
        Public SellDoc As OrderDocument
        Public LossDoc As OrderDocument
        Public ZeroDoc As OrderDocument
        Public MemberDoc As OrderDocument  ' the members' lines (grid 2)
        Public PurchaseTotal As Decimal
        Public ProfitTotal As Decimal
        Public StockOut As Decimal         ' the loss: what _Stock paid in grid 1 less what the members took (unsold items, Delivery, VAT)
        Public SoldCost As Decimal         ' the cost of the items the members took (credited to _Stock in grid 2)
        Public OwedToStock As New List(Of String)   ' "Fatima 3.900": members on account (they owe _Stock the cost)
        Public OwedToProfit As New List(Of String)  ' "Fatima 1.825": ... and _Profit the profit
        Public Bought As New List(Of String)       ' "20 Banana"
        Public ProfitLines As New List(Of String)  ' "18 Banana to Fatima: 18 x 0.025 = 0.450"
        Public KeptLines As New List(Of String)    ' the items on _Stock's own row
    End Class

    Private Function BuildShopPosting(st As OrderState, shop As ShopOrderAccounts) As ShopPosting
        Dim r As New ShopPosting
        Dim typed As OrderDocument = BuildOrderDocument(st)
        Dim shopKeys As New HashSet(Of String)(typed.Lines.Where(Function(l) IsLineOfShop(shop, l.MemberId)).Select(Function(l) l.LineKey),
                                               StringComparer.OrdinalIgnoreCase)
        Dim stockKey As String = shop.Stock.LineKey

        ' ---- Grid 1: every item bought (no Profits), paid by _Cash ----
        Dim purchase As New OrderDocument With {.OrderDate = typed.OrderDate, .OrderNo = typed.OrderNo, .Source = typed.Source,
                                               .Delivery = typed.Delivery, .Vat = typed.Vat}
        For Each it As OrderItem In typed.Items.Where(Function(x) Not x.IsProfits)
            Dim q As Decimal = it.Quantities.Values.Sum()
            If q = 0D Then Continue For
            Dim pit As New OrderItem With {.Name = it.Name, .UnitPrice = it.UnitPrice}
            pit.Quantities(stockKey) = q
            purchase.Items.Add(pit)
            r.Bought.Add(Qty(q) & " " & it.Name)
        Next
        Dim stockRow As PreviewLine = st.Preview.FirstOrDefault(Function(x) Not x.AutoRow AndAlso SameName(x.LineKey, stockKey))
        Dim cashFund As Integer = If(stockRow Is Nothing OrElse stockRow.PaidFrom = "", 0, FundIdByName(shop.Cash.Id, stockRow.PaidFrom))
        If cashFund = 0 Then cashFund = FirstFundId(shop.Cash.Id)
        purchase.Lines.Add(New OrderLine With {.LineKey = stockKey, .MemberId = shop.Stock.Id, .LineType = OrderLineType.Normal})
        r.PurchaseTotal = Math.Round(OrderPosting.ExternalTotal(OrderPosting.Charges(purchase)), 3)
        If r.PurchaseTotal <= 0D Then Throw New LedgerException("The order total is zero: nothing to pay.")

        ' ---- Grid 2: the members as usual (no Delivery / VAT: paid in grid 1) ----
        r.MemberDoc = New OrderDocument With {.OrderDate = typed.OrderDate, .OrderNo = typed.OrderNo, .Source = typed.Source}
        For Each it As OrderItem In typed.Items
            Dim mit As New OrderItem With {.Name = it.Name, .UnitPrice = it.UnitPrice, .UnitProfit = it.UnitProfit, .IsProfits = it.IsProfits}
            For Each kv As KeyValuePair(Of String, Decimal) In it.Quantities
                If Not shopKeys.Contains(kv.Key) Then mit.Quantities(kv.Key) = kv.Value
            Next
            r.MemberDoc.Items.Add(mit)
        Next
        For Each l As OrderLine In typed.Lines.Where(Function(x) Not shopKeys.Contains(x.LineKey))
            r.MemberDoc.Lines.Add(l)
        Next
        ' Rows "on account" (the shop paid, the member owes it): posted by hand below; the others as usual
        Dim onAccount As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each p As PreviewLine In st.Preview.Where(Function(x) Not x.AutoRow AndAlso SameName(x.Account, OnAccountName(shop)))
            Dim m As MemberLine = st.Members.FirstOrDefault(Function(x) SameName(x.LineKey, p.LineKey))
            If m IsNot Nothing AndAlso m.Splits.Count = 0 AndAlso m.Credit = 0D AndAlso m.Withdraw = 0D Then onAccount.Add(m.LineKey)
        Next
        Dim postDoc As New OrderDocument With {.OrderDate = typed.OrderDate, .OrderNo = typed.OrderNo, .Source = typed.Source}
        For Each it As OrderItem In r.MemberDoc.Items
            Dim pit As New OrderItem With {.Name = it.Name, .UnitPrice = it.UnitPrice, .UnitProfit = it.UnitProfit, .IsProfits = it.IsProfits}
            For Each kv As KeyValuePair(Of String, Decimal) In it.Quantities
                If Not onAccount.Contains(kv.Key) Then pit.Quantities(kv.Key) = kv.Value
            Next
            postDoc.Items.Add(pit)
        Next
        postDoc.Lines.AddRange(r.MemberDoc.Lines.Where(Function(x) Not onAccount.Contains(x.LineKey)))
        Dim j2 As Journal
        If postDoc.Lines.Count > 0 Then
            j2 = Ledger.BuildOrder(postDoc)
            r.MemberDoc.DerivedPayments.AddRange(postDoc.PaymentsToSave())
        Else
            j2 = New Journal With {.JournalType = JournalTypes.Order, .Memo = "Order " & typed.Source, .JournalDate = typed.OrderDate}
        End If

        ' Profit sold to the members (the Profits column), and its wording per item
        r.ProfitTotal = Math.Round(r.MemberDoc.Items.Where(Function(x) x.IsProfits).Sum(Function(x) x.Quantities.Values.Sum() * x.UnitPrice), 3)
        For Each m As MemberLine In st.Members.Where(Function(x) Not x.IsShopLine AndAlso Not IsLineOfShop(shop, x.Id) AndAlso RowHasItems(x))
            For c As Integer = 0 To Math.Min(m.Qty.Count, st.Items.Count) - 1
                Dim it As ItemCol = st.Items(c)
                If it.IsProfits OrElse m.Qty(c) = 0D OrElse it.Profit = 0D Then Continue For
                r.ProfitLines.Add(Qty(m.Qty(c)) & " " & it.Title & " to " & If(m.OwnerName <> "", m.OwnerName, m.Name) & ": " &
                                  Qty(m.Qty(c)) & " x " & Money(it.Profit) & " = " & Money(Math.Round(m.Qty(c) * it.Profit, 3)))
            Next
        Next

        ' What goes out of stock: the items on _Stock's own row (not taken by the members)
        For Each it As OrderItem In typed.Items.Where(Function(x) Not x.IsProfits)
            Dim q As Decimal = 0D
            If Not it.Quantities.TryGetValue(stockKey, q) OrElse q = 0D Then Continue For
            r.StockOut += q * it.UnitPrice
            r.KeptLines.Add(Qty(q) & " " & it.Name & " (" & Qty(q) & " x " & Money(it.UnitPrice) & ")")
        Next
        ' Selling: _Stock is credited what the members took (at cost); the rest of what it paid is the loss
        r.SoldCost = Math.Round(r.MemberDoc.Items.Where(Function(x) Not x.IsProfits).Sum(Function(x) x.Quantities.Values.Sum() * x.UnitPrice), 3)
        r.StockOut = Math.Round(r.PurchaseTotal - r.SoldCost, 3)
        If typed.Delivery <> 0D Then r.KeptLines.Add("Delivery " & Money(typed.Delivery))
        If typed.Vat <> 0D Then r.KeptLines.Add("VAT " & Money(typed.Vat))

        Dim memo As String = "Order " & typed.Source
        Dim accounts As New List(Of Account)(Ledger.Store.GetAccounts())
        accounts.AddRange(j2.Entries.Where(Function(e) e.PendingAccount IsNot Nothing).Select(Function(e) e.PendingAccount).Distinct())
        Dim b2 As New JournalBuilder(accounts, j2)
        ' On account: the member owes _Stock the cost and _Profit the profit of what they took
        Dim owedCost As Decimal = 0D, owedProfit As Decimal = 0D
        For Each c As LineCharge In OrderPosting.Charges(r.MemberDoc).Where(Function(x) onAccount.Contains(x.Line.LineKey))
            Dim who As String = c.Line.MemberId
            Dim cost As Decimal = Math.Round(c.Items - c.Profit, 3), prof As Decimal = Math.Round(c.Profit, 3)
            Dim whoName As String = LedgerName(PersonOf(who))
            b2.AddTo(who, AccountKind.Expense, AccountNames.Expense, "", cost + prof, "Bought from " & LedgerName(shop.ShopId) & ": " & memo)
            b2.AddTo(who, AccountKind.Payable, "Payable", shop.Stock.Id, -cost, "Owes " & shop.Stock.Name & ": " & memo)
            b2.AddTo(who, AccountKind.Payable, "Payable", shop.Profit.Id, -prof, "Owes " & shop.Profit.Name & ": " & memo)
            b2.AddTo(shop.Stock.Id, AccountKind.Receivable, "Receivable", who, cost, "Owed by " & whoName & ": " & memo)
            b2.AddTo(shop.Stock.Id, AccountKind.Equity, "Sales", "", -cost, "Owed by " & whoName & ": " & memo)
            b2.AddTo(shop.Profit.Id, AccountKind.Receivable, "Receivable", who, prof, "Owed by " & whoName & ": " & memo)
            b2.AddTo(shop.Profit.Id, AccountKind.Income, "Profit", "", -prof, "Owed by " & whoName & ": " & memo)
            c.Line.PayerMemberId = shop.Stock.Id
            owedCost += cost : owedProfit += prof
            If cost <> 0D Then r.OwedToStock.Add(whoName & " " & Money(cost))
            If prof <> 0D Then r.OwedToProfit.Add(whoName & " " & Money(prof))
        Next
        ' Paid by the members themselves (or someone else): _Stock and _Profit are credited directly
        b2.Add(shop.Profit.Id, FirstFundId(shop.Profit.Id), r.ProfitTotal - owedProfit, "Profit from selling: " & memo)
        b2.AddTo(shop.Profit.Id, AccountKind.Income, "Profit", "", -(r.ProfitTotal - owedProfit), "Profit from selling: " & memo)
        b2.AddTo(shop.Stock.Id, AccountKind.Stock, "Goods", "", r.SoldCost - owedCost, "Sold to members: " & memo)
        b2.AddTo(shop.Stock.Id, AccountKind.Equity, "Sales", "", -(r.SoldCost - owedCost), "Sold to members: " & memo)

        ' Grid 1's entries: _Cash +total (its fund), _Stock -total (the goods), each against Equity "Purchases"
        Dim addGrid1 As Action(Of JournalBuilder) =
            Sub(b)
                b.Add(shop.Cash.Id, cashFund, r.PurchaseTotal, memo)
                b.AddTo(shop.Cash.Id, AccountKind.Equity, "Purchases", "", -r.PurchaseTotal, memo)
                b.AddTo(shop.Stock.Id, AccountKind.Stock, "Goods", "", -r.PurchaseTotal, memo)
                b.AddTo(shop.Stock.Id, AccountKind.Equity, "Purchases", "", r.PurchaseTotal, memo)
            End Sub
        ' Grid 3's entries: the loss (unsold items, Delivery, VAT) - _Loss withdraws it, _Stock is credited it (back to zero)
        Dim lossFund As Integer = FirstFundId(shop.Loss.Id)
        Dim addGrid3 As Action(Of JournalBuilder) =
            Sub(b)
                b.AddTo(shop.Loss.Id, AccountKind.Expense, "Loss", "", r.StockOut, "Not sold: " & memo)
                b.Add(shop.Loss.Id, lossFund, -r.StockOut, "Not sold: " & memo)
                b.AddTo(shop.Stock.Id, AccountKind.Stock, "Goods", "", r.StockOut, "Not sold, to " & shop.Loss.Name & ": " & memo)
                b.AddTo(shop.Stock.Id, AccountKind.Equity, "Not sold", "", -r.StockOut, "Not sold, to " & shop.Loss.Name & ": " & memo)
            End Sub
        ' Grid 4 (Zeroing): from the balances after grid 3. Each change goes to the account's fund, against Equity "Zeroing"
        Dim sums As Dictionary(Of String, MemberFigures) = SummariesById()
        r.ProfitToZero = Math.Round(Bal(FiguresOf(sums, shop.Profit.Id)) + r.ProfitTotal, 3)
        r.LossToZero = Math.Round(Bal(FiguresOf(sums, shop.Loss.Id)) - r.StockOut, 3)
        Dim addGrid4 As Action(Of JournalBuilder) =
            Sub(b)
                Dim moves As New List(Of Tuple(Of String, Decimal, String)) From {
                    Tuple.Create(shop.Profit.Id, -r.ProfitToZero, "Zeroing: profit to " & shop.Budget.Name),
                    Tuple.Create(shop.Loss.Id, -r.LossToZero, "Zeroing: loss to " & shop.Budget.Name),
                    Tuple.Create(shop.Budget.Id, r.ProfitToZero + r.LossToZero, "Zeroing: profit less loss")}
                For Each mv As Tuple(Of String, Decimal, String) In moves
                    b.Add(mv.Item1, FirstFundId(mv.Item1), mv.Item2, mv.Item3)
                    b.AddTo(mv.Item1, AccountKind.Equity, "Zeroing", "", -mv.Item2, mv.Item3)
                Next
            End Sub
        ' Four journals (one per grid). The accounts made on the spot are shared, so posting them one after the
        ' other never makes the same account twice.
        Dim sharedAccounts As New List(Of Account)(accounts)
        Dim addPending As Action(Of Journal) =
            Sub(jj)
                For Each a As Account In jj.Entries.Where(Function(e) e.PendingAccount IsNot Nothing).Select(Function(e) e.PendingAccount).Distinct()
                    If Not sharedAccounts.Contains(a) Then sharedAccounts.Add(a)
                Next
            End Sub
        addPending(j2)
        r.Grid1 = New Journal With {.JournalType = JournalTypes.Order, .Memo = memo, .JournalDate = typed.OrderDate}
        addGrid1(New JournalBuilder(sharedAccounts, r.Grid1)) : addPending(r.Grid1)
        r.Grid3 = New Journal With {.JournalType = JournalTypes.Order, .Memo = memo, .JournalDate = typed.OrderDate}
        addGrid3(New JournalBuilder(sharedAccounts, r.Grid3)) : addPending(r.Grid3)
        r.Grid4 = New Journal With {.JournalType = JournalTypes.Order, .Memo = memo, .JournalDate = typed.OrderDate}
        addGrid4(New JournalBuilder(sharedAccounts, r.Grid4)) : addPending(r.Grid4)
        r.Grid2 = j2

        ' The orders Process saves, one per grid
        ' _Buying: every item on _Stock's line, paid by _Cash, with Delivery and VAT
        r.BuyDoc = New OrderDocument With {.OrderDate = typed.OrderDate, .OrderNo = typed.OrderNo, .Source = typed.Source,
                                          .Delivery = typed.Delivery, .Vat = typed.Vat}
        For Each it As OrderItem In purchase.Items
            r.BuyDoc.Items.Add(it)
        Next
        r.BuyDoc.Lines.Add(New OrderLine With {.LineKey = stockKey, .MemberId = shop.Stock.Id, .LineType = OrderLineType.Normal,
                                               .PaidFromFundId = cashFund, .PayerMemberId = shop.Cash.Id, .PayerFundId = cashFund})
        r.BuyDoc.DerivedPayments.Add(New OrderPayment With {.PayerMemberId = shop.Cash.Id, .FundAccountId = cashFund, .Amount = r.PurchaseTotal})
        ' _Selling: the members' lines as usual
        r.SellDoc = r.MemberDoc
        ' _Loss: the items not sold, on _Loss's line (given by _Stock)
        r.LossDoc = New OrderDocument With {.OrderDate = typed.OrderDate, .OrderNo = typed.OrderNo, .Source = typed.Source}
        For Each it As OrderItem In typed.Items.Where(Function(x) Not x.IsProfits)
            Dim q As Decimal = 0D
            If Not it.Quantities.TryGetValue(stockKey, q) OrElse q = 0D Then Continue For
            Dim lit As New OrderItem With {.Name = it.Name, .UnitPrice = it.UnitPrice}
            lit.Quantities(shop.Loss.LineKey) = q
            r.LossDoc.Items.Add(lit)
        Next
        If r.LossDoc.Items.Count > 0 Then
            r.LossDoc.Lines.Add(New OrderLine With {.LineKey = shop.Loss.LineKey, .MemberId = shop.Loss.Id, .LineType = OrderLineType.Normal,
                                                    .PayerMemberId = shop.Stock.Id})
        End If
        ' _Zeroing: no items (the journal moves Profit and Loss to Budget)
        r.ZeroDoc = New OrderDocument With {.OrderDate = typed.OrderDate, .OrderNo = typed.OrderNo, .Source = typed.Source}
        Return r
    End Function

    ''' <summary>The shop order's Preview in one grid: each account's four steps together, figures from the ledger.</summary>
    Private Sub RecalculateShopPreview(st As OrderState, shop As ShopOrderAccounts)
        ' Rows (one grid): _Stock (its own row, paid by _Cash), _Cash, _Profit, _Loss, _Budget, the members, other payers
        Dim stockRow As PreviewLine = st.Preview.FirstOrDefault(Function(x) Not x.AutoRow AndAlso SameName(x.LineKey, shop.Stock.LineKey))
        If stockRow Is Nothing Then stockRow = New PreviewLine With {.MemberId = shop.Stock.Id, .LineKey = shop.Stock.LineKey, .Name = shop.Stock.Name}
        stockRow.Section = 0 : stockRow.TxType = TxNormal : stockRow.TypeLocked = True : stockRow.AccountLocked = True
        stockRow.Account = shop.Cash.Name
        RefreshPaidFrom(stockRow)   ' _Cash's funds
        Dim members As List(Of PreviewLine) = st.Preview.Where(Function(x) Not x.AutoRow AndAlso Not IsLineOfShop(shop, x.MemberId)).ToList()
        ' The shop paid: a member's row is on account (they owe the shop) unless another payer is picked
        For Each p As PreviewLine In members
            If p.ShopDefaulted Then Continue For
            p.ShopDefaulted = True
            Dim m As MemberLine = st.Members.FirstOrDefault(Function(x) SameName(x.LineKey, p.LineKey))
            If m Is Nothing OrElse m.Splits.Count > 0 OrElse m.Credit <> 0D OrElse m.Withdraw <> 0D OrElse Not RowHasItems(m) Then Continue For
            If Not SameName(p.Account, m.Name) Then Continue For
            p.Account = OnAccountName(shop)
            RefreshPaidFrom(p)
        Next
        For Each p As PreviewLine In members
            p.Section = 1
        Next

        Dim r As ShopPosting = Nothing
        Dim problem As String = Nothing
        Dim ch1 As New Dictionary(Of String, MemberFigures)(StringComparer.OrdinalIgnoreCase)
        Dim ch2 As New Dictionary(Of String, MemberFigures)(StringComparer.OrdinalIgnoreCase)
        Dim ch3 As New Dictionary(Of String, MemberFigures)(StringComparer.OrdinalIgnoreCase)
        Dim ch4 As New Dictionary(Of String, MemberFigures)(StringComparer.OrdinalIgnoreCase)
        Dim charges As New List(Of LineCharge)
        Try
            r = BuildShopPosting(st, shop)
            For Each kv As KeyValuePair(Of String, MemberFigures) In Ledger.Preview(r.Grid1).Changes : ch1(kv.Key) = kv.Value : Next
            If r.Grid2.Entries.Count > 0 Then
                For Each kv As KeyValuePair(Of String, MemberFigures) In Ledger.Preview(r.Grid2).Changes : ch2(kv.Key) = kv.Value : Next
            End If
            If r.StockOut <> 0D Then
                For Each kv As KeyValuePair(Of String, MemberFigures) In Ledger.Preview(r.Grid3).Changes : ch3(kv.Key) = kv.Value : Next
            End If
            If r.Grid4.Entries.Count > 0 Then
                For Each kv As KeyValuePair(Of String, MemberFigures) In Ledger.Preview(r.Grid4).Changes : ch4(kv.Key) = kv.Value : Next
            End If
            charges = OrderPosting.Charges(r.MemberDoc)
        Catch ex As LedgerException
            problem = ex.Message
        Catch ex As Exception
            problem = "The Preview could not be calculated: " & ex.Message
        End Try

        ' One grid: one row per account, showing the four steps (1 Buying, 2 Selling, 3 Loss, 4 Zeroing) together
        Dim stepsOf As New Dictionary(Of String, List(Of String))(StringComparer.OrdinalIgnoreCase)
        Dim creditOf As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        Dim debtOf As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        Dim addStep As Action(Of String, String, Decimal, IEnumerable(Of String)) =
            Sub(memberId, title, change, lines)
                If change = 0D Then Return
                If Not stepsOf.ContainsKey(memberId) Then stepsOf(memberId) = New List(Of String)
                stepsOf(memberId).Add(title & " (" & If(change > 0D, "+", "") & Money(change) & "):")
                stepsOf(memberId).AddRange(lines.Select(Function(x) "   " & x))
                If change > 0D Then
                    creditOf(memberId) = If(creditOf.ContainsKey(memberId), creditOf(memberId), 0D) + change
                Else
                    debtOf(memberId) = If(debtOf.ContainsKey(memberId), debtOf(memberId), 0D) - change
                End If
            End Sub
        If r IsNot Nothing Then
            Dim bought As New List(Of String)(r.Bought)
            If st.Delivery <> 0D Then bought.Add("Delivery " & Money(st.Delivery))
            If st.VAT <> 0D Then bought.Add("VAT " & Money(st.VAT))
            Dim kept As List(Of String) = r.KeptLines.Select(Function(x) x.Replace(")", " BD)")).ToList()
            Dim net As Decimal = r.ProfitToZero + r.LossToZero
            addStep(shop.Cash.Id, "1. Buying - pay " & Money(r.PurchaseTotal) & " for", r.PurchaseTotal, bought)
            addStep(shop.Stock.Id, "1. Buying - takes", -r.PurchaseTotal, r.Bought)
            addStep(shop.Profit.Id, "2. Selling - profit from selling", r.ProfitTotal,
                    r.ProfitLines.Concat(If(r.OwedToProfit.Count > 0, {"Owed by: " & String.Join(", ", r.OwedToProfit)}, New String() {})))
            addStep(shop.Stock.Id, "2. Selling - pay for the items sold to members", r.SoldCost,
                    {"All items " & Money(r.PurchaseTotal) & ", sold " & Money(r.SoldCost) & ", the rest is the loss"}.
                        Concat(If(r.OwedToStock.Count > 0, {"Owed by: " & String.Join(", ", r.OwedToStock)}, New String() {})))
            addStep(shop.Loss.Id, "3. Loss - not sold", -r.StockOut, kept)   ' unsold items (+ Delivery, VAT)
            addStep(shop.Stock.Id, "3. Loss - not sold, to " & shop.Loss.Name, r.StockOut, kept)
            addStep(shop.Profit.Id, "4. Zeroing - profit to " & shop.Budget.Name, -r.ProfitToZero, {"back to zero"})
            addStep(shop.Loss.Id, "4. Zeroing - loss covered by " & shop.Budget.Name, -r.LossToZero, {"back to zero"})
            addStep(shop.Budget.Id, "4. Zeroing - profit less loss", net,
                    {"Profit " & Money(r.ProfitToZero) & " from " & shop.Profit.Name, "Loss " & Money(r.LossToZero) & " from " & shop.Loss.Name})
        End If

        st.Preview.Clear()
        st.Preview.Add(stockRow)   ' _Stock: its own row (Paid From = _Cash's fund)
        For Each acc As MemberLine In {shop.Cash, shop.Profit, shop.Loss, shop.Budget}
            If stepsOf.ContainsKey(acc.Id) Then st.Preview.Add(New PreviewLine With {.MemberId = acc.Id, .Name = acc.Name, .AutoRow = True})
        Next
        st.Preview.AddRange(members)
        For Each id As String In ch2.Keys
            If IsLineOfShop(shop, id) OrElse st.Preview.Any(Function(x) SameName(x.MemberId, id)) Then Continue For
            st.Preview.Add(New PreviewLine With {.MemberId = id, .Name = LedgerName(id), .AutoRow = True})
        Next

        ' Figures: from the balances now to the balances after all four steps (what Process posts in one go)
        Dim before As Dictionary(Of String, MemberFigures) = SummariesById()
        For Each p As PreviewLine In st.Preview
            p.Section = 0
            p.ExBalance = Nothing : p.ExCash = Nothing : p.ExExpense = Nothing : p.ExReceivable = Nothing : p.ExPayable = Nothing
            p.NextBalance = Nothing : p.Cash = Nothing : p.Expense = Nothing : p.Receivable = Nothing : p.Payable = Nothing
            If Not (p.AutoRow OrElse SameName(p.LineKey, p.MemberId)) Then Continue For
            Dim b As MemberFigures = FiguresOf(before, p.MemberId)
            Dim a As MemberFigures = b
            For Each ch As Dictionary(Of String, MemberFigures) In {ch1, ch2, ch3, ch4}
                a = Plus(a, FiguresOf(ch, p.MemberId))
            Next
            p.ExBalance = Bal(b) : p.ExCash = CashOf(b) : p.ExExpense = b.Expense : p.ExReceivable = b.OwedToThem : p.ExPayable = b.TheyOwe
            p.NextBalance = Bal(a) : p.Cash = CashOf(a) : p.Expense = a.Expense : p.Receivable = a.OwedToThem : p.Payable = a.TheyOwe
        Next

        ' Texts and totals
        For Each p As PreviewLine In st.Preview
            If r Is Nothing Then Exit For
            If stepsOf.ContainsKey(p.MemberId) AndAlso (p.AutoRow OrElse SameName(p.LineKey, shop.Stock.LineKey)) Then
                p.Lines = String.Join(vbLf, stepsOf(p.MemberId))
                p.Credit = If(creditOf.ContainsKey(p.MemberId), creditOf(p.MemberId), 0D)
                p.Debt = If(debtOf.ContainsKey(p.MemberId), debtOf(p.MemberId), 0D)
                p.Total = Math.Abs(p.Credit.GetValueOrDefault() - p.Debt.GetValueOrDefault())
            ElseIf Not p.AutoRow Then
                Dim rowKey As String = p.LineKey
                p.Total = charges.Where(Function(c) IsKeyOfRow(c.Line.LineKey, rowKey)).Sum(Function(c) c.Total)
            Else
                ' another payer of a member's line
                Dim paid As New List(Of String)
                For Each c As LineCharge In charges
                    If c.Line.PaidFromFundId = 0 Then Continue For
                    Dim fund As Account = LedgerAccounts().FirstOrDefault(Function(x) x.Id = c.Line.PaidFromFundId)
                    If fund Is Nothing OrElse Not SameName(fund.MemberId, p.MemberId) Then Continue For
                    paid.Add("Pays " & Money(c.Total) & " for " & LedgerName(c.Line.MemberId) & " (from " & fund.Name & ")")
                Next
                p.Lines = String.Join(vbLf, paid)
            End If
        Next

        ' Split payments of the members must add up
        If problem Is Nothing Then
            For Each p As PreviewLine In members
                Dim m As MemberLine = st.Members.FirstOrDefault(Function(x) SameName(x.LineKey, p.LineKey))
                If m Is Nothing OrElse m.Splits.Count = 0 OrElse p.TxType <> TxNormal Then Continue For
                If Math.Abs(m.Splits.Sum(Function(x) x.Amount) - If(p.Total, 0D)) > 0.0015D Then
                    problem = "the split payment of " & p.Name & " adds up to " & Money(m.Splits.Sum(Function(x) x.Amount)) &
                              " but the line is " & Money(If(p.Total, 0D)) & ": click + on that row to fix it"
                    Exit For
                End If
            Next
        End If

        If problem IsNot Nothing Then
            st.StatusKind = "bad"
            st.StatusText = ChrW(&H2716) & "  Cannot process " & ChrW(&H2013) & " " & problem
        Else
            st.StatusKind = "ok"
            st.StatusText = ChrW(&H2714) & "  Ready to process " & ChrW(&H2013) & " " & shop.Cash.Name & " pays the vendor " & Money(r.PurchaseTotal)
        End If
        st.PreviewCurrent = True
    End Sub

    ' ---------- Calculate ----------

    Protected Sub btnCalculate_Click(sender As Object, e As EventArgs) Handles btnCalculate.Click
        BuildPreview()
        mvOrder.ActiveViewIndex = 1
    End Sub

    ' ---------- Preview selections ----------

    ''' <summary>A row's Account / Transaction Type / Paid From changed (already read in SyncFromUi).</summary>
    Protected Sub PreviewSelection_Changed(sender As Object, e As EventArgs)
        For Each p As PreviewLine In State.Preview
            RefreshPaidFrom(p)   ' another payer (or type) = another list of funds
        Next
        RecalculatePreview()
    End Sub

    Protected Sub ddlHeaderAccount_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlHeaderAccount.SelectedIndexChanged
        If ddlHeaderAccount.SelectedValue = "" Then Return
        For Each p As PreviewLine In State.Preview
            If p.AutoRow OrElse p.AccountLocked Then Continue For
            p.Account = ddlHeaderAccount.SelectedValue
            RefreshPaidFrom(p)
        Next
        RecalculatePreview()
    End Sub

    Protected Sub ddlHeaderType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlHeaderType.SelectedIndexChanged
        If ddlHeaderType.SelectedValue = "" Then Return
        For Each p As PreviewLine In State.Preview
            If p.AutoRow OrElse p.TypeLocked Then Continue For
            p.TxType = ddlHeaderType.SelectedValue
            RefreshPaidFrom(p)
        Next
        RecalculatePreview()
    End Sub

    ''' <summary>One fund for every line that offers it.</summary>
    Protected Sub ddlHeaderPaidFrom_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlHeaderPaidFrom.SelectedIndexChanged
        Dim value As String = ddlHeaderPaidFrom.SelectedValue
        If value = "" Then Return
        For Each p As PreviewLine In State.Preview
            If Not p.AutoRow AndAlso p.PaidFromOptions.Contains(value) Then p.PaidFrom = value
        Next
        RecalculatePreview()
    End Sub

    ' ---------- New Order ----------

    Protected Sub btnNewOrder_Click(sender As Object, e As EventArgs) Handles btnNewOrder.Click
        If Not IsAdmin Then Return   ' administrator only
        ResetOrder()
    End Sub

    ' ---------- Cancel Order (administrator, a saved order that is open) ----------

    Protected Sub btnCancelOrder_Click(sender As Object, e As EventArgs) Handles btnCancelOrder.Click
        If Not IsAdmin OrElse Not IsViewing Then Return
        Dim orderId As String = State.ViewingOrderId
        Try
            Ledger.CancelOrder(orderId, "Cancelled order " & orderId & " (by " & CurrentStaffId & ")")
        Catch ex As Exception
            ShowMsg("The order was not cancelled: " & ex.Message, True)
            Return
        End Try
        ResetLedgerCache()
        Try
            SyncLoanPairs()   ' the loans' _Spent accounts follow the reversal
        Catch
        End Try
        LoadSavedOrder(orderId)   ' shows it again, marked CANCELLED
        ShowMsg("Order " & orderId & " cancelled: a reversing journal put every balance back.", False)
    End Sub

    ' ====================================================================
    '  NEW ORDER / LOAD MEMBERS
    ' ====================================================================

    ''' <summary>Clears everything for a new order and reloads the members with their figures from the ledger.</summary>
    Private Sub ResetOrder(Optional includeFrozen As Boolean = False)
        Dim st As New OrderState With {
            .OrderDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            .OrderNo = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture)}
        Try
            st.ShopId = FindShopId()
        Catch
        End Try
        State = st
        st.Members = LoadMembers(includeFrozen)
        mvOrder.ActiveViewIndex = 0
    End Sub

    ''' <summary>
    ''' The accounts of the active people (most active people first; each person's accounts together, Capital
    ''' then Loans, "_Spent" loans left out), the shop, then the shop's two goods lines. A person who has no
    ''' accounts is a line of their own. Frozen people / accounts are left out, except when showing a saved order.
    ''' </summary>
    Private Function LoadMembers(Optional includeFrozen As Boolean = False) As List(Of MemberLine)
        Dim list As New List(Of MemberLine)
        Try
            Dim sums As Dictionary(Of String, MemberFigures) = SummariesById()
            Dim order As List(Of String) = OrdersDB.GetDataTable("SELECT Member_ID FROM Members ORDER BY Val(Member_Movements & '') DESC, Member_Name").
                                           Rows.Cast(Of DataRow)().Select(Function(r) r("Member_ID").ToString()).ToList()
            Dim usable As Func(Of Member, Boolean) = Function(m) m.IsActive AndAlso (includeFrozen OrElse Not m.IsFrozen)
            ' People and the shop (not banks, not accounts: those come under their person)
            Dim people As List(Of Member) = LedgerMembers().Where(Function(m) usable(m) AndAlso m.MemberType <> MemberTypes.Bank AndAlso
                                                                              Not IsAccountMember(m)).
                                                            OrderBy(Function(m) order.IndexOf(m.Id)).ToList()
            Dim lineOf As Func(Of Member, MemberLine) =
                Function(m)
                    Dim f As MemberFigures = FiguresOf(sums, m.Id)
                    Return New MemberLine With {
                        .Id = m.Id, .LineKey = m.Id, .Name = m.Name, .ShortName = m.Name, .GroupIds = New List(Of Integer)(m.GroupIds),
                        .ExBalance = Bal(f), .ExCash = CashOf(f), .ExExpense = f.Expense, .ExReceivable = f.OwedToThem, .ExPayable = f.TheyOwe}
                End Function
            For Each p As Member In people
                Dim accounts As List(Of Member) = If(IsPerson(p), AccountsOfPerson(p.Id, includeSpent:=False).Where(usable).ToList(), New List(Of Member))
                If accounts.Count = 0 Then
                    list.Add(lineOf(p))   ' the shop, or a person without accounts
                    Continue For
                End If
                For Each a As Member In accounts
                    Dim line As MemberLine = lineOf(a)
                    line.OwnerId = p.Id
                    line.OwnerName = p.Name
                    line.ShortName = ShortAccountName(a)
                    line.AccountType = AccountTypeOf(a.Id)
                    line.GroupIds = New List(Of Integer)(p.GroupIds)   ' the group filter goes by the person
                    list.Add(line)
                Next
            Next
            Dim shopId As String = State.ShopId
            If shopId <> "" Then
                list.Add(New MemberLine With {.Id = shopId, .LineKey = shopId & ":stock", .Name = ShopStockName, .IsShopLine = True})
                list.Add(New MemberLine With {.Id = shopId, .LineKey = shopId & ":loss", .Name = ShopLossName, .IsShopLine = True})
            End If
            If people.Count = 0 Then
                ShowMsg("No active members found in " & OrdersDB.DBPath & " (check Members.Is_Active).", True)
            End If
        Catch ex As Exception
            ShowMsg("Could not load members from the database: " & ex.Message, True)
        End Try
        Return list
    End Function

    ''' <summary>Members' names for the Account lists (not the shop's goods lines).</summary>
    Private Function MemberNames(st As OrderState) As List(Of String)
        Return st.Members.Where(Function(m) Not m.IsShopLine).Select(Function(m) m.Name).
                          Where(Function(n) n <> "").Distinct(StringComparer.OrdinalIgnoreCase).ToList()
    End Function

    ' ====================================================================
    '  ORDER DOCUMENT (the screen -> Ledger.Core) and the PREVIEW (the ledger's dry run of it)
    ' ====================================================================

    Private Shared Function ParseOrderDate(text As String) As Date
        Dim d As DateTime
        If DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, d) Then Return d.Date
        Return Date.Today
    End Function

    Private Function BuildOrderDocument(st As OrderState) As OrderDocument
        Dim doc As New OrderDocument With {
            .OrderDate = ParseOrderDate(st.OrderDate), .OrderNo = st.OrderNo, .Source = st.Source,
            .Delivery = st.Delivery, .Vat = st.VAT}

        ' Items (one per column) and their quantities per line
        For c As Integer = 0 To st.Items.Count - 1
            Dim it As New OrderItem With {.Name = st.Items(c).Title, .UnitPrice = st.Items(c).Price,
                                          .UnitProfit = st.Items(c).Profit, .IsProfits = st.Items(c).IsProfits}
            For Each m As MemberLine In st.Members
                Dim q As Decimal = If(c < m.Qty.Count, m.Qty(c), 0D)
                If q <> 0D Then it.Quantities(m.LineKey) = q
            Next
            doc.Items.Add(it)
        Next

        ' Lines: every row with items, Credit or Withdraw; type, payer and fund from its Preview row
        For Each m As MemberLine In st.Members
            Dim hasItems As Boolean = m.Qty.Any(Function(q) q <> 0D)
            Dim credit As Decimal = If(m.IsShopLine, 0D, m.Credit)
            Dim debt As Decimal = If(m.IsShopLine, 0D, m.Withdraw)
            If Not hasItems AndAlso credit = 0D AndAlso debt = 0D Then Continue For

            Dim p As PreviewLine = st.Preview.FirstOrDefault(Function(x) Not x.AutoRow AndAlso SameName(x.LineKey, m.LineKey))
            Dim typeText As String = If(p Is Nothing OrElse m.IsShopLine, DefaultTypeOf(m), p.TxType)
            Dim line As New OrderLine With {.LineKey = m.LineKey, .MemberId = m.Id, .Withdraw = debt, .Credit = credit}
            Select Case typeText
                Case TxShopPurchase : line.LineType = OrderLineType.ShopPurchase
                Case TxShopSale : line.LineType = OrderLineType.ShopSale
                Case TxShopLoss : line.LineType = OrderLineType.ShopLoss
                Case Else : line.LineType = OrderLineType.Normal
            End Select

            If p IsNot Nothing Then
                If line.LineType = OrderLineType.ShopSale Then
                    If p.PaidFrom = OnCreditText Then
                        line.PayFromFundId = -1
                    ElseIf p.PaidFrom <> "" Then
                        line.PayFromFundId = FundIdByName(m.Id, p.PaidFrom)
                    End If
                ElseIf line.LineType <> OrderLineType.ShopLoss Then
                    Dim payerId As String = IdByName(p.Account)
                    If payerId IsNot Nothing AndAlso p.PaidFrom <> "" Then line.PaidFromFundId = FundIdByName(payerId, p.PaidFrom)
                End If
            End If
            If line.LineType = OrderLineType.Normal AndAlso Not m.IsShopLine AndAlso m.Splits.Count > 0 Then
                AddSplitLines(doc, m, line)
                Continue For
            End If
            doc.Lines.Add(line)
        Next
        Return doc
    End Function

    ' ---------- Split payment ----------
    ' A row paid by several accounts becomes one order line per payer: line keys <key>, <key>#2, <key>#3 ...
    ' Each takes its share (its amount / the total) of the row's items and Withdraw, so each payer pays
    ' its part and the member owes each of them that part. Credit stays on the first line.

    Private Const SplitKeySep As String = "#"

    Private Shared Function SplitLineKey(lineKey As String, index As Integer) As String
        Return If(index = 0, lineKey, lineKey & SplitKeySep & (index + 1).ToString(CultureInfo.InvariantCulture))
    End Function

    ''' <summary>Is this line key one of the row's split lines (the row's own key or key#n)?</summary>
    Private Shared Function IsKeyOfRow(key As String, rowKey As String) As Boolean
        Return SameName(key, rowKey) OrElse (key IsNot Nothing AndAlso key.StartsWith(rowKey & SplitKeySep, StringComparison.OrdinalIgnoreCase))
    End Function

    Private Sub AddSplitLines(doc As OrderDocument, m As MemberLine, template As OrderLine)
        Dim parts As List(Of SplitPart) = m.Splits.Where(Function(x) x.Amount > 0D).ToList()
        Dim total As Decimal = parts.Sum(Function(x) x.Amount)
        If parts.Count = 0 OrElse total <= 0D Then
            doc.Lines.Add(template)
            Return
        End If
        Dim givenQty(doc.Items.Count - 1) As Decimal
        Dim givenWithdraw As Decimal = 0D
        For i As Integer = 0 To parts.Count - 1
            Dim last As Boolean = (i = parts.Count - 1)
            Dim share As Decimal = parts(i).Amount / total
            Dim key As String = SplitLineKey(m.LineKey, i)
            Dim w As Decimal = If(last, template.Withdraw - givenWithdraw, Math.Round(template.Withdraw * share, 3))
            givenWithdraw += w
            Dim line As New OrderLine With {.LineKey = key, .MemberId = m.Id, .LineType = OrderLineType.Normal,
                                            .Withdraw = w, .Credit = If(i = 0, template.Credit, 0D)}
            Dim payerId As String = IdByName(parts(i).Account)
            If payerId IsNot Nothing AndAlso parts(i).PaidFrom <> "" Then line.PaidFromFundId = FundIdByName(payerId, parts(i).PaidFrom)
            For c As Integer = 0 To doc.Items.Count - 1
                Dim q As Decimal = If(c < m.Qty.Count, m.Qty(c), 0D)
                If q = 0D Then Continue For
                Dim qi As Decimal = If(last, q - givenQty(c), Math.Round(q * share, 6))
                givenQty(c) += qi
                If qi <> 0D Then doc.Items(c).Quantities(key) = qi Else doc.Items(c).Quantities.Remove(key)
            Next
            doc.Lines.Add(line)
        Next
    End Sub

    Private Sub BuildPreview()
        Dim st As OrderState = State
        st.Preview.Clear()

        For Each m As MemberLine In st.Members
            If m.Name.Trim() = "" Then Continue For

            ' Items: "<qty> x <item> (<qty> x <price>)", one per line
            Dim lines As New List(Of String)
            For c As Integer = 0 To st.Items.Count - 1
                Dim q As Decimal = If(c < m.Qty.Count, m.Qty(c), 0D)
                If q = 0D Then Continue For
                lines.Add(Qty(q) & " x " & st.Items(c).Title & " (" & Qty(q) & " x " & Money(st.Items(c).Price) & ")")
            Next
            If lines.Count = 0 AndAlso m.Credit = 0D AndAlso m.Withdraw = 0D Then Continue For
            If m.Withdraw <> 0D Then lines.Add("-Withdraw " & Money(m.Withdraw))
            If m.Credit <> 0D Then lines.Add("-Credit " & Money(m.Credit))

            Dim p As New PreviewLine With {
                .MemberId = m.Id, .LineKey = m.LineKey, .Name = m.Name,
                .Credit = m.Credit, .Debt = m.Withdraw, .Lines = String.Join(vbLf, lines), .TxType = DefaultTypeOf(m)}
            If m.IsShopLine Then
                p.TypeLocked = True
                If p.TxType = TxShopLoss Then p.AccountLocked = True Else p.Account = LedgerName(st.ShopId)   ' the shop pays for its stock
            Else
                p.Account = m.Name   ' who pays: the member themselves (change it to another member who paid)
            End If
            RefreshPaidFrom(p)
            st.Preview.Add(p)
        Next

        st.PreviewBuilt = True
        RecalculatePreview()
    End Sub

    ''' <summary>Paid From list of a row: the payer's funds (the buyer's funds + "(on credit)" for a Shop Sale).</summary>
    Private Sub RefreshPaidFrom(p As PreviewLine)
        Dim options As New List(Of String)
        If If(p.Account, "").EndsWith(OnAccountSuffix, StringComparison.OrdinalIgnoreCase) Then
            p.PaidFromOptions = New List(Of String) From {"(owes the shop)"}
            p.PaidFrom = "(owes the shop)"
            Return
        End If
        If Not p.AutoRow AndAlso p.TxType <> TxShopLoss Then
            Dim payerId As String = If(p.TxType = TxShopSale, p.MemberId, IdByName(p.Account))
            If Not String.IsNullOrEmpty(payerId) Then options.AddRange(FundsOf(payerId).Select(Function(a) a.Name))
            If p.TxType = TxShopSale Then
                options.Add(OnCreditText)
            ElseIf Not String.IsNullOrEmpty(payerId) Then
                ' bank loans / cards: paying with one raises what is owed on it (no Borrow step)
                options.AddRange(LoansOf(payerId).Select(Function(a) a.Name & LoanSuffix))
            End If
        End If
        p.PaidFromOptions = options
        If Not options.Contains(If(p.PaidFrom, "")) Then p.PaidFrom = If(options.Count > 0, options(0), "")
    End Sub

    ''' <summary>
    ''' The Preview figures from the ledger's dry run of the order (exactly what Process saves).
    ''' ex* = the member's balances now; the right-hand figures after.
    ''' </summary>
    Private Sub RecalculatePreview()
        Dim st As OrderState = State
        Dim preview As PostingPreview = Nothing
        Dim problem As String = Nothing
        Dim shop As ShopOrderAccounts = ShopOrderOf(st)
        If shop IsNot Nothing Then
            RecalculateShopPreview(st, shop)   ' a shop order: two grids
            Return
        End If
        Try
            Dim doc As OrderDocument = BuildOrderDocument(st)
            If doc.Lines.Count > 0 Then preview = Ledger.PreviewOrder(doc)
        Catch ex As LedgerException
            problem = ex.Message
        Catch ex As Exception
            problem = "The Preview could not be calculated: " & ex.Message
        End Try

        ' Changes per member
        Dim changes As New Dictionary(Of String, MemberFigures)(StringComparer.OrdinalIgnoreCase)
        If preview IsNot Nothing Then
            For Each kv As KeyValuePair(Of String, MemberFigures) In preview.Changes
                changes(kv.Key) = kv.Value
            Next
        End If

        Dim charges As New Dictionary(Of String, LineCharge)(StringComparer.OrdinalIgnoreCase)
        If preview IsNot Nothing Then
            For Each c As LineCharge In preview.Charges
                charges(c.Line.LineKey) = c
            Next
        End If
        Dim before As Dictionary(Of String, MemberFigures) = SummariesById()

        ' A grey row for every member the order changes who has no line of their own; old ones go
        For Each id As String In changes.Keys.ToList()
            If st.Preview.Any(Function(x) SameName(x.MemberId, id) AndAlso (x.AutoRow OrElse SameName(x.LineKey, id))) Then Continue For
            st.Preview.Add(New PreviewLine With {.MemberId = id, .Name = LedgerName(id), .AutoRow = True})
        Next
        st.Preview.RemoveAll(Function(x) x.AutoRow AndAlso Not changes.ContainsKey(x.MemberId))

        For Each p As PreviewLine In st.Preview
            Dim c As LineCharge = Nothing
            If p.AutoRow Then
                p.Total = Nothing
            ElseIf charges.TryGetValue(p.LineKey, c) Then
                ' A split row: the total of all its lines (<key>, <key>#2 ...)
                Dim rowKey As String = p.LineKey
                p.Total = charges.Values.Where(Function(x) IsKeyOfRow(x.Line.LineKey, rowKey)).
                                         Sum(Function(x) If(x.IsExternal, x.Total, x.Items))
            Else
                p.Total = 0D
            End If

            ' Member figures on the member's own row (the shop's goods lines: on the Shop's row)
            p.ExBalance = Nothing : p.ExCash = Nothing : p.ExExpense = Nothing : p.ExReceivable = Nothing : p.ExPayable = Nothing
            p.NextBalance = Nothing : p.Cash = Nothing : p.Expense = Nothing : p.Receivable = Nothing : p.Payable = Nothing
            If Not (p.AutoRow OrElse SameName(p.LineKey, p.MemberId)) Then Continue For

            Dim b As MemberFigures = FiguresOf(before, p.MemberId)
            Dim a As MemberFigures = Plus(b, FiguresOf(changes, p.MemberId))
            p.ExBalance = Bal(b) : p.ExCash = CashOf(b) : p.ExExpense = b.Expense : p.ExReceivable = b.OwedToThem : p.ExPayable = b.TheyOwe
            p.NextBalance = Bal(a) : p.Cash = CashOf(a) : p.Expense = a.Expense : p.Receivable = a.OwedToThem : p.Payable = a.TheyOwe
        Next

        ' Grey rows (a payer without a line of its own): what it pays, for whom, and from which fund
        For Each p As PreviewLine In st.Preview.Where(Function(x) x.AutoRow)
            Dim paid As New List(Of String)
            If preview IsNot Nothing Then
                For Each c As LineCharge In preview.Charges
                    If c.Line Is Nothing OrElse c.Line.PaidFromFundId = 0 Then Continue For
                    Dim fund As Account = LedgerAccounts().FirstOrDefault(Function(x) x.Id = c.Line.PaidFromFundId)
                    If fund Is Nothing OrElse Not SameName(fund.MemberId, p.MemberId) Then Continue For
                    paid.Add("Pays " & Money(If(c.IsExternal, c.Total, c.Items)) & " for " & LedgerName(c.Line.MemberId) & " (from " & fund.Name & ")")
                Next
            End If
            p.Lines = String.Join(vbLf, paid)
        Next

        ' Split payment: the payers' amounts must add up to the row's total
        If problem Is Nothing AndAlso preview IsNot Nothing Then
            For Each p As PreviewLine In st.Preview.Where(Function(x) Not x.AutoRow)
                Dim m As MemberLine = st.Members.FirstOrDefault(Function(x) SameName(x.LineKey, p.LineKey))
                If m Is Nothing OrElse m.Splits.Count = 0 OrElse p.TxType <> TxNormal Then Continue For
                Dim given As Decimal = m.Splits.Sum(Function(x) x.Amount)
                Dim due As Decimal = If(p.Total, 0D)
                If Math.Abs(given - due) > 0.0015D Then
                    problem = "the split payment of " & p.Name & " adds up to " & Money(given) & " but the line is " & Money(due) &
                              ": click + on that row to fix it"
                    Exit For
                End If
            Next
        End If

        ' Status bar
        If problem IsNot Nothing Then
            st.StatusKind = "bad"
            st.StatusText = ChrW(&H2716) & "  Cannot process " & ChrW(&H2013) & " " & problem
        ElseIf preview Is Nothing Then
            st.StatusKind = "warn"
            st.StatusText = ChrW(&H2139) & "  Nothing to process: no items, Credit or Withdraw."
        Else
            Dim parts As New List(Of String)
            parts.Add("to pay the vendor " & Money(preview.ExternalTotal))
            Dim negative As List(Of String) = st.Preview.Where(Function(p) p.Cash.HasValue AndAlso p.Cash.Value < 0D).Select(Function(p) p.Name).ToList()
            st.StatusKind = "ok"
            st.StatusText = ChrW(&H2714) & "  Ready to process " & ChrW(&H2013) & " " & String.Join(", ", parts) &
                            If(negative.Count > 0, "   (cash below zero after this: " & String.Join(", ", negative) & ")", "")
        End If
        st.PreviewCurrent = True
    End Sub

    Private Sub MarkPreviewOutdated()
        Dim st As OrderState = State
        If Not st.PreviewCurrent Then Return
        st.PreviewCurrent = False
        st.StatusKind = "warn"
        st.StatusText = ChrW(&H26A0) & "  Order changed " & ChrW(&H2013) & " click Calculate again to check"
    End Sub

    ''' <summary>The Profits amount charged on a line (0 without a Profits item).</summary>
    Private Function ProfitOfLine(st As OrderState, lineKey As String) As Decimal
        Dim pIdx As Integer = ProfitsIndex(st)
        If pIdx < 0 Then Return 0D
        Dim m As MemberLine = st.Members.FirstOrDefault(Function(x) SameName(x.LineKey, lineKey))
        If m Is Nothing OrElse pIdx >= m.Qty.Count Then Return 0D
        Return m.Qty(pIdx)
    End Function

    ' ---------- Order TOTAL = item subtotals + all Withdraw + Delivery + VAT ----------

    Private Function ItemCount(st As OrderState, c As Integer) As Decimal
        Return st.Members.Sum(Function(x) If(c < x.Qty.Count, x.Qty(c), 0D))
    End Function

    Private Function ItemsTotal(st As OrderState) As Decimal
        Dim t As Decimal = 0D
        For c As Integer = 0 To st.Items.Count - 1
            t += ItemCount(st, c) * st.Items(c).Price
        Next
        Return t
    End Function

    Private Function OrderTotal(st As OrderState) As Decimal
        Return ItemsTotal(st) + st.Members.Sum(Function(x) x.Withdraw) + st.Delivery + st.VAT
    End Function

    Private Function IdByName(memberName As String) As String
        If String.IsNullOrEmpty(memberName) Then Return Nothing
        Dim m As MemberLine = State.Members.FirstOrDefault(Function(x) String.Equals(x.Name, memberName, StringComparison.OrdinalIgnoreCase))
        Return If(m Is Nothing OrElse m.Id = "", Nothing, m.Id)
    End Function

    Private Function IsSettlementItem(itemName As String) As Boolean
        Return If(itemName, "").StartsWith(SettlementPrefix, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function SavedItemLine(itemName As String, q As Decimal, price As Decimal) As String
        If IsSettlementItem(itemName) Then Return itemName & " " & Money(q * price)
        Return Qty(q) & " x " & itemName & " (" & Qty(q) & " x " & Money(price) & ")"
    End Function

    ' ====================================================================
    '  PROCESS: the order and its journal in one transaction (Ledger.PostOrder)
    ' ====================================================================

    Protected Sub btnProcess_Click(sender As Object, e As EventArgs) Handles btnProcess.Click
        If Not IsAdmin Then Return   ' administrator only
        Dim st As OrderState = State
        If IsViewing Then Return
        If Not st.PreviewCurrent Then
            ShowMsg("The order changed since the last Calculate. Click Calculate first.", True)
            Return
        End If

        ' Check everything before asking for the title
        Dim doc As OrderDocument = BuildOrderDocument(st)
        If doc.Lines.Count = 0 Then
            ShowMsg("There is nothing to save.", True)
            Return
        End If
        Dim shop As ShopOrderAccounts = ShopOrderOf(st)
        Try
            If shop IsNot Nothing Then
                Dim r As ShopPosting = BuildShopPosting(st, shop)
                For Each jj As Journal In {r.Grid1, r.Grid2, r.Grid3, r.Grid4}
                    If jj.Entries.Count > 0 Then Ledger.Preview(jj)
                Next
            Else
                Ledger.PreviewOrder(doc)
            End If
        Catch ex As LedgerException
            ShowMsg("Nothing can be saved: " & ex.Message, True)
            Return
        End Try

        lblProcessInfo.Text = Server.HtmlEncode(doc.Lines.Count & " order line(s) will be saved.")

        ' Members charged a profit should normally be buying from the shop
        Dim warnings As New List(Of String)
        Dim profitNoSale As List(Of String) = st.Preview.
            Where(Function(p) shop Is Nothing AndAlso Not p.AutoRow AndAlso p.TxType <> TxShopSale AndAlso ProfitOfLine(st, p.LineKey) <> 0D).
            Select(Function(p) p.Name).ToList()
        If profitNoSale.Count > 0 Then
            warnings.Add("Charged Profits but not """ & TxShopSale & """: " & String.Join(", ", profitNoSale) & vbLf &
                         "The shop will not receive their payment, and its stock and profit will not change.")
        End If
        lblProcessWarn.Text = Server.HtmlEncode(String.Join(vbLf & vbLf, warnings))

        ' Suggested title: yyyyMMdd OrderSource HHmm
        Dim suggested As String = ParseOrderDate(st.OrderDate).ToString("yyyyMMdd", CultureInfo.InvariantCulture) & " " &
                                  st.Source & " " & OrderTimeSuffix(st.OrderNo)
        txtOrderTitle.Text = String.Join(" ", suggested.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries))
        If shop IsNot Nothing Then
            lblProcessInfo.Text = Server.HtmlEncode("A shop order: four entries are saved, named after the title, e.g. " &
                                                    ShopStepTitle(txtOrderTitle.Text, "_01Buying") & ", " & ShopStepTitle(txtOrderTitle.Text, "_02Selling") & ", " &
                                                    ShopStepTitle(txtOrderTitle.Text, "_03Loss") & ", " & ShopStepTitle(txtOrderTitle.Text, "_04Zeroing") &
                                                    " (an empty one is skipped).")
        End If

        pnlProcess.Visible = True
        txtOrderTitle.Focus()
    End Sub

    Protected Sub btnProcessCancel_Click(sender As Object, e As EventArgs) Handles btnProcessCancel.Click
        pnlProcess.Visible = False
    End Sub

    Protected Sub btnProcessSave_Click(sender As Object, e As EventArgs) Handles btnProcessSave.Click
        Dim title As String = txtOrderTitle.Text.Trim()
        If title = "" Then
            ShowMsg("Please enter an order title.", True)
            pnlProcess.Visible = True
            Return
        End If
        pnlProcess.Visible = False
        DoProcess(title)
    End Sub

    ''' <summary>
    ''' Saves the order through the ledger: Orders, Order_Items, Order_Member_Items, Order_Members, Order_Payments
    ''' and its journal, in one transaction.
    ''' </summary>
    Private Sub DoProcess(orderTitle As String)
        If Not IsAdmin Then Return   ' administrator only
        Dim st As OrderState = State
        If Not st.PreviewCurrent OrElse IsViewing Then Return

        Dim doc As OrderDocument = BuildOrderDocument(st)
        doc.Title = orderTitle
        If doc.Lines.Count = 0 Then Return
        Dim savedId As String = ""
        Try
            Dim shop As ShopOrderAccounts = ShopOrderOf(st)
            If shop IsNot Nothing Then
                ' A shop order: the four grids, one after the other, each as its own order
                Dim r As ShopPosting = BuildShopPosting(st, shop)
                Dim steps As New List(Of Tuple(Of Journal, OrderDocument, String)) From {
                    Tuple.Create(r.Grid1, r.BuyDoc, "_01Buying"), Tuple.Create(r.Grid2, r.SellDoc, "_02Selling"),
                    Tuple.Create(r.Grid3, r.LossDoc, "_03Loss"), Tuple.Create(r.Grid4, r.ZeroDoc, "_04Zeroing")}
                Dim saved As New List(Of String)
                For Each stp As Tuple(Of Journal, OrderDocument, String) In steps
                    If stp.Item1.Entries.Count = 0 Then Continue For
                    Try
                        stp.Item2.Title = ShopStepTitle(orderTitle, stp.Item3)
                        stp.Item1.Memo = stp.Item2.Title
                        Ledger.Post(stp.Item1, stp.Item2)
                        saved.Add(stp.Item2.OrderId & " """ & stp.Item2.Title & """")
                    Catch ex As Exception
                        ResetLedgerCache()
                        ShowMsg("Saved: " & If(saved.Count = 0, "nothing", String.Join(", ", saved)) & ". Not saved: """ & ShopStepTitle(orderTitle, stp.Item3) &
                                """ and after it - " & ex.Message, True)
                        Return
                    End Try
                Next
                savedId = String.Join(", ", saved)
            Else
                Ledger.PostOrder(doc)
                savedId = doc.OrderId
            End If
        Catch ex As Exception
            ResetLedgerCache()
            ShowMsg("Nothing was saved: " & ex.Message, True)
            Return
        End Try

        ' Saved: loans follow (_Spent = _Have - limit), then a fresh order (members reload with their new figures)
        ResetLedgerCache()
        Dim loanNote As String = ""
        Try
            SyncLoanPairs()
        Catch ex As Exception
            loanNote = " The loan accounts were not updated: " & ex.Message
        End Try
        ResetOrder()
        If savedId.Contains("""") Then
            ShowMsg("Orders saved: " & savedId & "." & loanNote, loanNote <> "")   ' a shop order: four entries
        Else
            ShowMsg("Order " & savedId & " """ & orderTitle & """ saved." & loanNote, loanNote <> "")
        End If
    End Sub

    ' ====================================================================
    '  LOAD PREVIOUS ORDER (read-only view until New Order) and its cancellation status
    ' ====================================================================

    Private Function OrderJournal(orderId As String) As Journal
        Return Ledger.Store.GetJournals().FirstOrDefault(Function(j) j.JournalType = JournalTypes.Order AndAlso SameName(j.OrderId, orderId))
    End Function

    Protected Sub btnLoadPrevious_Click(sender As Object, e As EventArgs) Handles btnLoadPrevious.Click
        Try
            Dim dt As DataTable = OrdersDB.GetDataTable("SELECT Order_ID, Order_Date, Order_No, Order_Source FROM Orders ORDER BY Val(Order_ID & '') DESC")
            If dt.Rows.Count = 0 Then
                ShowMsg("There are no saved orders yet.", True)
                Return
            End If
            Dim cancelled As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim q As LedgerQueries = LQ()
            For Each j As Journal In Ledger.Store.GetJournals().Where(Function(x) x.JournalType = JournalTypes.Order)
                If q.IsCancelled(j.Id) Then cancelled.Add(j.OrderId)
            Next
            dt.Columns.Add("Status", GetType(String))
            For Each r As DataRow In dt.Rows
                r("Status") = If(cancelled.Contains(r("Order_ID").ToString()), "Cancelled", "")
            Next
            gvOrders.DataSource = dt
            gvOrders.DataBind()
            pnlLoad.Visible = True
        Catch ex As Exception
            ShowMsg("Could not read the orders: " & ex.Message, True)
        End Try
    End Sub

    Protected Sub btnLoadCancel_Click(sender As Object, e As EventArgs) Handles btnLoadCancel.Click
        pnlLoad.Visible = False
    End Sub

    Protected Sub gvOrders_RowCommand(sender As Object, e As GridViewCommandEventArgs) Handles gvOrders.RowCommand
        If Not IsAdmin Then Return   ' administrator only
        If e.CommandName <> "OpenOrder" Then Return
        pnlLoad.Visible = False
        LoadSavedOrder(Convert.ToString(e.CommandArgument))
    End Sub

    Private Shared Function LineTypeText(saved As String) As String
        Select Case saved
            Case OrderLineType.ShopPurchase.ToString() : Return TxShopPurchase
            Case OrderLineType.ShopSale.ToString() : Return TxShopSale
            Case OrderLineType.ShopLoss.ToString() : Return TxShopLoss
            Case Else : Return TxNormal
        End Select
    End Function

    ''' <summary>The grid line a saved line belongs to: the shop's goods lines, or the member's own line.</summary>
    Private Function LineForSaved(st As OrderState, memberId As String, lineType As String) As MemberLine
        If lineType = OrderLineType.ShopPurchase.ToString() Then Return st.Members.FirstOrDefault(Function(x) x.IsShopLine AndAlso SameName(x.Name, ShopStockName))
        If lineType = OrderLineType.ShopLoss.ToString() Then Return st.Members.FirstOrDefault(Function(x) x.IsShopLine AndAlso SameName(x.Name, ShopLossName))
        Return st.Members.FirstOrDefault(Function(x) Not x.IsShopLine AndAlso x.Id = memberId)
    End Function

    Private Sub LoadSavedOrder(orderId As String)
        Dim idSql As String = OrdersDB.SqlText(orderId)
        Dim ord, items, qtys, oms As DataTable
        Try
            ord = OrdersDB.GetDataTable("SELECT * FROM Orders WHERE Order_ID = " & idSql)
            items = OrdersDB.GetDataTable("SELECT * FROM Order_Items WHERE Order_ID = " & idSql & " ORDER BY Val(OrderItem_ID & '')")
            qtys = OrdersDB.GetDataTable("SELECT * FROM Order_Member_Items WHERE Order_ID = " & idSql)
            oms = OrdersDB.GetDataTable("SELECT * FROM Order_Members WHERE Order_ID = " & idSql)
        Catch ex As Exception
            ShowMsg("Could not load the order: " & ex.Message, True)
            Return
        End Try
        If ord.Rows.Count = 0 Then Return
        Dim o As DataRow = ord.Rows(0)

        ResetOrder(includeFrozen:=True)   ' a saved order shows everyone in it, frozen or not
        Dim st As OrderState = State

        ' Header
        st.Source = ColText(o, "Order_Source")
        Dim d As DateTime
        st.OrderDate = If(DateTime.TryParse(ColText(o, "Order_Date"), CultureInfo.InvariantCulture, DateTimeStyles.None, d),
                          d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), ColText(o, "Order_Date"))
        st.OrderNo = ColText(o, "Order_No")
        st.Delivery = DbDecimal(o("Delivery_Charge"))
        st.VAT = DbDecimal(o("VAT"))
        st.SavedTotal = DbDecimal(o("Order_Total"))

        ' Item columns (with the saved Profit per unit) and quantities per line
        Dim colByItem As New Dictionary(Of String, Integer)
        Dim itemInfo As New Dictionary(Of String, Tuple(Of String, Decimal))
        For Each it As DataRow In items.Rows
            Dim title As String = ColText(it, "Item_Name")
            itemInfo(it("OrderItem_ID").ToString()) = Tuple.Create(title, DbDecimal(it("Unit_Price")))
            If IsSettlementItem(title) Then Continue For
            AddItemColumn(title, DbDecimal(it("Unit_Price")))
            Dim col As ItemCol = st.Items(st.Items.Count - 1)
            col.Profit = OptDecimal(it, "Unit_Profit")
            col.IsProfits = items.Columns.Contains("Is_Profits") AndAlso Not IsDBNull(it("Is_Profits")) AndAlso CBool(it("Is_Profits"))
            colByItem(it("OrderItem_ID").ToString()) = st.Items.Count - 1
        Next
        For Each qr As DataRow In qtys.Rows
            Dim col As Integer
            Dim m As MemberLine = LineForSaved(st, qr("Member_ID").ToString(), ColText(qr, "Line_Type"))
            ' added up: a split payment saved the member's quantities on several lines
            If m IsNot Nothing AndAlso colByItem.TryGetValue(qr("OrderItem_ID").ToString(), col) Then m.Qty(col) += DbDecimal(qr("Quantity"))
        Next

        ' Credit and Withdraw as saved
        For Each om As DataRow In oms.Rows
            Dim m As MemberLine = LineForSaved(st, om("Member_ID").ToString(), ColText(om, "Transaction_Type"))
            If m Is Nothing OrElse m.IsShopLine Then Continue For
            m.Credit += DbDecimal(om("Credit"))       ' added up (a split payment: several lines)
            m.Withdraw += DbDecimal(om("Withdraw"))
        Next

        ' Preview: the saved lines, and every member's change in the order's journal
        Dim journal As Journal = Nothing
        Try
            journal = OrderJournal(orderId)
            FillPreviewFromJournal(st, journal, oms, qtys, itemInfo)
        Catch ex As Exception
            ShowMsg("The order's figures could not be read from the ledger: " & ex.Message, True)
        End Try
        Dim inOrder As New HashSet(Of String)(st.Preview.Select(Function(p) p.MemberId), StringComparer.OrdinalIgnoreCase)
        For Each m As MemberLine In st.Members
            m.InViewedOrder = inOrder.Contains(m.Id)
        Next

        ' Read-only until New Order
        st.ViewingOrderId = orderId
        st.IsCancelled = journal IsNot Nothing AndAlso LQ().IsCancelled(journal.Id)
        Dim title2 As String = If(ord.Columns.Contains("Order_Title"), CleanTitle(ColText(o, "Order_Title")), "")
        st.StatusKind = If(st.IsCancelled, "bad", "info")
        st.StatusText = ChrW(&H2139) & "  Viewing saved order " & orderId & If(title2 <> "", " " & ChrW(&H2013) & " " & title2, "") &
                        If(st.IsCancelled, "  (CANCELLED)", "") & " (read-only). Click New Order to start a new one."
        mvOrder.ActiveViewIndex = 0
    End Sub

    Private Sub FillPreviewFromJournal(st As OrderState, journal As Journal, oms As DataTable, qtys As DataTable,
                                       itemInfo As Dictionary(Of String, Tuple(Of String, Decimal)))
        st.Preview.Clear()
        For Each om As DataRow In oms.Rows
            Dim memberId As String = om("Member_ID").ToString()
            Dim lt As String = ColText(om, "Transaction_Type")
            Dim notes() As String = ColText(om, "Notes").Split("|"c)   ' "lineKey|payerFundId"
            Dim key As String = If(notes(0).Trim() <> "", notes(0).Trim(), memberId)
            Dim fundId As Integer = 0
            If notes.Length > 1 Then Integer.TryParse(notes(1), fundId)

            Dim lines As New List(Of String)
            Dim isSplitPart As Boolean = key.Contains(SplitKeySep)
            If isSplitPart Then lines.Add("Split payment: share " & Money(DbDecimal(om("Items_Amount"))))
            For Each qr As DataRow In qtys.Rows
                If isSplitPart Then Exit For
                If qr("Member_ID").ToString() <> memberId Then Continue For
                Dim qlt As String = ColText(qr, "Line_Type")
                If qlt <> "" AndAlso qlt <> lt Then Continue For
                Dim info As Tuple(Of String, Decimal) = Nothing
                If itemInfo.TryGetValue(qr("OrderItem_ID").ToString(), info) Then lines.Add(SavedItemLine(info.Item1, DbDecimal(qr("Quantity")), info.Item2))
            Next
            If Not isSplitPart AndAlso oms.Rows.Cast(Of DataRow)().Any(Function(r) ColText(r, "Notes").StartsWith(key & SplitKeySep, StringComparison.OrdinalIgnoreCase)) Then
                lines.Add("Split payment: this line's share " & Money(DbDecimal(om("Items_Amount"))))
            End If
            Dim credit As Decimal = DbDecimal(om("Credit")), debt As Decimal = DbDecimal(om("Withdraw"))
            If debt <> 0D Then lines.Add("-Withdraw " & Money(debt))
            If credit <> 0D Then lines.Add("-Credit " & Money(credit))

            Dim name As String = If(lt = OrderLineType.ShopPurchase.ToString(), ShopStockName,
                                    If(lt = OrderLineType.ShopLoss.ToString(), ShopLossName, LedgerName(memberId)))
            Dim payer As String = ColText(om, "Account_ID").Trim()
            Dim p As New PreviewLine With {
                .MemberId = memberId, .LineKey = key, .Name = name, .Credit = credit, .Debt = debt,
                .Total = DbDecimal(om("Items_Amount")) + debt, .Lines = String.Join(vbLf, lines),
                .TxType = LineTypeText(lt), .Account = If(payer = "", "", LedgerName(payer))}
            If fundId <> 0 Then
                Dim paidWith As Account = LedgerAccounts().FirstOrDefault(Function(x) x.Id = fundId)
                p.PaidFrom = If(paidWith Is Nothing, "", paidWith.Name & If(paidWith.Kind = AccountKind.Loan, LoanSuffix, ""))
                p.PaidFromOptions = New List(Of String) From {p.PaidFrom}
            End If
            st.Preview.Add(p)
        Next
        If journal Is Nothing Then Return

        ' Before / after of every member in the journal, from their statement
        Dim q As LedgerQueries = LQ()
        For Each memberId As String In journal.Entries.Select(Function(x) x.MemberId).Distinct(StringComparer.OrdinalIgnoreCase)
            Dim sl As StatementLine = q.Statement(memberId).FirstOrDefault(Function(x) x.Journal.Id = journal.Id)
            If sl Is Nothing Then Continue For
            Dim b As MemberFigures = Minus(sl.After, sl.Change)
            Dim p As PreviewLine = st.Preview.FirstOrDefault(Function(x) SameName(x.MemberId, memberId) AndAlso SameName(x.LineKey, memberId))
            If p Is Nothing Then
                p = New PreviewLine With {.MemberId = memberId, .Name = LedgerName(memberId), .AutoRow = True}
                st.Preview.Add(p)
            End If
            p.ExBalance = Bal(b) : p.ExCash = CashOf(b) : p.ExExpense = b.Expense : p.ExReceivable = b.OwedToThem : p.ExPayable = b.TheyOwe
            p.NextBalance = Bal(sl.After) : p.Cash = CashOf(sl.After) : p.Expense = sl.After.Expense
            p.Receivable = sl.After.OwedToThem : p.Payable = sl.After.TheyOwe
            Dim m As MemberLine = st.Members.FirstOrDefault(Function(x) Not x.IsShopLine AndAlso SameName(x.Id, memberId))
            If m IsNot Nothing Then m.ExBalance = Bal(b)   ' the Order grid shows the balance before this order
        Next
    End Sub

    ''' <summary>"20261101 Carfour 0931" -> "Carfour" (date prefix and time suffix removed).</summary>
    ' The time suffix (HHmm) of a suggested title: the order's own time from Order No. ("13:25", "1325", "1:25 PM"),
    ' or the time now only when Order No. holds no time
    Private Shared Function OrderTimeSuffix(orderNo As String) As String
        Dim t As String = If(orderNo, "").Trim()
        Dim parsed As DateTime
        Dim formats As String() = {"H:mm", "HH:mm", "HHmm", "Hmm", "h:mm tt", "h:mmtt", "hh:mm tt", "H.mm", "HH.mm"}
        If DateTime.TryParseExact(t, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, parsed) OrElse
           DateTime.TryParseExact(t.ToUpperInvariant(), formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, parsed) Then
            Return parsed.ToString("HHmm", CultureInfo.InvariantCulture)
        End If
        Return DateTime.Now.ToString("HHmm", CultureInfo.InvariantCulture)
    End Function

    Private Function CleanTitle(title As String) As String
        Dim t As String = If(title, "").Trim()
        t = System.Text.RegularExpressions.Regex.Replace(t, "^\d{8}[\s\-_]*", "")
        t = System.Text.RegularExpressions.Regex.Replace(t, "[\s\-_]*\d{4}$", "")
        Return If(t = "", If(title, "").Trim(), t)
    End Function
    ' ====================================================================
    '  SHOP RESULT (Order tab header: Shop_Profit - Shop_Loss)
    ' ====================================================================
    ''' <summary>The shop's Profit (income) and Loss (expense) to date. False if there is no shop (or the ledger cannot be read).</summary>
    Private Function ReadShopResult(ByRef profit As Decimal, ByRef loss As Decimal) As Boolean
        profit = 0D : loss = 0D
        Try
            Dim shopId As String = FindShopId()
            If shopId = "" Then Return False
            For Each b As AccountBalance In LQ().Balances(shopId)
                If b.Account.Kind = AccountKind.Income AndAlso SameName(b.Account.Name, AccountNames.ShopProfit) Then profit += b.Amount
                If b.Account.Kind = AccountKind.Expense AndAlso SameName(b.Account.Name, AccountNames.ShopLoss) Then loss += b.Amount
            Next
            Return True
        Catch
            Return False
        End Try
    End Function

    ' ====================================================================
    '  ITEMS
    '  What was bought in all saved orders (Order_Member_Items + Order_Items), for All or one member:
    '  Item | Quantity | Amount (quantity x unit price), largest amount first. Settlements are not items.
    ' ====================================================================

    Protected Sub btnItemsRefresh_Click(sender As Object, e As EventArgs) Handles btnItemsRefresh.Click
        ' rebuilt in PreRender
    End Sub

    ''' <summary>Fills a member drop-down (first item given), keeping the current choice when possible.</summary>
    Private Sub FillMemberFilter(ddl As DropDownList, firstText As String, firstValue As String)
        Dim current As String = ddl.SelectedValue
        ddl.Items.Clear()
        ddl.Items.Add(New ListItem(firstText, firstValue))
        ' People (with all their accounts) and the shop; not the accounts themselves, not banks
        For Each m As Member In LedgerMembers().Where(Function(x) x.IsActive AndAlso x.MemberType <> MemberTypes.Bank AndAlso Not IsAccountMember(x)).
                                                OrderBy(Function(x) x.Name)
            ddl.Items.Add(New ListItem(m.Name, m.Id))
        Next
        Dim it As ListItem = ddl.Items.FindByValue(If(current, ""))
        If it IsNot Nothing Then ddl.SelectedValue = it.Value
    End Sub

    Private Sub RefreshItems()
        Dim dt As DataTable
        Try
            FillMemberFilter(ddlItemsMember, "All", "*")
            dt = OrdersDB.GetDataTable(
                "SELECT oi.Item_Name, omi.Member_ID, omi.Quantity, oi.Unit_Price " &
                "FROM Order_Member_Items AS omi INNER JOIN Order_Items AS oi ON omi.OrderItem_ID = oi.OrderItem_ID")
        Catch ex As Exception
            lblItemsMsg.Text = Server.HtmlEncode("Could not read the items: " & ex.Message)
            lblItemsMsg.Visible = True
            litItems.Text = ""
            Return
        End Try

        Dim memberKey As String = ddlItemsMember.SelectedValue
        ' A person: their own rows and all their accounts' rows
        Dim keys As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {memberKey}
        If memberKey <> "*" Then
            For Each a As Member In LedgerMembers().Where(Function(x) SameName(OwnerOf(x.Id), memberKey))
                keys.Add(a.Id)
            Next
        End If
        Dim qtyByItem As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        Dim amountByItem As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        For Each dr As DataRow In dt.Rows
            If memberKey <> "*" AndAlso Not keys.Contains(dr("Member_ID").ToString()) Then Continue For
            Dim itemName As String = ColText(dr, "Item_Name").Trim()
            If itemName = "" OrElse IsSettlementItem(itemName) Then Continue For
            Dim q As Decimal = DbDecimal(dr("Quantity"))
            AddTo(qtyByItem, itemName, q)
            AddTo(amountByItem, itemName, q * DbDecimal(dr("Unit_Price")))
        Next

        Dim sb As New System.Text.StringBuilder()
        sb.Append("<table class=""items-grid""><tr><th>Item</th><th class=""r"">Quantity</th><th class=""r"">Amount</th></tr>")
        Dim totalQty As Decimal = 0D, totalAmount As Decimal = 0D
        For Each kv As KeyValuePair(Of String, Decimal) In amountByItem.OrderByDescending(Function(x) x.Value)
            Dim q As Decimal = qtyByItem(kv.Key)
            sb.AppendFormat("<tr><td>{0}</td><td class=""r"">{1}</td><td class=""r b"">{2}</td></tr>",
                            Server.HtmlEncode(kv.Key), Qty(q), Money(kv.Value))
            totalQty += q : totalAmount += kv.Value
        Next
        sb.AppendFormat("<tr class=""total""><td>Total</td><td class=""r"">{0}</td><td class=""r"">{1}</td></tr></table>",
                        Qty(totalQty), Money(totalAmount))
        litItems.Text = sb.ToString()
    End Sub

    ' ====================================================================
    '  MATRICES (administrator): who must repay whom, and who lent to whom, per person
    '  From the ledger's debts between accounts (PairwiseDebts: lender -> borrower -> amount), added up per
    '  person (an account counts for its owner). When two people owe each other, only the difference shows.
    '    <Person> Repay: (total)    the people they owe, each with the amount
    '    <Person> Lend:  (total)    the people who owe them, each with the amount
    '  A Member list (All first) shows one person only.
    ' ====================================================================

    ''' <summary>The person an account belongs to (anyone else: themselves).</summary>
    Private Function PersonOf(memberId As String) As String
        Dim owner As String = OwnerOf(memberId)
        Return If(owner <> "", owner, memberId)
    End Function

    Private Sub RefreshMatrices()
        litMatrices.Text = ""
        Try
            ' Member list: All, then the people (keeps the pick)
            Dim current As String = ddlMxMember.SelectedValue
            ddlMxMember.Items.Clear()
            ddlMxMember.Items.Add(New ListItem("All", ""))
            For Each m As Member In LedgerMembers().Where(Function(x) x.IsActive AndAlso x.MemberType <> MemberTypes.Bank AndAlso Not IsAccountMember(x)).
                                                    OrderBy(Function(x) x.Name)
                ddlMxMember.Items.Add(New ListItem(m.Name, m.Id))
            Next
            Dim it As ListItem = ddlMxMember.Items.FindByValue(If(current, ""))
            If it IsNot Nothing Then ddlMxMember.SelectedValue = it.Value
            Dim only As String = ddlMxMember.SelectedValue

            ' Debts per pair of people (lender, borrower) and, per pair, which of the lender's accounts lent how much
            Dim owed As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)          ' "lender|borrower" -> amount
            Dim byAccount As New Dictionary(Of String, Dictionary(Of String, Decimal))(StringComparer.OrdinalIgnoreCase)
            For Each c As FlowCell In LQ().PairwiseDebts()
                If c.Amount = 0D Then Continue For
                Dim lender As String = PersonOf(c.FromId), borrower As String = PersonOf(c.ToId)
                If SameName(lender, borrower) Then Continue For   ' between one person's own accounts
                Dim key As String = lender & "|" & borrower
                owed(key) = If(owed.ContainsKey(key), owed(key), 0D) + c.Amount
                If Not byAccount.ContainsKey(key) Then byAccount(key) = New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
                byAccount(key)(c.FromId) = If(byAccount(key).ContainsKey(c.FromId), byAccount(key)(c.FromId), 0D) + c.Amount
            Next
            ' Net out: when two people owe each other, keep only the difference
            Dim net As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
            For Each kv As KeyValuePair(Of String, Decimal) In owed
                Dim parts As String() = kv.Key.Split("|"c)
                Dim back As String = parts(1) & "|" & parts(0)
                Dim amount As Decimal = Math.Round(kv.Value - If(owed.ContainsKey(back), owed(back), 0D), 3)
                If amount > 0D Then net(kv.Key) = amount
            Next

            Dim people As List(Of String) = net.Keys.SelectMany(Function(k) k.Split("|"c)).Distinct(StringComparer.OrdinalIgnoreCase).
                                                OrderBy(Function(id) LedgerName(id)).ToList()
            If only <> "" Then people = people.Where(Function(id) SameName(id, only)).ToList()

            Dim sb As New System.Text.StringBuilder("<div class=""mx-wrap"">")
            For Each pid As String In people
                Dim personId As String = pid
                Dim repay As List(Of KeyValuePair(Of String, Decimal)) = net.Where(Function(kv) SameName(kv.Key.Split("|"c)(1), personId)).
                                                                             OrderBy(Function(kv) LedgerName(kv.Key.Split("|"c)(0))).ToList()
                Dim lend As List(Of KeyValuePair(Of String, Decimal)) = net.Where(Function(kv) SameName(kv.Key.Split("|"c)(0), personId)).
                                                                            OrderBy(Function(kv) LedgerName(kv.Key.Split("|"c)(1))).ToList()
                If repay.Count = 0 AndAlso lend.Count = 0 Then Continue For
                Dim name As String = Server.HtmlEncode(LedgerName(personId))
                sb.Append("<fieldset class=""mx-person""><legend>").Append(name).Append("</legend><table class=""mx-list"">")
                ' Two titles under the person's name: Lend (who owes them) and Repay (whom they owe)
                sb.AppendFormat("<tr class=""head lend""><td>Lend</td><td class=""r"">{0}</td></tr>",
                                If(lend.Count = 0, "&ndash;", "(" & Money(lend.Sum(Function(kv) kv.Value)) & ")"))
                For Each kv As KeyValuePair(Of String, Decimal) In lend
                    sb.AppendFormat("<tr class=""part""><td class=""who"">{0}{1}</td><td class=""r"">{2}</td></tr>",
                                    Server.HtmlEncode(LedgerName(kv.Key.Split("|"c)(1))), ViaText(byAccount, kv.Key, personId), Money(kv.Value))
                Next
                sb.AppendFormat("<tr class=""head repay""><td>Repay</td><td class=""r"">{0}</td></tr>",
                                If(repay.Count = 0, "&ndash;", "(" & Money(repay.Sum(Function(kv) kv.Value)) & ")"))
                For Each kv As KeyValuePair(Of String, Decimal) In repay
                    Dim lender As String = kv.Key.Split("|"c)(0)
                    sb.AppendFormat("<tr class=""part""><td class=""who"">{0}{1}</td><td class=""r"">{2}</td></tr>",
                                    Server.HtmlEncode(LedgerName(lender)), ViaText(byAccount, kv.Key, lender), Money(kv.Value))
                Next
                sb.Append("</table></fieldset>")
            Next
            sb.Append("</div>")
            litMatrices.Text = If(people.Count = 0, "<div class=""tab-note"">" & If(only = "", "Nobody owes anybody.", "Nothing to repay or lend for this member.") & "</div>",
                                  sb.ToString())
        Catch ex As Exception
            lblMxMsg.Text = Server.HtmlEncode("Could not read the debts: " & ex.Message)
            lblMxMsg.Visible = True
        End Try
    End Sub

    ''' <summary>"via BBK_0117 12.000, Cash 8.000": which of the lender's accounts lent, when more than one did.</summary>
    Private Function ViaText(byAccount As Dictionary(Of String, Dictionary(Of String, Decimal)), key As String, lenderId As String) As String
        If Not byAccount.ContainsKey(key) Then Return ""
        Dim accs As List(Of KeyValuePair(Of String, Decimal)) = byAccount(key).Where(Function(kv) kv.Value <> 0D).ToList()
        If accs.Count < 2 Then Return ""
        Return "<span class=""via"">via " & Server.HtmlEncode(String.Join(", ", accs.Select(Function(kv)
                                                                                                   Dim m As Member = LedgerMembers().FirstOrDefault(Function(x) SameName(x.Id, kv.Key))
                                                                                                   Return If(m Is Nothing, kv.Key, ShortAccountName(m)) & " " & Money(kv.Value)
                                                                                               End Function))) & "</span>"
    End Function

    ' ====================================================================
    '  MAIN DASHBOARD (administrator)
    '  Pick a group (All first) -> every person in it with their balance. A person with several accounts:
    '  a total row (all their accounts) and each account under it; with one account: one row.
    '  Balance = money held - money owed (see Bal). Then the shop, and the total of everyone listed.
    ' ====================================================================

    Private Sub RefreshDashboard()
        litDashboard.Text = ""
        Try
            ' Groups, "All" first (keeps the pick)
            Dim current As String = ddlDashGroup.SelectedValue
            ddlDashGroup.Items.Clear()
            ddlDashGroup.Items.Add(New ListItem("All", "0"))
            For Each g As MemberGroup In GroupList().OrderBy(Function(x) x.Name)
                ddlDashGroup.Items.Add(New ListItem(g.Name, g.Id.ToString(CultureInfo.InvariantCulture)))
            Next
            Dim it As ListItem = ddlDashGroup.Items.FindByValue(If(current, ""))
            If it IsNot Nothing Then ddlDashGroup.SelectedValue = it.Value
            Dim groupId As Integer = 0
            Integer.TryParse(ddlDashGroup.SelectedValue, groupId)

            Dim sums As Dictionary(Of String, MemberFigures) = SummariesById()
            Dim people As List(Of Member) = LedgerMembers().Where(Function(m) m.IsActive AndAlso IsPerson(m) AndAlso
                                                                              (groupId = 0 OrElse (m.GroupIds IsNot Nothing AndAlso m.GroupIds.Contains(groupId)))).
                                                            OrderBy(Function(m) m.Name).ToList()
            Dim sb As New System.Text.StringBuilder("<table class=""db-grid""><tr><th>Member</th><th class=""r"">Balance</th>" &
                                                    "<th class=""r"">Cash</th><th class=""r"">Receivable</th><th class=""r"">Payable</th></tr>")
            Dim grand As New MemberFigures
            Dim row As Action(Of String, String, MemberFigures, String) =
                Sub(cssClass, nameHtml, f, title)
                    Dim balance As Decimal = Bal(f)
                    sb.AppendFormat("<tr class=""{0}""{1}><td class=""name"">{2}</td><td class=""r{3}"">{4}</td><td class=""r"">{5}</td><td class=""r"">{6}</td><td class=""r"">{7}</td></tr>",
                                    cssClass, If(title = "", "", " title=""" & Server.HtmlEncode(title) & """"), nameHtml,
                                    If(balance < 0D, " neg", ""), Money(balance), Money(CashOf(f)), Money(f.OwedToThem), Money(f.TheyOwe))
                End Sub

            For Each p As Member In people
                Dim accounts As List(Of Member) = AccountsOfPerson(p.Id)
                Dim frozenNote As String = If(p.IsFrozen, "<span class=""note"">(frozen)</span>", "") &
                                           If(IsShopCustomer(p.Id), "<span class=""note"">shop</span>", "")
                If accounts.Count <= 1 Then
                    ' One account (or none yet): one row with the person's name
                    Dim f As MemberFigures = If(accounts.Count = 1, FiguresOf(sums, accounts(0).Id), FiguresOf(sums, p.Id))
                    row("person", Server.HtmlEncode(p.Name) & frozenNote, f, If(accounts.Count = 1, accounts(0).Name, ""))
                    grand = Plus(grand, f)
                    Continue For
                End If
                ' Several accounts: the total first, then each account
                Dim total As MemberFigures = PersonFigures(sums, p.Id)
                row("person", Server.HtmlEncode(p.Name) & frozenNote & "<span class=""note"">" & accounts.Count & " accounts</span>", total, "")
                grand = Plus(grand, total)
                Dim isShop As Boolean = IsShopCustomer(p.Id)
                Dim allProfit As Decimal = 0D, allLoss As Decimal = 0D
                If isShop Then
                    ' all profits ever (_Profit's income) and all losses ever (_Loss's expense)
                    For Each a As Member In accounts
                        If a.Name.EndsWith("_Profit", StringComparison.OrdinalIgnoreCase) Then allProfit = FiguresOf(sums, a.Id).Income
                        If a.Name.EndsWith("_Loss", StringComparison.OrdinalIgnoreCase) Then allLoss = FiguresOf(sums, a.Id).Expense
                    Next
                End If
                For Each a As Member In accounts
                    Dim isLoan As Boolean = AccountTypeOf(a.Id) = AccTypeLoan
                    Dim fa As MemberFigures = FiguresOf(sums, a.Id)
                    Dim nameHtml As String = Server.HtmlEncode(ShortAccountName(a)) & "<span class=""type"">" & Server.HtmlEncode(AccountTypeOf(a.Id)) & "</span>"
                    ' A shop's _Profit / _Loss: the total of all profits / all losses ever (Zeroing does not reset it)
                    If isShop AndAlso (a.Name.EndsWith("_Profit", StringComparison.OrdinalIgnoreCase) OrElse a.Name.EndsWith("_Loss", StringComparison.OrdinalIgnoreCase)) Then
                        Dim isProfit As Boolean = a.Name.EndsWith("_Profit", StringComparison.OrdinalIgnoreCase)
                        Dim v As Decimal = If(isProfit, fa.Income, -fa.Expense)
                        sb.AppendFormat("<tr class=""acc"" title=""{0}""><td class=""name"">{1}<span class=""note"">all {2}</span></td><td class=""r{3}"">{4}</td>" &
                                        "<td class=""r"">{5}</td><td class=""r"">{6}</td><td class=""r"">{7}</td></tr>",
                                        Server.HtmlEncode(a.Name), nameHtml, If(isProfit, "profits", "losses"), If(v < 0D, " neg", ""), Money(v),
                                        Money(CashOf(fa)), Money(fa.OwedToThem), Money(fa.TheyOwe))
                        If isProfit Then
                            ' Directly under _Profit: all profits - all losses (green when positive, red when negative)
                            Dim pl As Decimal = allProfit - allLoss
                            sb.AppendFormat("<tr class=""acc pl {0}""><td class=""name"">Profits &minus; Losses</td><td class=""r"">{1}</td><td></td><td></td><td></td></tr>",
                                            If(pl < 0D, "pl-neg", "pl-pos"), Money(pl))
                        End If
                        Continue For
                    End If
                    row("acc" & If(isLoan, " loan", ""), nameHtml, fa, a.Name)
                Next
            Next

            ' The shop (all groups only)
            If groupId = 0 Then
                For Each s As Member In LedgerMembers().Where(Function(m) m.IsActive AndAlso m.MemberType = MemberTypes.Shop)
                    Dim f As MemberFigures = FiguresOf(sums, s.Id)
                    row("person", Server.HtmlEncode(s.Name) & "<span class=""note"">shop</span>", f, "")
                    grand = Plus(grand, f)
                Next
            End If

            If people.Count = 0 Then
                sb.Append("<tr><td colspan=""5"" class=""tab-note"">Nobody in this group.</td></tr>")
            Else
                row("total", "TOTAL (" & people.Count & If(people.Count = 1, " member", " members") & ")", grand, "")
            End If
            sb.Append("</table>")
            litDashboard.Text = sb.ToString()
        Catch ex As Exception
            lblDashMsg.Text = Server.HtmlEncode("Could not read the balances: " & ex.Message)
            lblDashMsg.Visible = True
        End Try
    End Sub

    ' ====================================================================
    '  TRANSACTIONS (from the ledger)
    '  Pick a person -> their details and the total of their accounts (cards), then one grid per account:
    '  every journal that touched the account, newest first (orders with their items, Credit, Withdraw and
    '  payer, what it paid for others, deposits, cancellations ...). The ex columns can be hidden.
    '  Other users see only their own.
    ' ====================================================================

    ' caption | main colour | light background
    Private Shared ReadOnly TxCards As String()() = {
        New String() {"BALANCE", "#111827", "#e8ecf4"},
        New String() {"CASH", "#15803d", "#e2f5e9"},
        New String() {"RECEIVABLE", "#1d4ed8", "#e1ecfc"},
        New String() {"PAYABLE", "#c2410c", "#feebde"},
        New String() {"EXPENSE", "#7e22ce", "#f0e6fc"},
        New String() {"ORDERS", "#475569", "#eceff3"}}

    Private Function TxCardsHtml(f As MemberFigures, orders As Integer, small As Boolean) As String
        Dim values As Decimal() = {Bal(f), CashOf(f), f.OwedToThem, f.TheyOwe, f.Expense, orders}
        Dim sb As New System.Text.StringBuilder("<div class=""tx-cards" & If(small, " small", "") & """>")
        For i As Integer = 0 To TxCards.Length - 1
            Dim c As String() = TxCards(i)
            Dim v As Decimal = values(i)
            Dim isOrders As Boolean = (c(0) = "ORDERS")
            sb.AppendFormat("<div class=""tx-card"" style=""background:{0}""><div class=""bar"" style=""background:{1}""></div>" &
                            "<div class=""body""><div class=""cap"">{2}</div><div class=""val{3}"" style=""color:{1}"">{4}</div></div></div>",
                            c(2), c(1), c(0), If(Not isOrders AndAlso v < 0D, " neg", ""),
                            If(isOrders, v.ToString("0", CultureInfo.InvariantCulture), Money(v)))
        Next
        sb.Append("</div>")
        Return sb.ToString()
    End Function

    Private Sub RefreshTransactions()
        litTxPerson.Text = ""
        litTransactions.Text = ""
        If hfTxEx.Value = "" Then hfTxEx.Value = "0"
        Try
            FillMemberFilter(ddlTxMember, "[Select A Member]", "")
        Catch ex As Exception
            lblTxMsg.Text = Server.HtmlEncode("Could not read the members: " & ex.Message)
            lblTxMsg.Visible = True
            Return
        End Try

        ' Other users see only their own transactions: the list is fixed to them and locked
        ddlTxMember.Enabled = IsAdmin
        If Not IsAdmin Then
            Dim own As ListItem = ddlTxMember.Items.FindByValue(CurrentMemberId)
            ddlTxMember.ClearSelection()
            If own IsNot Nothing Then
                own.Selected = True
            Else
                lblTxMsg.Text = Server.HtmlEncode(NotRegisteredText())
                lblTxMsg.Visible = True
                Return
            End If
        End If

        Dim personId As String = ddlTxMember.SelectedValue
        If personId = "" Then Return   ' "[Select A Member]"
        Dim person As Member = LedgerMembers().FirstOrDefault(Function(m) SameName(m.Id, personId))
        If person Is Nothing Then Return

        Try
            Dim q As LedgerQueries = LQ()
            ' The person's accounts (a person without accounts, or the shop: itself)
            Dim accounts As List(Of Member) = If(IsPerson(person), AccountsOfPerson(person.Id), New List(Of Member))
            If accounts.Count = 0 Then accounts = New List(Of Member) From {person}
            Dim ids As List(Of String) = accounts.Select(Function(a) a.Id).ToList()
            Dim statements As New Dictionary(Of String, List(Of StatementLine))(StringComparer.OrdinalIgnoreCase)
            For Each id As String In ids
                statements(id) = q.Statement(id)
            Next

            ' Orders: date / time / title; each account's lines (Credit, Withdraw, payer) and items; what it paid for others
            Dim orderInfo As New Dictionary(Of String, String())
            For Each o As DataRow In OrdersDB.GetDataTable("SELECT * FROM Orders").Rows
                orderInfo(o("Order_ID").ToString()) = New String() {ColText(o, "Order_Date"), ColText(o, "Order_No"), ColText(o, "Order_Title")}
            Next
            Dim inList As String = String.Join(", ", ids.Select(Function(x) OrdersDB.SqlText(x)))
            Dim oms As DataTable = OrdersDB.GetDataTable("SELECT * FROM Order_Members WHERE Member_ID IN (" & inList & ") OR Account_ID IN (" & inList & ")")
            Dim omis As DataTable = OrdersDB.GetDataTable(
                "SELECT omi.Order_ID, omi.Member_ID, oi.Item_Name, omi.Quantity, oi.Unit_Price " &
                "FROM Order_Member_Items AS omi INNER JOIN Order_Items AS oi ON omi.OrderItem_ID = oi.OrderItem_ID " &
                "WHERE omi.Member_ID IN (" & inList & ")")

            ' The person: details and the total of all their accounts
            Dim total As New MemberFigures With {.MemberId = person.Id}
            For Each id As String In ids
                total = Plus(total, q.Summary(id))
            Next
            Dim allOrders As Integer = statements.Values.SelectMany(Function(s) s).
                Where(Function(s) s.Journal.JournalType = JournalTypes.Order AndAlso s.Journal.OrderId <> "").
                Select(Function(s) s.Journal.OrderId).Distinct().Count()
            Dim groupNames As String = String.Join(", ", GroupList().Where(Function(g) person.GroupIds IsNot Nothing AndAlso person.GroupIds.Contains(g.Id)).Select(Function(g) g.Name))
            Dim info As New List(Of String)
            info.Add("<span>Member ID: <b>" & Server.HtmlEncode(person.Id) & "</b></span>")
            If Convert.ToString(person.StaffId) <> "" Then info.Add("<span>Staff ID: <b>" & Server.HtmlEncode(Convert.ToString(person.StaffId)) & "</b></span>")
            If Convert.ToString(person.Mobile) <> "" Then info.Add("<span>Mobile: <b>" & Server.HtmlEncode(person.Mobile) & "</b></span>")
            If Convert.ToString(person.Email) <> "" Then info.Add("<span>Email: <b>" & Server.HtmlEncode(person.Email) & "</b></span>")
            If groupNames <> "" Then info.Add("<span>Groups: <b>" & Server.HtmlEncode(groupNames) & "</b></span>")
            info.Add("<span>Status: <b>" & Server.HtmlEncode(If(person.IsFrozen, "Frozen", "Active")) & "</b></span>")
            info.Add("<span>Accounts: <b>" & Server.HtmlEncode(String.Join(", ", accounts.Select(Function(a) ShortAccountName(a)))) & "</b></span>")
            litTxPerson.Text = "<div class=""tx-person""><h2>" & Server.HtmlEncode(person.Name) & "</h2>" &
                               "<div class=""info"">" & String.Join("", info) & "</div>" & TxCardsHtml(total, allOrders, False) & "</div>"

            ' One grid per account
            Dim headers As String() = {"Date &amp; Time", "Order ID", "Title", "Type", "Account",
                                       "exBalance", "exCash", "exExpense", "exReceivable", "exPayable",
                                       "Credit", "Withdraw", "Items", "Items Amount", "NewBalance",
                                       "Cash", "Expense", "Receivable", "Payable"}
            Dim exCols As New HashSet(Of Integer) From {5, 6, 7, 8, 9}
            Dim sb As New System.Text.StringBuilder()
            For Each a As Member In accounts
                Dim statement As List(Of StatementLine) = statements(a.Id)
                Dim isLoan As Boolean = AccountTypeOf(a.Id) = AccTypeLoan
                Dim typeText As String = If(SameName(a.Id, person.Id), "", If(AccountTypeOf(a.Id) = "", AccTypeCapital, AccountTypeOf(a.Id)))
                Dim accOrders As Integer = statement.Where(Function(s) s.Journal.JournalType = JournalTypes.Order).Count()
                ' A group box per account: its name (and type) on the top border, its own cards, then its grid
                sb.Append("<fieldset class=""tx-acc").Append(If(isLoan, " loan", "")).Append(""">")
                sb.Append("<legend>").Append(Server.HtmlEncode(ShortAccountName(a)))
                If typeText <> "" Then sb.Append("<span class=""type"">").Append(Server.HtmlEncode(typeText)).Append("</span>")
                sb.Append("</legend>").Append(TxCardsHtml(q.Summary(a.Id), accOrders, True))

                sb.Append("<div class=""tx-scroll""><table class=""tx-grid""><tr>")
                For i As Integer = 0 To headers.Length - 1
                    sb.Append("<th").Append(If(exCols.Contains(i), " class=""exc""", "")).Append(">").Append(headers(i)).Append("</th>")
                Next
                sb.Append("</tr>")

                For Each sl As StatementLine In Enumerable.Reverse(statement)
                    Dim j As Journal = sl.Journal
                    Dim b As MemberFigures = Minus(sl.After, sl.Change)
                    Dim oi As String() = Nothing
                    Dim isOrder As Boolean = j.JournalType = JournalTypes.Order AndAlso j.OrderId <> "" AndAlso orderInfo.TryGetValue(j.OrderId, oi)

                    Dim lines As New List(Of String)
                    Dim credit As Decimal = 0D, debt As Decimal = 0D
                    Dim payers As New List(Of String)
                    Dim accId As String = a.Id
                    Dim orderId As String = j.OrderId
                    If isOrder Then
                        For Each it As DataRow In omis.Rows.Cast(Of DataRow)().Where(Function(r) r("Order_ID").ToString() = orderId AndAlso SameName(r("Member_ID").ToString(), accId))
                            lines.Add(SavedItemLine(ColText(it, "Item_Name"), DbDecimal(it("Quantity")), DbDecimal(it("Unit_Price"))))
                        Next
                        For Each om As DataRow In oms.Rows.Cast(Of DataRow)().Where(Function(r) r("Order_ID").ToString() = orderId)
                            Dim lineMember As String = om("Member_ID").ToString()
                            Dim payer As String = ColText(om, "Account_ID").Trim()
                            If SameName(lineMember, accId) Then
                                ' This account's own line(s): Credit, Withdraw, who paid
                                credit += DbDecimal(om("Credit")) : debt += DbDecimal(om("Withdraw"))
                                If payer <> "" AndAlso Not SameName(payer, accId) AndAlso Not payers.Contains(LedgerName(payer)) Then payers.Add(LedgerName(payer))
                            ElseIf SameName(payer, accId) Then
                                ' It paid for someone else
                                lines.Add("Pays " & Money(DbDecimal(om("Items_Amount")) + DbDecimal(om("Withdraw"))) & " for " & LedgerName(lineMember))
                            End If
                        Next
                        If debt <> 0D Then lines.Add("-Withdraw " & Money(debt))
                        If credit <> 0D Then lines.Add("-Credit " & Money(credit))
                    Else
                        lines.AddRange(sl.Entries.Select(Function(x) x.Memo).Where(Function(x) x <> "").Distinct())
                    End If

                    Dim whenText As String = If(isOrder, (oi(0) & " " & oi(1)).Trim(), j.JournalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                    Dim titleText As String = If(isOrder, CleanTitle(oi(2)), j.Memo)
                    Dim jType As String = j.JournalType & If(q.IsCancelled(j.Id), " (cancelled)", "")

                    sb.Append("<tr>")
                    sb.Append("<td>").Append(Server.HtmlEncode(whenText)).Append("</td>")
                    sb.Append("<td>").Append(Server.HtmlEncode(j.OrderId)).Append("</td>")
                    sb.Append("<td>").Append(Server.HtmlEncode(titleText)).Append("</td>")
                    sb.Append("<td>").Append(Server.HtmlEncode(jType)).Append("</td>")
                    sb.Append("<td>").Append(String.Join("<br />", payers.Select(Function(x) Server.HtmlEncode(x)))).Append("</td>")
                    For Each v As Decimal In {Bal(b), CashOf(b), b.Expense, b.OwedToThem, b.TheyOwe}
                        sb.Append("<td class=""r ex exc"">").Append(Money(v)).Append("</td>")
                    Next
                    sb.Append("<td class=""r"">").Append(Money(credit)).Append("</td>")
                    sb.Append("<td class=""r"">").Append(Money(debt)).Append("</td>")
                    sb.Append("<td class=""items"">").Append(String.Join("<br />", lines.Select(Function(l) Server.HtmlEncode(l)))).Append("</td>")
                    sb.Append("<td class=""r"">").Append(Money(sl.Change.Expense)).Append("</td>")
                    sb.Append("<td class=""r b").Append(If(Bal(sl.After) < 0D, " neg", "")).Append(""">").Append(Money(Bal(sl.After))).Append("</td>")
                    For Each v As Decimal In {CashOf(sl.After), sl.After.Expense, sl.After.OwedToThem, sl.After.TheyOwe}
                        sb.Append("<td class=""r b"">").Append(Money(v)).Append("</td>")
                    Next
                    sb.Append("</tr>")
                Next
                If statement.Count = 0 Then
                    sb.Append("<tr><td colspan=""").Append(headers.Length).Append(""" class=""tab-note"">No transactions on this account yet.</td></tr>")
                End If
                sb.Append("</table></div></fieldset>")
            Next
            litTransactions.Text = sb.ToString()
        Catch ex As Exception
            lblTxMsg.Text = Server.HtmlEncode("Could not load transactions: " & ex.Message)
            lblTxMsg.Visible = True
        End Try
    End Sub

    ' ====================================================================
    '  ACCOUNTS (administrator)
    '  Pick a person -> Create New Account is enabled, and the person's accounts are listed below.
    '  Every account is a member of its own (see PERSONS AND ACCOUNTS):
    '    Capital = one account;   Loan = two accounts, <Name>_Have and <Name>_Spent (both of type Loan).
    '  An account can be renamed, and deleted when its balance is 0.000 (not the person's last account):
    '  removed if it was never used, otherwise hidden (Is_Active = False) for the history of past orders.
    ' ====================================================================

    Private Const AccTypeCapital As String = "Capital"
    Private Const AccTypeLoan As String = "Loan"
    Private Const LoanHaveSuffix As String = "_Have"
    Private Const LoanSpentSuffix As String = "_Spent"

    Private Sub ShowAccMsg(text As String, isError As Boolean)
        lblAccMsg.Text = Server.HtmlEncode(text)
        lblAccMsg.CssClass = "order-msg " & If(isError, "err", "ok")
        lblAccMsg.Visible = True
    End Sub

    ''' <summary>The person picked on the Accounts tab ("" when none).</summary>
    Private Function AccMemberId() As String
        Return If(ddlAccMember.SelectedValue, "")
    End Function

    Private Function AccPerson() As Member
        Dim id As String = AccMemberId()
        Return LedgerMembers().FirstOrDefault(Function(m) SameName(m.Id, id) AndAlso IsPerson(m))
    End Function

    Protected Sub ddlAccMember_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlAccMember.SelectedIndexChanged
        pnlAccNew.Visible = False   ' another person: start again
        pnlAccRename.Visible = False
        pnlAccBalance.Visible = False
        AccRenameId = ""
    End Sub

    Protected Sub btnAccNew_Click(sender As Object, e As EventArgs) Handles btnAccNew.Click
        If Not IsAdmin OrElse AccMemberId() = "" Then Return
        txtAccName.Text = ""
        txtAccSpent.Text = ""
        txtAccOpening.Text = ""
        txtAccLimit.Text = ""
        ddlAccType.SelectedValue = AccTypeCapital
        pnlAccRename.Visible = False
        pnlAccBalance.Visible = False
        pnlAccNew.Visible = True
        txtAccName.Focus()
    End Sub

    Protected Sub btnAccCancel_Click(sender As Object, e As EventArgs) Handles btnAccCancel.Click
        pnlAccNew.Visible = False
    End Sub

    Private Shared Function CleanName(text As String) As String
        Return String.Join(" ", If(text, "").Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries))
    End Function

    ''' <summary>An account name the person already uses (other than the account being renamed), or "".</summary>
    Private Function TakenAccountName(person As Member, names As IEnumerable(Of String), Optional exceptId As String = "") As String
        Dim existing As List(Of String) = AccountsOfPerson(person.Id).Where(Function(a) Not SameName(a.Id, exceptId)).
                                                                     Select(Function(a) ShortAccountName(a)).ToList()
        Return If(names.FirstOrDefault(Function(n) existing.Any(Function(x) SameName(x, n))), "")
    End Function

    ''' <summary>Reads an amount typed in a box: empty = 0. False when it is not a number (or negative when not allowed).</summary>
    Private Shared Function TryAmount(text As String, allowNegative As Boolean, ByRef amount As Decimal) As Boolean
        amount = 0D
        Dim s As String = If(text, "").Trim().Replace(",", "")
        If s = "" Then Return True
        If Not Decimal.TryParse(s, NumberStyles.Number Or NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, amount) Then Return False
        If amount < 0D AndAlso Not allowNegative Then Return False
        amount = Math.Round(amount, 3)
        Return True
    End Function

    Protected Sub btnAccSave_Click(sender As Object, e As EventArgs) Handles btnAccSave.Click
        If Not IsAdmin Then Return
        Dim person As Member = AccPerson()
        Dim accName As String = CleanName(txtAccName.Text)
        Dim isLoan As Boolean = (ddlAccType.SelectedValue = AccTypeLoan)
        If person Is Nothing Then
            ShowAccMsg("Please pick a member.", True)
            Return
        End If
        If IsShopCustomer(person.Id) Then
            ShowAccMsg(person.Name & " is a shop: it has its five accounts only.", True)
            Return
        End If
        If accName = "" Then
            ShowAccMsg("Please enter the account name.", True)
            Return
        End If

        ' Capital: its opening balance (positive = in hand, negative = owed).
        ' Loan: its limit and what was already spent: _Have = limit - spent, _Spent = -spent.
        Dim opening, limit, initialSpent As Decimal
        If isLoan Then
            If Not TryAmount(txtAccLimit.Text, False, limit) OrElse limit <= 0D Then
                ShowAccMsg("Please enter the Loan Limit, an amount above 0 (e.g. 3000).", True)
                Return
            End If
            If Not TryAmount(txtAccSpent.Text, False, initialSpent) Then
                ShowAccMsg("Initial Spent should be an amount of 0 or more, e.g. 1145.633.", True)
                Return
            End If
            If initialSpent > limit Then
                ShowAccMsg("Initial Spent (" & Money(initialSpent) & ") cannot be more than the Loan Limit (" & Money(limit) & ").", True)
                Return
            End If
        ElseIf Not TryAmount(txtAccOpening.Text, True, opening) Then
            ShowAccMsg("Opening Balance should be an amount, e.g. 250.000 or -40.500.", True)
            Return
        End If

        Dim names As List(Of String) = If(isLoan, New List(Of String) From {accName & LoanHaveSuffix, accName & LoanSpentSuffix},
                                                  New List(Of String) From {accName})
        Dim taken As String = TakenAccountName(person, names)
        If taken <> "" Then
            ShowAccMsg(person.Name & " already has an account named """ & taken & """.", True)
            Return
        End If

        Dim created As New List(Of Member)
        Try
            If isLoan Then
                created.Add(CreateAccountMember(person, names(0), AccTypeLoan, limit - initialSpent))
                created.Add(CreateAccountMember(person, names(1), AccTypeLoan, -initialSpent))
                Using cn As System.Data.OleDb.OleDbConnection = OpenOrdersDb()
                    SaveLoanLink(cn, created(0).Id, created(1).Id, limit)
                End Using
            Else
                created.Add(CreateAccountMember(person, accName, AccTypeCapital, opening))
            End If
        Catch ex As Exception
            ResetLedgerCache()
            ShowAccMsg(If(created.Count > 0, """" & names(0) & """ was created, but the rest was not: ", "Nothing was created: ") & ex.Message, True)
            Return
        End Try

        ResetLedgerCache()
        RefreshOrderMembers()   ' the new account is on the Order grid straight away
        pnlAccNew.Visible = False
        If isLoan Then
            ShowAccMsg("Loan " & accName & " created for " & person.Name & ": limit " & Money(limit) & ", " &
                       names(0) & " " & Money(limit - initialSpent) & ", " & names(1) & " " & Money(-initialSpent) & ".", False)
        Else
            ShowAccMsg("Capital account """ & accName & """ created for " & person.Name &
                       If(opening <> 0D, ", opening at " & Money(opening), "") & ".", False)
        End If
    End Sub

    ''' <summary>One row of the Accounts list (Repeater).</summary>
    Public Class AccRow
        Public Property Id As String = ""
        Public Property Name As String = ""
        Public Property TypeText As String = ""
        Public Property IsLoan As Boolean
        Public Property BalanceText As String = ""
        Public Property CanDelete As Boolean
        Public Property CanSetBalance As Boolean
        Public Property BalanceAction As String = "Set Balance"   ' Set Balance / Set Spent / Set Limit
    End Class

    Private Sub RefreshAccounts()
        rptAccounts.DataSource = Nothing
        rptAccounts.DataBind()
        litAccEmpty.Text = ""
        Try
            ' The people (their accounts are listed under them)
            Dim current As String = AccMemberId()
            ddlAccMember.Items.Clear()
            ddlAccMember.Items.Add(New ListItem("-- Select a member --", ""))
            For Each m As Member In LedgerMembers().Where(Function(x) x.IsActive AndAlso IsPerson(x)).OrderBy(Function(x) x.Name)
                ddlAccMember.Items.Add(New ListItem(m.Name, m.Id))
            Next
            Dim it As ListItem = ddlAccMember.Items.FindByValue(current)
            If it IsNot Nothing Then ddlAccMember.SelectedValue = it.Value

            Dim person As Member = AccPerson()
            btnAccNew.Enabled = (person IsNot Nothing AndAlso Not IsShopCustomer(person.Id))   ' a shop: its five accounts only
            btnAccNew.ToolTip = If(person IsNot Nothing AndAlso IsShopCustomer(person.Id), "A shop has its five accounts only", "")
            If person Is Nothing Then
                pnlAccNew.Visible = False
                pnlAccRename.Visible = False
                pnlAccBalance.Visible = False
                Return
            End If
            lblAccNewFor.Text = Server.HtmlEncode(person.Name)

            SyncLoanPairs()
            Dim sums As Dictionary(Of String, MemberFigures) = SummariesById()
            Dim accounts As List(Of Member) = AccountsOfPerson(person.Id)
            Dim rows As New List(Of AccRow)
            For Each a As Member In accounts
                Dim net As Decimal = Math.Round(Bal(FiguresOf(sums, a.Id)), 3)
                rows.Add(New AccRow With {.Id = a.Id, .Name = ShortAccountName(a), .TypeText = AccountTypeOf(a.Id),
                                          .IsLoan = (AccountTypeOf(a.Id) = AccTypeLoan), .BalanceText = Money(net),
                                          .CanDelete = (net = 0D AndAlso accounts.Count > 1 AndAlso Not IsShopCustomer(person.Id)),
                                          .CanSetBalance = True,
                                          .BalanceAction = If(IsSpentAccount(a), "Set Spent", If(IsHaveAccount(a), "Set Limit", "Set Balance"))})
                ' A loan's _Have: its limit next to the type
                Dim limit As Decimal? = If(IsHaveAccount(a), LoanLimitOf(a.Id), CType(Nothing, Decimal?))
                If limit.HasValue Then rows(rows.Count - 1).TypeText &= " (limit " & Money(limit.Value) & ")"
            Next
            If rows.Count = 0 Then
                litAccEmpty.Text = "<div class=""tab-note"">No accounts yet: click Create New Account.</div>"
            Else
                rptAccounts.DataSource = rows
                rptAccounts.DataBind()
            End If
        Catch ex As Exception
            ShowAccMsg("Could not read the accounts: " & ex.Message, True)
        End Try
    End Sub

    ' ---------- Rename / Delete ----------

    ''' <summary>The account being renamed (its Member_ID), between Rename and Save.</summary>
    Private Property AccRenameId As String
        Get
            Return Convert.ToString(ViewState("AccRenameId"))
        End Get
        Set(value As String)
            ViewState("AccRenameId") = value
        End Set
    End Property

    ''' <summary>An active account of the person picked on the Accounts tab, or Nothing.</summary>
    Private Function PickedPersonAccount(accountId As String) As Member
        If String.IsNullOrEmpty(accountId) Then Return Nothing
        Return AccountsOfPerson(AccMemberId()).FirstOrDefault(Function(a) SameName(a.Id, accountId))
    End Function

    Protected Sub rptAccounts_ItemCommand(source As Object, e As RepeaterCommandEventArgs) Handles rptAccounts.ItemCommand
        If Not IsAdmin Then Return
        Dim accountId As String = Convert.ToString(e.CommandArgument)
        Select Case e.CommandName
            Case "BalanceAcc" : StartSetBalance(accountId)
            Case "RenameAcc" : StartRenameAccount(accountId)
            Case "DeleteAcc" : DeleteAccount(accountId)
        End Select
    End Sub

    ' ---------- Set Balance / Set Spent / Set Limit (opening balances in Members) ----------
    ' Capital  : Set Balance - the amount in hand (negative = owed).
    ' _Spent   : Set Spent   - what was used (a positive amount); _Have becomes Loan Limit - spent.
    ' _Have    : Set Limit   - the loan limit; what was spent stays, _Have becomes Limit - spent.
    ' The balance shown = the opening balance + the account's journals.

    Private Const BalModeOpening As String = "opening"
    Private Const BalModeSpent As String = "spent"
    Private Const BalModeLimit As String = "limit"

    Private Function IsSpentAccount(a As Member) As Boolean
        Return AccountTypeOf(a.Id) = AccTypeLoan AndAlso a.Name.EndsWith(LoanSpentSuffix, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function IsHaveAccount(a As Member) As Boolean
        Return AccountTypeOf(a.Id) = AccTypeLoan AndAlso Not IsSpentAccount(a)
    End Function

    ''' <summary>The account whose balance is being set (its Member_ID), between the link and Save.</summary>
    Private Property AccBalanceId As String
        Get
            Return Convert.ToString(ViewState("AccBalanceId"))
        End Get
        Set(value As String)
            ViewState("AccBalanceId") = value
        End Set
    End Property

    Private Function AccBalanceMode(a As Member) As String
        If IsSpentAccount(a) Then Return BalModeSpent
        If IsHaveAccount(a) Then Return BalModeLimit
        Return BalModeOpening
    End Function

    ''' <summary>For a shop's &lt;Shop&gt;_Budget account: the same shop's &lt;Shop&gt;_Cash account; otherwise Nothing.</summary>
    Private Function ShopCashOfBudget(a As Member) As Member
        If a Is Nothing OrElse Not a.Name.EndsWith("_Budget", StringComparison.OrdinalIgnoreCase) Then Return Nothing
        Dim shopId As String = OwnerOf(a.Id)
        If shopId = "" OrElse Not IsShopCustomer(shopId) Then Return Nothing
        Return AccountsOfPerson(shopId).FirstOrDefault(Function(m) m.Name.EndsWith("_Cash", StringComparison.OrdinalIgnoreCase))
    End Function

    Private Function OpeningBalanceOf(memberId As String) As Decimal
        Dim dt As DataTable = OrdersDB.GetDataTable("SELECT Member_Opening_Balance FROM Members WHERE Member_ID = " & OrdersDB.SqlText(memberId))
        Return If(dt.Rows.Count = 0, 0D, DbDecimal(dt.Rows(0)("Member_Opening_Balance")))
    End Function

    Private Sub StartSetBalance(accountId As String)
        Dim a As Member = PickedPersonAccount(accountId)
        If a Is Nothing Then
            ShowAccMsg("That account is no longer there.", True)
            Return
        End If
        Dim sums As Dictionary(Of String, MemberFigures) = SummariesById()
        AccBalanceId = a.Id
        lblAccBalanceFor.Text = Server.HtmlEncode(ShortAccountName(a))
        Select Case AccBalanceMode(a)
            Case BalModeSpent
                lblAccBalanceWhat.Text = "Spent"
                lblAccBalanceHint.Text = "what was used / is owed (a positive amount); _Have becomes the Loan Limit minus this"
                txtAccBalance.Text = Money(-Bal(FiguresOf(sums, a.Id)))
            Case BalModeLimit
                Dim limit As Decimal? = LoanLimitOf(a.Id)
                lblAccBalanceWhat.Text = "Loan Limit"
                lblAccBalanceHint.Text = "what was spent stays; " & ShortAccountName(a) & " becomes the limit minus it"
                txtAccBalance.Text = If(limit.HasValue, Money(limit.Value), "")
            Case Else
                lblAccBalanceWhat.Text = "Opening Balance"
                lblAccBalanceHint.Text = "positive = money in hand, negative = money owed"
                Dim cashAcc As Member = ShopCashOfBudget(a)
                If cashAcc IsNot Nothing Then lblAccBalanceHint.Text &= "; " & Server.HtmlEncode(ShortAccountName(cashAcc)) & " gets the opposite (the two add up to 0)"
                txtAccBalance.Text = Money(OpeningBalanceOf(a.Id))
        End Select
        pnlAccNew.Visible = False
        pnlAccRename.Visible = False
        pnlAccBalance.Visible = True
        txtAccBalance.Focus()
    End Sub

    Protected Sub btnAccBalanceCancel_Click(sender As Object, e As EventArgs) Handles btnAccBalanceCancel.Click
        pnlAccBalance.Visible = False
        AccBalanceId = ""
    End Sub

    Protected Sub btnAccBalanceSave_Click(sender As Object, e As EventArgs) Handles btnAccBalanceSave.Click
        If Not IsAdmin Then Return
        Dim a As Member = PickedPersonAccount(AccBalanceId)
        If a Is Nothing Then
            pnlAccBalance.Visible = False
            ShowAccMsg("That account is no longer there.", True)
            Return
        End If
        Dim mode As String = AccBalanceMode(a)
        pnlAccBalance.Visible = True   ' stays open until a valid amount is saved
        Dim amount As Decimal
        If Not TryAmount(txtAccBalance.Text, mode = BalModeOpening, amount) Then
            ShowAccMsg(If(mode = BalModeOpening, "Please enter an amount, e.g. 250.000 or -40.500.", "Please enter an amount of 0 or more, e.g. 1145.633."), True)
            Return
        End If

        Dim done As String
        Try
            Select Case mode
                Case BalModeOpening
                    ' A shop's _Budget: its _Cash gets the opposite opening balance (_Budget + _Cash = 0)
                    Dim cashAcc As Member = ShopCashOfBudget(a)
                    Using cn As System.Data.OleDb.OleDbConnection = OpenOrdersDb()
                        SetOpeningBalance(cn, a.Id, amount)
                        If cashAcc IsNot Nothing Then SetOpeningBalance(cn, cashAcc.Id, -amount)
                    End Using
                    done = "opening balance set to " & Money(amount)
                    If cashAcc IsNot Nothing Then done &= "; " & ShortAccountName(cashAcc) & " set to " & Money(-amount)

                Case BalModeSpent
                    Dim have As Member = HaveOf(a)
                    Dim limit As Decimal? = If(have Is Nothing, CType(Nothing, Decimal?), LoanLimitOf(have.Id))
                    If have Is Nothing OrElse Not limit.HasValue Then
                        ' No limit set yet: only this account changes (set the limit on its _Have account)
                        Dim net As Decimal = Bal(FiguresOf(SummariesById(), a.Id))
                        Using cn As System.Data.OleDb.OleDbConnection = OpenOrdersDb()
                            SetOpeningBalance(cn, a.Id, OpeningBalanceOf(a.Id) + (-amount - net))
                        End Using
                        done = "spent set to " & Money(amount) & " (no Loan Limit yet: use Set Limit on its _Have account)"
                    Else
                        If amount > limit.Value Then
                            ShowAccMsg("Spent (" & Money(amount) & ") cannot be more than the Loan Limit (" & Money(limit.Value) & ").", True)
                            Return
                        End If
                        SetLoanFigures(have, a, limit.Value, amount)
                        done = "spent set to " & Money(amount) & "; " & ShortAccountName(have) & " is now " & Money(limit.Value - amount)
                    End If

                Case Else   ' BalModeLimit
                    Dim spent As Member = SpentOf(a)
                    If spent Is Nothing Then
                        ShowAccMsg("The _Spent account of " & ShortAccountName(a) & " was not found.", True)
                        Return
                    End If
                    Dim spentAmount As Decimal = Math.Round(-Bal(FiguresOf(SummariesById(), spent.Id)), 3)
                    If amount <= 0D OrElse amount < spentAmount Then
                        ShowAccMsg("The Loan Limit must be above 0 and at least what was spent (" & Money(spentAmount) & ").", True)
                        Return
                    End If
                    SetLoanFigures(a, spent, amount, spentAmount)
                    done = "Loan Limit set to " & Money(amount) & "; " & ShortAccountName(a) & " is now " & Money(amount - spentAmount)
            End Select
        Catch ex As Exception
            ResetLedgerCache()
            ShowAccMsg("Nothing was saved: " & ex.Message, True)
            Return
        End Try
        ResetLedgerCache()
        RefreshOrderMembers()   ' the Order grid's exBalance follows
        pnlAccBalance.Visible = False
        AccBalanceId = ""
        ShowAccMsg(ShortAccountName(a) & ": " & done & ".", False)
    End Sub

    Private Sub StartRenameAccount(accountId As String)
        Dim a As Member = PickedPersonAccount(accountId)
        If a Is Nothing Then
            ShowAccMsg("That account is no longer there.", True)
            Return
        End If
        AccRenameId = a.Id
        lblAccRenameOld.Text = Server.HtmlEncode(ShortAccountName(a))
        txtAccRename.Text = ShortAccountName(a)
        pnlAccNew.Visible = False
        pnlAccBalance.Visible = False
        pnlAccRename.Visible = True
        txtAccRename.Focus()
    End Sub

    Protected Sub btnAccRenameCancel_Click(sender As Object, e As EventArgs) Handles btnAccRenameCancel.Click
        pnlAccRename.Visible = False
        AccRenameId = ""
    End Sub

    ''' <summary>Renames an account member (and its fund, which Paid From shows) to "<Person> - <name>".</summary>
    Private Sub RenameAccountMember(a As Member, ownerName As String, newShortName As String)
        Ledger.UpdateMember(a.Id, ownerName & AccountNameSep & newShortName, a.StaffId, a.Mobile, a.Cpr, a.Email, a.GroupIds, a.Status, a.Comment)
        Using cn As System.Data.OleDb.OleDbConnection = OpenOrdersDb()
            Using cmd As New System.Data.OleDb.OleDbCommand("UPDATE Accounts SET Account_Name = ? WHERE Member_ID = ? AND Is_Default_Fund = True", cn)
                cmd.Parameters.Add("@name", System.Data.OleDb.OleDbType.VarWChar, 100).Value = newShortName
                cmd.Parameters.Add("@id", System.Data.OleDb.OleDbType.VarWChar, 255).Value = a.Id
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Protected Sub btnAccRenameSave_Click(sender As Object, e As EventArgs) Handles btnAccRenameSave.Click
        If Not IsAdmin Then Return
        Dim person As Member = AccPerson()
        Dim a As Member = PickedPersonAccount(AccRenameId)
        If person Is Nothing OrElse a Is Nothing Then
            pnlAccRename.Visible = False
            ShowAccMsg("That account is no longer there.", True)
            Return
        End If
        Dim oldName As String = ShortAccountName(a)
        Dim newName As String = CleanName(txtAccRename.Text)
        pnlAccRename.Visible = True   ' stays open until the name is saved
        If newName = "" Then
            ShowAccMsg("Please enter the new name.", True)
            Return
        End If
        If newName = oldName Then
            pnlAccRename.Visible = False
            Return
        End If
        If TakenAccountName(person, {newName}, a.Id) <> "" Then
            ShowAccMsg(person.Name & " already has an account named """ & newName & """.", True)
            Return
        End If
        Try
            RenameAccountMember(a, person.Name, newName)
        Catch ex As Exception
            ResetLedgerCache()
            ShowAccMsg("The account was not renamed: " & ex.Message, True)
            Return
        End Try
        ResetLedgerCache()
        RefreshOrderMembers()
        pnlAccRename.Visible = False
        AccRenameId = ""
        ShowAccMsg("""" & oldName & """ renamed to """ & newName & """.", False)
    End Sub

    ''' <summary>
    ''' Deletes an account whose balance is 0.000 (never the person's last one). An account already used
    ''' (in journals or saved orders) is hidden instead (Is_Active = False), so past orders keep it.
    ''' </summary>
    Private Sub DeleteAccount(accountId As String)
        Dim person As Member = AccPerson()
        Dim a As Member = PickedPersonAccount(accountId)
        If person Is Nothing OrElse a Is Nothing Then
            ShowAccMsg("That account is no longer there.", True)
            Return
        End If
        Dim shortName As String = ShortAccountName(a)
        If IsShopCustomer(person.Id) Then
            ShowAccMsg(person.Name & " is a shop: its five accounts cannot be deleted.", True)
            Return
        End If
        If AccountsOfPerson(person.Id).Count <= 1 Then
            ShowAccMsg("""" & shortName & """ is " & person.Name & "'s only account, so it cannot be deleted.", True)
            Return
        End If
        Dim net As Decimal = Math.Round(Bal(FiguresOf(SummariesById(), a.Id)), 3)
        If net <> 0D Then
            ShowAccMsg("""" & shortName & """ has a balance of " & Money(net) & ": only an account with 0.000 can be deleted.", True)
            Return
        End If

        Dim hidden As Boolean
        Try
            Using cn As System.Data.OleDb.OleDbConnection = OpenOrdersDb()
                Dim idSql As String = OrdersDB.SqlText(a.Id)
                Dim used As Integer = 0
                For Each sql As String In {"SELECT COUNT(*) FROM Entries WHERE Member_ID = " & idSql,
                                           "SELECT COUNT(*) FROM Order_Members WHERE Member_ID = " & idSql & " OR Account_ID = " & idSql,
                                           "SELECT COUNT(*) FROM Order_Member_Items WHERE Member_ID = " & idSql,
                                           "SELECT COUNT(*) FROM Order_Payments WHERE Payer_Member_ID = " & idSql}
                    Using cmd As New System.Data.OleDb.OleDbCommand(sql, cn)
                        used += Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture)
                    End Using
                Next
                hidden = (used > 0)
                Dim changes As String() = If(hidden,
                    {"UPDATE Accounts SET Is_Active = False WHERE Member_ID = " & idSql,
                     "UPDATE Members SET Is_Active = False WHERE Member_ID = " & idSql},
                    {"DELETE FROM Accounts WHERE Member_ID = " & idSql,
                     "DELETE FROM Members WHERE Member_ID = " & idSql})
                For Each sql As String In changes
                    Using cmd As New System.Data.OleDb.OleDbCommand(sql, cn)
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End Using
        Catch ex As Exception
            ShowAccMsg("The account was not deleted: " & ex.Message, True)
            Return
        End Try
        ResetLedgerCache()
        RefreshOrderMembers()
        If SameName(AccRenameId, a.Id) Then pnlAccRename.Visible = False : AccRenameId = ""
        ShowAccMsg("""" & shortName & """ deleted" & If(hidden, " (it was used before, so it is kept hidden for the history of past orders).", "."), False)
    End Sub

    ''' <summary>An open connection to Orders.mdb (the same database the ledger uses).</summary>
    Private Shared Function OpenOrdersDb() As System.Data.OleDb.OleDbConnection
        ' OrdersDB.DBConnection may be a connection string or a connection object: take its connection string
        Dim source As Object = OrdersDB.DBConnection
        Dim cs As String
        If TypeOf source Is System.Data.Common.DbConnection Then
            cs = CType(source, System.Data.Common.DbConnection).ConnectionString
        Else
            cs = Convert.ToString(source)
        End If
        Dim cn As New System.Data.OleDb.OleDbConnection(cs)
        cn.Open()
        Return cn
    End Function

    ' ====================================================================
    '  SMALL HELPERS
    ' ====================================================================

    Private Sub ShowMsg(text As String, isError As Boolean)
        lblOrderMsg.Text = Server.HtmlEncode(text)
        lblOrderMsg.CssClass = "order-msg " & If(isError, "err", "ok")
        lblOrderMsg.Visible = True
    End Sub

    Private Shared Sub AddTo(d As Dictionary(Of String, Decimal), key As String, value As Decimal)
        If d.ContainsKey(key) Then d(key) += value Else d(key) = value
    End Sub

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("0.000", CultureInfo.InvariantCulture)
    End Function

    Private Shared Function BlankMoney(value As Decimal) As String
        Return If(value = 0D, "", Money(value))
    End Function

    Private Shared Function Qty(value As Decimal) As String
        Return value.ToString("0.###", CultureInfo.InvariantCulture)
    End Function

    ''' <summary>Reads a value as a number; empty or non-numeric counts as 0.</summary>
    Private Shared Function ParseNumber(value As Object) As Decimal
        If value Is Nothing OrElse IsDBNull(value) Then Return 0D
        Dim text As String = value.ToString().Trim()
        Dim result As Decimal
        If Decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, result) Then Return result
        If Decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, result) Then Return result
        Return 0D
    End Function

    Private Shared Function DbDecimal(value As Object) As Decimal
        Return ParseNumber(value)
    End Function

    Private Shared Function ColText(row As DataRow, column As String) As String
        If Not row.Table.Columns.Contains(column) OrElse IsDBNull(row(column)) Then Return ""
        Return row(column).ToString()
    End Function

    Private Shared Function OptDecimal(row As DataRow, column As String) As Decimal
        Return If(row.Table.Columns.Contains(column), DbDecimal(row(column)), 0D)
    End Function

    ' ====================================================================
    '  ReadOnly mode of the whole page (?mode=ReadOnly)
    ' ====================================================================

    Private ReadOnly Property IsReadOnlyMode As Boolean
        Get
            Return String.Equals(Convert.ToString(lblMode.Text).Trim(), "ReadOnly", StringComparison.OrdinalIgnoreCase)
        End Get
    End Property

    Private Sub ApplyReadOnlyMode()
        If Not IsReadOnlyMode Then Exit Sub
        LockFields(MultiView3)
    End Sub

    Private Sub LockFields(Parent As Control)
        For Each c As Control In Parent.Controls
            If TypeOf c Is TextBox Then
                CType(c, TextBox).ReadOnly = True
            ElseIf TypeOf c Is ListControl Then
                CType(c, ListControl).Enabled = False
            ElseIf TypeOf c Is CheckBox Then
                CType(c, CheckBox).Enabled = False
            ElseIf TypeOf c Is Button OrElse TypeOf c Is ImageButton Then
                c.Visible = False
            ElseIf TypeOf c Is System.Web.UI.WebControls.Calendar Then
                c.Visible = False
            End If
            If c.HasControls() Then LockFields(c)
        Next
    End Sub
End Class
