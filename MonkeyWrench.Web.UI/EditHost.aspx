<%@ Page Language="C#" MasterPageFile="~/Master.master" AutoEventWireup="true" Inherits="EditHost" CodeBehind="EditHost.aspx.cs" %>

<%@ Register Src="~/EnvironmentVariablesEditor.ascx" TagName="EnvironmentVariablesEditor" TagPrefix="ucs" %>
<asp:Content ID="Content2" ContentPlaceHolderID="content" runat="Server">

    <script type="text/javascript" src="EditHost.js"></script>

    <script type="text/javascript" src="MonkeyWrench.js"></script>

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
                <td>
                    Enabled
                </td>
                <td>
                    <asp:CheckBox ID="chkEnabled" runat="server" />
                </td>
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
        <br />
        Master hosts this host will work for:<br />
        If no hosts are specified it will work for itself, if there are any master hosts specified, you need to add itself to the list below for it to work for itself.
        <br />
        <asp:Table ID="tblMasters" runat="server" CssClass="buildstatus">
            <asp:TableHeaderRow>
                <asp:TableHeaderCell>Master Host</asp:TableHeaderCell><asp:TableHeaderCell>Action</asp:TableHeaderCell>
            </asp:TableHeaderRow>
            <asp:TableRow>
                <asp:TableCell>
                    <asp:DropDownList ID="cmbMasterHosts" runat="server" />
                </asp:TableCell>
                <asp:TableCell>
                    <asp:LinkButton runat="server" ID="cmdAddMasterHost" CommandName="AddMasterHost" OnCommand="OnLinkButtonCommand">Add</asp:LinkButton>
                </asp:TableCell>
            </asp:TableRow>
        </asp:Table>
        <br />
        Slave hosts of this host:
        <br />
        <asp:Table ID="tblSlaves" runat="server" CssClass="buildstatus">
            <asp:TableHeaderRow>
                <asp:TableHeaderCell>Slave Host</asp:TableHeaderCell><asp:TableHeaderCell>Action</asp:TableHeaderCell>
            </asp:TableHeaderRow>
        </asp:Table>
        <br />
        <ucs:EnvironmentVariablesEditor ID="editorVariables" runat="server"></ucs:EnvironmentVariablesEditor>
        <br />
    </div>
</asp:Content>
