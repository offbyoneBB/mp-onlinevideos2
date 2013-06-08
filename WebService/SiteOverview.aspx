<%@ Page Title="OnlineVideos Sites Overview" Language="C#" AutoEventWireup="true" MasterPageFile="~/OnlineVideos.Master" CodeBehind="SiteOverview.aspx.cs" Inherits="OnlineVideos.WebService.SiteOverview" %>
<asp:Content ID="Content1" ContentPlaceHolderID="body" runat="server">
	<div style="font-style:italic; text-align:right; font-size:smaller; background-color:#006699; color:White; padding: 5px">
		<asp:Label runat="server" ID="txtNumSitesTotal" CssClass="normal" Font-Bold="true" /> 
		<asp:LinkButton runat="server" ID="btnFilterNone" Text="Sites" Font-Bold="true"
			CssClass="normal" onclick="btnFilterNone_Click" ToolTip="Show all Sites" />&nbsp;: 
		<asp:Label runat="server" ID="txtNumReportedSites" CssClass="reported" /> 
		<asp:LinkButton runat="server" ID="btnFilterReported" Text="Reported" 
			CssClass="reported" onclick="btnFilterReported_Click" ToolTip="Show only reported sites" />&nbsp;/ 
		<asp:Label runat="server" ID="txtNumBrokenSites" CssClass="broken" /> 
		<asp:LinkButton runat="server" ID="btnFilterBroken" Text="Broken" 
			CssClass="broken" onclick="btnFilterBroken_Click" ToolTip="Show only broken Sites" />
	</div>
    <div>        
        <asp:GridView ID="siteOverview" runat="server" AutoGenerateColumns="False" AllowSorting="True"
            CellPadding="3" 
            onrowdatabound="siteOverview_RowDataBound" BackColor="#EEEEEE" 
            BorderColor="#CCCCCC" BorderStyle="None" BorderWidth="1px" 
            onsorting="siteOverview_Sorting" EnableViewState="False" 
            EnableModelValidation="True" Font-Names="Calibri">
            <RowStyle ForeColor="#000066" />
        <Columns>
            <asp:TemplateField HeaderText="Nr." ItemStyle-HorizontalAlign="Right">
                <ItemTemplate>
                    <asp:Label runat="server" Text='<%# ((int)DataBinder.Eval(Container, "DataItemIndex")+1).ToString() + "." %>' />
                </ItemTemplate>
            </asp:TemplateField>
            <asp:ImageField HeaderText="Logo" DataImageUrlField="Name" 
                DataImageUrlFormatString="./Icons/{0}.png" ControlStyle-Width="48" 
                ControlStyle-Height="48">
                <ControlStyle Height="48px" Width="48px"></ControlStyle>
            </asp:ImageField>
            <asp:TemplateField HeaderText="Site" SortExpression="Name" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Bottom" ItemStyle-Font-Bold="true">
                <ItemTemplate>
                    <div style="position:relative">
                    &nbsp;<br />
                    <asp:HyperLink ID="HyperLink1" runat="server" Text='<%# Eval("Name") %>' NavigateUrl='<%# "Reports.aspx?site=" + HttpUtility.UrlEncode((string)Eval("Name")) %>' />
                    <br />&nbsp;
                    <span title="Reports" style="position:absolute;bottom:0;right:0;font-size:small;font-weight:normal"><%# Eval("ReportCount") %></span>
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField HeaderText="Creator" DataField="Owner_FK" SortExpression="Owner_FK" ItemStyle-HorizontalAlign="Center"/>
            <asp:TemplateField HeaderText="Language" SortExpression="Language" ItemStyle-HorizontalAlign="Center">
				<ItemTemplate>
					<asp:Image runat="server" ImageUrl='<%# "./Langs/"+(string)Eval("Language")+".png" %>' Height="36" />
                    <asp:Label runat="server" Text='<%# LanguageName((string)Eval("Language")) %>' style="display:block" />
				</ItemTemplate>
            </asp:TemplateField>
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
</asp:Content>