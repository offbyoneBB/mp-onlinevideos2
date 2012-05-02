<%@ Page Title="OnlineVideos Sites Management Login" Language="C#" AutoEventWireup="true" MasterPageFile="~/OnlineVideos.Master" CodeBehind="Login.aspx.cs" Inherits="OnlineVideos.WebService.LoginForm" %>
<asp:Content ID="Content1" ContentPlaceHolderID="body" runat="server">
    <table style="width: 100%;">
        <tr>
            <td>
                <asp:Login ID="Login1" runat="server" DestinationPageUrl="~/SiteOverview.aspx" 
            BackColor="#EFF3FB" BorderColor="#B5C7DE" BorderPadding="4" BorderStyle="Solid" 
            BorderWidth="1px" Font-Names="Verdana" Font-Size="0.9em" ForeColor="#333333" 
                    DisplayRememberMe="False">
            <InstructionTextStyle Font-Italic="True" ForeColor="Black" />
            <LoginButtonStyle BackColor="White" BorderColor="#507CD1" BorderStyle="Solid" 
                BorderWidth="1px" Font-Names="Verdana" Font-Size="0.8em" ForeColor="#284E98" />
            <TextBoxStyle Font-Size="0.8em" />
            <TitleTextStyle BackColor="#507CD1" Font-Bold="True" Font-Size="0.9em" 
                ForeColor="White" />
        </asp:Login>
            </td>
            <td valign="top" align="right">
                <asp:PasswordRecovery ID="PasswordRecovery1" runat="server" BackColor="#EFF3FB" 
            BorderColor="#B5C7DE" BorderPadding="4" BorderStyle="Solid" BorderWidth="1px" 
            Font-Names="Verdana" Font-Size="0.9em">
            <SubmitButtonStyle BackColor="White" BorderColor="#507CD1" BorderStyle="Solid" 
                BorderWidth="1px" Font-Names="Verdana" Font-Size="0.8em" ForeColor="#284E98" />
            <InstructionTextStyle Font-Italic="True" ForeColor="Black" />
            <SuccessTextStyle Font-Bold="True" ForeColor="#507CD1" />
            <TextBoxStyle Font-Size="0.8em" />
            <TitleTextStyle BackColor="#507CD1" Font-Bold="True" Font-Size="0.9em" 
                ForeColor="White" />
        </asp:PasswordRecovery>
            </td>
        </tr>
    </table>
</asp:Content>