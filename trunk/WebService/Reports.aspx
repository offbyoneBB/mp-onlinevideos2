<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Reports.aspx.cs" Inherits="OnlineVideos.WebService.Reports" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <asp:HyperLink runat="server" ID="linkOverview" NavigateUrl="~/SiteOverview.aspx" Text="..." Visible="false" />
    <asp:Button runat="server" ID="btnDeleteSite" Visible="false" Text="Delete Site" 
            onclick="btnDeleteSite_Click" />
    <asp:GridView ID="reports" runat="server" AutoGenerateColumns="False" AllowSorting="True"
            CellPadding="3" 
            BackColor="#EEEEEE" 
            BorderColor="#CCCCCC" BorderStyle="None" BorderWidth="1px" 
            EnableViewState="true" 
            EnableModelValidation="True" Font-Names="Calibri" 
            onrowcommand="reports_RowCommand">
            <RowStyle ForeColor="#000066" />
        <Columns>
            <asp:BoundField HeaderText="Type" DataField="Type" ItemStyle-HorizontalAlign="Center"/>
            <asp:BoundField HeaderText="Date" DataField="Date" DataFormatString="{0:g}"/>
            <asp:BoundField HeaderText="Message" DataField="Message"/>
            <asp:ButtonField Text="Delete" ButtonType="Button" CommandName="DeleteReport" />
        </Columns>
            <FooterStyle BackColor="White" ForeColor="#000066" />
            <PagerStyle BackColor="White" ForeColor="#000066" HorizontalAlign="Left" />
            <SelectedRowStyle BackColor="#669999" Font-Bold="True" ForeColor="White" />
            <HeaderStyle BackColor="#006699" Font-Bold="True" ForeColor="White" />
            <AlternatingRowStyle BackColor="LightBlue" />
        </asp:GridView>
    </div>
    </form>
</body>
</html>
