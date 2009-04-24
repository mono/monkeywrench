<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" CodeFile="EditHost.aspx.cs"
    Inherits="EditHost" %>

<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">

    <script type="text/javascript" src="EditHost.js"></script>

    <div>
        <br />
        <br />
        <table>
            <tr>
                <td>
                    Id:
                </td>
                <td>
                    <asp:TextBox ID="txtID" runat="server" ReadOnly="True" Width="471px"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td>
                    Host:
                </td>
                <td>
                    <asp:TextBox ID="txtHost" runat="server" Width="471px"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td>
                    Queue management:
                </td>
                <td>
                    <asp:DropDownList ID="cmbQueueManagement" runat="server" Width="471px">
                        <asp:ListItem Text="Completely finish a revision before starting a new one" Value="0"></asp:ListItem>
                        <asp:ListItem Text="Execute last revision as soon as possible" Value="1"></asp:ListItem>
                    </asp:DropDownList>
                </td>
            </tr>
            <tr>
                <td>
                    Description:
                </td>
                <td>
                    <asp:TextBox ID="txtDescription" runat="server" Width="471px"></asp:TextBox>
                </td>
                <td>
                    (informational purposes only)
                </td>
            </tr>
            <tr>
                <td>
                    Architecture:
                </td>
                <td>
                    <asp:TextBox ID="txtArchitecture" runat="server" Width="471px"></asp:TextBox>
                </td>
                <td>
                    (informational purposes only)
                </td>
            </tr>
            <tr>
            <td>Enabled</td><td><asp:CheckBox ID="chkEnabled" runat="server" /></td>
            </tr>
            <tr>
                <td colspan='2'>
                    <center>
                        <asp:Button ID="cmdSave" runat="server" Text="Save" OnClick="cmdSave_Click" />
                    </center>
                </td>
            </tr>
        </table>
        <br />
        Lanes configured for this host:<br />
        <br />
        <asp:Table ID="tblLanes" runat="server" CssClass="buildstatus">
        </asp:Table>
    </div>
</asp:Content>
