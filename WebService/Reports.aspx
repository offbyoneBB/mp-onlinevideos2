<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/OnlineVideos.Master" CodeBehind="Reports.aspx.cs" Inherits="OnlineVideos.WebService.Reports" %>
<asp:Content ID="Content1" ContentPlaceHolderID="body" runat="server">
  <div>
    <asp:Button runat="server" ID="btnDeleteSite" Visible="false" Text="Delete Site" OnClientClick="return confirm('Sure to delete ?')"
            onclick="btnDeleteSite_Click" />
    <asp:GridView ID="reports" runat="server" AutoGenerateColumns="False" AllowSorting="True"
            CellPadding="3" 
            BackColor="#EEEEEE" 
            BorderColor="#CCCCCC" BorderStyle="None" BorderWidth="1px" 
            EnableModelValidation="True" Font-Names="Calibri" 
            onrowcommand="reports_RowCommand">
            <RowStyle ForeColor="#000066" />
        <Columns>
            <asp:TemplateField HeaderText="Type">
				<ItemTemplate>
					<asp:Label runat="server" Text='<%# Eval("Type") %>'></asp:Label>
				</ItemTemplate>
				<FooterTemplate>
					<asp:DropDownList runat="server" ID="ddType">
						<asp:ListItem Text="Fixed" Value="Fixed" Selected="True" />
						<asp:ListItem Text="Suggestion" Value="Suggestion" />
						<asp:ListItem Text="ConfirmedBroken" Value="ConfirmedBroken" />
						<asp:ListItem Text="RejectedBroken" Value="RejectedBroken" />
						<asp:ListItem Text="Broken" Value="Broken" />
					</asp:DropDownList>
				</FooterTemplate>
				<ItemStyle HorizontalAlign="Center" />
			</asp:TemplateField>
			<asp:TemplateField HeaderText="Date">
				<ItemTemplate>
					<asp:Label runat="server" Text='<%# Eval("Date", "{0:g}") %>'></asp:Label>
				</ItemTemplate>
				<FooterTemplate>
					<asp:Label runat="server" Text='<%# DateTime.Now.ToString("g") %>'></asp:Label>
				</FooterTemplate>
			</asp:TemplateField>
			<asp:TemplateField HeaderText="Message">
				<ItemTemplate>
					<asp:Label runat="server" Text='<%# Eval("Message") %>'></asp:Label>
				</ItemTemplate>
				<FooterTemplate>
                    <asp:TextBox runat="server" ID="tbxNewMessage" Width="99%" />
                </FooterTemplate>
			</asp:TemplateField>
            <asp:TemplateField ShowHeader="False">
				<ItemTemplate>
					<asp:Button runat="server" CommandName="DeleteReport" Text="Delete" />
				</ItemTemplate>
				<FooterTemplate>
                    <asp:Button runat="server" CommandName="AddReport" Text="Add" Width="99%" />
                </FooterTemplate>
			</asp:TemplateField>
        </Columns>
            <FooterStyle BackColor="#EEEEEE" ForeColor="#000066" />
            <PagerStyle BackColor="White" ForeColor="#000066" HorizontalAlign="Left" />
            <SelectedRowStyle BackColor="#669999" Font-Bold="True" ForeColor="White" />
            <HeaderStyle BackColor="#006699" Font-Bold="True" ForeColor="White" />
            <AlternatingRowStyle BackColor="LightBlue" />
        </asp:GridView>
    </div>
</asp:Content>