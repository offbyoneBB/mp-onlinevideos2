<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/OnlineVideos.Master" CodeBehind="Reports.aspx.cs" Inherits="OnlineVideos.WebService.Reports" %>
<asp:Content ID="Content1" ContentPlaceHolderID="body" runat="server">
  <div>
    <asp:Button runat="server" ID="btnDeleteSite" Visible="false" Text="Delete Site" OnClientClick="return confirm('Sure to delete ?')"
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
</asp:Content>