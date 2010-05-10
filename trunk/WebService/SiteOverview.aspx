<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SiteOverview.aspx.cs" Inherits="OnlineVideos.WebService.SiteOverview" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>OnlineVideos Sites Overview</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>        
        <asp:GridView ID="siteOverview" runat="server" AutoGenerateColumns="False" AllowSorting="true"
            CellPadding="3" 
            onrowdatabound="siteOverview_RowDataBound" BackColor="White" 
            BorderColor="#CCCCCC" BorderStyle="None" BorderWidth="1px" 
            onsorting="siteOverview_Sorting" EnableViewState="false">
            <RowStyle ForeColor="#000066" />
        <Columns>
            <asp:ImageField HeaderText="Logo" DataImageUrlField="Name" 
                DataImageUrlFormatString="./Icons/{0}.png" ControlStyle-Width="48" 
                ControlStyle-Height="48" >                
<ControlStyle Height="48px" Width="48px"></ControlStyle>
            </asp:ImageField>
            <asp:BoundField HeaderText="Site" DataField="Name" SortExpression="Name"
                ItemStyle-HorizontalAlign="Center" >
<ItemStyle HorizontalAlign="Center"></ItemStyle>
            </asp:BoundField>
            <asp:BoundField HeaderText="Creator" DataField="Owner_FK" SortExpression="Owner_FK"
                ItemStyle-HorizontalAlign="Center" >
<ItemStyle HorizontalAlign="Center"></ItemStyle>
            </asp:BoundField>
            <asp:BoundField HeaderText="Language" DataField="Language" SortExpression="Language"
                ItemStyle-HorizontalAlign="Center" >
<ItemStyle HorizontalAlign="Center"></ItemStyle>
            </asp:BoundField>
            <asp:BoundField HeaderText="Update" DataField="LastUpdated" DataFormatString="{0:g}" SortExpression="LastUpdated" />
            <asp:BoundField HeaderText="Description" DataField="Description" />            
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
