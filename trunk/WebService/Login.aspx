<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="OnlineVideos.WebService.LoginForm" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>OnlineVideos Sites Management Login</title>
</head>
<body>
    <form id="form1" runat="server">
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
    </form>
</body>
</html>
